using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;

namespace FontStashSharp.Rasterizers.SixLabors.Fonts
{
	internal class GlyphPath
	{
		public float Size;
		public int Codepoint;
		public Rectangle Bounds;
		public IPathCollection Paths;
	}
}
