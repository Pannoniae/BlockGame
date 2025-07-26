using System.Numerics;
using BlockGame.GL.vertexformats;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace BlockGame.GL;

/// <summary>
/// Keep track of all graphics resources here.
/// </summary>
public class Graphics {
    // SpriteBatches
    public readonly SpriteBatch mainBatch;
    public readonly SpriteBatch immediateBatch;

    // Shaders
    public readonly InstantShader batchShader;

    public readonly InstantShader instantTextureShader = new InstantShader(Game.GL, nameof(instantTextureShader),
        "shaders/instantVertex.vert", "shaders/instantVertex.frag");

    public readonly InstantShader instantColourShader = new InstantShader(Game.GL, nameof(instantColourShader),
        "shaders/instantVertexColour.vert", "shaders/instantVertexColour.frag");

    public readonly Shader fxaaShader =
        new Shader(Game.GL, nameof(fxaaShader), "shaders/fxaa.vert", "shaders/fxaa.frag");

    public readonly Silk.NET.OpenGL.GL GL;

    private readonly int[] viewportParams = new int[4]; // x, y, width, height

    private int vao;
    public bool fullbright;

    /// <summary>
    /// A buffer of indices for the maximum amount of quads.
    /// </summary>
    public uint fatQuadIndices;

    public Graphics() {
        GL = Game.GL;
        mainBatch = new SpriteBatch(GL);
        immediateBatch = new SpriteBatch(GL);

        batchShader = new InstantShader(Game.GL, nameof(batchShader), "shaders/batch.vert", "shaders/batch.frag");
        mainBatch.setShader(batchShader);
        immediateBatch.setShader(batchShader);
    }

    public void clearColor(Color4b color) {
        GL.ClearColor(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
    }

    public void saveViewport() {
        GL.GetInteger(GLEnum.Viewport, viewportParams);
    }

    public void restoreViewport() {
        GL.Viewport(viewportParams[0], viewportParams[1], (uint)viewportParams[2], (uint)viewportParams[3]);
    }

    public void saveVAO() {
        GL.GetInteger(GetPName.VertexArrayBinding, out vao);
    }

    public void restoreVAO() {
        GL.BindVertexArray((uint)vao);
    }

    public void resize(Vector2D<int> size) {
        GL.Viewport(0, 0, (uint)size.X, (uint)size.Y);
        var ortho = Matrix4x4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, -1f, 1f);

        batchShader.World = Matrix4x4.Identity;
        batchShader.View = Matrix4x4.Identity;
        batchShader.Projection = ortho;
    }
}