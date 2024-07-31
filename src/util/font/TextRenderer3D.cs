using System.Numerics;
using BlockGame.ui;
using FontStashSharp;
using FontStashSharp.Interfaces;
using Silk.NET.Maths;
using TrippyGL;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.util.font;

public class TextRenderer3D : IFontStashRenderer {
    private readonly SimpleShaderProgram shaderProgram;
    private readonly TextureBatcher tb;
    private readonly Texture2DManager _textureManager;

    public ITexture2DManager TextureManager => _textureManager;

    public GraphicsDevice GraphicsDevice => _textureManager.GraphicsDevice;

    public TextRenderer3D(GraphicsDevice graphicsDevice) {
        _textureManager = new Texture2DManager(graphicsDevice);
        tb = new TextureBatcher(GraphicsDevice);
        shaderProgram = SimpleShaderProgram.Create<VertexColorTexture>(graphicsDevice);
        tb.SetShaderProgram(shaderProgram);
    }

    public void OnViewportChanged(Vector2D<int> size) {
    }

    public void setMatrix(ref Matrix4x4 mat) {
        shaderProgram.World = mat;
    }

    public void renderTick(double interp) {
        shaderProgram.View = Screen.GAME_SCREEN.world.player.camera.getViewMatrix(interp);
        shaderProgram.Projection = Screen.GAME_SCREEN.world.player.camera.getProjectionMatrix();
    }

    public void begin() {
        tb.Begin();
    }

    public void end() {
        tb.End();
    }

    /// <summary>
    /// Finish drawing text.
    /// </summary>
    public void flush() {
        tb.End();
        tb.Begin();
    }

    public void Draw(object texture, Vector2 pos, ref Matrix4x4 worldMatrix, Rectangle? src, FSColor color, float rotation, Vector2 scale, float depth) {
        var tex = (Texture2D)texture;
        // texture height
        tb.Draw(tex,
            pos,
            ref worldMatrix,
            src,
            color.ToTrippy(),
            scale,
            rotation,
            Vector2.Zero,
            depth);
        //Console.Out.WriteLine(new Vector3(pos, depth));
        //Console.Out.WriteLine(Vector4.Transform(new Vector3(pos, depth), shaderProgram.World * shaderProgram.View * shaderProgram.Projection));
    }
}