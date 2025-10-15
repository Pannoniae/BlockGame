using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.render.model;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.chunk;
using Molten;
using Molten.DoublePrecision;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.OpenGL.Legacy.Extensions.NV;
using BoundingFrustum = BlockGame.util.meth.BoundingFrustum;
using PrimitiveType = Silk.NET.OpenGL.Legacy.PrimitiveType;
using Shader = BlockGame.GL.Shader;

namespace BlockGame.render;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct ChunkUniforms(Vector3 uChunkPos) {
    public readonly Vector4 data = new(uChunkPos, 1);
}

public sealed partial class WorldRenderer : WorldListener, IDisposable {
    public World? world;
    private int currentAnisoLevel = -1;
    private int currentMSAA = -1;
    private bool currentAffineMapping;
    private bool currentVertexJitter;

    public Silk.NET.OpenGL.Legacy.GL GL;

    public Shader worldShader = null!;
    public Shader dummyShader = null!;
    public Shader waterShader = null!;


    //public int uColor;
    public int blockTexture;
    public int lightTexture;
    public int uMVP;
    public int dummyuMVP;
    public int uCameraPos;
    public int drawDistance;
    public int fogStart;
    public int fogEnd;
    public int fogColour;
    public int horizonColour;


    public int waterBlockTexture;
    public int waterLightTexture;
    public int wateruMVP;
    public int wateruCameraPos;
    public int waterFogStart;
    public int waterFogEnd;
    public int waterFogColour;
    public int waterHorizonColour;

    // chunk pos uniforms for non-UBO path
    public int uChunkPos;
    public int dummyuChunkPos;
    public int wateruChunkPos;

    public static BoundingFrustum frustum;

    /// <summary>
    /// What needs to be meshed at the end of the frame
    /// </summary>
    public Queue<SubChunkCoord> meshingQueue = new();

    public InstantDrawColour idc = new InstantDrawColour(8192);
    public InstantDrawTexture idt = new InstantDrawTexture(8192);

    private static readonly Vector3[] starPositions = generateStarPositions();

    public static Color defaultClearColour = new Color(168, 204, 232);
    public static Color defaultFogColour = Color.White;

    private readonly HashSet<SubChunkCoord> chunksToMesh = [];

    public bool fastChunkSwitch = true;
    public uint chunkVAO;
    public ulong elementAddress;
    public uint elementLen;

    public UniformBuffer chunkUBO = null!;
    public ShaderStorageBuffer chunkSSBO = null!;
    public CommandBuffer chunkCMD = null!;
    public BindlessIndirectBuffer bindlessBuffer = null!;


    public WorldRenderer() {
        GL = Game.GL;

        idc.setup();
        idt.setup();

        var mode = Settings.instance.rendererMode;

        reloadRenderer(mode, mode);
    }

    public void onWorldLoad() {
    }

    public void onWorldUnload() {
    }

    public void onWorldTick(float delta) {
    }

    public void onWorldRender(float delta) {
    }

    public void onChunkLoad(ChunkCoord coord) {
        // added in meshChunk
    }

    public void onChunkUnload(ChunkCoord coord) {
        foreach (var subChunk in world!.getChunk(coord).subChunks) {
            subChunk.vao?.Dispose();
            subChunk.watervao?.Dispose();
            subChunk.vao = null;
            subChunk.watervao = null;
        }
    }

    // remeshing methods
    public void onDirtyChunk(SubChunkCoord coord) {
        chunksToMesh.Add(coord);
    }

    public void onDirtyChunksBatch(ReadOnlySpan<SubChunkCoord> coords) {
        foreach (var coord in coords) {
            chunksToMesh.Add(coord);
        }
    }

    public void onDirtyArea(Vector3I min, Vector3I max) {
        // iterate through all chunks in the area and add them to the meshing

        // Don't clear, these will be processed in the next frame
        //chunksToMesh.Clear();
        for (int x = min.X; x <= max.X; x++) {
            for (int y = min.Y; y <= max.Y; y++) {
                for (int z = min.Z; z <= max.Z; z++) {
                    // section coord
                    var coord = World.getChunkSectionPos(x, y, z);
                    world!.getSubChunkMaybe(coord, out SubChunk? subChunk);
                    if (subChunk != null && !subChunk.isEmpty) {
                        // add to meshing list
                        chunksToMesh.Add(coord);
                    }
                }
            }
        }
    }

    public void setWorld(World? world) {
        this.world?.unlisten(this);
        this.world = world;
        this.world?.listen(this);
    }

    private void genFatQuadIndices() {
        var indices = new ushort[65535];
        // 0 1 2 0 2 3
        for (int i = 0; i < 65535 / 6; i++) {
            indices[i * 6] = (ushort)(i * 4);
            indices[i * 6 + 1] = (ushort)(i * 4 + 1);
            indices[i * 6 + 2] = (ushort)(i * 4 + 2);
            indices[i * 6 + 3] = (ushort)(i * 4);
            indices[i * 6 + 4] = (ushort)(i * 4 + 2);
            indices[i * 6 + 5] = (ushort)(i * 4 + 3);
        }

        // delete old buffer if any
        GL.DeleteBuffer(Game.graphics.fatQuadIndices);
        Game.graphics.fatQuadIndices = 0;
        Game.graphics.fatQuadIndicesLen = 0;
        Game.graphics.fatQuadIndices = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, Game.graphics.fatQuadIndices);
        unsafe {
            fixed (ushort* pIndices = indices) {
                GL.BufferStorage(BufferStorageTarget.ElementArrayBuffer, (uint)(indices.Length * sizeof(ushort)),
                    pIndices, BufferStorageMask.None);
                GL.ObjectLabel(ObjectIdentifier.Buffer, Game.graphics.fatQuadIndices, uint.MaxValue,
                    "Shared quad indices");
            }

            Game.graphics.fatQuadIndicesLen = (uint)(indices.Length * sizeof(ushort));

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

    public void bindQuad() {
        GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, Game.graphics.fatQuadIndices);
    }

