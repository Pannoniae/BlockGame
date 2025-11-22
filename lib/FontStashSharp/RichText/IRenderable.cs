#if MONOGAME || FNA
using Microsoft.Xna.Framework;
#elif STRIDE
using Stride.Core.Mathematics;
#else
using SixLabors.ImageSharp;
using System.Numerics;
using Color = FontStashSharp.FSColor;
#endif

namespace FontStashSharp.RichText
{
	public interface IRenderable
	{
		Point Size { get; }

		void Draw(FSRenderContext context, Vector2 position, Color color);
	}
}
