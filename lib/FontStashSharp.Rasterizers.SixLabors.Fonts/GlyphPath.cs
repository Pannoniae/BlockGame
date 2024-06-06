using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;

namespace FontStashSharp.Samples.SixLabors
{
	internal class GlyphPath
	{
		public float Size;
		public int Codepoint;
		public Rectangle Bounds;
		public IPathCollection Paths;
	}
}