    private Shader createWorldShader() {
        var settings = Settings.instance;
        var anisoLevel = settings.anisotropy;
        currentAnisoLevel = anisoLevel;
        currentAffineMapping = settings.affineMapping;
        currentVertexJitter = settings.vertexJitter;

        var defs = new List<Definition> {
            new("ANISO_LEVEL", anisoLevel.ToString()),
            new("DEBUG_ANISO", "0"),
            new("ALPHA_TO_COVERAGE", Settings.instance.msaa > 1 && false ? "1" : "0"),
            new("AFFINE_MAPPING", settings.affineMapping ? "1" : "0"),
            new("VERTEX_JITTER", settings.vertexJitter ? "1" : "0")
        };

        return Shader.createVariant(GL, nameof(worldShader), "shaders/world/shader.vert", "shaders/world/shader.frag",
            null, defs);
    }

    private Shader createWaterShader() {
        var settings = Settings.instance;

        var defs = new List<Definition> {
            new("ANISO_LEVEL", settings.anisotropy.ToString()),
            new("DEBUG_ANISO", "0"),
            new("AFFINE_MAPPING", settings.affineMapping ? "1" : "0"),
            new("VERTEX_JITTER", settings.vertexJitter ? "1" : "0")
        };

        return Shader.createVariant(GL, nameof(waterShader), "shaders/world/waterShader.vert",
            "shaders/world/waterShader.frag", null, defs);
    }

    private Shader createDummyShader() {
        var settings = Settings.instance;

        var defs = new List<Definition> {
            new("AFFINE_MAPPING", settings.affineMapping ? "1" : "0"),
            new("VERTEX_JITTER", settings.vertexJitter ? "1" : "0")
        };
        if (Game.isNVCard) {
            return Shader.createVariant(GL, nameof(dummyShader), "shaders/world/dummyShader.vert", null, null, defs);
        }
        else {
            return Shader.createVariant(GL, nameof(dummyShader), "shaders/world/dummyShader.vert",
                "shaders/world/dummyShader.frag", null, defs);
        }
    }

    private void initializeShaders() {
        worldShader = createWorldShader();

        // some integrated AMD cards shit themselves when they see a vertex shader-only program. IDK which ones (it works on my dedicated AMD card)
        // but for safety, I'll just make it render fragments too on any non-NV card

        dummyShader = createDummyShader();

        waterShader = createWaterShader();
    }

    private void initializeUniforms() {
        // world shader uniforms
        blockTexture = worldShader.getUniformLocation(nameof(blockTexture));
        lightTexture = worldShader.getUniformLocation(nameof(lightTexture));
        uMVP = worldShader.getUniformLocation(nameof(uMVP));
        uCameraPos = worldShader.getUniformLocation(nameof(uCameraPos));
        fogStart = worldShader.getUniformLocation(nameof(fogStart));
        fogEnd = worldShader.getUniformLocation(nameof(fogEnd));
        fogColour = worldShader.getUniformLocation(nameof(fogColour));
        horizonColour = worldShader.getUniformLocation(nameof(horizonColour));

        // dummy shader uniforms
        dummyuMVP = dummyShader.getUniformLocation(nameof(uMVP));

        // water shader uniforms
        waterBlockTexture = waterShader.getUniformLocation(nameof(blockTexture));
        waterLightTexture = waterShader.getUniformLocation(nameof(lightTexture));
        wateruMVP = waterShader.getUniformLocation(nameof(uMVP));
        wateruCameraPos = waterShader.getUniformLocation(nameof(uCameraPos));
        waterFogStart = waterShader.getUniformLocation(nameof(fogStart));
        waterFogEnd = waterShader.getUniformLocation(nameof(fogEnd));
        waterFogColour = waterShader.getUniformLocation(nameof(fogColour));
        waterHorizonColour = waterShader.getUniformLocation(nameof(horizonColour));

        // chunk position uniforms for non-UBO path
        if (Settings.instance.getActualRendererMode() < RendererMode.Instanced) {
            uChunkPos = worldShader.getUniformLocation("uChunkPos");
            dummyuChunkPos = dummyShader.getUniformLocation("uChunkPos");
            wateruChunkPos = waterShader.getUniformLocation("uChunkPos");
        }
    }

