using System.Numerics;
using System.Text;
using BlockGame.GL.vertexformats;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.AMD;

namespace BlockGame.GL;

/// <summary>
/// Keep track of all graphics resources here.
/// </summary>
public class Graphics : IDisposable {
    // SpriteBatches
    public readonly SpriteBatch mainBatch;
    public readonly SpriteBatch immediateBatch;

    // Shaders
    public readonly InstantShader batchShader;

    public readonly InstantShader instantTextureShader = new InstantShader(Game.GL, nameof(instantTextureShader),
        "shaders/ui/instantVertex.vert", "shaders/ui/instantVertex.frag");

    public readonly InstantShader instantColourShader = new InstantShader(Game.GL, nameof(instantColourShader),
        "shaders/ui/instantVertexColour.vert", "shaders/ui/instantVertexColour.frag");

    // Post-processing shaders
    public readonly Shader fxaaShader =
        new(Game.GL, nameof(fxaaShader), "shaders/postprocess/post.vert", "shaders/postprocess/fxaa_only.frag");
    
    public readonly Shader ssaaShader =
        new(Game.GL, nameof(ssaaShader), "shaders/postprocess/post.vert", "shaders/postprocess/ssaa.frag");
    
    public readonly Shader simplePostShader =
        new(Game.GL, nameof(simplePostShader), "shaders/postprocess/post.vert", "shaders/postprocess/simple_post.frag");

    public readonly Silk.NET.OpenGL.GL GL;

    private readonly int[] viewportParams = new int[4]; // x, y, width, height
    private int currentViewportX, currentViewportY, currentViewportWidth, currentViewportHeight;

    private int vao;
    public bool fullbright;

    /// <summary>
    /// A buffer of indices for the maximum amount of quads.
    /// </summary>
    public uint fatQuadIndices;
    public uint fatQuadIndicesLen;
    
    public int groupCount;

    public MatrixStack modelView = new MatrixStack().reversed();
    public MatrixStack projection = new MatrixStack().reversed();
    
    /** List of currently bound textures.
     * TODO make the size dynamic, rn it's just 16
     */
    private uint[] textures = new uint[16];

    public Graphics() {
        GL = Game.GL;
        mainBatch = new SpriteBatch(GL);
        immediateBatch = new SpriteBatch(GL);

        batchShader = new InstantShader(Game.GL, nameof(batchShader), "shaders/ui/batch.vert", "shaders/ui/batch.frag");
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
    
    public void tex(uint idx, BTexture2D texture) {
        if (textures[idx] != texture.handle) {
            textures[idx] = texture.handle;
            GL.BindTextureUnit(idx, texture.handle);
        }
    }
    
    public void tex(uint idx, uint handle) {
        if (textures[idx] != handle) {
            textures[idx] = handle;
            GL.BindTextureUnit(idx, handle);
        }
    }
    
    public void updateTexMapping(int idx, uint handle) {
        textures[idx] = handle;
    }

    /// <summary>
    /// Invalidate cached texture handle to force rebinding next time.
    /// Call this when a texture handle is deleted/recreated.
    /// </summary>
    public void invalidateTexture(uint handle) {
        for (int i = 0; i < textures.Length; i++) {
            if (textures[i] == handle) {
                textures[i] = 0; // 0 is never a valid GL handle
            }
        }
    }

    public void resize(Vector2D<int> size) {
        setViewport(0, 0, size.X, size.Y);
        var ortho = Matrix4x4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, -1f, 1f);

        batchShader.World = Matrix4x4.Identity;
        batchShader.View = Matrix4x4.Identity;
        batchShader.Projection = ortho;
    }

    public void popGroup() {
        if (groupCount <= 0) {
            return;
        }
        groupCount--;
        
        GL.PopDebugGroup();
    }

    public unsafe void pushGroup(string group, Color4b colour) {
        groupCount++;
        Span<byte> buffer = stackalloc byte[128];
        int bytesWritten = Encoding.UTF8.GetBytes(group, buffer);
        fixed (byte* ptr = buffer) {
            GL.PushDebugGroup(DebugSource.DebugSourceApplication, 0, (uint)bytesWritten, ptr);
        }
    }

    private void ReleaseUnmanagedResources() {
        mainBatch.Dispose();
        immediateBatch.Dispose();
        batchShader.Dispose();
        instantTextureShader.Dispose();
        instantColourShader.Dispose();
        fxaaShader.Dispose();
        ssaaShader.Dispose();
        simplePostShader.Dispose();
    }

    private void Dispose(bool disposing) {
        ReleaseUnmanagedResources();
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~Graphics() {
        Dispose(false);
    }
}