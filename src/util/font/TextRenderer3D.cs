using System.Numerics;
using BlockGame.GL;
using BlockGame.ui;
using FontStashSharp;
using FontStashSharp.Interfaces;
using Molten;
using TrippyGL;
using Color4b = BlockGame.GL.vertexformats.Color4b;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.util.font;

public class TextRenderer3D : IFontStashRenderer {
    private readonly SpriteBatch tb;
    private readonly BTexture2DManager _textureManager;
    private readonly Shader shader;

    private readonly int uMVP;

    public ITexture2DManager TextureManager => _textureManager;

    public GraphicsDevice GraphicsDevice => _textureManager.GraphicsDevice;

    public TextRenderer3D(GraphicsDevice graphicsDevice) {
        _textureManager = new BTexture2DManager(graphicsDevice);
        shader = new Shader(Game.GL, "shaders/batch.vert", "shaders/batch.frag");
        uMVP = shader.getUniformLocation("uMVP");
        tb = new SpriteBatch(Game.GL, shader);
    }

    public void OnViewportChanged(Vector2I size) {
    }

    public void setMatrix(ref Matrix4x4 mat) {
        //shaderProgram.World = mat;
    }

    public void renderTick(double interp) {
        //shaderProgram.View = Screen.GAME_SCREEN.world.player.camera.getViewMatrix(interp);
        //shaderProgram.Projection = Screen.GAME_SCREEN.world.player.camera.getProjectionMatrix();

        // set combined VP matrix
        var mat = Screen.GAME_SCREEN.world.player.camera.getViewMatrix(interp)
                  * Screen.GAME_SCREEN.world.player.camera.getProjectionMatrix();
        shader.setUniform(uMVP, mat);
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

    public void Draw(object texture, Vector2 pos, ref Matrix4x4 worldMatrix, Rectangle? src, FSColor color,
        float rotation, Vector2 scale, float depth) {
        var tex = (BTexture2D)texture;
        // texture height
        tb.Draw(tex,
            pos,
            ref worldMatrix,
            src,
            new Color4b(color.R, color.G, color.B, color.A),
            scale,
            rotation,
            Vector2.Zero,
            depth);
        //Console.Out.WriteLine(new Vector3(pos, depth));
        //Console.Out.WriteLine(Vector4.Transform(new Vector3(pos, depth), shaderProgram.World * shaderProgram.View * shaderProgram.Projection));
    }
}