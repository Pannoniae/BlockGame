using System.Numerics;
using BlockGame.ui;
using BlockGame.util;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.NV;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TrippyGL;
using DepthFunction = Silk.NET.OpenGL.DepthFunction;
using PrimitiveType = Silk.NET.OpenGL.PrimitiveType;

namespace BlockGame;

public class WorldRenderer {
    public World world;

    public GL GL;

    public Shader shader;
    public Shader dummyShader;
    public Shader waterShader;

    //public int uColor;
    public int blockTexture;
    public int lightTexture;
    public int uMVP;
    public int dummyuMVP;
    public int uCameraPos;
    public int drawDistance;
    public int fogMax;
    public int fogMin;
    public int fogColour;
    public int skyColour;

    public int waterBlockTexture;
    public int waterLightTexture;
    public int wateruMVP;
    public int wateruCameraPos;
    public int waterFogMax;
    public int waterFogMin;
    public int waterFogColour;
    public int waterSkyColour;

    public static BoundingFrustum frustum;


    public Shader outline;
    private uint outlineVao;
    private uint outlineVbo;
    private uint outlineCount;
    //private int outline_uModel;
    private int outline_uView;
    private int outline_uProjection;

    public static Color4b defaultClearColour = new Color4b(70, 190, 225);
    public static Color4b defaultFogColour = new Color4b(210, 210, 210);

    public bool fastChunkSwitch = true;
    public uint chunkVAO;

    public WorldRenderer(World world) {
        this.world = world;
        GL = Game.GL;
        chunkVAO = GL.GenVertexArray();

        shader = new Shader(GL, "shaders/shader.vert", "shaders/shader.frag");
        Game.worldShader = shader;
        dummyShader = new Shader(GL, "shaders/dummyShader.vert");
        Game.dummyShader = dummyShader;
        waterShader = new Shader(GL, "shaders/waterShader.vert", "shaders/waterShader.frag");
        Game.waterShader = waterShader;
        blockTexture = shader.getUniformLocation("blockTexture");
        lightTexture = shader.getUniformLocation("lightTexture");
        uMVP = shader.getUniformLocation(nameof(uMVP));
        dummyuMVP = dummyShader.getUniformLocation(nameof(uMVP));
        uCameraPos = shader.getUniformLocation(nameof(uCameraPos));
        fogMax = shader.getUniformLocation(nameof(fogMax));
        fogMin = shader.getUniformLocation(nameof(fogMin));
        fogColour = shader.getUniformLocation(nameof(fogColour));
        skyColour = shader.getUniformLocation(nameof(skyColour));
        //drawDistance = shader.getUniformLocation(nameof(drawDistance));

        waterBlockTexture = waterShader.getUniformLocation("blockTexture");
        waterLightTexture = waterShader.getUniformLocation("lightTexture");
        wateruMVP = waterShader.getUniformLocation(nameof(uMVP));
        wateruCameraPos = waterShader.getUniformLocation(nameof(uCameraPos));
        waterFogMax = waterShader.getUniformLocation(nameof(fogMax));
        waterFogMin = waterShader.getUniformLocation(nameof(fogMin));
        waterFogColour = waterShader.getUniformLocation(nameof(fogColour));
        waterSkyColour = waterShader.getUniformLocation(nameof(skyColour));
        outline = new Shader(Game.GL, "shaders/outline.vert", "shaders/outline.frag");
        frustum = world.player.camera.frustum;


        shader.use();
        shader.setUniform(blockTexture, 0);
        shader.setUniform(lightTexture, 1);
        //shader.setUniform(drawDistance, dd);

        shader.setUniform(fogColour, defaultFogColour);
        shader.setUniform(skyColour, defaultClearColour);

        waterShader.use();
        waterShader.setUniform(waterBlockTexture, 0);
        waterShader.setUniform(waterLightTexture, 1);
        //shader.setUniform(drawDistance, dd);

        waterShader.setUniform(waterFogColour, defaultFogColour);
        waterShader.setUniform(waterSkyColour, defaultClearColour);

        setUniforms();
    }

    public void setUniforms() {

        var dd = Settings.instance.renderDistance * Chunk.CHUNKSIZE;

        // the problem is that with two chunks, the two values would be the same. so let's adjust them if so
        var fogMaxValue = dd - 16;
        var fogMinValue = (int)(dd * 0.5f);

        if (fogMaxValue <= fogMinValue) {
            fogMinValue = fogMaxValue - 16;
        }

        shader.use();
        shader.setUniform(fogMax, fogMaxValue);
        shader.setUniform(fogMin, fogMinValue);
        waterShader.use();
        waterShader.setUniform(waterFogMax, fogMaxValue);
        waterShader.setUniform(waterFogMin, fogMinValue);
    }