    /// <summary>
    /// Reloads shaders and reinitializes renderer based on current settings.
    /// Call this when renderer mode changes in settings.
    /// </summary>
    /// <param name="old"></param>
    public void reloadRenderer(RendererMode oldm, RendererMode newm) {
        // dispose existing shaders
        worldShader?.Dispose();
        dummyShader?.Dispose();
        waterShader?.Dispose();

        // dispose mode-specific resources
        chunkCMD?.Dispose();
        chunkCMD = null;
        bindlessBuffer?.Dispose();
        bindlessBuffer = null;
        chunkSSBO?.Dispose();
        chunkSSBO = null;

        // destroy all sharedchunkVAOs
        foreach (var chunk in world?.chunkList ?? []) {
            foreach (var subChunk in chunk.subChunks) {
                subChunk.vao?.Dispose();
                subChunk.watervao?.Dispose();
                subChunk.vao = null;
                subChunk.watervao = null;
            }
            chunk.status = ChunkStatus.LIGHTED;
        }

        // CMDLIST FIX: clear default FBO because we don't do it normally! :P
        var isActualCMDL = newm == RendererMode.Auto && Game.hasCMDL;
        if (oldm == RendererMode.CommandList || newm == RendererMode.CommandList || isActualCMDL) {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.ClearColor(defaultClearColour.R / 255f, defaultClearColour.G / 255f,
                defaultClearColour.B / 255f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        // switch to new
        Settings.instance.rendererMode = newm;

        var effectiveMode = Settings.instance.getActualRendererMode();

        // create shared chunk VAO
        GL.DeleteVertexArray(chunkVAO);
        chunkVAO = GL.GenVertexArray();

        // initialize shaders
        initializeShaders();

        if (effectiveMode >= RendererMode.Instanced) {
            // allocate SSBO for chunk positions (64KB initial, grows as needed)
            chunkSSBO = new ShaderStorageBuffer(GL, 64 * 1024, 0);

            if (effectiveMode == RendererMode.CommandList) {
                // allocate command buffer for chunk rendering (64KB initial, grows as needed)
                chunkCMD = new CommandBuffer(GL, 64 * 1024);
            }

            // allocate bindless indirect buffer if supported
            if (effectiveMode == RendererMode.BindlessMDI) {
                // 64KB initial, grows as needed
                bindlessBuffer = new BindlessIndirectBuffer(GL, 64 * 1024);
            }
        }

        initializeUniforms();

        if (effectiveMode >= RendererMode.Instanced) {
            chunkData = new List<Vector4>(256 * Chunk.CHUNKHEIGHT);
        }
        else {
            chunkData = null!;
        }


        worldShader.setUniform(blockTexture, 0);
        worldShader.setUniform(lightTexture, 1);
        //shader.setUniform(drawDistance, dd);

        worldShader.setUniform(fogColour, defaultFogColour.toVec4());
        worldShader.setUniform(horizonColour, defaultClearColour.toVec4());

        waterShader.setUniform(waterBlockTexture, 0);
        waterShader.setUniform(waterLightTexture, 1);
        //shader.setUniform(drawDistance, dd);

        waterShader.setUniform(waterFogColour, defaultFogColour.toVec4());
        waterShader.setUniform(waterHorizonColour, defaultClearColour.toVec4());


        // initialize chunk UBO (16 bytes: vec3 + padding)
        //chunkUBO = new UniformBuffer(GL, 256, 0);

        if (effectiveMode >= RendererMode.Instanced) {
            chunkSSBO.makeResident(out ssboaddr);
        }

        currentAnisoLevel = -1;
        currentMSAA = -1;

        // regen shared quad indices for unified memory
        genFatQuadIndices();
    }

    public void updateAF() {
        var settings = Settings.instance;
        var anisoLevel = settings.anisotropy;
        var msaa = settings.msaa;
        var affineMapping = settings.affineMapping;
        var vertexJitter = settings.vertexJitter;

        if (currentAnisoLevel != anisoLevel || currentMSAA != msaa || currentAffineMapping != affineMapping || currentVertexJitter != vertexJitter) {
            // reload worldShader
            worldShader?.Dispose();
            worldShader = createWorldShader();

            currentAnisoLevel = anisoLevel;
            currentMSAA = msaa;
            currentAffineMapping = affineMapping;
            currentVertexJitter = vertexJitter;

            // re-get uniform locations since we have a new shader
            blockTexture = worldShader.getUniformLocation(nameof(blockTexture));
            lightTexture = worldShader.getUniformLocation(nameof(lightTexture));
            uMVP = worldShader.getUniformLocation(nameof(uMVP));
            uCameraPos = worldShader.getUniformLocation(nameof(uCameraPos));
            fogStart = worldShader.getUniformLocation(nameof(fogStart));
            fogEnd = worldShader.getUniformLocation(nameof(fogEnd));
            fogColour = worldShader.getUniformLocation(nameof(fogColour));
            horizonColour = worldShader.getUniformLocation(nameof(horizonColour));

            // re-bind texture units
            worldShader.setUniform(blockTexture, 0);
            worldShader.setUniform(lightTexture, 1);

            // reload waterShader too
            waterShader?.Dispose();
            waterShader = createWaterShader();

            // re-get water shader uniforms
            waterBlockTexture = waterShader.getUniformLocation(nameof(blockTexture));
            waterLightTexture = waterShader.getUniformLocation(nameof(lightTexture));
            wateruMVP = waterShader.getUniformLocation(nameof(uMVP));
            wateruCameraPos = waterShader.getUniformLocation(nameof(uCameraPos));
            waterFogStart = waterShader.getUniformLocation(nameof(fogStart));
            waterFogEnd = waterShader.getUniformLocation(nameof(fogEnd));
            waterFogColour = waterShader.getUniformLocation(nameof(fogColour));
            waterHorizonColour = waterShader.getUniformLocation(nameof(horizonColour));

            // re-bind water shader texture units
            waterShader.setUniform(waterBlockTexture, 0);
            waterShader.setUniform(waterLightTexture, 1);

            // reload dummy shader too
            dummyShader?.Dispose();
            dummyShader = createDummyShader();

            dummyuMVP = dummyShader.getUniformLocation(nameof(uMVP));

            // re-get chunk position uniforms if needed
            if (Settings.instance.getActualRendererMode() < RendererMode.Instanced) {
                uChunkPos = worldShader.getUniformLocation("uChunkPos");
                wateruChunkPos = waterShader.getUniformLocation("uChunkPos");
            }
        }
    }

    public void setUniforms() {
        updateAF();
        var dd = Settings.instance.renderDistance * Chunk.CHUNKSIZE;

        // the problem is that with two chunks, the two values would be the same. so let's adjust them if so
        float fogMaxValue = dd - 16;
        float fogMinValue = (int)(dd * 0.25f);

        if (fogMaxValue <= fogMinValue) {
            fogMinValue = fogMaxValue - 16;
        }

        // don't let the fog in value be more than 8 chunks, otherwise the game will feel empty!
        if (fogMinValue > 8 * Chunk.CHUNKSIZE) {
            fogMinValue = 8 * Chunk.CHUNKSIZE;
        }


        // make the fog slightly less dense if the render distance is higher
        fogMinValue += (dd - 8 * Chunk.CHUNKSIZE) * 0.1f;


        worldShader.setUniform(fogStart, fogMaxValue);
        worldShader.setUniform(fogEnd, fogMinValue);
        waterShader.setUniform(waterFogStart, fogMaxValue);
        waterShader.setUniform(waterFogEnd, fogMinValue);

        if (world.player.isUnderWater()) {
            // set fog colour to blue
            worldShader.setUniform(fogColour, Color.CornflowerBlue.toVec4());
            waterShader.setUniform(waterFogColour, Color.CornflowerBlue.toVec4());
            worldShader.setUniform(horizonColour, Color.CornflowerBlue.toVec4());
            waterShader.setUniform(waterHorizonColour, Color.CornflowerBlue.toVec4());
            worldShader.setUniform(fogEnd, 0f);
            waterShader.setUniform(waterFogEnd, 0f);
            worldShader.setUniform(fogStart, 24f);
            waterShader.setUniform(waterFogStart, 24f);
        }
        else {
            // use time-based colours
            var currentFogColour = world.getFogColour(world.worldTick);
            var currentHorizonColour = world.getHorizonColour(world.worldTick);

            worldShader.setUniform(fogColour, currentFogColour.toVec4());
            waterShader.setUniform(waterFogColour, currentFogColour.toVec4());
            worldShader.setUniform(horizonColour, currentHorizonColour.toVec4());
            waterShader.setUniform(waterHorizonColour, currentHorizonColour.toVec4());
        }
    }

    public void mesh(SubChunkCoord coord) {
        if (!meshingQueue.Contains(coord)) {
            meshingQueue.Enqueue(coord);
        }
    }

    /**
     * This runs once every tick
     * <param name="dt"></param>
     */
    public void update(double dt) {
        // enqueue the to-be-meshed-list
        // this can *probably* be eliminated in the future but yk, keeping it for now
        foreach (var subChunk in chunksToMesh) {
            if (!meshingQueue.Contains(subChunk)) {
                meshingQueue.Enqueue(subChunk);
                Game.metrics.chunksUpdated++;
            }
        }

        chunksToMesh.Clear();

        var startTime = Game.permanentStopwatch.Elapsed.TotalMilliseconds;
        var limit = World.MAX_CHUNKLOAD_FRAMETIME;
        // empty the meshing queue
        while (Game.permanentStopwatch.Elapsed.TotalMilliseconds - startTime < limit &&
               meshingQueue.TryDequeue(out var sectionCoord)) {
            // if this chunk doesn't exist anymore (because we unloaded it)
            // then don't mesh! otherwise we'll fucking crash
            if (!world!.isChunkSectionInWorld(sectionCoord)) {
                continue;
            }

            var section = world.getSubChunk(sectionCoord);
            Game.blockRenderer.meshChunk(section);
        }
    }

    /** NOTE: read <see cref="CommandBuffer"/> for NV_command_list-specific noobtraps and shit.*/
    public void render(double interp) {
        //Game.GD.ResetStates();

        frustum = Game.camera.frustum;


        // determine rendering path based on settings
        var effectiveMode = Settings.instance.getActualRendererMode();
        var usingCMDL = effectiveMode == RendererMode.CommandList;
        var usingBindlessMDI = effectiveMode == RendererMode.BindlessMDI;


        setUniforms();

        // if not a2c, blend
        GL.Enable(EnableCap.Blend);
        GL.DepthMask(false);
        // render sky

        renderSky(interp);
        GL.DepthMask(true);

        // no blending solid shit!
        //GL.Disable(EnableCap.Blend);

        // Enable A2C whenever MSAA is active for alpha-tested geometry (leaves)
        /*if (Settings.instance.msaa > 1) {
            GL.Disable(EnableCap.SampleAlphaToCoverage);
        }
        else {
            GL.Disable(EnableCap.SampleAlphaToCoverage);
        }*/

        //worldShader.use();

        Game.graphics.vao(chunkVAO);
        bindQuad();
        // we'll be using this for a while
        //chunkUBO.bind();
        //chunkUBO.bindToPoint();

        worldShader.use();

        // enable unified memory for chunk rendering
        if (Settings.instance.getActualRendererMode() >= RendererMode.BindlessMDI) {
            #pragma warning disable CS0618 // Type or member is obsolete
            Game.GL.EnableClientState((EnableCap)NV.VertexAttribArrayUnifiedNV);
            Game.GL.EnableClientState((EnableCap)NV.ElementArrayUnifiedNV);
            Game.GL.EnableClientState((EnableCap)NV.UniformBufferUnifiedNV);
            #pragma warning restore CS0618 // Type or member is obsolete

            // set up element array address (shared index buffer)
            Game.vbum.BufferAddressRange(NV.ElementArrayAddressNV, 0,
                elementAddress,
                Game.graphics.fatQuadIndicesLen);
        }

        // format it again
        /*GL.VertexAttribIFormat(0, 3, VertexAttribIType.UnsignedShort, 0);
        GL.VertexAttribIFormat(1, 2, VertexAttribIType.UnsignedShort, 0 + 3 * sizeof(ushort));
        GL.VertexAttribFormat(2, 4, VertexAttribType.UnsignedByte, true, 0 + 5 * sizeof(ushort));

        GL.VertexAttribBinding(0, 0);
        GL.VertexAttribBinding(1, 0);
        GL.VertexAttribBinding(2, 0);

        // get first vertex buffer
        //var firstBuffer = chunkList[0].subChunks[0].vao?.buffer ?? throw new InvalidOperationException("No vertex buffer found for chunk rendering");
        // bind the vertex buffer to the VAO
        Game.vbum.VertexAttribIFormat(0, 3, (Silk.NET.OpenGL.Legacy.Extensions.NV.NV)VertexAttribIType.UnsignedShort, 7 * sizeof(ushort));
        Game.vbum.VertexAttribIFormat(1, 2, (Silk.NET.OpenGL.Legacy.Extensions.NV.NV)VertexAttribIType.UnsignedShort, 7 * sizeof(ushort));
        Game.vbum.VertexAttribFormat(2, 4, (Silk.NET.OpenGL.Legacy.Extensions.NV.NV)VertexAttribType.UnsignedByte, true, 7 * sizeof(ushort));
        //GL.BindVertexBuffer(0, firstBuffer, 0, 7 * sizeof(ushort));*/

        var tex = Game.textures.blockTexture;
        var lightTex = Game.textures.lightTexture;

        // bind textures
        Game.graphics.tex(0, tex);
        Game.graphics.tex(1, lightTex);


        var viewProj = Game.camera.getStaticViewMatrix(interp) * Game.camera.getProjectionMatrix();
        var chunkList = CollectionsMarshal.AsSpan(world.chunkList);

        var cameraPos = Game.camera.renderPosition(interp);
        worldShader.setUniform(uMVP, viewProj);
        worldShader.setUniform(uCameraPos, new Vector3(0));

        // chunkData index
        int cd = 0;
        if (effectiveMode >= RendererMode.Instanced) {
            chunkData.Clear();
        }

        var noCulling = !Settings.instance.frustumCulling;

        // gather chunks to render
        for (int i = 0; i < chunkList.Length; i++) {
            Chunk chunk = chunkList[i];
            var test = !chunk.destroyed && (chunk.status >= ChunkStatus.MESHED) && (noCulling || chunk.isVisible(frustum));
            chunk.isRendered = test;
            if (test) {
                // updates isRendered
                if (noCulling) {
                    for (int sc = 0; sc < Chunk.CHUNKHEIGHT; sc++) {
                        var subChunk = chunk.subChunks[sc];
                        subChunk.isRendered = true;
                    }
                }
                else {
                    isVisibleEight(chunk.subChunks, frustum);
                }

                // if using the UBO path, upload to UBO
                if (effectiveMode >= RendererMode.Instanced) {
                    for (int sc = 0; sc < Chunk.CHUNKHEIGHT; sc++) {
                        var subChunk = chunk.subChunks[sc];
                        if (subChunk.isRendered) {
                            // calculate chunkpos
                            // s.setUniformBound(uChunkPos, (float)(coord.x * 16 - cameraPos.X), (float)(coord.y * 16 - cameraPos.Y), (float)(coord.z * 16 - cameraPos.Z));
                            chunkData.Add(new Vector4((float)(subChunk.worldX - cameraPos.X),
                                (float)(subChunk.worldY - cameraPos.Y), (float)(subChunk.worldZ - cameraPos.Z), 1));
                        }
                    }
                }
            }
        }
        //chunksToRender.Sort(new ChunkComparer(world.player));

        // upload chunkdata to ssbo
        if (effectiveMode >= RendererMode.Instanced) {
            // upload to SSBO
            chunkSSBO.bind();
            chunkSSBO.updateData(CollectionsMarshal.AsSpan(chunkData));
            chunkSSBO.upload();
            chunkSSBO.bindToPoint();
            // get chunkpos uniform

            //var uChunkPos = worldShader.getUniformLocation("chunkPos");
            //var uChunkPosWater = waterShader.getUniformLocation("chunkPos");
            //var uChunkPosWaterDummy = dummyShader.getUniformLocation("chunkPos");


            //worldShader.setUniform(uChunkPos, ssboaddr);
            //waterShader.setUniform(uChunkPosWater, ssboaddr);
            //dummyShader.setUniform(uChunkPosWaterDummy, ssboaddr);

            //chunkSSBO.bindToPoint();
            // unbind ssbo
            //GL.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 0, 0);
        }

        if (Settings.instance.getActualRendererMode() >= RendererMode.BindlessMDI) {
            // why is this necessary?? otherwise it just says
            // DebugSourceApi [DebugTypeOther] [DebugSeverityMedium] (65537): ,  BufferAddressRange (address=0x0000000000000000, length=0x0000000000000000) for attrib 0 is not contained in a resident buffer. This may not be fatal depending on which addresses are actually referenced.
            // wtf
            //
            // status update: probably because the validation happens before the indirect buffer is parsed, so it complains
            // well, this is cheap enough for me to not care! :P
            // status update 2.0: apparently, the error message in the driver refers to the attrib, which *is* hooked onto index 0.... so just select the pointer on index 0 and we're good
            Game.vbum.BufferAddressRange(NV.VertexAttribArrayAddressNV, 0,
                elementAddress, 0);
            Game.vbum.BufferAddressRange(NV.VertexAttribArrayAddressNV, 1,
                elementAddress, 0);

            //Game.vbum.BufferAddressRange((Silk.NET.OpenGL.Legacy.Extensions.NV.NV)NV.UniformBufferAddressNV, i,
            //    elementAddress, 0);
        }

        cd = 0;

        if (usingCMDL) {
            chunkCMD.clear();
        }

        // clear bindless buffer for opaque rendering
        if (usingBindlessMDI) {
            bindlessBuffer.clear();
        }

        // try formatting again?
        foreach (var chunk in chunkList) {
            if (!chunk.isRendered) {
                continue;
            }

            Game.metrics.renderedChunks += 1;
            for (int j = 0; j < Chunk.CHUNKHEIGHT; j++) {
                var subChunk = chunk.subChunks[j];
                if (!subChunk.isRendered) {
                    continue;
                }

                if (usingCMDL) {
                    drawOpaqueCMDL(subChunk, (uint)cd++);
                }
                else if (usingBindlessMDI) {
                    addOpaqueToBindlessBuffer(subChunk, (uint)cd++);
                }
                else if (effectiveMode >= RendererMode.Instanced) {
                    drawOpaqueUBO(subChunk, (uint)cd++);
                }
                else {
                    drawOpaque(subChunk, cameraPos);
                }
            }
        }

        // execute bindless opaque rendering if commands were added
        if (usingBindlessMDI && bindlessBuffer.commands > 0) {
            //Console.WriteLine($"Executing {bindlessBuffer.getCommandCount()} opaque bindless draw commands");
            bindlessBuffer.executeDrawCommands();
        }

        if (usingCMDL) {
            chunkCMD.upload();

            // dump validity
            //chunkCMD.dumpCommands();

            chunkCMD.drawCommands(PrimitiveType.Triangles, 0);

            chunkCMD.clear();
        }

        //goto skip;

        // TRANSLUCENT DEPTH PRE-PASS

        // if integrated shit, don't do depth pre-pass
        if (Game.isAMDIntegratedCard || Game.isIntelIntegratedCard) {
            waterShader.use();
            waterShader.setUniform(wateruMVP, viewProj);
            waterShader.setUniform(wateruCameraPos, new Vector3(0));
        }
        else {
            dummyShader.use();
            dummyShader.setUniform(dummyuMVP, viewProj);
        }

        GL.ColorMask(false, false, false, false);

        cd = 0;
        if (usingCMDL) {
            chunkCMD.clear();
        }

        if (usingBindlessMDI) {
            bindlessBuffer.clear();
        }

        foreach (var chunk in chunkList) {
            if (!chunk.isRendered) {
                continue;
            }

            for (int j = 0; j < Chunk.CHUNKHEIGHT; j++) {
                var subChunk = chunk.subChunks[j];
                if (!subChunk.isRendered) {
                    continue;
                }

                if (usingCMDL) {
                    drawTransparentCMDL(subChunk, (uint)cd++);
                }
                else if (usingBindlessMDI) {
                    // use bindless multi draw indirect for batch rendering
                    addTransparentToBindlessBuffer(subChunk, (uint)cd++);
                }
                else if (effectiveMode >= RendererMode.Instanced) {
                    drawTransparentUBO(subChunk, (uint)cd++);
                }
                else {
                    if (Game.isAMDIntegratedCard || Game.isIntelIntegratedCard) {
                        drawTransparent(subChunk, cameraPos);
                    }
                    else {
                        drawTransparentDummy(subChunk, cameraPos);
                    }
                }
            }
        }

        // execute bindless transparent dummy rendering if commands were added
        if (usingBindlessMDI && bindlessBuffer.commands > 0) {
            //Console.WriteLine($"Executing {bindlessBuffer.getCommandCount()} transparent dummy bindless draw commands");
            bindlessBuffer.executeDrawCommands();
        }

        if (usingCMDL) {
            chunkCMD.upload();
            chunkCMD.drawCommands(PrimitiveType.Triangles, 0);

            chunkCMD.clear();
        }

        // start blending at transparent stuff
        //GL.Enable(EnableCap.Blend);
        //GL.Disable(EnableCap.SampleAlphaToCoverage);

        GL.Disable(EnableCap.CullFace);

        // TRANSLUCENT PASS

        waterShader.use();
        waterShader.setUniform(wateruMVP, viewProj);
        waterShader.setUniform(wateruCameraPos, new Vector3(0));

        GL.ColorMask(true, true, true, true);
        //GL.DepthMask(false);
        //Game.graphics.setDepthFunc();

        cd = 0;
        if (usingCMDL) {
            chunkCMD.clear();
        }

        if (usingBindlessMDI) {
            bindlessBuffer.clear();
        }

        foreach (var chunk in chunkList) {
            if (!chunk.isRendered) {
                continue;
            }

            for (int j = 0; j < Chunk.CHUNKHEIGHT; j++) {
                var subChunk = chunk.subChunks[j];
                if (!subChunk.isRendered) {
                    continue;
                }

                if (usingCMDL) {
                    drawTransparentCMDL(subChunk, (uint)cd++);
                }
                else if (usingBindlessMDI) {
                    // use bindless multi draw indirect for batch rendering
                    addTransparentToBindlessBuffer(subChunk, (uint)cd++);
                }
                else if (effectiveMode >= RendererMode.Instanced) {
                    drawTransparentUBO(subChunk, (uint)cd++);
                }
                else {
                    drawTransparent(subChunk, cameraPos);
                }
            }
        }

        // execute bindless transparent rendering if commands were added
        if (usingBindlessMDI && bindlessBuffer.commands > 0) {
            //Console.WriteLine($"Executing {bindlessBuffer.getCommandCount()} transparent bindless draw commands");
            bindlessBuffer.executeDrawCommands();
        }

        if (usingCMDL) {
            chunkCMD.upload();
            chunkCMD.drawCommands(PrimitiveType.Triangles, 0);
        }

        skip: ;
        //GL.Enable(EnableCap.Blend);

        // disable unified memory after all chunk rendering passes
        if (Settings.instance.getActualRendererMode() >= RendererMode.BindlessMDI) {
            #pragma warning disable CS0618 // Type or member is obsolete
            Game.GL.DisableClientState((EnableCap)NV.ElementArrayUnifiedNV);
            Game.GL.DisableClientState((EnableCap)NV.VertexAttribArrayUnifiedNV);
            Game.GL.DisableClientState((EnableCap)NV.UniformBufferUnifiedNV);
            #pragma warning restore CS0618 // Type or member is obsolete
        }

        // disable dynamic state
        /*GL.DisableVertexAttribArray(2);
        GL.DisableVertexAttribArray(1);
        GL.DisableVertexAttribArray(0);
        GL.BindBufferBase(BufferTargetARB.UniformBuffer, 0, 0);
        GL.BindVertexBuffer(0, 0, 0, 0);
        GL.BindVertexBuffer(1, 0, 0, 0);
        GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);*/

        //GL.DepthMask(true);
        GL.Enable(EnableCap.CullFace);


        renderEntities(interp);

        // render breaking block overlay if any
        renderBreakBlock(interp);


        // no depth writes!
        GL.DepthMask(false);
        world.particles.render(interp);
        GL.DepthMask(true);
    }

    public void renderEntities(double interp) {
        var mat = Game.graphics.model;
        mat.push();
        mat.loadIdentity();

        var ide = EntityRenderers.ide;

        // Set matrix components for automatic computation
        ide.model(mat);
        ide.view(Game.camera.getViewMatrix(interp));
        ide.proj(Game.camera.getProjectionMatrix());

        // render all entities
        foreach (var entity in world.entities) {
            var renderer = EntityRenderers.get(Entities.getID(entity.type));
            if (renderer == null) {
                continue;
            }

            // frustum cull entities
            if (Settings.instance.frustumCulling && frustum.outsideCamera(entity.aabb.toBB())) {
                continue;
            }

            mat.push();
            mat.loadIdentity();

            // interpolate position and rotation
            var interpPos = Vector3D.Lerp(entity.prevPosition, entity.position, interp);
            var interpRot = Vector3.Lerp(entity.prevRotation, entity.rotation, (float)interp);
            var interpBodyRot = Vector3.Lerp(entity.prevBodyRotation, entity.bodyRotation, (float)interp);

            // translate to entity position
            mat.translate((float)interpPos.X, (float)interpPos.Y, (float)interpPos.Z);

            // apply entity body rotation (no X-axis rotation for body)
            mat.rotate(interpBodyRot.Y, 0, 1, 0);
            //mat.rotate(90, 0, 1, 0);  // investigate why this 90-degree offset is needed
            mat.rotate(interpBodyRot.Z, 0, 0, 1);

            // get light level at player position and look up in lightmap
            var pos = entity.position.toBlockPos();
            var light = entity.world.inWorld(pos.X, pos.Y, pos.Z) ? entity.world.getLight(pos.X, pos.Y, pos.Z) : (byte)15;
            var blocklight = (byte)((light >> 4) & 0xF);
            var skylight = (byte)(light & 0xF);
            var lightVal = Game.textures.light(blocklight, skylight);

            EntityRenderers.ide.setColour(new Color(lightVal.R, lightVal.G, lightVal.B, (byte)255));

            // render entity using its renderer
            renderer.render(mat, entity, 1f / 16f, interp);

            mat.pop();
        }

        mat.pop(); // THIS is why it was leaking
    }

    /**
     * Basically render the block at the position, just with the texture overridden to be the breaking texture. (UVPair x = 0 to 8, y = 7)
     */
    public void renderBreakBlock(double interp) {
        if (Game.player == null || !Game.player.isBreaking) {
            return;
        }

        var pos = Game.player.breaking;
        var block = Block.get(world!.getBlock(pos));
        var metadata = world.getBlockMetadata(pos);

        if (block == null || block.id == 0) {
            return;
        }

        // calculate interpolated break progress
        var progress = Meth.lerp(Game.player.prevBreakProgress, Game.player.breakProgress, interp);

        // determine break stage (0-9, where 9 is almost broken)
        var breakStage = (int)(progress * 13);
        breakStage = Math.Clamp(breakStage, 0, 12);

        // only render if there's visible progress
        if (breakStage <= 0) {
            return;
        }

        breakStage--; // adjust to 0-7 range

        // set up block renderer for standalone rendering
        Game.blockRenderer.setupStandalone();

        // force the breaking texture
        Game.blockRenderer.forceTex = new UVPair(breakStage, 12);

        // render the block using BlockRenderer
        breakVertices.Clear();
        Game.blockRenderer.renderBlock(block, metadata, new Vector3I(0, 0, 0), breakVertices, VertexConstructionMode.OPAQUE, 15, default, false);

        // reset forceTex
        Game.blockRenderer.forceTex = new UVPair(-1, -1);

        if (breakVertices.Count == 0) {
            return;
        }

        //var yes = GL.TryGetExtension(out NVBlendEquationAdvanced nvblend);

        // setup rendering state
        //GL.Enable(EnableCap.Blend);
        // multiply!
        //GL.BlendEquation(BlendEquationMode);o
        //GL.BlendFunc(BlendingFactor.DstColor, BlendingFactor.SrcColor);
        Game.graphics.setBlendFuncOverlay();

        //if (yes) {
            //nvblend.BlendParameter(NV.BlendPremultipliedSrcNV, 1);
            // todo add this shit as an optional feature
            //GL.BlendEquation((BlendEquationModeEXT)NV.OverlayNV);
        //}

        GL.DepthMask(false); // don't write to depth buffer
        //GL.Disable(EnableCap.CullFace); // render both sides of the breaking block

        GL.Enable(EnableCap.PolygonOffsetFill);
        //GL.Disable(EnableCap.DepthTest); // disable depth testing to ensure visibility

        var mat = Game.graphics.model;
        mat.push();
        mat.loadIdentity();
        mat.translate(pos.X, pos.Y, pos.Z);
        //mat.scale(1f + Constants.epsilonF, 1f + Constants.epsilonF, 1f + Constants.epsilonF); // slight scale to prevent z-fighting

        // setup matrices
        var view = Game.camera.getViewMatrix(interp);
        var projection = Game.camera.getProjectionMatrix();

        // use idt to render BlockVertexTinted vertices
        idt.model(mat);
        idt.view(view);
        idt.proj(projection);
        idt.enableFog(false);
        Game.graphics.tex(0, Game.textures.blockTexture);
        idt.begin(PrimitiveType.Quads);
        foreach (var vertex in breakVertices) {
            idt.addVertex(vertex);
        }

        idt.end();

        mat.pop();

        GL.DepthMask(true);
        //GL.Enable(EnableCap.CullFace);
        //GL.BlendEquation(BlendEquationModeEXT.FuncAdd);
        Game.graphics.setBlendFunc();
        GL.Disable(EnableCap.PolygonOffsetFill);
        //GL.Enable(EnableCap.DepthTest);
    }

    public ulong testidx;
    public ulong ssboaddr;

    /** Stores the chunk positions! */
    private List<Vector4> chunkData = null!;

    private List<BlockVertexTinted> breakVertices = [];

    private static readonly List<AABB> AABBList = [];

    public void updateRandom(double dt) {
        var player = Game.player;
        const int r = 16;

        const int n = 2048;
        for (int i = 0; i < n; i++) {
            var x = player.position.X + Game.clientRandom.Next(-r, r);
            var y = player.position.Y + Game.clientRandom.Next(-r, r);
            var z = player.position.Z + Game.clientRandom.Next(-r, r);

            var block = Block.get(world.getBlock((int)Math.Floor(x), (int)Math.Floor(y), (int)Math.Floor(z)));
            block?.renderUpdate(world, (int)Math.Floor(x), (int)Math.Floor(y), (int)Math.Floor(z));
        }
    }


    public void drawBlockOutline(double interp) {
        var pos = Game.instance.targetedPos;
        if (pos == null) return;

        var targetPos = pos.Value;
        world.getAABBs(AABBList, targetPos.X, targetPos.Y, targetPos.Z);

        if (AABBList.Count == 0) {
            return;
        }

        // disable fog for outline rendering
        idc.enableFog(false);

        var view = Game.camera.getViewMatrix(interp);
        var viewProj = view * Game.camera.getProjectionMatrix();

        idc.setMV(view);
        idc.setMVP(viewProj);
        idc.begin(PrimitiveType.Lines);

        var outlineColor = new Color(0, 0, 0, 255);

        foreach (var aabb in AABBList) {
            const float OFFSET = 0.005f;
            var minX = (float)aabb.min.X - OFFSET;
            var minY = (float)aabb.min.Y - OFFSET;
            var minZ = (float)aabb.min.Z - OFFSET;
            var maxX = (float)aabb.max.X + OFFSET;
            var maxY = (float)aabb.max.Y + OFFSET;
            var maxZ = (float)aabb.max.Z + OFFSET;

            // bottom face
            idc.addVertex(new VertexTinted(minX, minY, minZ, outlineColor));
            idc.addVertex(new VertexTinted(minX, minY, maxZ, outlineColor));
            idc.addVertex(new VertexTinted(minX, minY, maxZ, outlineColor));
            idc.addVertex(new VertexTinted(maxX, minY, maxZ, outlineColor));
            idc.addVertex(new VertexTinted(maxX, minY, maxZ, outlineColor));
            idc.addVertex(new VertexTinted(maxX, minY, minZ, outlineColor));
            idc.addVertex(new VertexTinted(maxX, minY, minZ, outlineColor));
            idc.addVertex(new VertexTinted(minX, minY, minZ, outlineColor));

            // top face
            idc.addVertex(new VertexTinted(minX, maxY, minZ, outlineColor));
            idc.addVertex(new VertexTinted(minX, maxY, maxZ, outlineColor));
            idc.addVertex(new VertexTinted(minX, maxY, maxZ, outlineColor));
            idc.addVertex(new VertexTinted(maxX, maxY, maxZ, outlineColor));
            idc.addVertex(new VertexTinted(maxX, maxY, maxZ, outlineColor));
            idc.addVertex(new VertexTinted(maxX, maxY, minZ, outlineColor));
            idc.addVertex(new VertexTinted(maxX, maxY, minZ, outlineColor));
            idc.addVertex(new VertexTinted(minX, maxY, minZ, outlineColor));

            // vertical edges
            idc.addVertex(new VertexTinted(minX, minY, minZ, outlineColor));
            idc.addVertex(new VertexTinted(minX, maxY, minZ, outlineColor));
            idc.addVertex(new VertexTinted(maxX, minY, minZ, outlineColor));
            idc.addVertex(new VertexTinted(maxX, maxY, minZ, outlineColor));
            idc.addVertex(new VertexTinted(minX, minY, maxZ, outlineColor));
            idc.addVertex(new VertexTinted(minX, maxY, maxZ, outlineColor));
            idc.addVertex(new VertexTinted(maxX, minY, maxZ, outlineColor));
            idc.addVertex(new VertexTinted(maxX, maxY, maxZ, outlineColor));
        }

        idc.end();
    }


    public static readonly float[] aoArray = [1.0f, 0.75f, 0.5f, 0.25f];

    public static readonly float[] a = [
        0.8f, 0.8f, 0.6f, 0.6f, 0.6f, 1
    ];

    public Color getLightColourDarken(byte blocklight, byte skylight) {
        var px = Game.textures.light(blocklight, skylight);
        var lightVal = new Color4(px.R / 255f, px.G / 255f, px.B / 255f, px.A / 255f);
        // apply darken
        var darken = world.getSkyDarkenFloat(world.worldTick) / 16f; // 0 to 1 range
        var a = lightVal.A;
        lightVal *= 1 - darken;
        lightVal.A = a; // keep alpha the same
        return lightVal.toC();
    }

    public static Color getLightColour(byte blocklight, byte skylight) {
        var px = Game.textures.light(blocklight, skylight);
        var lightVal = new Color(px.R, px.G, px.B, px.A);
        return lightVal;
    }

    public static Color calculateTint(byte dir, byte ao, byte light) {
        dir = (byte)(dir & 0b111);
        var blocklight = (byte)(light >> 4);
        var skylight = (byte)(light & 0xF);
        var lightVal = Game.textures.light(blocklight, skylight);

        // apply darken
        //var darken = Game.world.getSkyDarken(Game.world.worldTick);
        //lightVal *= 1 - darken;

        float tint = a[dir] * aoArray[ao];
        var ab = new Color(lightVal.R / 255f * tint, lightVal.G / 255f * tint, lightVal.B / 255f * tint, 1);
        return ab;
    }

    private void ReleaseUnmanagedResources() {
        if (chunkVAO != 0) {
            Game.GL.DeleteVertexArray(chunkVAO);
            chunkVAO = 0;
        }


        if (worldShader != null!) {
            worldShader.Dispose();
            worldShader = null!;
        }

        if (waterShader != null!) {
            waterShader.Dispose();
            waterShader = null!;
        }

        chunkUBO?.Dispose();
        bindlessBuffer?.Dispose();
    }

    public void Dispose() {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~WorldRenderer() {
        ReleaseUnmanagedResources();
    }
}