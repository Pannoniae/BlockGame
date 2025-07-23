using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.ui;
using BlockGame.util;
using Molten;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp.PixelFormats;
using BoundingFrustum = System.Numerics.BoundingFrustum;
using Color = Molten.Color;
using DepthFunction = Silk.NET.OpenGL.DepthFunction;
using PrimitiveType = Silk.NET.OpenGL.PrimitiveType;
using Shader = BlockGame.GL.Shader;

namespace BlockGame;

public sealed partial class WorldRenderer : WorldListener, IDisposable {
    public World? world;

    public Silk.NET.OpenGL.GL GL;

    public Shader worldShader;
    public Shader dummyShader;
    public Shader waterShader;

    public Shader outline;
    private uint outlineVao;
    private uint outlineVbo;

    private uint outlineCount;

    //private int outline_uModel;
    private int outline_uView;
    private int outline_uProjection;

    //public int uColor;
    public int blockTexture;
    public int uMVP;
    public int dummyuMVP;
    public int uCameraPos;
    public int drawDistance;
    public int fogStart;
    public int fogEnd;
    public int fogColour;
    public int skyColour;
    
    public int aniso;

    public int waterBlockTexture;
    public int wateruMVP;
    public int wateruCameraPos;
    public int waterFogStart;
    public int waterFogEnd;
    public int waterFogColour;
    public int waterSkyColour;

    public static BoundingFrustum frustum;

    public InstantDrawColour idc = new InstantDrawColour(256);
    public InstantDrawTexture idt = new InstantDrawTexture(256);

    public static Color4b defaultClearColour = new Color4b(168, 204, 232);
    public static Color4b defaultFogColour = Color4b.White;

    public bool fastChunkSwitch = true;
    public uint chunkVAO;

    public WorldRenderer() {
        GL = Game.GL;
        chunkVAO = GL.GenVertexArray();

        genFatQuadIndices();

        idc.setup();
        idt.setup();

        worldShader = new Shader(GL, nameof(worldShader), "shaders/shader.vert", "shaders/shader.frag");
        dummyShader = new Shader(GL, nameof(dummyShader), "shaders/dummyShader.vert");
        waterShader = new Shader(GL, nameof(waterShader), "shaders/waterShader.vert", "shaders/waterShader.frag");

        blockTexture = worldShader.getUniformLocation("blockTexture");
        uMVP = worldShader.getUniformLocation(nameof(uMVP));
        dummyuMVP = dummyShader.getUniformLocation(nameof(uMVP));
        uCameraPos = worldShader.getUniformLocation(nameof(uCameraPos));
        fogStart = worldShader.getUniformLocation(nameof(fogStart));
        fogEnd = worldShader.getUniformLocation(nameof(fogEnd));
        fogColour = worldShader.getUniformLocation(nameof(fogColour));
        skyColour = worldShader.getUniformLocation(nameof(skyColour));
        //drawDistance = shader.getUniformLocation(nameof(drawDistance));
        
        aniso = worldShader.getUniformLocation(nameof(aniso));

        waterBlockTexture = waterShader.getUniformLocation("blockTexture");
        wateruMVP = waterShader.getUniformLocation(nameof(uMVP));
        wateruCameraPos = waterShader.getUniformLocation(nameof(uCameraPos));
        waterFogStart = waterShader.getUniformLocation(nameof(fogStart));
        waterFogEnd = waterShader.getUniformLocation(nameof(fogEnd));
        waterFogColour = waterShader.getUniformLocation(nameof(fogColour));
        waterSkyColour = waterShader.getUniformLocation(nameof(skyColour));
        uChunkPos = worldShader.getUniformLocation("uChunkPos");
        dummyuChunkPos = dummyShader.getUniformLocation("uChunkPos");
        wateruChunkPos = waterShader.getUniformLocation("uChunkPos");
        outline = new Shader(Game.GL, nameof(outline), "shaders/outline.vert", "shaders/outline.frag");


        worldShader.setUniform(blockTexture, 0);
        //shader.setUniform(drawDistance, dd);

        worldShader.setUniform(fogColour, defaultFogColour);
        worldShader.setUniform(skyColour, defaultClearColour);

        waterShader.setUniform(waterBlockTexture, 0);
        //shader.setUniform(drawDistance, dd);

        waterShader.setUniform(waterFogColour, defaultFogColour);
        waterShader.setUniform(waterSkyColour, defaultClearColour);

        initBlockOutline();
        
        // Initialize EWA filter uniforms
        

        //setUniforms();
    }
    
