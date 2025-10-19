using System.Numerics;
using System.Text;
using BlockGame.GL;
using BlockGame.main;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.world;
using Molten;
using Silk.NET.Maths;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.OpenGL.Legacy.Extensions.NV;
using Shader = BlockGame.GL.Shader;

namespace BlockGame.render;

/// <summary>
/// Keep track of all graphics resources here.
/// </summary>
public class Graphics : IDisposable {
    // SpriteBatches
    public readonly SpriteBatch mainBatch;
    public readonly SpriteBatch immediateBatch;

    // InstantRenderers
    public InstantDrawColour idc;
    public InstantDrawTexture idt;

    // Shaders
    public readonly InstantShader batchShader;

    public readonly InstantShader instantTextureShader;

    public readonly InstantShader instantColourShader;

    public readonly InstantShader instantEntityShader;

    // Post-processing shaders
    public readonly Shader fxaaShader;

    public readonly Shader ssaaShader;

    public readonly Shader simplePostShader;

    public readonly Shader crtShader;

    public readonly Silk.NET.OpenGL.Legacy.GL GL;

    private readonly int[] viewportParams = new int[4]; // x, y, width, height
    private int currentViewportX, currentViewportY, currentViewportWidth, currentViewportHeight;

    public bool fullbright;

    // state tracking
    private uint cshader = 0;
    private uint cvao = 0;

    // samplers
    public uint noMipmapSampler;

    /// <summary>
    /// A buffer of indices for the maximum amount of quads.
    /// </summary>
    public uint fatQuadIndices;
    public uint fatQuadIndicesLen;

    public ulong elementAddress;
    public uint elementLen;

    public int groupCount;

    private bool blendFuncTint = true;

    public MatrixStack model = new MatrixStack().reversed();

    /** List of currently bound textures.
     * TODO make the size dynamic, rn it's just 16
     */
    private uint[] textures = new uint[16];

    public readonly List<Shader> shaders = [];

    public Graphics() {
        GL = Game.GL;
        Game.graphics = this;

        instantTextureShader = new InstantShader(Game.GL, nameof(instantTextureShader),
            "shaders/common/base.vert", "shaders/common/base.frag", [new Definition("HAS_TEXTURE")]);
        instantColourShader = new InstantShader(Game.GL, nameof(instantColourShader),
            "shaders/common/base_colour.vert", "shaders/common/base_colour.frag");
        instantEntityShader = new InstantShader(Game.GL, nameof(instantEntityShader),
            "shaders/common/base.vert", "shaders/common/base.frag",
            [new Definition("HAS_NORMALS"), new Definition("HAS_TEXTURE")]);

        fxaaShader =
            new Shader(Game.GL, nameof(fxaaShader), "shaders/postprocess/post.vert", "shaders/postprocess/fxaa_only.frag");
         ssaaShader =
             new Shader(Game.GL, nameof(ssaaShader), "shaders/postprocess/post.vert", "shaders/postprocess/ssaa.frag");
         simplePostShader =
             new Shader(Game.GL, nameof(simplePostShader), "shaders/postprocess/post.vert", "shaders/postprocess/simple_post.frag");
         crtShader =
             new Shader(Game.GL, nameof(crtShader), "shaders/postprocess/post.vert", "shaders/postprocess/crt.frag");


         // gen some indices!
         genFatQuadIndices();



        mainBatch = new SpriteBatch(GL);
        immediateBatch = new SpriteBatch(GL);

        batchShader = new InstantShader(Game.GL, nameof(batchShader), "shaders/ui/batch.vert", "shaders/ui/batch.frag");
        mainBatch.setShader(batchShader);
        immediateBatch.setShader(batchShader);
    }

    public void init() {

        idc = new InstantDrawColour(1024);
        idc.setup();

        idt = new InstantDrawTexture(1024);
        idt.setup();

        // create sampler without mipmaps (is it faster?)
        noMipmapSampler = GL.CreateSampler();
        GL.SamplerParameter(noMipmapSampler, SamplerParameterI.WrapS, (int)GLEnum.ClampToEdge);
        GL.SamplerParameter(noMipmapSampler, SamplerParameterI.WrapT, (int)GLEnum.ClampToEdge);
        GL.SamplerParameter(noMipmapSampler, SamplerParameterI.MinFilter, (int)GLEnum.Nearest);
        GL.SamplerParameter(noMipmapSampler, SamplerParameterI.MagFilter, (int)GLEnum.Nearest);
        GL.SamplerParameter(noMipmapSampler, SamplerParameterF.MaxLod, 0);
        GL.SamplerParameter(noMipmapSampler, SamplerParameterF.MinLod, 0);
    }

    public void clearColor(Color color) {
        //GL.ClearColor(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        // we cheat! this is faster maybe?, IF YOU DRAW EVERYTHING OVER.
        // if you don't, we're fucked
        GL.ClearColor(255, 255, 255, 255);
    }

    /// <summary>
    /// Sets up depth testing with correct function and clear value based on reverse-Z setting.
    /// </summary>
    public void setupDepthTesting() {
        GL.Enable(EnableCap.DepthTest);
        if (Settings.instance.reverseZ) {
            GL.DepthFunc(DepthFunction.Gequal);
            GL.ClearDepth(0.0);
        }
        else {
            GL.DepthFunc(DepthFunction.Lequal);
            GL.ClearDepth(1.0);
        }

        Game.graphics.clearColor(Color.Black);
    }

    public void setupBlend() {
        GL.Enable(EnableCap.Blend);
        GL.BlendEquation(BlendEquationModeEXT.FuncAdd);
        setBlendFunc();
    }