    /// TODO add a path where there's only one VAO for all chunks
    /// and only the VBO is swapped (with glBindVertexBuffer) + the IBO.
    /// Obviously changing the setting in-game would trigger a complete remesh since a different BlockVAO class would be needed
    /// (one which doesn't use a separate VAO but a shared one, and only stores the VBO handle
    /// maybe this will cut down on the VAO switching time??
    public void render(double interp) {
        //Game.GD.ResetStates();
        var tex = Game.textureManager.blockTexture;
        var lightTex = Game.textureManager.lightTexture;
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, tex.handle);
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, lightTex.handle);

        if (fastChunkSwitch) {
            GL.BindVertexArray(chunkVAO);
        }

        var viewProj = world.player.camera.getViewMatrix(interp) * world.player.camera.getProjectionMatrix();
        var chunkList = world.chunkList;
        // gather chunks to render
        for (int i = 0; i < chunkList.Count; i++) {
            var chunk = chunkList[i];
            chunk.isRendered = chunk.status >= ChunkStatus.MESHED && chunk.isVisible(frustum);
        }
        //chunksToRender.Sort(new ChunkComparer(world.player));

        // OPAQUE PASS
        shader.use();
        shader.setUniform(uMVP, viewProj);
        shader.setUniform(uCameraPos, world.player.camera.renderPosition(interp));
        for (int i = 0; i < chunkList.Count; i++) {
            var chunk = chunkList[i];
            if (!chunk.isRendered) {
                continue;
            }
            for (int j = 0; j < Chunk.CHUNKHEIGHT; j++) {
                Game.metrics.renderedChunks += 1;
                chunk.subChunks[j].renderer.drawOpaque();
            }
        }
        // TRANSLUCENT DEPTH PRE-PASS
        dummyShader.use();
        dummyShader.setUniform(dummyuMVP, viewProj);
        GL.Disable(EnableCap.CullFace);
        GL.ColorMask(false, false, false, false);
        for (int i = 0; i < chunkList.Count; i++) {
            var chunk = chunkList[i];
            if (!chunk.isRendered) {
                continue;
            }
            for (int j = 0; j < Chunk.CHUNKHEIGHT; j++) {
                chunk.subChunks[j].renderer.drawTransparent(true);
            }
        }

        Game.GD.BlendingEnabled = true;
        Game.GD.BlendState = Game.initialBlendState;
        // TRANSLUCENT PASS
        waterShader.use();
        waterShader.setUniform(wateruMVP, viewProj);
        waterShader.setUniform(wateruCameraPos, world.player.camera.renderPosition(interp));
        GL.ColorMask(true, true, true, true);
        GL.DepthMask(false);
        GL.DepthFunc(DepthFunction.Lequal);
        for (int i = 0; i < chunkList.Count; i++) {
            var chunk = chunkList[i];
            if (!chunk.isRendered) {
                continue;
            }
            for (int j = 0; j < Chunk.CHUNKHEIGHT; j++) {
                chunk.subChunks[j].renderer.drawTransparent(false);
            }
        }
        GL.DepthMask(true);
        //GL.DepthFunc(DepthFunction.Lequal);
        //Game.GD.BlendingEnabled = false;
        world.particleManager.render(interp);
        GL.Enable(EnableCap.CullFace);
    }

    public void initBlockOutline() {
        unsafe {
            var GL = Game.GL;

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


            Span<float> vertices = stackalloc float[24 * 3] {
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
            };
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, outlineVbo);
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

    public static void meshBlock(Block block, ref List<BlockVertexPacked> vertices, ref List<ushort> indices) {
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
        Vector2D<float> tex;
        Vector2D<float> texMax;
        float u;
        float v;
        float maxU;
        float maxV;

        ushort data1;
        ushort data2;
        ushort data3;
        ushort data4;

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

        Span<BlockVertexPacked> tempVertices = stackalloc BlockVertexPacked[4];
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

            data1 = Block.packData((byte)dir, 0, 15);
            data2 = Block.packData((byte)dir, 0, 15);
            data3 = Block.packData((byte)dir, 0, 15);
            data4 = Block.packData((byte)dir, 0, 15);


            // add vertices

            tempVertices[0] = new BlockVertexPacked(x1, y1, z1, u, v, data1);
            tempVertices[1] = new BlockVertexPacked(x2, y2, z2, u, maxV, data2);
            tempVertices[2] = new BlockVertexPacked(x3, y3, z3, maxU, maxV, data3);
            tempVertices[3] = new BlockVertexPacked(x4, y4, z4, maxU, v, data4);
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

    public static void meshBlockTinted(Block block, ref List<BlockVertexTinted> vertices, ref List<ushort> indices, byte light) {
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
        Vector2D<float> tex;
        Vector2D<float> texMax;
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