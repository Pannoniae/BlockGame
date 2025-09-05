using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.ui;
using BlockGame.util;
using Molten;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.OpenGL.Legacy.Extensions.NV;
using BoundingFrustum = BlockGame.util.meth.BoundingFrustum;
using Color = Molten.Color;
using DepthFunction = Silk.NET.OpenGL.Legacy.DepthFunction;
using PrimitiveType = Silk.NET.OpenGL.Legacy.PrimitiveType;
using Shader = BlockGame.GL.Shader;

namespace BlockGame;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct ChunkUniforms(Vector3 uChunkPos) {
    public readonly Vector4 data = new(uChunkPos, 1);
}

public sealed partial class WorldRenderer : WorldListener, IDisposable {
    public World? world;
    private int currentAnisoLevel = -1;
    private int currentMSAA = -1;

    public Silk.NET.OpenGL.Legacy.GL GL;

    public Shader worldShader;
    public Shader dummyShader;
    public Shader waterShader;


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

    public static Color4b defaultClearColour = new Color4b(168, 204, 232);
    public static Color4b defaultFogColour = Color4b.White;

    private readonly HashSet<SubChunkCoord> chunksToMesh = [];

    public bool fastChunkSwitch = true;
    public uint chunkVAO;
    public ulong elementAddress;
    public uint elementLen;

    public UniformBuffer chunkUBO;
    public ShaderStorageBuffer chunkSSBO;
    public CommandBuffer chunkCMD;
    public BindlessIndirectBuffer bindlessBuffer;


