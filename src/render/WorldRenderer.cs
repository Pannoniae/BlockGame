using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using BlockGame.render.model;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.util.log;
using BlockGame.util.stuff;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.chunk;
using BlockGame.world.entity;
using Molten;
using Molten.DoublePrecision;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.OpenGL.Legacy.Extensions.NV;
using SixLabors.ImageSharp.PixelFormats;
using BoundingFrustum = BlockGame.util.meth.BoundingFrustum;
using Entity = BlockGame.world.entity.Entity;
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
    public int fogType;
    public int fogDensity;
    public int fogColour;
    public int horizonColour;

    public int texSize;
    public int atlasSize;


    public int waterBlockTexture;
    public int waterLightTexture;
    public int wateruMVP;
    public int wateruCameraPos;
    public int waterFogStart;
    public int waterFogEnd;
    public int waterFogType;
    public int waterFogDensity;
    public int waterFogColour;
    public int waterHorizonColour;

    public int waterTexSize;
    public int waterAtlasSize;

    // chunk pos uniforms for non-UBO path
    public int uChunkPos;
    public int dummyuChunkPos;
    public int wateruChunkPos;

    public static BoundingFrustum frustum;

    /// <summary>
    /// What needs to be meshed at the end of the frame
    /// </summary>
    public Queue<SubChunkCoord> meshingQueue = new();

    private static readonly Vector3[] starPositions = generateStarPositions();

    public static Color defaultClearColour = new Color(168, 204, 232);
    public static Color defaultFogColour = Color.White;

    private readonly HashSet<SubChunkCoord> chunksToMesh = [];

    public bool fastChunkSwitch = true;
    public uint chunkVAO;

    public UniformBuffer chunkUBO = null!;
    public ShaderStorageBuffer chunkSSBO = null!;
    public CommandBuffer chunkCMD = null!;
    public BindlessIndirectBuffer bindlessBuffer = null!;


    public WorldRenderer() {
        GL = Game.GL;

        var mode = Settings.instance.rendererMode;

        cloudidt.setup();

        // load cloud texture
        var p = Game.textures.cloudTexture.imageData.Span;

        pixels = new bool[p.Length];

        for (int i = 0; i < p.Length; i++) {
            pixels[i] = p[i].A > 0;
        }

        // pre-calc max verts for full 256x256 texture - static, never changes
        cloudMaxVerts = 0;
        for (int yy = 0; yy < 256; yy++) {
            for (int xx = 0; xx < 256; xx++) {
                if (!pixels[(yy << 8) + xx]) continue;

                // top+bottom = 8 verts always
                int fc = 8;

                // check 4 adjacents for side faces (4 verts each)
                int adj = xx - 1;
                adj = adj < 0 ? 255 : adj;
                if (!pixels[(yy << 8) + adj]) fc += 4;

                adj = xx + 1;
                adj = adj >= 256 ? 0 : adj;
                if (!pixels[(yy << 8) + adj]) fc += 4;

                adj = yy - 1;
                adj = adj < 0 ? 255 : adj;
                if (!pixels[(adj << 8) + xx]) fc += 4;

                adj = yy + 1;
                adj = adj >= 256 ? 0 : adj;
                if (!pixels[(adj << 8) + xx]) fc += 4;

                cloudMaxVerts += fc;
            }
        }

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
                    if (subChunk != null) {
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
        fogType = worldShader.getUniformLocation(nameof(fogType));
        fogDensity = worldShader.getUniformLocation(nameof(fogDensity));
        fogColour = worldShader.getUniformLocation(nameof(fogColour));
        horizonColour = worldShader.getUniformLocation(nameof(horizonColour));
        texSize = worldShader.getUniformLocationOpt(nameof(texSize));
        atlasSize = worldShader.getUniformLocationOpt(nameof(atlasSize));

        // dummy shader uniforms
        dummyuMVP = dummyShader.getUniformLocation(nameof(uMVP));

        // water shader uniforms
        waterBlockTexture = waterShader.getUniformLocation(nameof(blockTexture));
        waterLightTexture = waterShader.getUniformLocation(nameof(lightTexture));
        wateruMVP = waterShader.getUniformLocation(nameof(uMVP));
        wateruCameraPos = waterShader.getUniformLocation(nameof(uCameraPos));
        waterFogStart = waterShader.getUniformLocation(nameof(fogStart));
        waterFogEnd = waterShader.getUniformLocation(nameof(fogEnd));
        waterFogType = waterShader.getUniformLocation(nameof(fogType));
        waterFogDensity = waterShader.getUniformLocation(nameof(fogDensity));
        waterFogColour = waterShader.getUniformLocation(nameof(fogColour));
        waterHorizonColour = waterShader.getUniformLocation(nameof(horizonColour));
        waterTexSize = waterShader.getUniformLocationOpt(nameof(texSize));
        waterAtlasSize = waterShader.getUniformLocationOpt(nameof(atlasSize));

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

        SharedBlockVAO.VAOFormat(chunkVAO);

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

        reloadTextures();


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

        Game.graphics.genFatQuadIndices();


        // initialize chunk UBO (16 bytes: vec3 + padding)
        //chunkUBO = new UniformBuffer(GL, 256, 0);

        if (effectiveMode >= RendererMode.Instanced) {
            chunkSSBO.makeResident(out ssboaddr);
        }

        currentAnisoLevel = -1;
        currentMSAA = -1;
    }

    /**
     * Is everything backwards? Yes. This needs to be fixed!!
     */
    public void reloadTextures() {
        // assign uniforms for texture sizes
        worldShader.setUniform(texSize, new Vector2I(Game.textures.blockTexture.atlasSize));
        worldShader.setUniform(atlasSize, new Vector2I(Game.textures.blockTexture.width, Game.textures.blockTexture.height));
        waterShader.setUniform(waterTexSize, new Vector2I(Game.textures.blockTexture.atlasSize));
        waterShader.setUniform(waterAtlasSize, new Vector2I(Game.textures.blockTexture.width, Game.textures.blockTexture.height));
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
            fogType = worldShader.getUniformLocation(nameof(fogType));
            fogDensity = worldShader.getUniformLocation(nameof(fogDensity));
            fogColour = worldShader.getUniformLocation(nameof(fogColour));
            horizonColour = worldShader.getUniformLocation(nameof(horizonColour));
            texSize = worldShader.getUniformLocationOpt(nameof(texSize));
            atlasSize = worldShader.getUniformLocationOpt(nameof(atlasSize));

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
            waterFogType = waterShader.getUniformLocation(nameof(fogType));
            waterFogDensity = waterShader.getUniformLocation(nameof(fogDensity));
            waterFogColour = waterShader.getUniformLocation(nameof(fogColour));
            waterHorizonColour = waterShader.getUniformLocation(nameof(horizonColour));
            waterTexSize = waterShader.getUniformLocationOpt(nameof(texSize));
            waterAtlasSize = waterShader.getUniformLocationOpt(nameof(atlasSize));

            // re-bind water shader texture units
            waterShader.setUniform(waterBlockTexture, 0);
            waterShader.setUniform(waterLightTexture, 1);

            // reload dummy shader too
            dummyShader?.Dispose();
            dummyShader = createDummyShader();

            dummyuMVP = dummyShader.getUniformLocation(nameof(uMVP));

            // chunk position uniforms for non-UBO path
            if (Settings.instance.getActualRendererMode() < RendererMode.Instanced) {
                uChunkPos = worldShader.getUniformLocation("uChunkPos");
                dummyuChunkPos = dummyShader.getUniformLocation("uChunkPos");
                wateruChunkPos = waterShader.getUniformLocation("uChunkPos");
            }

            reloadTextures();
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


        worldShader.setUniform(fogStart, fogMinValue);
        worldShader.setUniform(fogEnd, fogMaxValue);
        waterShader.setUniform(waterFogStart, fogMinValue);
        waterShader.setUniform(waterFogEnd, fogMaxValue);

        var liquid = Game.player.getBlockAtEyes();

        if (liquid == Block.WATER) {
            // set fog colour to blue
            worldShader.setUniform(fogColour, Color.CornflowerBlue.toVec4());
            waterShader.setUniform(waterFogColour, Color.CornflowerBlue.toVec4());
            worldShader.setUniform(horizonColour, Color.CornflowerBlue.toVec4());
            waterShader.setUniform(waterHorizonColour, Color.CornflowerBlue.toVec4());

            // do exp2 close
            worldShader.setUniform(fogType, 1);
            waterShader.setUniform(waterFogType, 1);
            worldShader.setUniform(fogDensity, 0.15f);
            waterShader.setUniform(waterFogDensity, 0.15f);
        }
        else if (liquid == Block.LAVA) {
            // set fog colour to orange
            worldShader.setUniform(fogColour, Color.OrangeRed.toVec4());
            waterShader.setUniform(waterFogColour, Color.OrangeRed.toVec4());
            worldShader.setUniform(horizonColour, Color.OrangeRed.toVec4());
            waterShader.setUniform(waterHorizonColour, Color.OrangeRed.toVec4());

            // do exp2 close
            worldShader.setUniform(fogType, 1);
            waterShader.setUniform(waterFogType, 1);
            worldShader.setUniform(fogDensity, 1.5f);
            waterShader.setUniform(waterFogDensity, 1.5f);
        }
        else {
            // use time-based colours
            var currentFogColour = Game.graphics.getFogColour(world, world.worldTick);
            var currentHorizonColour = Game.graphics.getHorizonColour(world, world.worldTick);

            worldShader.setUniform(fogType, 0);
            waterShader.setUniform(waterFogType, 0);
            worldShader.setUniform(fogColour, currentFogColour.toVec4());
            waterShader.setUniform(waterFogColour, currentFogColour.toVec4());
            worldShader.setUniform(horizonColour, currentHorizonColour.toVec4());
            waterShader.setUniform(waterHorizonColour, currentHorizonColour.toVec4());
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
        const double limit = World.MAX_MESHLOAD_FRAMETIME;
        // empty the meshing queue
        while (Game.permanentStopwatch.Elapsed.TotalMilliseconds - startTime < limit &&
               meshingQueue.TryDequeue(out var sectionCoord)) {
            // if this chunk doesn't exist anymore (because we unloaded it)
            // then don't mesh! otherwise we'll fucking crash
            if (!world!.isChunkSectionInWorld(sectionCoord)) {
                continue;
            }

            // in multiplayer, wait for neighbors before meshing
            // otherwise we mesh against air/null and create holes
            if (Net.mode.isMPC()) {
                var anyMissing = false;

                Span<ChunkCoord> neighbours = [
                    new(sectionCoord.x - 1, sectionCoord.z),
                    new(sectionCoord.x + 1, sectionCoord.z),
                    new(sectionCoord.x, sectionCoord.z - 1),
                    new(sectionCoord.x, sectionCoord.z + 1),
                    new(sectionCoord.x - 1, sectionCoord.z - 1),
                    new(sectionCoord.x - 1, sectionCoord.z + 1),
                    new(sectionCoord.x + 1, sectionCoord.z - 1),
                    new(sectionCoord.x + 1, sectionCoord.z + 1)
                ];

                foreach (var neighbourCoord in neighbours) {
                    // if isn't at least LIGHTED, then skip meshing for now
                    if (!world.getChunkMaybe(neighbourCoord, out var neighbourChunk) ||
                        neighbourChunk.status < ChunkStatus.LIGHTED) {
                        anyMissing = true;
                        break;
                    }
                }

                // if neighbouring chunks are not loaded, re-queue for later
                if (anyMissing) {
                    // add back to chunksToMesh so it gets retried next tick
                    chunksToMesh.Add(sectionCoord);
                    continue;
                }
            }

            var section = world.getSubChunk(sectionCoord);
            var chunk = section.chunk;

            // mesh the subchunk
            Game.blockRenderer.meshChunk(section);

            //Console.Out.WriteLine($"MESHED {sectionCoord}, chunk status: {chunk.status}");

            // update chunk status to MESHED (once per chunk, not per subchunk)
            // this makes the chunk visible in the renderer
            if (chunk.status < ChunkStatus.MESHED && Net.mode.isMPC()) {
                chunk.status = ChunkStatus.MESHED;
                //Console.Out.WriteLine($"  -> Updated status to MESHED");
            }
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
                Game.graphics.elementAddress,
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
        var chunkList = world.chunkList.AsSpan();

        var cameraPos = Game.camera.renderPosition(interp);
        worldShader.setUniform(uMVP, ref viewProj);
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
                Game.graphics.elementAddress, 0);
            Game.vbum.BufferAddressRange(NV.VertexAttribArrayAddressNV, 1,
                Game.graphics.elementAddress, 0);

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
            waterShader.setUniform(wateruMVP, ref viewProj);
            waterShader.setUniform(wateruCameraPos, new Vector3(0));
        }
        else {
            dummyShader.use();
            dummyShader.setUniform(dummyuMVP, ref viewProj);
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
        waterShader.setUniform(wateruMVP, ref viewProj);
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
        //GL.DepthMask(false);
        world.particles.render(interp);
        //GL.DepthMask(true);

        // last thing!
        renderSkyPost(interp);
    }

    /**
     * Now also renders fancy block entities because I'm lazy to copy all the matrix code over to somewhere else.
     */
    public void renderEntities(double interp) {
        var mat = Game.graphics.model;
        mat.push();
        mat.loadIdentity();

        var ide = EntityRenderers.ide;

        // Set matrix components for automatic computation
        ide.model(mat);
        ide.view(Game.camera.getViewMatrix(interp));
        ide.proj(Game.camera.getProjectionMatrix());

        // use unified horizon colour handling
        var currentHorizonColour = Game.graphics.getHorizonColour(world, world.worldTick);

        // set up fog
        ide.setFogType(FogType.Linear);
        ide.enableFog(true);
        ide.fogColor(currentHorizonColour.toVec4());
        ide.fogDistance(Settings.instance.renderDistance * Chunk.CHUNKSIZE * 0.25f, Settings.instance.renderDistance * Chunk.CHUNKSIZE - 16);

        // render all entities
        foreach (var entity in world.entities) {
            // don't render player in first-person
            if (entity == Game.player && Game.camera.mode == CameraMode.FirstPerson) {
                continue;
            }

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

            var interpPos = Vector3D.Lerp(entity.prevPosition, entity.position, interp);
            var interpRot = Vector3.Lerp(entity.prevRotation, entity.rotation, (float)interp);
            var interpBodyRot = Vector3.Lerp(entity.prevBodyRotation, entity.bodyRotation, (float)interp);

            // translate to entity position
            mat.translate((float)interpPos.X, (float)interpPos.Y, (float)interpPos.Z);

            mat.rotate(interpBodyRot.Y, 0, 1, 0);
            mat.rotate(interpBodyRot.Z, 0, 0, 1);

            // get light level at player position and look up in lightmap
            var pos = entity.position.toBlockPos();
            var light = entity.world.inWorld(pos.X, pos.Y, pos.Z) ? entity.world.getLight(pos.X, pos.Y, pos.Z) : (byte)15;
            var blocklight = (byte)((light >> 4) & 0xF);
            var skylight = (byte)(light & 0xF);
            var lightVal = Game.textures.light(blocklight, skylight);

            EntityRenderers.ide.setColour(new Color(lightVal.R, lightVal.G, lightVal.B, (byte)255));

            renderer.render(mat, entity, 1f / 16f, interp);

            // render fire effect if entity is on fire
            if (entity.fireTicks > 0) {
                renderEntityFire(mat, entity, interp);
            }

            mat.pop();
        }

        foreach (var be in world.blockEntities) {

            var type = be.type;
            var isRendered = Registry.BLOCK_ENTITIES.hasRenderer[Registry.BLOCK_ENTITIES.getID(type)];
            if (!isRendered) {
                continue;
            }

            mat.push();
            mat.loadIdentity();

            mat.translate(be.pos.X, be.pos.Y, be.pos.Z);
            var renderer = BlockEntityRenderers.get(Registry.BLOCK_ENTITIES.getID(type));
            renderer.render(mat, be, 1, interp);

            mat.pop();
        }

        mat.pop(); // THIS is why it was leaking
    }

    /** render fire effect on burning entities using their AABB */
    private static void renderEntityFire(MatrixStack mat, Entity entity, double interp) {
        var idt = Game.graphics.idt;
        idt.setTexture(Game.textures.blockTexture);

        idt.model(mat);
        idt.view(Game.camera.getViewMatrix(interp));
        idt.proj(Game.camera.getProjectionMatrix());

        // rotate back to the head pos instead of body pos!
        var interpRot = Vector3.Lerp(entity.prevRotation, entity.rotation, (float)interp);
        var interpBodyRot = Vector3.Lerp(entity.prevBodyRotation, entity.bodyRotation, (float)interp);
        mat.rotate(-interpBodyRot.Y + interpRot.Y, 0, 1, 0);
        mat.rotate(-interpBodyRot.Z + interpRot.Z, 0, 0, 1);

        // fire is fullbright
        var tint = getLightColour(15, 15);
        idt.setColour(tint);

        var uv = new UVPair(3, 14);
        var uvn = UVPair.texCoords(Game.textures.blockTexture, uv);
        var uvx = UVPair.texCoords(Game.textures.blockTexture, uv + 1);

        var aabb = entity.aabb;
        var w = (float)(aabb.max.X - aabb.min.X);
        var h = (float)(aabb.max.Y - aabb.min.Y);
        var d = (float)(aabb.max.Z - aabb.min.Z);

        var cx = (float)(aabb.min.X - entity.position.X + w / 2);
        var y = (float)(aabb.min.Y - entity.position.Y);
        var cz = (float)(aabb.min.Z - entity.position.Z + d / 2);
        var zn = (float)(aabb.min.Z - entity.position.Z);
        var zx = (float)(aabb.min.Z - entity.position.Z + d);

        var xn = (float)(aabb.min.X - entity.position.X);
        var xx = (float)(aabb.min.X - entity.position.X + w);

        const float sc = 1.2f;
        var fw = w * sc;
        var fd = d * sc;

        var fxn = cx - fw / 2;
        var fxx = cx + fw / 2;
        var fzn = cz - fd / 2;
        var fzx = cz + fd / 2;

        // render fire every 0.5 blocks for denser overlapping effect, but each quad is 1 actually block tall
        const float step = 0.5f;
        const float fireHeight = 1.0f;
        int layers = (int)Math.Ceiling(h / step);

        idt.begin(PrimitiveType.Quads);


        var t = new Color(tint.R, tint.G, tint.B, (byte)255);

        for (int i = 0; i < layers; i++) {
            float y0 = y + i * step;
            float y1 = float.Min(y0 + fireHeight, y + h);
            float hh = y1 - y0;

            // adjust for partial heights
            float uvnx = uvn.Y;
            float uvnn = uvn.Y + (uvx.Y - uvn.Y) * (hh / fireHeight);

            idt.addVertex(new BlockVertexTinted(fxn, y0, zn, uvn.X, uvnn, t));
            idt.addVertex(new BlockVertexTinted(fxx, y0, zn, uvx.X, uvnn, t));
            idt.addVertex(new BlockVertexTinted(fxx, y1, zn, uvx.X, uvnx, t));
            idt.addVertex(new BlockVertexTinted(fxn, y1, zn, uvn.X, uvnx, t));

            idt.addVertex(new BlockVertexTinted(fxx, y0, zn, uvn.X, uvnn, t));
            idt.addVertex(new BlockVertexTinted(fxn, y0, zn, uvx.X, uvnn, t));
            idt.addVertex(new BlockVertexTinted(fxn, y1, zn, uvx.X, uvnx, t));
            idt.addVertex(new BlockVertexTinted(fxx, y1, zn, uvn.X, uvnx, t));

            idt.addVertex(new BlockVertexTinted(fxn, y0, zx, uvn.X, uvnn, t));
            idt.addVertex(new BlockVertexTinted(fxx, y0, zx, uvx.X, uvnn, t));
            idt.addVertex(new BlockVertexTinted(fxx, y1, zx, uvx.X, uvnx, t));
            idt.addVertex(new BlockVertexTinted(fxn, y1, zx, uvn.X, uvnx, t));

            idt.addVertex(new BlockVertexTinted(fxx, y0, zx, uvn.X, uvnn, t));
            idt.addVertex(new BlockVertexTinted(fxn, y0, zx, uvx.X, uvnn, t));
            idt.addVertex(new BlockVertexTinted(fxn, y1, zx, uvx.X, uvnx, t));
            idt.addVertex(new BlockVertexTinted(fxx, y1, zx, uvn.X, uvnx, t));


            idt.addVertex(new BlockVertexTinted(xn, y0, fzn, uvn.X, uvnn, t));
            idt.addVertex(new BlockVertexTinted(xn, y0, fzx, uvx.X, uvnn, t));
            idt.addVertex(new BlockVertexTinted(xn, y1, fzx, uvx.X, uvnx, t));
            idt.addVertex(new BlockVertexTinted(xn, y1, fzn, uvn.X, uvnx, t));

            idt.addVertex(new BlockVertexTinted(xn, y0, fzx, uvn.X, uvnn, t));
            idt.addVertex(new BlockVertexTinted(xn, y0, fzn, uvx.X, uvnn, t));
            idt.addVertex(new BlockVertexTinted(xn, y1, fzn, uvx.X, uvnx, t));
            idt.addVertex(new BlockVertexTinted(xn, y1, fzx, uvn.X, uvnx, t));

            idt.addVertex(new BlockVertexTinted(xx, y0, fzn, uvn.X, uvnn, t));
            idt.addVertex(new BlockVertexTinted(xx, y0, fzx, uvx.X, uvnn, t));
            idt.addVertex(new BlockVertexTinted(xx, y1, fzx, uvx.X, uvnx, t));
            idt.addVertex(new BlockVertexTinted(xx, y1, fzn, uvn.X, uvnx, t));

            idt.addVertex(new BlockVertexTinted(xx, y0, fzx, uvn.X, uvnn, t));
            idt.addVertex(new BlockVertexTinted(xx, y0, fzn, uvx.X, uvnn, t));
            idt.addVertex(new BlockVertexTinted(xx, y1, fzn, uvx.X, uvnx, t));
            idt.addVertex(new BlockVertexTinted(xx, y1, fzx, uvn.X, uvnx, t));
        }

        idt.end();
        idt.setColour(Color.White);
    }

    /**
     * Basically render the block at the position, just with the texture overridden to be the breaking texture. (UVPair x = 0 to 8, y = 7)
     */
    public void renderBreakBlock(double interp) {
        if (Game.player == null || !Game.player.isBreaking) {
            return;
        }

        var idt = Game.graphics.idt;

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
        if (Game.raycast.type != Result.BLOCK) {
            return;
        }

        var targetPos = Game.raycast.block;
        world.getAABBs(AABBList, targetPos.X, targetPos.Y, targetPos.Z);

        if (AABBList.Count == 0) {
            return;
        }

        var idc = Game.graphics.idc;

        // disable fog for outline rendering
        idc.enableFog(false);

        var view = Game.camera.getViewMatrix(interp);
        var viewProj = view * Game.camera.getProjectionMatrix();

        idc.setMV(ref view);
        idc.setMVP(ref viewProj);
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

    public Color getLightColourDarken(byte skylight, byte blocklight) {
        var px = Game.textures.light(blocklight, skylight);
        var lightVal = new Color4(px.R / 255f, px.G / 255f, px.B / 255f, px.A / 255f);
        // apply darken
        var darken = world.getSkyDarkenFloat(world.worldTick) / 16f; // 0 to 1 range
        var a = lightVal.A;
        lightVal *= 1 - darken;
        lightVal.A = a; // keep alpha the same
        return lightVal.toC();
    }

    public static Color getLightColour(byte skylight, byte blocklight) {
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