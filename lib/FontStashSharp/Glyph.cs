#if MONOGAME || FNA
using Microsoft.Xna.Framework;
#elif STRIDE
using Stride.Core.Mathematics;
#else
using SixLabors.ImageSharp;
#endif

namespace FontStashSharp
{
	public struct Glyph
	{
		public int Index;
		public int Codepoint;
		public Rectangle Bounds;
		public int XAdvance;
	}
}
