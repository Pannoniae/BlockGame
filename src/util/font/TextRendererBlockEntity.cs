using System.Numerics;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using FontStashSharp;
using FontStashSharp.Interfaces;

namespace BlockGame.util.font;

/**
 * This one just yeets the text into the world.
 */
public class TextRendererBlockEntity : IFontStashRenderer {
    private readonly BTexture2DManager _textureManager;

    /**
     * This will be assigned! (If it's not, you fucked up)
     */
    public InstantDrawEntity ide;

    public ITexture2DManager TextureManager => _textureManager;

    public TextRendererBlockEntity() {
        _textureManager = new BTexture2DManager();
    }

    public void Draw(object texture, Vector2 pos, ref Matrix4x4 worldMatrix, Rectangle? src, FSColor color, float rotation, Vector2 scale, float depth) {
        var tex = (BTexture2D)texture;
        var srcRect = src ?? new Rectangle(0, 0, tex.width, tex.height);

        System.Diagnostics.Debug.Assert(ide != null, "TextRendererBlockEntity.ide is null, skill issue");

        Game.graphics.tex(0, tex);

        // corners
        // DON'T apply worldMatrix here - InstantDraw applies model matrix in shader
        var w = srcRect.Width * scale.X;
        var h = srcRect.Height * scale.Y;

        var u0 = srcRect.X / (float)tex.width;
        var v0 = srcRect.Y / (float)tex.height;
        var u1 = (srcRect.X + srcRect.Width) / (float)tex.width;
        var v1 = (srcRect.Y + srcRect.Height) / (float)tex.height;

        var c = new Color(color.R, color.G, color.B, color.A);

        // apply rotation if needed
        if (rotation != 0) {
            var cos = float.Cos(rotation);
            var sin = float.Sin(rotation);

            ide.addVertex(new EntityVertex(pos.X, pos.Y, depth, u0, v0, c));
            ide.addVertex(new EntityVertex(pos.X - h * sin, pos.Y + h * cos, depth, u0, v1, c));
            ide.addVertex(new EntityVertex(pos.X + w * cos - h * sin, pos.Y + w * sin + h * cos, depth, u1, v1, c));
            ide.addVertex(new EntityVertex(pos.X + w * cos, pos.Y + w * sin, depth, u1, v0, c));
        }
        else {
            ide.addVertex(new EntityVertex(pos.X, pos.Y, depth, u0, v0, c));
            ide.addVertex(new EntityVertex(pos.X, pos.Y + h, depth, u0, v1, c));
            ide.addVertex(new EntityVertex(pos.X + w, pos.Y + h, depth, u1, v1, c));
            ide.addVertex(new EntityVertex(pos.X + w, pos.Y, depth, u1, v0, c));
        }
    }

    public void Draw(object texture, Vector2 pos, Rectangle? src, FSColor color, float rotation, Vector2 scale, float depth) {
        var identity = Matrix4x4.Identity;
        Draw(texture, pos, ref identity, src, color, rotation, scale, depth);
    }
}