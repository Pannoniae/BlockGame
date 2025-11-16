using System.Numerics;
using BlockGame.GL;
using BlockGame.main;
using FontStashSharp;
using FontStashSharp.Interfaces;
using Molten;

namespace BlockGame.util.font;

public class TextRenderer3D : IFontStashRenderer {
    private readonly SpriteBatch tb;
    private readonly BTexture2DManager _textureManager;
    private readonly Shader batchShader3D;

    private readonly int uMVP;

    public ITexture2DManager TextureManager => _textureManager;

    public TextRenderer3D() {
        _textureManager = new BTexture2DManager();
        batchShader3D = new Shader(Game.GL, nameof(batchShader3D), "shaders/ui/batch.vert", "shaders/ui/batch.frag");
        uMVP = batchShader3D.getUniformLocation("uMVP");
        tb = new SpriteBatch(Game.GL);
        tb.NoScreenSpace = true;
        tb.setShader(batchShader3D);
    }

    public void OnViewportChanged(Vector2I size) {
    }

    public void setMatrix(ref Matrix4x4 mat) {
        //shaderProgram.World = mat;
    }

    public void renderTick(double interp) {
        //shaderProgram.View = Game.world.player.camera.getViewMatrix(interp);
        //shaderProgram.Projection = Game.world.player.camera.getProjectionMatrix();

        // set combined VP matrix
        var mat = Game.camera.getViewMatrix(interp)
                  * Game.camera.getProjectionMatrix();
        batchShader3D.setUniform(uMVP, ref mat);
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
            src.GetValueOrDefault(),
            new Color(color.R, color.G, color.B, color.A),
            scale,
            rotation,
            Vector2.Zero,
            depth);
        //Console.Out.WriteLine(new Vector3(pos, depth));
        //Console.Out.WriteLine(Vector4.Transform(new Vector3(pos, depth), shaderProgram.World * shaderProgram.View * shaderProgram.Projection));
    }

    public void Draw(object texture, Vector2 pos, Rectangle? src, FSColor color, float rotation, Vector2 scale, float depth) {
        var tex = (BTexture2D)texture;
        // texture height
        tb.Draw(tex,
            pos,
            src.GetValueOrDefault(),
            new Color(color.R, color.G, color.B, color.A),
            scale,
            rotation,
            Vector2.Zero,
            depth);
    }
}