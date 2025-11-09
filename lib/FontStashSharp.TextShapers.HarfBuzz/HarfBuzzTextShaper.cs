using FontStashSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace FontStashSharp
{
	public class HarfBuzzTextShaper : ITextShaper
	{
		private int _lastId = 0;
		private readonly Dictionary<int, HarfBuzzFont> _harfBuzzFonts = new Dictionary<int, HarfBuzzFont>();

		/// <summary>
		/// Enable bidirectional (BiDi) text support for mixed LTR/RTL text
		/// When enabled, text with mixed Latin and RTL scripts (Arabic, Hebrew, etc.) will be displayed in correct order
		/// Only applies when UseTextShaping is true
		/// Default: true
		/// </summary>
		public bool EnableBiDi { get; set; } = true;

		public int RegisterTtfFont(byte[] data)
		{
			var hbFont = new HarfBuzzFont(data);

			_harfBuzzFonts[_lastId] = hbFont;
			var result = _lastId;

			++_lastId;

			return result;
		}

		public void RemoveFont(int id)
		{
			var font = _harfBuzzFonts[id];
			font.Dispose();

			_harfBuzzFonts.Remove(id);
		}

		private struct FontRun
		{
			public int Start;
			public int Length;
			public int FontSourceId;
		}

		private static List<FontRun> SegmentTextIntoFontRuns(string text, int start, int length, ITextShapingInfoProvider infoProvider)
		{
			var runs = new List<FontRun>();
			int currentRunStart = start;
			int? currentFontSourceId = null;
			int end = start + length;

			for (int i = start; i < end;)
			{
				// Get the codepoint at position i
				int codepoint;
				int charCount;
				if (i < text.Length - 1 && char.IsSurrogatePair(text, i))
				{
					codepoint = char.ConvertToUtf32(text, i);
					charCount = 2;
				}
				else
				{
					codepoint = text[i];
					charCount = 1;
				}

				// Find which font source has this codepoint
				var fontSourceId = infoProvider.GetFontSourceId(codepoint) ?? 0;

				// If this is a new font source, start a new run
				if (currentFontSourceId == null || fontSourceId != currentFontSourceId.Value)
				{
					// Save the previous run if it exists
					if (currentFontSourceId != null)
					{
						runs.Add(new FontRun
						{
							Start = currentRunStart,
							Length = i - currentRunStart,
							FontSourceId = currentFontSourceId.Value
						});
					}

					// Start new run
					currentRunStart = i;
					currentFontSourceId = fontSourceId;
				}

				i += charCount;
			}

			// Add the final run
			if (currentFontSourceId != null)
			{
				runs.Add(new FontRun
				{
					Start = currentRunStart,
					Length = end - currentRunStart,
					FontSourceId = currentFontSourceId.Value
				});
			}

			return runs;
		}

		/// <summary>
		/// Shape text using HarfBuzz
		/// </summary>
		/// <param name="text">The text to shape</param>
		/// <param name="fontSize">The font size</param>
		/// <param name="infoProvider">Provides info for the text shaping</param>
		/// <returns>Shaped text with glyph information</returns>
		public ShapedText Shape(string text, float fontSize, ITextShapingInfoProvider infoProvider)
		{
			if (string.IsNullOrEmpty(text))
			{
				return new ShapedText
				{
					Glyphs = new ShapedGlyph[0],
					OriginalText = text ?? string.Empty,
					FontSize = fontSize
				};
			}

			var allShapedGlyphs = new List<ShapedGlyph>(text.Length);

			// Step 1: Analyze text for bidirectional runs (if enabled)
			List<DirectionalRun> directionalRuns;
			if (EnableBiDi)
			{
				directionalRuns = BiDiAnalyzer.SegmentIntoDirectionalRuns(text);
			}
			else
			{
				// BiDi disabled - treat entire text as single LTR run
				directionalRuns = new List<DirectionalRun>
				{
					new DirectionalRun
					{
						Start = 0,
						Length = text.Length,
						Direction = TextDirection.LTR
					}
				};
			}

			// Step 2: Process each directional run
			foreach (var dirRun in directionalRuns)
			{
				// Step 3: Within each directional run, segment by font source
				var fontRuns = SegmentTextIntoFontRuns(text, dirRun.Start, dirRun.Length, infoProvider);

				// Step 4: Shape each font run with its appropriate font
				foreach (var fontRun in fontRuns)
				{
					var textShaperFontId = infoProvider.GetTextShaperFontId(fontRun.FontSourceId);

					HarfBuzzFont hbFont;
					if (!_harfBuzzFonts.TryGetValue(textShaperFontId, out hbFont))
					{
						throw new InvalidOperationException($"HarfBuzz font not available for font source {textShaperFontId}. Ensure font data is cached.");
					}

					var scale = infoProvider.CalculateScale(fontRun.FontSourceId, fontSize);
					using (var buffer = new HarfBuzzSharp.Buffer())
					{
						// Add text run to buffer
						var sb = new StringBuilder();
						buffer.AddUtf16(text, fontRun.Start, fontRun.Length);
						buffer.GuessSegmentProperties();
						hbFont.Shape(buffer);

						// Get the shaped output
						var glyphInfos = buffer.GlyphInfos;
						var glyphPositions = buffer.GlyphPositions;

						// Convert to our ShapedGlyph format
						for (int i = 0; i < glyphInfos.Length; i++)
						{
							var info = glyphInfos[i];
							var pos = glyphPositions[i];

							// After the shaping, info.Codepoint contains glyph id and not original unicode codepoint
							allShapedGlyphs.Add(new ShapedGlyph
							{
								GlyphId = (int)info.Codepoint,
								Cluster = (int)info.Cluster + fontRun.Start,
								FontSourceId = fontRun.FontSourceId,
								XAdvance = pos.XAdvance * scale,
								YAdvance = pos.YAdvance * scale,
								XOffset = pos.XOffset * scale,
								YOffset = -pos.YOffset * scale
							});
						}
					}
				}
			}

			return new ShapedText
			{
				Glyphs = allShapedGlyphs.ToArray(),
				OriginalText = text,
				FontSize = fontSize
			};
		}
	}
}
