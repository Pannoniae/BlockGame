using System.Drawing;
using System.Numerics;
using BlockGame.GL;
using BlockGame.main;
using FontStashSharp;
using FontStashSharp.Interfaces;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.util.font;

public class TextRenderer : IFontStashRenderer {
    private readonly SpriteBatch tb;
    private readonly BTexture2DManager _textureManager;

    public ITexture2DManager TextureManager => _textureManager;

    public TextRenderer() {
        _textureManager = new BTexture2DManager();
        tb = Game.graphics.mainBatch;
        // disable because it's mostly transparent and it fucks performance with drawtexture
        tb.NoScreenSpace = true;
    }

    public void Draw(object texture, Vector2 pos, ref Matrix4x4 worldMatrix, Rectangle? src, FSColor color, float rotation, Vector2 scale, float depth) {
        var tex = (BTexture2D)texture;
        var intPos = new Vector2((int)pos.X, (int)pos.Y);
        // texture height
        tb.Draw(tex,
            intPos,
            src,
            new Color4b(color.R, color.G, color.B, color.A),
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

    public static Color4b ToTrippy(this FSColor c) {
        return new Color4b(c.R, c.G, c.B, c.A);
    }

    public static FSColor toFS(this Color4b c) {
        return new FSColor(c.R, c.G, c.B, c.A);
    }
}