    public void setBlendFunc() {
        if (blendFuncTint) {
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            blendFuncTint = false;
        }
    }

    public void setBlendFuncOverlay() {
        // destination is irrelevant so we can kind of "cheat" here
        // we abuse blending to achieve tinting with DrawTextureNV
        if (!blendFuncTint) {
            GL.BlendFunc(BlendingFactor.DstColor, BlendingFactor.OneMinusSrcAlpha);
            blendFuncTint = true;
        }
    }

    public void setBlendFuncTint() {
        // destination is irrelevant so we can kind of "cheat" here
        // we abuse blending to achieve tinting with DrawTextureNV
        if (!blendFuncTint) {
            GL.BlendFunc(BlendingFactor.ConstantColor, BlendingFactor.OneMinusSrcAlpha);
            blendFuncTint = true;
        }
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
        GL.GetInteger(GetPName.VertexArrayBinding, out var cvao);
        this.cvao = (uint)cvao;
    }

    public void restoreVAO() {
        GL.BindVertexArray(cvao);
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

    public void invalidateTextures() {
        Array.Fill(textures, 0u);
    }

    public void shader(uint handle) {
        if (cshader != handle) {
            cshader = handle;
            GL.UseProgram(handle);
        }
    }

    public void vao(uint handle) {
        if (cvao != handle) {
            cvao = handle;
            GL.BindVertexArray(handle);
        }
    }

    public void regShader(Shader shader) {
        shaders.Add(shader);
    }

    public void resize(Vector2D<int> size) {
        setViewport(0, 0, size.X, size.Y);
        var ortho = Matrix4x4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, -1f, 1f);

        batchShader.World = Matrix4x4.Identity;
        batchShader.View = Matrix4x4.Identity;
        batchShader.Projection = ortho;
    }

    public void scissor(int x, int y, int w, int h) {
        GL.Enable(EnableCap.ScissorTest);
        // convert from top-left screen coords to bottom-left OpenGL coords
        GL.Scissor(x, Game.height - y - h, (uint)w, (uint)h);
    }

    /**
     * Takes UI coords!
     */
    public void scissorUI(int x, int y, int w, int h) {
        var scale = GUI.guiScale;
        scissor(x * scale, y * scale, w * scale, h * scale);
    }

    public void noScissor() {
        GL.Disable(EnableCap.ScissorTest);
    }

    public void popGroup() {
        if (groupCount <= 0) {
            return;
        }

        groupCount--;

        GL.PopDebugGroup();
    }

    public unsafe void pushGroup(string group, Color colour) {
        groupCount++;
        Span<byte> buffer = stackalloc byte[128];
        int bytesWritten = Encoding.UTF8.GetBytes(group, buffer);
        fixed (byte* ptr = buffer) {
            GL.PushDebugGroup(DebugSource.DebugSourceApplication, 0, (uint)bytesWritten, ptr);
        }
    }

    public void genFatQuadIndices() {
        // max quads we can fit: 1000k quads = 4M verts, 6M indices
        const int maxQuads = 1000000;
        var indices = new uint[maxQuads * 6];
        // 0 1 2 0 2 3
        for (int i = 0; i < maxQuads; i++) {
            indices[i * 6] = (uint)(i * 4);
            indices[i * 6 + 1] = (uint)(i * 4 + 1);
            indices[i * 6 + 2] = (uint)(i * 4 + 2);
            indices[i * 6 + 3] = (uint)(i * 4);
            indices[i * 6 + 4] = (uint)(i * 4 + 2);
            indices[i * 6 + 5] = (uint)(i * 4 + 3);
        }

        // delete old buffer if any
        GL.DeleteBuffer(Game.graphics.fatQuadIndices);
        Game.graphics.fatQuadIndices = 0;
        Game.graphics.fatQuadIndicesLen = 0;
        Game.graphics.fatQuadIndices = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, Game.graphics.fatQuadIndices);
        unsafe {
            fixed (uint* pIndices = indices) {
                GL.BufferStorage(BufferStorageTarget.ElementArrayBuffer, (uint)(indices.Length * sizeof(uint)),
                    pIndices, BufferStorageMask.None);
                GL.ObjectLabel(ObjectIdentifier.Buffer, Game.graphics.fatQuadIndices, uint.MaxValue,
                    "Shared quad indices");
            }

            Game.graphics.fatQuadIndicesLen = (uint)(indices.Length * sizeof(uint));

            // make element buffer resident for unified memory if supported
            if (Settings.instance.getActualRendererMode() >= RendererMode.BindlessMDI) {
                Game.sbl.MakeBufferResident((NV)BufferTargetARB.ElementArrayBuffer,
                    (NV)GLEnum.ReadOnly);
                Game.sbl.GetBufferParameter((NV)BufferTargetARB.ElementArrayBuffer,
                    NV.BufferGpuAddressNV, out elementAddress);
                elementLen = Game.graphics.fatQuadIndicesLen;
            }
        }
    }

    private void ReleaseUnmanagedResources() {
        mainBatch.Dispose();
        immediateBatch.Dispose();
        batchShader.Dispose();
        instantTextureShader.Dispose();
        instantColourShader.Dispose();
        instantEntityShader.Dispose();
        fxaaShader.Dispose();
        ssaaShader.Dispose();
        simplePostShader.Dispose();
        crtShader.Dispose();
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

    public void polyOffset(float f, float u) {
        if (Settings.instance.reverseZ) {
            GL.PolygonOffset(-f, -u);
        }
        else {
            GL.PolygonOffset(f, u);
        }
    }
}