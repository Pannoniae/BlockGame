using FontStashSharp.Interfaces;
using FreeTypeSharp;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FontStashSharp.Rasterizers.FreeType
{
	internal unsafe class FreeTypeSource : IFontSource
	{
		private static FT_LibraryRec_* _libraryHandle;
		private GCHandle _memoryHandle;
		private FT_FaceRec_* _faceHandle;

		public FreeTypeSource(byte[] data)
		{
			FT_Error err;
			if (_libraryHandle == default)
			{
				FT_LibraryRec_* libraryRef;
				err = FT.FT_Init_FreeType(&libraryRef);

				if (err != FT_Error.FT_Err_Ok)
					throw new FreeTypeException(err);

				_libraryHandle = libraryRef;
			}

			_memoryHandle = GCHandle.Alloc(data, GCHandleType.Pinned);

			FT_FaceRec_* faceRef;
			err = FT.FT_New_Memory_Face(_libraryHandle, (byte*)_memoryHandle.AddrOfPinnedObject(), (IntPtr)data.Length, IntPtr.Zero, &faceRef);

			if (err != FT_Error.FT_Err_Ok)
				throw new FreeTypeException(err);

			_faceHandle = faceRef;
		}

		~FreeTypeSource()
		{
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_faceHandle != default)
			{
				FT.FT_Done_Face(_faceHandle);
				_faceHandle = default;
			}

			if (_memoryHandle.IsAllocated)
				_memoryHandle.Free();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public int? GetGlyphId(int codepoint)
		{
			var result = FT.FT_Get_Char_Index(_faceHandle, (UIntPtr)codepoint);
			if (result == 0)
			{
                //Console.Out.WriteLine(
                //    $"FreeTypeSource.GetGlyphId: FT_Get_Char_Index returned 0 for codepoint {codepoint}");
                //Console.Out.WriteLine(Environment.StackTrace);
				return null;
			}

			return (int?)result;
		}

		public int GetGlyphKernAdvance(int previousGlyphId, int glyphId, float fontSize)
		{
			FT_Vector_ kerning;
			if (FT.FT_Get_Kerning(_faceHandle, (uint)previousGlyphId, (uint)glyphId, FT_Kerning_Mode_.FT_KERNING_DEFAULT, &kerning) != FT_Error.FT_Err_Ok)
			{
                Console.Out.WriteLine("FreeTypeSource.GetGlyphKernAdvance: FT_Get_Kerning failed");
				return 0;
			}

			return (int)kerning.x >> 6;
		}

		private void SetPixelSizes(float width, float height)
		{
			var err = FT.FT_Set_Pixel_Sizes(_faceHandle, (uint)width, (uint)height);
			if (err != FT_Error.FT_Err_Ok)
				throw new FreeTypeException(err);
		}

		private void LoadGlyph(int glyphId)
		{
			var err = FT.FT_Load_Glyph(_faceHandle, (uint)glyphId, FT_LOAD.FT_LOAD_DEFAULT | FT_LOAD.FT_LOAD_COLOR);
			if (err != FT_Error.FT_Err_Ok)
				throw new FreeTypeException(err);
		}

		private unsafe void GetCurrentGlyph(out FT_GlyphSlotRec_ glyph)
		{
			glyph = Marshal.PtrToStructure<FT_GlyphSlotRec_>((IntPtr)_faceHandle->glyph);
		}

        public void GetGlyphMetrics(int glyphId, float fontSize, out int advance, out int x0, out int y0, out int x1,
            out int y1) {
            if (_faceHandle == (FT_FaceRec_*)0) {
                advance = x0 = y0 = x1 = y1 = 0;
                Console.Out.WriteLine("FreeTypeSource.GetGlyphMetrics: _faceHandle is null");
                return;
            }

            SetPixelSizes(0, fontSize);
            LoadGlyph(glyphId);

            FT_GlyphSlotRec_ glyph;
            GetCurrentGlyph(out glyph);
            advance = (int)glyph.advance.x >> 6;
            x0 = (int)glyph.metrics.horiBearingX >> 6;
            y0 = -(int)glyph.metrics.horiBearingY >> 6;
            x1 = x0 + ((int)glyph.metrics.width >> 6);
            y1 = y0 + ((int)glyph.metrics.height >> 6);

            // NOTE: this font which we are using is 9x16, 4 below the line, 12 above the line. It kind of renders off-centre though
            // hacky solution: render it slightly lower lol
            // this used to exist in the GUI code (just render the text lower) but this is probably a better place for it, it does it globally
            y0 += 2;
            y1 += 2;
        }

		public unsafe void GetMetricsForSize(float fontSize, out int ascent, out int descent, out int lineHeight)
		{
			SetPixelSizes(0, fontSize);
			var sizeRec = _faceHandle->size;

			ascent = (int)sizeRec->metrics.ascender >> 6;
			descent = (int)sizeRec->metrics.descender >> 6;
			lineHeight = (int)sizeRec->metrics.height >> 6;
		}

		public unsafe void RasterizeGlyphBitmap(int glyphId, float fontSize, byte[] buffer, int startIndex, int outWidth, int outHeight, int outStride)
		{
			SetPixelSizes(0, fontSize);
			LoadGlyph(glyphId);

			FT.FT_Render_Glyph(_faceHandle->glyph, FT_Render_Mode_.FT_RENDER_MODE_NORMAL);

			FT_GlyphSlotRec_ glyph;
			GetCurrentGlyph(out glyph);
			var ftbmp = glyph.bitmap;

			fixed (byte* bptr = buffer)
			{
				for (var y = 0; y < outHeight; ++y)
				{
					var pos = (y * outStride) + startIndex;

					byte* dst = bptr + pos;
					byte* src = ftbmp.buffer + y * ftbmp.pitch;

					if (ftbmp.pixel_mode == FT_Pixel_Mode_.FT_PIXEL_MODE_GRAY)
					{
						for (var x = 0; x < outWidth; ++x)
						{
							*dst++ = *src++;
						}
					}
					else if (ftbmp.pixel_mode == FT_Pixel_Mode_.FT_PIXEL_MODE_MONO)
					{
						for (var x = 0; x < outWidth; x += 8)
						{
							var bits = *src++;
							for (int b = 0; b < Math.Min(8, ftbmp.width - x); b++)
							{
								var color = ((bits >> (7 - b)) & 1) == 0 ? 0 : 255;
								*dst++ = (byte)color;
							}
						}
					}
				}
			}
		}

		public float CalculateScaleForTextShaper(float fontSize)
		{
			return fontSize / (float)_faceHandle->units_per_EM;
		}
	}
}