    public void onWorldLoad(World world) {
        
    }

    public void onWorldUnload(World world) {
        
    }

    public void onWorldTick(World world, float delta) {
        
    }

    public void onWorldRender(World world, float delta) {
        
    }

    public void onChunkLoad(World world, ChunkCoord coord) {
        // added in meshChunk
    }

    public void onChunkUnload(World world, ChunkCoord coord) {

        foreach (var subChunk in world.getChunk(coord).subChunks) {
            vao.GetValueOrDefault(subChunk.coord)?.Dispose();
            watervao.GetValueOrDefault(subChunk.coord)?.Dispose();
            vao.Remove(subChunk.coord);
            watervao.Remove(subChunk.coord);
        }
    }
    
    public void setWorld(World world) {
        this.world?.unlisten(this);
        this.world = world;
        this.world.listen(this);
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
                GL.ObjectLabel(ObjectIdentifier.Buffer, Game.graphics.fatQuadIndices, uint.MaxValue, "Shared quad indices");
            }
        }
    }

    public void bindQuad() {
        GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, Game.graphics.fatQuadIndices);
    }

    public void updateAF() {
        var settings = Settings.instance;
        var anisoLevel = settings.anisotropy;

        worldShader.setUniform(aniso, (float)anisoLevel);
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
            worldShader.setUniform(skyColour, Color4b.CornflowerBlue);
            waterShader.setUniform(waterSkyColour, Color4b.CornflowerBlue);
            worldShader.setUniform(fogEnd, 0f);
            waterShader.setUniform(waterFogEnd, 0f);
            worldShader.setUniform(fogStart, 24f);
            waterShader.setUniform(waterFogStart, 24f);
        }
        else {
            // set fog colour to default
            worldShader.setUniform(fogColour, defaultFogColour);
            waterShader.setUniform(waterFogColour, defaultFogColour);
            worldShader.setUniform(skyColour, defaultClearColour);
            waterShader.setUniform(waterSkyColour, defaultClearColour);
        }
    }


    /// TODO add a path where there's only one VAO for all chunks
    /// and only the VBO is swapped (with glBindVertexBuffer) + the IBO.
    /// Obviously changing the setting in-game would trigger a complete remesh since a different BlockVAO class would be needed
    /// (one which doesn't use a separate VAO but a shared one, and only stores the VBO handle
    /// maybe this will cut down on the VAO switching time??
    public void render(double interp) {
        //Game.GD.ResetStates();

        frustum = Game.player.camera.frustum;

        GL.BindVertexArray(chunkVAO);
        bindQuad();

        setUniforms();

        GL.DepthMask(false);
        // render sky
        renderSky(interp);
        GL.DepthMask(true);

        // no blending solid shit!
        GL.Disable(EnableCap.Blend);

        GL.BindVertexArray(chunkVAO);

        var tex = Game.textureManager.blockTexture;
        var lightTex = Game.textureManager.lightTexture;
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, tex.handle);
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, lightTex.handle);

        var viewProj = world.player.camera.getStaticViewMatrix(interp) * world.player.camera.getProjectionMatrix();
        var chunkList = CollectionsMarshal.AsSpan(world.chunkList);
        // gather chunks to render
        foreach (var chunk in chunkList) {
            var test = chunk.status >= ChunkStatus.MESHED && chunk.isVisible(frustum);
            chunk.isRendered = test;
            if (test) {
                for (int j = 0; j < Chunk.CHUNKHEIGHT; j++) {
                    var subChunk = chunk.subChunks[j];
                    subChunk.isRendered = isVisible(subChunk, frustum);
                }
            }
        }
        //chunksToRender.Sort(new ChunkComparer(world.player));

        // OPAQUE PASS
        worldShader.use();
        var cameraPos = world.player.camera.renderPosition(interp);
        worldShader.setUniform(uMVP, viewProj);
        worldShader.setUniform(uCameraPos, new Vector3(0));
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

                drawOpaque(subChunk, cameraPos);
            }
        }

        // TRANSLUCENT DEPTH PRE-PASS
        dummyShader.use();
        dummyShader.setUniform(dummyuMVP, viewProj);
        GL.ColorMask(false, false, false, false);
        foreach (var chunk in chunkList) {
            if (!chunk.isRendered) {
                continue;
            }

            for (int j = 0; j < Chunk.CHUNKHEIGHT; j++) {
                var subChunk = chunk.subChunks[j];
                if (!subChunk.isRendered) {
                    continue;
                }

                drawTransparentDummy(subChunk, cameraPos);
            }
        }

        // start blending at transparent stuff
        GL.Enable(EnableCap.Blend);

        GL.Disable(EnableCap.CullFace);
        // TRANSLUCENT PASS
        waterShader.use();
        waterShader.setUniform(wateruMVP, viewProj);
        waterShader.setUniform(wateruCameraPos, new Vector3(0));
        GL.ColorMask(true, true, true, true);
        GL.DepthMask(false);
        GL.DepthFunc(DepthFunction.Lequal);
        foreach (var chunk in chunkList) {
            if (!chunk.isRendered) {
                continue;
            }

            for (int j = 0; j < Chunk.CHUNKHEIGHT; j++) {
                var subChunk = chunk.subChunks[j];
                if (!subChunk.isRendered) {
                    continue;
                }

                drawTransparent(subChunk, cameraPos);
            }
        }

        GL.DepthMask(true);
        GL.Enable(EnableCap.CullFace);
        world.particleManager.render(interp);
    }

    private void renderSky(double interp) {
        // render a flat plane at y = 16
        var viewProj = world.player.camera.getStaticViewMatrix(interp) * world.player.camera.getProjectionMatrix();
        var modelView = world.player.camera.getStaticViewMatrix(interp);
        var sky = new Vector3(0, 8, 0);
        const int skySize = 512;

        // slightly bluer than the default clear colour
        var skyColour = new Color(100, 180, 255);
        // deep blue
        var underSkyColour = new Color(110, 175, 245);

        // Enable fog for sky rendering
        idc.enableFog(true);
        idc.fogColour(defaultClearColour.ToVector4());

        var rd = Settings.instance.renderDistance * Chunk.CHUNKSIZE;
        // cap rd to 12 max
        if (rd > 8 * Chunk.CHUNKSIZE) {
            rd = 8 * Chunk.CHUNKSIZE;
        }
        
        idc.fogDistance(rd * 0.005f, rd);

        //idc.instantShader.use();
        idc.setMVP(viewProj);
        idc.setMV(modelView);
        
        idc.begin(PrimitiveType.Quads);

        // add 6 vertices for the quad
        idc.addVertex(new VertexTinted(sky.X - skySize, sky.Y, sky.Z - skySize, skyColour));
        idc.addVertex(new VertexTinted(sky.X - skySize, sky.Y, sky.Z + skySize, skyColour));
        idc.addVertex(new VertexTinted(sky.X + skySize, sky.Y, sky.Z + skySize, skyColour));
        idc.addVertex(new VertexTinted(sky.X + skySize, sky.Y, sky.Z - skySize, skyColour));
        idc.end();


        var underSky = new Vector3(0, -64, 0);
        
        idc.begin(PrimitiveType.Quads);
        idc.fogDistance(float.Max(rd * 0.005f, 4 * Chunk.CHUNKSIZE), Settings.instance.renderDistance * Chunk.CHUNKSIZE);

        // render the "undersky" - the darker shit below so it doesn't look stupid (BUT WE DONT NEED THIS RN - add when theres actually star rendering n shit)
        idc.addVertex(new VertexTinted(underSky.X - skySize, underSky.Y, underSky.Z - skySize, underSkyColour));
        idc.addVertex(new VertexTinted(underSky.X + skySize, underSky.Y, underSky.Z - skySize, underSkyColour));
        idc.addVertex(new VertexTinted(underSky.X + skySize, underSky.Y, underSky.Z + skySize, underSkyColour));
        idc.addVertex(new VertexTinted(underSky.X - skySize, underSky.Y, underSky.Z + skySize, underSkyColour));

        idc.end();

        // Disable fog after rendering sky
        idc.enableFog(false);
        //GL.Enable(EnableCap.CullFace);

        // Render sun
        var sunPos = new Vector3(16, 0, 0);
        var sunPosRotated = sunPos;
        var sunSize = 1f;

        idt.setMVP(viewProj);
        idt.setMV(modelView);
        Game.GL.ActiveTexture(TextureUnit.Texture0);
        Game.GL.BindTexture(TextureTarget.Texture2D, Game.textureManager.sunTexture.handle);

        // bind the texture

        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        idc.begin(PrimitiveType.Triangles);
        idt.addVertex(
            new BlockVertexTinted(sunPosRotated.X, sunPosRotated.Y - sunSize, sunPosRotated.Z - sunSize,
                0f, 0f));
        idt.addVertex(
            new BlockVertexTinted(sunPosRotated.X, sunPosRotated.Y - sunSize, sunPosRotated.Z + sunSize,
                0f, 1f));
        idt.addVertex(
            new BlockVertexTinted(sunPosRotated.X, sunPosRotated.Y + sunSize, sunPosRotated.Z + sunSize,
                1f, 1f));
        idt.addVertex(
            new BlockVertexTinted(sunPosRotated.X, sunPosRotated.Y + sunSize, sunPosRotated.Z - sunSize,
                1f, 0f));
        idt.addVertex(
            new BlockVertexTinted(sunPosRotated.X, sunPosRotated.Y - sunSize, sunPosRotated.Z - sunSize,
                0f, 0f));
        idt.addVertex(
            new BlockVertexTinted(sunPosRotated.X, sunPosRotated.Y + sunSize, sunPosRotated.Z + sunSize,
                1f, 1f));
        idt.end();
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
    }

    public void initBlockOutline() {
        unsafe {
            var GL = Game.GL;

            GL.DeleteVertexArray(outlineVao);
            outlineVao = GL.GenVertexArray();
            GL.BindVertexArray(outlineVao);

            // 24 verts of 3 floats
            GL.DeleteBuffer(outlineVbo);
            outlineVbo = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, outlineVbo);
            GL.BufferData(BufferTargetARB.ArrayBuffer, 24 * 3 * sizeof(float), 0,
                BufferUsageARB.StreamDraw);

            outlineCount = 24;
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
            GL.EnableVertexAttribArray(0);

            outline.use();
            //outline_uModel = outline.getUniformLocation("uModel");
            outline_uView = outline.getUniformLocation("uView");
            outline_uProjection = outline.getUniformLocation("uProjection");
        }
    }

    public void meshBlockOutline() {
        unsafe {
            var GL = Game.GL;
            var pos = Game.instance.targetedPos!.Value;
            var block = world.getBlock(pos);
            var s = world.getSelectionAABB(pos.X, pos.Y, pos.Z, block)!;
            if (!s.HasValue) {
                return;
            }

            var sel = s.Value;
            const float OFFSET = 0.005f;
            var minX = (float)sel.min.X - OFFSET;
            var minY = (float)sel.min.Y - OFFSET;
            var minZ = (float)sel.min.Z - OFFSET;
            var maxX = (float)sel.max.X + OFFSET;
            var maxY = (float)sel.max.Y + OFFSET;
            var maxZ = (float)sel.max.Z + OFFSET;

            GL.BindVertexArray(outlineVao);


            Span<float> vertices = [
                // bottom
                minX, minY, minZ,
                minX, minY, maxZ,
                minX, minY, maxZ,
                maxX, minY, maxZ,
                maxX, minY, maxZ,
                maxX, minY, minZ,
                maxX, minY, minZ,
                minX, minY, minZ,

                // top
                minX, maxY, minZ,
                minX, maxY, maxZ,
                minX, maxY, maxZ,
                maxX, maxY, maxZ,
                maxX, maxY, maxZ,
                maxX, maxY, minZ,
                maxX, maxY, minZ,
                minX, maxY, minZ,

                // sides
                minX, minY, minZ,
                minX, maxY, minZ,
                maxX, minY, minZ,
                maxX, maxY, minZ,
                minX, minY, maxZ,
                minX, maxY, maxZ,
                maxX, minY, maxZ,
                maxX, maxY, maxZ
            ];
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, outlineVbo);
            GL.InvalidateBufferData(outlineVbo);
            fixed (float* data = vertices) {
                GL.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (uint)(vertices.Length * sizeof(float)), data);
            }
        }
    }

    public void drawBlockOutline(double interp) {
        var GL = Game.GL;
        //var block = Game.instance.targetedPos!.Value;
        GL.BindVertexArray(outlineVao);
        outline.use();
        //outline.setUniform(outline_uModel, Matrix4x4.CreateTranslation(block.X, block.Y, block.Z));
        outline.setUniform(outline_uView, world.player.camera.getViewMatrix(interp));
        outline.setUniform(outline_uProjection, world.player.camera.getProjectionMatrix());
        GL.DrawArrays(PrimitiveType.Lines, 0, outlineCount);
    }

    public static void meshBlock(Block block, ref List<BlockVertexTinted> vertices, ref List<ushort> indices) {
        ushort i = 0;
        const int wx = 0;
        const int wy = 0;
        const int wz = 0;

        int c = 0;
        int ci = 0;

        vertices.Clear();
        indices.Clear();


        Block b = block;
        Face face;

        UVPair texCoords;
        UVPair texCoordsMax;
        Vector2F tex;
        Vector2F texMax;
        float u;
        float v;
        float maxU;
        float maxV;

        Rgba32 data1;
        Rgba32 data2;
        Rgba32 data3;
        Rgba32 data4;

        float offset = 0.0004f;

        float x1;
        float y1;
        float z1;
        float x2;
        float y2;
        float z2;
        float x3;
        float y3;
        float z3;
        float x4;
        float y4;
        float z4;

        Span<BlockVertexTinted> tempVertices = stackalloc BlockVertexTinted[4];
        Span<ushort> tempIndices = stackalloc ushort[6];

        var faces = b.model.faces;

        for (int d = 0; d < faces.Length; d++) {
            face = faces[d];
            var dir = face.direction;

            texCoords = face.min;
            texCoordsMax = face.max;
            tex = Block.texCoords(texCoords);
            texMax = Block.texCoords(texCoordsMax);
            u = tex.X;
            v = tex.Y;
            maxU = texMax.X;
            maxV = texMax.Y;

            x1 = wx + face.x1;
            y1 = wy + face.y1;
            z1 = wz + face.z1;
            x2 = wx + face.x2;
            y2 = wy + face.y2;
            z2 = wz + face.z2;
            x3 = wx + face.x3;
            y3 = wy + face.y3;
            z3 = wz + face.z3;
            x4 = wx + face.x4;
            y4 = wy + face.y4;
            z4 = wz + face.z4;

            data1 = calculateTint((byte)dir, 0, 15);
            data2 = calculateTint((byte)dir, 0, 15);
            data3 = calculateTint((byte)dir, 0, 15);
            data4 = calculateTint((byte)dir, 0, 15);


            // add vertices

            tempVertices[0] = new BlockVertexTinted(x1, y1, z1, u, v, new Color(data1.R, data1.G, data1.B, data1.A));
            tempVertices[1] = new BlockVertexTinted(x2, y2, z2, u, maxV, new Color(data2.R, data2.G, data2.B, data2.A));
            tempVertices[2] =
                new BlockVertexTinted(x3, y3, z3, maxU, maxV, new Color(data3.R, data3.G, data3.B, data3.A));
            tempVertices[3] = new BlockVertexTinted(x4, y4, z4, maxU, v, new Color(data4.R, data4.G, data4.B, data4.A));
            vertices.AddRange(tempVertices);
            c += 4;
            tempIndices[0] = i;
            tempIndices[1] = (ushort)(i + 1);
            tempIndices[2] = (ushort)(i + 2);
            tempIndices[3] = (ushort)(i + 0);
            tempIndices[4] = (ushort)(i + 2);
            tempIndices[5] = (ushort)(i + 3);
            indices.AddRange(tempIndices);
            i += 4;
            ci += 6;
        }
    }

    public static void meshBlockTinted(Block block, ref List<BlockVertexTinted> vertices, ref List<ushort> indices,
        byte light) {
        ushort i = 0;
        const int wx = 0;
        const int wy = 0;
        const int wz = 0;

        int c = 0;
        int ci = 0;

        vertices.Clear();
        indices.Clear();


        Block b = block;
        Face face;

        UVPair texCoords;
        UVPair texCoordsMax;
        Vector2F tex;
        Vector2F texMax;
        float u;
        float v;
        float maxU;
        float maxV;

        float offset = 0.0004f;

        float x1;
        float y1;
        float z1;
        float x2;
        float y2;
        float z2;
        float x3;
        float y3;
        float z3;
        float x4;
        float y4;
        float z4;

        Span<BlockVertexTinted> tempVertices = stackalloc BlockVertexTinted[4];
        Span<ushort> tempIndices = stackalloc ushort[6];

        var faces = b.model.faces;

        for (int d = 0; d < faces.Length; d++) {
            face = faces[d];
            var dir = face.direction;

            texCoords = face.min;
            texCoordsMax = face.max;
            tex = Block.texCoords(texCoords);
            texMax = Block.texCoords(texCoordsMax);
            u = tex.X;
            v = tex.Y;
            maxU = texMax.X;
            maxV = texMax.Y;

            x1 = wx + face.x1;
            y1 = wy + face.y1;
            z1 = wz + face.z1;
            x2 = wx + face.x2;
            y2 = wy + face.y2;
            z2 = wz + face.z2;
            x3 = wx + face.x3;
            y3 = wy + face.y3;
            z3 = wz + face.z3;
            x4 = wx + face.x4;
            y4 = wy + face.y4;
            z4 = wz + face.z4;

            var tint = calculateTint((byte)dir, 0, light);


            // add vertices

            tempVertices[0] = new BlockVertexTinted(x1, y1, z1, u, v, tint.R, tint.G, tint.B, tint.A);
            tempVertices[1] = new BlockVertexTinted(x2, y2, z2, u, maxV, tint.R, tint.G, tint.B, tint.A);
            tempVertices[2] = new BlockVertexTinted(x3, y3, z3, maxU, maxV, tint.R, tint.G, tint.B, tint.A);
            tempVertices[3] = new BlockVertexTinted(x4, y4, z4, maxU, v, tint.R, tint.G, tint.B, tint.A);
            vertices.AddRange(tempVertices);
            c += 4;
            tempIndices[0] = i;
            tempIndices[1] = (ushort)(i + 1);
            tempIndices[2] = (ushort)(i + 2);
            tempIndices[3] = (ushort)(i + 0);
            tempIndices[4] = (ushort)(i + 2);
            tempIndices[5] = (ushort)(i + 3);
            indices.AddRange(tempIndices);
            i += 4;
            ci += 6;
        }
    }

    public static readonly float[] aoArray = [1.0f, 0.75f, 0.5f, 0.25f];

    public static readonly float[] a = [
        0.8f, 0.8f, 0.6f, 0.6f, 0.6f, 1
    ];

    private static Rgba32 calculateTint(byte dir, byte ao, byte light) {
        dir = (byte)(dir & 0b111);
        var blocklight = (byte)(light >> 4);
        var skylight = (byte)(light & 0xF);
        var lightVal = Game.textureManager.lightTexture.getPixel(blocklight, skylight);
        float tint = a[dir] * aoArray[ao];
        var ab = new Rgba32(lightVal.R / 255f * tint, lightVal.G / 255f * tint, lightVal.B / 255f * tint, 1);
        return ab;
    }

    private void ReleaseUnmanagedResources() {
        foreach (var vao in vao.Values) {
            vao?.Dispose();
        }

        foreach (var watervao in watervao.Values) {
            watervao?.Dispose();
        }
    }

    public void Dispose() {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~WorldRenderer() {
        ReleaseUnmanagedResources();
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