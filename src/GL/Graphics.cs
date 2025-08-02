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
    private int currentViewportX, currentViewportY, currentViewportWidth, currentViewportHeight;

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

    public void setViewport(int x, int y, int width, int height) {
        GL.Viewport(x, y, (uint)width, (uint)height);
        currentViewportX = x;
        currentViewportY = y;
        currentViewportWidth = width;
        currentViewportHeight = height;
    }

    public void saveViewport() {
        viewportParams[0] = currentViewportX;
        viewportParams[1] = currentViewportY;
        viewportParams[2] = currentViewportWidth;
        viewportParams[3] = currentViewportHeight;
    }

    public void restoreViewport() {
        setViewport(viewportParams[0], viewportParams[1], viewportParams[2], viewportParams[3]);
    }

    public void saveVAO() {
        GL.GetInteger(GetPName.VertexArrayBinding, out vao);
    }

    public void restoreVAO() {
        GL.BindVertexArray((uint)vao);
    }

    public void resize(Vector2D<int> size) {
        setViewport(0, 0, size.X, size.Y);
        var ortho = Matrix4x4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, -1f, 1f);

        batchShader.World = Matrix4x4.Identity;
        batchShader.View = Matrix4x4.Identity;
        batchShader.Projection = ortho;
    }
}