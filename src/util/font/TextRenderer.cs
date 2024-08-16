using System.Drawing;
using System.Numerics;
using FontStashSharp;
using FontStashSharp.Interfaces;
using TrippyGL;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.util.font;

public class TextRenderer : IFontStashRenderer {
    private readonly TextureBatcher tb;
    private readonly Texture2DManager _textureManager;

    public ITexture2DManager TextureManager => _textureManager;

    public GraphicsDevice GraphicsDevice => _textureManager.GraphicsDevice;

    public TextRenderer(GraphicsDevice graphicsDevice) {
        _textureManager = new Texture2DManager(graphicsDevice);
        tb = Game.graphics.mainBatch;
    }

    public void Draw(object texture, Vector2 pos, ref Matrix4x4 worldMatrix, Rectangle? src, FSColor color, float rotation, Vector2 scale, float depth) {
        var tex = (Texture2D)texture;
        var intPos = new Vector2((int)pos.X, (int)pos.Y);
        // texture height
        tb.Draw(tex,
            intPos,
            src,
            color.ToTrippy(),
            scale,
            rotation,
            Vector2.Zero,
            depth);
    }
}

internal static class Utility {
    public static Vector2 ToSystemNumeric(Point p) {
        return new Vector2(p.X, p.Y);
    }

    public static Rectangle ToSystemDrawing(this Viewport r) {
        return new Rectangle(r.X, r.Y, (int)r.Width, (int)r.Height);
    }

    public static Viewport ToTrippy(this Rectangle r) {
        return new Viewport(r);
    }

    public static Color4b ToTrippy(this FSColor c) {
        return new Color4b(c.R, c.G, c.B, c.A);
    }

    public static FSColor toFS(this Color4b c) {
        return new FSColor(c.R, c.G, c.B, c.A);
    }
}