    public WorldRenderer() {
        GL = Game.GL;

        chunkVAO = GL.GenVertexArray();

        genFatQuadIndices();

        idc.setup();
        idt.setup();


        // initialize shaders
        initializeShaders();


        // start with reasonable initial sizes, buffers will dynamically resize as needed
        if (Game.hasInstancedUBO) {
            // allocate SSBO for chunk positions (2MB initial, grows as needed)
            chunkSSBO = new ShaderStorageBuffer(GL, 2 * 1024 * 1024, 0);

            if (Game.hasCMDL) {
                // allocate command buffer for chunk rendering (1MB initial, grows as needed)
                chunkCMD = new CommandBuffer(GL, 1024 * 1024);
            }

            // allocate bindless indirect buffer if supported
            if (Game.hasBindlessMDI) {
                // 512KB initial (~7K commands), grows as needed
                bindlessBuffer = new BindlessIndirectBuffer(GL, 512 * 1024);
            }
        }

        initializeUniforms();

        if (Game.hasInstancedUBO) {
            chunkData = new List<Vector4>(8192 * Chunk.CHUNKHEIGHT);
        }
        else {
            chunkData = null!;
        }


        worldShader.setUniform(blockTexture, 0);
        worldShader.setUniform(lightTexture, 1);
        //shader.setUniform(drawDistance, dd);

        worldShader.setUniform(fogColour, defaultFogColour);
        worldShader.setUniform(horizonColour, defaultClearColour);

        waterShader.setUniform(waterBlockTexture, 0);
        waterShader.setUniform(waterLightTexture, 1);
        //shader.setUniform(drawDistance, dd);

        waterShader.setUniform(waterFogColour, defaultFogColour);
        waterShader.setUniform(waterHorizonColour, defaultClearColour);


        // initialize chunk UBO (16 bytes: vec3 + padding)
        //chunkUBO = new UniformBuffer(GL, 256, 0);

        // make resident
        // get address of the ssbo

        if (Game.hasSBL) {
            Game.sbl.MakeNamedBufferResident(chunkSSBO.handle, (NV)GLEnum.ReadOnly);
            Game.sbl.GetNamedBufferParameter(chunkSSBO.handle, NV.BufferGpuAddressNV,
                out ssboaddr);
        }

        //setUniforms();
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
        foreach (var subChunk in world.getChunk(coord).subChunks) {
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
                    world.getSubChunkMaybe(coord, out SubChunk? subChunk);
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
            if (Game.hasVBUM && Game.hasSBL) {
                Game.sbl.MakeBufferResident((Silk.NET.OpenGL.Legacy.Extensions.NV.NV)BufferTargetARB.ElementArrayBuffer,
                    (Silk.NET.OpenGL.Legacy.Extensions.NV.NV)GLEnum.ReadOnly);
                Game.sbl.GetBufferParameter((Silk.NET.OpenGL.Legacy.Extensions.NV.NV)BufferTargetARB.ElementArrayBuffer,
                    Silk.NET.OpenGL.Legacy.Extensions.NV.NV.BufferGpuAddressNV, out elementAddress);
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

        var defs = new List<Definition> {
            new("ANISO_LEVEL", anisoLevel.ToString()),
            new("DEBUG_ANISO", "0"),
            new("ALPHA_TO_COVERAGE", Settings.instance.msaa > 1 ? "1" : "0")
        };

        // Add variant-specific defines
        var variant = Game.hasCMDL ? ShaderVariant.CommandList :
            Game.hasInstancedUBO ? ShaderVariant.Instanced :
            ShaderVariant.Normal;

        return Shader.createVariant(GL, nameof(worldShader), "shaders/world/shader.vert", "shaders/world/shader.frag",
            variant, defs);
    }

    private void initializeShaders() {
        worldShader = createWorldShader();
        dummyShader = Shader.createVariant(GL, nameof(dummyShader), "shaders/world/dummyShader.vert");
        waterShader = Shader.createVariant(GL, nameof(waterShader), "shaders/world/waterShader.vert",
            "shaders/world/waterShader.frag");
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
        if (!Game.hasInstancedUBO) {
            uChunkPos = worldShader.getUniformLocation("uChunkPos");
            dummyuChunkPos = dummyShader.getUniformLocation("uChunkPos");
            wateruChunkPos = waterShader.getUniformLocation("uChunkPos");
        }
    }

    public void updateAF() {
        var settings = Settings.instance;
        var anisoLevel = settings.anisotropy;
        var msaa = settings.msaa;

        if (currentAnisoLevel != anisoLevel || currentMSAA != msaa) {
            worldShader?.Dispose();
            worldShader = createWorldShader();

            currentAnisoLevel = anisoLevel;
            currentMSAA = msaa;

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
            worldShader.setUniform(fogColour, Color4b.CornflowerBlue);
            waterShader.setUniform(waterFogColour, Color4b.CornflowerBlue);
            worldShader.setUniform(horizonColour, Color4b.CornflowerBlue);
            waterShader.setUniform(waterHorizonColour, Color4b.CornflowerBlue);
            worldShader.setUniform(fogEnd, 0f);
            waterShader.setUniform(waterFogEnd, 0f);
            worldShader.setUniform(fogStart, 24f);
            waterShader.setUniform(waterFogStart, 24f);
        }
        else {
            // use time-based colours
            var currentFogColour = world.getFogColour(world.worldTick);
            var currentHorizonColour = world.getHorizonColour(world.worldTick);

            worldShader.setUniform(fogColour, currentFogColour);
            waterShader.setUniform(waterFogColour, currentFogColour);
            worldShader.setUniform(horizonColour, currentHorizonColour);
            waterShader.setUniform(waterHorizonColour, currentHorizonColour);
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


        var usingCMDL = Game.hasCMDL;
        var usingBindlessMDI = Game.hasBindlessMDI && !usingCMDL;


        setUniforms();
        
        // if not a2c, blend
        GL.Enable(EnableCap.Blend);
        GL.DepthMask(false);
        // render sky
        
        renderSky(interp);
        GL.DepthMask(true);

        // no blending solid shit!
        GL.Disable(EnableCap.Blend);
        
        // Enable A2C whenever MSAA is active for alpha-tested geometry (leaves)
        if (Settings.instance.msaa > 1) {
            GL.Enable(EnableCap.SampleAlphaToCoverage);
        }
        else {
            GL.Disable(EnableCap.SampleAlphaToCoverage);
        }

        //worldShader.use();

        GL.BindVertexArray(chunkVAO);
        bindQuad();
        // we'll be using this for a while
        //chunkUBO.bind();
        //chunkUBO.bindToPoint();

        worldShader.use();

        // enable unified memory for chunk rendering
        if (Game.hasVBUM && Game.hasSBL) {
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
        chunkData.Clear();

        var noCulling = !Settings.instance.frustumCulling;

        // gather chunks to render
        for (int i = 0; i < chunkList.Length; i++) {
            Chunk chunk = chunkList[i];
            var test = noCulling || (chunk.status >= ChunkStatus.MESHED && chunk.isVisible(frustum));
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
                if (Game.hasInstancedUBO) {
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
        if (Game.hasCMDL || Game.hasInstancedUBO) {
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

        if (Game.hasVBUM && Game.hasSBL) {
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
                else if (Game.hasInstancedUBO) {
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
        dummyShader.use();
        dummyShader.setUniform(dummyuMVP, viewProj);

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
                else if (usingCMDL || Game.hasInstancedUBO) {
                    drawTransparentUBO(subChunk, (uint)cd++);
                }
                else {
                    drawTransparentDummy(subChunk, cameraPos);
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
        GL.Enable(EnableCap.Blend);
        GL.Disable(EnableCap.SampleAlphaToCoverage);

        GL.Disable(EnableCap.CullFace);

        // TRANSLUCENT PASS

        waterShader.use();
        waterShader.setUniform(wateruMVP, viewProj);
        waterShader.setUniform(wateruCameraPos, new Vector3(0));

        GL.ColorMask(true, true, true, true);
        GL.DepthMask(false);
        GL.DepthFunc(DepthFunction.Lequal);

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
                else if (usingCMDL || Game.hasInstancedUBO) {
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
        GL.Enable(EnableCap.Blend);

        // disable unified memory after all chunk rendering passes
        if (Game.hasVBUM && Game.hasSBL) {
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

        GL.DepthMask(true);
        GL.Enable(EnableCap.CullFace);
        world.particles.render(interp);
    }

    public ulong testidx;
    public ulong ssboaddr;

    /** Stores the chunk positions! */
    private readonly List<Vector4> chunkData;

    private static readonly List<AABB> AABBList = [];

    private void renderSky(double interp) {
        if (Settings.instance.renderDistance <= 4) {
            return;
        }
        
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);

        var viewProj = Game.camera.getStaticViewMatrix(interp) * Game.camera.getProjectionMatrix();
        var modelView = Game.camera.getStaticViewMatrix(interp);

        float dayPercent = world.getDayPercentage(world.worldTick);
        
        float sunAngle = world.getSunAngle(world.worldTick);
        
        var horizonColour = world.getHorizonColour(world.worldTick).toColor();
        var skyColour = world.getSkyColour(world.worldTick).toColor();
        var underSkyColour = new Color(skyColour.R / 255f * 0.3f, skyColour.G / 255f * 0.3f, skyColour.B / 255f * 0.4f);

        // Setup fog
        //idc.enableFog(true);
        //idc.fogColor(horizonColor.toVec4());
        //idc.setFogType(FogType.Exp2);
        //idc.setFogDensity(0.02f);
        idc.enableFog(true);
        idc.fogColor(horizonColour.toVec4());
        idc.setFogType(FogType.Linear);
        //idc.setFogDensity(0.002f);
        idc.fogDistance(0f, 128f);

        var mat = Game.graphics.modelView;
        mat.push();
        
        float tiltAngle = MathF.Sin(sunAngle) * 15f; // ±15
        mat.rotate(tiltAngle, 0, 0, 1);
        
        

        idc.setMVP(mat.top * viewProj);
        idc.setMV(mat.top * modelView);
        
        renderSkyDome(horizonColour, skyColour, underSkyColour);

        mat.pop();
        
        idc.enableFog(false);

        // Render sun and moon
        renderSunnyMoony(dayPercent, sunAngle, viewProj, modelView);

        // Render stars
        renderStars(dayPercent, viewProj, modelView);

        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
    }

    private void renderSkyDome(Color horizonColour, Color skyColour, Color underSkyColour) {
        const float radius = 128f;
        const float topHeight = 16f;
        const float bottomHeight = -48f;
        const int segments = 24;

        idc.begin(PrimitiveType.Triangles);

        // top cone
        for (int i = 0; i < segments; i++) {
            float v = (MathF.PI * 2f / segments) * i;
            float vn = (MathF.PI * 2f / segments) * (i + 1);

            var v1 = new Vector3(MathF.Sin(v) * radius, 0, MathF.Cos(v) * radius);
            var v2 = new Vector3(0, topHeight, 0);
            var v3 = new Vector3(MathF.Sin(vn) * radius, 0, MathF.Cos(vn) * radius);
            
            idc.addVertex(new VertexTinted(v1.X, v1.Y, v1.Z, horizonColour));
            idc.addVertex(new VertexTinted(v2.X, v2.Y, v2.Z, skyColour));
            idc.addVertex(new VertexTinted(v3.X, v3.Y, v3.Z, horizonColour));
        }

        // bottom cone
        for (int i = 0; i < segments; i++) {
            float v = (MathF.PI * 2f / segments) * i;
            float vn = (MathF.PI * 2f / segments) * (i + 1);

            var v1 = new Vector3(0, bottomHeight, 0);
            var v2 = new Vector3(MathF.Sin(v) * radius, 0, MathF.Cos(v) * radius);
            var v3 = new Vector3(MathF.Sin(vn) * radius, 0, MathF.Cos(vn) * radius);

            idc.addVertex(new VertexTinted(v1.X, v1.Y, v1.Z, underSkyColour));
            idc.addVertex(new VertexTinted(v2.X, v2.Y, v2.Z, horizonColour));
            idc.addVertex(new VertexTinted(v3.X, v3.Y, v3.Z, horizonColour));
        }

        idc.end();
    }

    private void renderSunnyMoony(float dayPercent, float sunAngle, Matrix4x4 viewProj, Matrix4x4 modelView) {
        const float sunDistance = 96f;
        const float sunSize = 8f;
        const float moonSize = sunSize * 0.75f;

        var mat = Game.graphics.modelView;
        mat.push();
        
        float sunElevation = world.getSunElevation(world.worldTick);
        float sunIntensity = MathF.Max(0, (sunElevation + (MathF.PI / 6f)) / (MathF.PI / 6f));
        float moonIntensity = MathF.Max(0, -(sunElevation - (MathF.PI / 6f)) / (MathF.PI / 6f));
        
        // Sunny
        Game.graphics.tex(0, Game.textures.sunTexture);
        mat.rotate(Meth.rad2deg(sunAngle), 0, 0, 1);

        idt.setMVP(mat.top * viewProj);
        idt.setMV(mat.top * modelView);
        idt.setColour(new Color(sunIntensity, sunIntensity, sunIntensity * 0.9f, sunIntensity));

        idt.begin(PrimitiveType.Quads);

        var v1 = new Vector3(sunDistance, -sunSize, -sunSize);
        var v2 = new Vector3(sunDistance, sunSize, -sunSize);
        var v3 = new Vector3(sunDistance, sunSize, sunSize);
        var v4 = new Vector3(sunDistance, -sunSize, sunSize);

        idt.addVertex(new BlockVertexTinted(v1.X, v1.Y, v1.Z, 0f, 0f));
        idt.addVertex(new BlockVertexTinted(v2.X, v2.Y, v2.Z, 0f, 1f));
        idt.addVertex(new BlockVertexTinted(v3.X, v3.Y, v3.Z, 1f, 1f));
        idt.addVertex(new BlockVertexTinted(v4.X, v4.Y, v4.Z, 1f, 0f));
        idt.end();

        // Moony
        Game.graphics.tex(0, Game.textures.moonTexture);

        mat.push();
        mat.rotate(180f, 0, 0, 1);
        idt.setMVP(mat.top * viewProj);
        idt.setMV(mat.top * modelView);
        idt.setColour(new Color(moonIntensity * 0.9f, moonIntensity * 0.9f, moonIntensity, moonIntensity));

        idt.begin(PrimitiveType.Quads);

        v1 = new Vector3(sunDistance, -moonSize, -moonSize);
        v2 = new Vector3(sunDistance, moonSize, -moonSize);
        v3 = new Vector3(sunDistance, moonSize, moonSize);
        v4 = new Vector3(sunDistance, -moonSize, moonSize);

        idt.addVertex(new BlockVertexTinted(v1.X, v1.Y, v1.Z, 0f, 0f));
        idt.addVertex(new BlockVertexTinted(v2.X, v2.Y, v2.Z, 0f, 1f));
        idt.addVertex(new BlockVertexTinted(v3.X, v3.Y, v3.Z, 1f, 1f));
        idt.addVertex(new BlockVertexTinted(v4.X, v4.Y, v4.Z, 1f, 0f));
        idt.end();

        mat.pop();
        mat.pop();
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

    public Color4 getLightColourDarken(byte blocklight, byte skylight) {
        var px = Game.textures.light(blocklight, skylight);
        var lightVal = new Color4(px.R / 255f, px.G / 255f, px.B / 255f, px.A / 255f);
        // apply darken
        var darken = world.getSkyDarkenFloat(world.worldTick) / 16f; // 0 to 1 range
        var a = lightVal.A;
        lightVal *= 1 - darken;
        lightVal.A = a; // keep alpha the same
        return lightVal;
    }

    public static Color4b getLightColour(byte blocklight, byte skylight) {
        var px = Game.textures.light(blocklight, skylight);
        var lightVal = new Color4b(px.R, px.G, px.B, px.A);
        return lightVal;
    }

    public static Color4b calculateTint(byte dir, byte ao, byte light) {
        dir = (byte)(dir & 0b111);
        var blocklight = (byte)(light >> 4);
        var skylight = (byte)(light & 0xF);
        var lightVal = Game.textures.light(blocklight, skylight);

        // apply darken
        //var darken = Game.world.getSkyDarken(Game.world.worldTick);
        //lightVal *= 1 - darken;

        float tint = a[dir] * aoArray[ao];
        var ab = new Color4b(lightVal.R / 255f * tint, lightVal.G / 255f * tint, lightVal.B / 255f * tint, 1);
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

    private static Vector3[] generateStarPositions() {
        var random = new XRandom(12345); // fixed seed for consistent stars
        var stars = new Vector3[1200];
        var starDistance = 100f;

        for (int i = 0; i < stars.Length; i++) {
            // generate random point on sphere using proper spherical coordinates
            float u = random.NextSingle();
            float v = random.NextSingle();

            float theta = 2 * MathF.PI * u; // azimuth
            float phi = MathF.Acos(2 * v - 1); // elevation (corrected distribution)

            float x = starDistance * MathF.Sin(phi) * MathF.Cos(theta);
            float y = starDistance * MathF.Cos(phi);
            float z = starDistance * MathF.Sin(phi) * MathF.Sin(theta);

            stars[i] = new Vector3(x, y, z);
        }

        return stars;
    }

    private void renderStars(float dayPercent, Matrix4x4 viewProj, Matrix4x4 modelView) {
        float starAlpha = 0f;

        if (dayPercent >= 0.65f && dayPercent <= 0.9f) {
            //night
            starAlpha = 1.0f;
        }
        else if (dayPercent >= 0.5f && dayPercent < 0.65f) {
            // evening
            starAlpha = Meth.fadeIn(dayPercent, 0.5f, 0.65f);
        }
        else if (dayPercent >= 0.9f) {
            // morning
            starAlpha = Meth.fadeOut(dayPercent, 0.9f, 1.0f);
        }

        if (starAlpha <= 0.0f) {
            return; // day
        }

        var starColour = new Color(1f, 1f, 1f, starAlpha);
        const float starSize = 0.15f;

        float continuousTime = dayPercent * 360;
        var mat = Game.graphics.modelView;

        mat.push();

        mat.rotate(continuousTime, 0.7f, 1f, 0.1f); // tilted celestial axis

        idc.setMVP(mat.top * viewProj);
        idc.setMV(mat.top * modelView);
        idc.enableFog(false);

        idc.begin(PrimitiveType.Quads);

        for (int i = 0; i < starPositions.Length; i++) {
            Vector3 starPos = starPositions[i];
            // create billboard quad that faces the camera (like the sun)
            var toCamera = Vector3.Normalize(-starPos); // direction from star to camera (at origin)
            var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, toCamera));
            var up = Vector3.Cross(toCamera, right);


            var v1 = starPos + (-right - up) * starSize;
            var v2 = starPos + (right - up) * starSize;
            var v3 = starPos + (right + up) * starSize;
            var v4 = starPos + (-right + up) * starSize;

            // generate flicker
            // so we do it per star
            var time = world.worldTick;

            var hash = XHash.hash(i);

            const int TOTAL = 5000;
            const int THRESHOLD = 4950;
            const int REM = 50;
            // so that the flicker is between 0 and 0.3
            const float DIVIDER = (REM) / 2f / 0.3f;

            // smash the hash into an offset into the function
            // below 80% 1, above 80% sin into 0
            var pc = Meth.mod(world.worldTick + hash, TOTAL);

            // 1 to 50 (REM)
            var rem = pc - THRESHOLD;

            // 0 to rem / 2 (25)
            var pc2 = float.Max(0, float.Max(rem - REM, REM - rem));
            var pc3 = pc2 / DIVIDER; // 0 to 0.3ish???
            var flicker = pc > THRESHOLD ? pc3 : 0f;

            var sc = starColour * (1 - flicker);


            idc.addVertex(new VertexTinted(v1.X, v1.Y, v1.Z, sc));
            idc.addVertex(new VertexTinted(v2.X, v2.Y, v2.Z, sc));
            idc.addVertex(new VertexTinted(v3.X, v3.Y, v3.Z, sc));
            idc.addVertex(new VertexTinted(v4.X, v4.Y, v4.Z, sc));
        }

        idc.end();

        mat.pop();
    }
}

public static class ArrayExtensions {
    public static void AddRange<T>(this T[] arr, int index, T[] elements) {
        for (int i = index; i < index + elements.Length; i++) {
            arr[i] = elements[i - index];
        }
    }

    public static void AddRange<T>(this Span<T> arr, int index, Span<T> elements) {
        for (int i = index; i < index + elements.Length; i++) {
            arr[i] = elements[i - index];
        }
    }

    public static void AddRange<T>(this T[] arr, int index, Span<T> elements) {
        for (int i = index; i < index + elements.Length; i++) {
            arr[i] = elements[i - index];
        }
    }
}