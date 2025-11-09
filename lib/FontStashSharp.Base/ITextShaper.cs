namespace FontStashSharp.Interfaces
{
	public interface ITextShapingInfoProvider
	{
		int? GetFontSourceId(int codepoint);
		int GetTextShaperFontId(int fontSourceId);
		float CalculateScale(int fontSourceId, float fontSize);
	}


	public interface ITextShaper
	{
		/// <summary>
		/// Registers a ttf font
		/// </summary>
		/// <param name="data"></param>
		/// <returns>Assigned id</returns>
		int RegisterTtfFont(byte[] data);

		/// <summary>
		/// Disposes and removes font from the text shaper
		/// </summary>
		/// <param name="id"></param>
		void RemoveFont(int id);

		/// <summary>
		/// Shape text using HarfBuzz
		/// </summary>
		/// <param name="text">The text to shape</param>
		/// <param name="fontSize">The font size</param>
		/// <param name="infoProvider">Provides info for the text shaping</param>
		/// <returns>Shaped text with glyph information</returns>
		ShapedText Shape(string text, float fontSize, ITextShapingInfoProvider infoProvider);
	}
}
