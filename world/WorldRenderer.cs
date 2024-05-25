using Silk.NET.Maths;
using Silk.NET.OpenGL;
using TrippyGL;
using DepthFunction = Silk.NET.OpenGL.DepthFunction;
using PrimitiveType = Silk.NET.OpenGL.PrimitiveType;

namespace BlockGame;

public class WorldRenderer {
    public World world;

    public GL GL;

    public Shader shader;
    public Shader dummyShader;

    public List<Chunk> chunksToRender = [];

    public int uProjection;

    //public int uColor;
    public int blockTexture;
    public int lightTexture;
    public int uMVP;
    public int uCameraPos;
    public int drawDistance;
    public int fogColour;


    public Shader outline;
    private uint outlineVao;
    private uint outlineVbo;
    private uint outlineCount;
    //private int outline_uModel;
    private int outline_uView;
    private int outline_uProjection;

    public static Color4b defaultClearColour = Color4b.DeepSkyBlue;

    public bool fastChunkSwitch = true;
    public uint chunkVAO;

    public WorldRenderer(World world) {
        this.world = world;
        GL = Game.GL;
        chunkVAO = GL.GenVertexArray();

        shader = new Shader(GL, "shaders/shader.vert", "shaders/shader.frag");
        dummyShader = new Shader(GL, "shaders/dummyShader.vert", "shaders/dummyShader.frag");
        blockTexture = shader.getUniformLocation("blockTexture");
        lightTexture = shader.getUniformLocation("lightTexture");
        uMVP = shader.getUniformLocation(nameof(uMVP));
        uCameraPos = shader.getUniformLocation(nameof(uCameraPos));
        drawDistance = shader.getUniformLocation(nameof(drawDistance));
        fogColour = shader.getUniformLocation(nameof(fogColour));
        outline = new Shader(Game.GL, "shaders/outline.vert", "shaders/outline.frag");
    }


    /// TODO add a path where there's only one VAO for all chunks
    /// and only the VBO is swapped (with glBindVertexBuffer) + the IBO.
    /// Obviously changing the setting in-game would trigger a complete remesh since a different BlockVAO class would be needed
    /// (one which doesn't use a separate VAO but a shared one, and only stores the VBO handle
    /// maybe this will cut down on the VAO switching time??
    public void render(double interp) {
        //Game.GD.ResetStates();
        var tex = Game.instance.blockTexture;
        var lightTex = Game.instance.lightTexture;
        var t = Game.GD.BindTextureSetActive(tex);
        var t2 = Game.GD.BindTextureSetActive(lightTex);

        if (fastChunkSwitch) {
            GL.BindVertexArray(chunkVAO);
        }

        var viewProj = world.player.camera.getViewMatrix(interp) * world.player.camera.getProjectionMatrix();
        // gather chunks to render
        chunksToRender.Clear();
        foreach (var chunk in world.chunks.Values) {
            if (chunk.status >= ChunkStatus.MESHED && chunk.isVisible(world.player.camera.frustum)) {
                chunksToRender.Add(chunk);
            }
        }
        //Console.Out.WriteLine(chunksToRender.Count);

        // OPAQUE PASS
        shader.use();
        shader.setUniform(uMVP, viewProj);
        shader.setUniform(uCameraPos, world.player.camera.renderPosition(interp));
        shader.setUniform(drawDistance, World.RENDERDISTANCE * Chunk.CHUNKSIZE);
        shader.setUniform(fogColour, defaultClearColour);
        shader.setUniform(blockTexture, t);
        shader.setUniform(lightTexture, t2);
        foreach (var chunk in chunksToRender) {
            chunk.drawOpaque(world.player.camera);
        }
        // TRANSLUCENT DEPTH PRE-PASS
        dummyShader.use();
        dummyShader.setUniform(uMVP, viewProj);
        GL.Disable(EnableCap.CullFace);
        GL.ColorMask(false, false, false, false);
        foreach (var chunk in chunksToRender) {
            chunk.drawTransparent(world.player.camera);
        }
        // TRANSLUCENT PASS
        shader.use();
        GL.ColorMask(true, true, true, true);
        //GL.DepthMask(false);
        //GL.DepthFunc(DepthFunction.Lequal);
        foreach (var chunk in chunksToRender) {
            chunk.drawTransparent(world.player.camera);
        }
        //GL.DepthMask(true);
        GL.DepthFunc(DepthFunction.Lequal);
        GL.Enable(EnableCap.CullFace);

    }

    public void initBlockOutline() {
        unsafe {
            var GL = Game.GL;

            outlineVao = GL.GenVertexArray();
            GL.BindVertexArray(outlineVao);

            // 24 verts of 3 floats
            outlineVbo = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, outlineVbo);
            GL.BufferData(BufferTargetARB.ArrayBuffer, 24 * 3 * sizeof(float), 0,
                BufferUsageARB.StreamDraw);

            outlineCount = 24;
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
            GL.EnableVertexAttribArray(0);

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
            var sel = world.getSelectionAABB(pos.X, pos.Y, pos.Z, block)!;
            const float OFFSET = 0.005f;
            var minX = (float)sel.min.X - OFFSET;
            var minY = (float)sel.min.Y - OFFSET;
            var minZ = (float)sel.min.Z - OFFSET;
            var maxX = (float)sel.max.X + OFFSET;
            var maxY = (float)sel.max.Y + OFFSET;
            var maxZ = (float)sel.max.Z + OFFSET;

            GL.BindVertexArray(outlineVao);


            float[] vertices = [
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
            fixed (float* data = vertices) {
                GL.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (uint)(vertices.Length * sizeof(float)), data);
            }
        }
    }

    public void drawBlockOutline(double interp) {
        var GL = Game.GL;
        var block = Game.instance.targetedPos!.Value;
        GL.BindVertexArray(outlineVao);
        outline.use();
        //outline.setUniform(outline_uModel, Matrix4x4.CreateTranslation(block.X, block.Y, block.Z));
        outline.setUniform(outline_uView, world.player.camera.getViewMatrix(interp));
        outline.setUniform(outline_uProjection, world.player.camera.getProjectionMatrix());
        GL.DrawArrays(PrimitiveType.Lines, 0, outlineCount);
    }

    public static void meshBlock(Block block, ref Span<BlockVertex> vertices, ref Span<ushort> indices) {
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

        Span<BlockVertex> tempVertices = stackalloc BlockVertex[4];
        Span<ushort> tempIndices = stackalloc ushort[6];

        var faces = b.model.faces;

        for (int d = 0; d < faces.Length; d++) {
            face = faces[d];
            var dir = face.direction;

            texCoords = faces[d].min;
            texCoordsMax = faces[d].max;
            tex = Block.texCoords(texCoords);
            texMax = Block.texCoords(texCoordsMax);
            u = tex.X;
            v = tex.Y;
            maxU = texMax.X;
            maxV = texMax.Y;

            x1 = wx + faces[d].x1;
            y1 = wy + faces[d].y1;
            z1 = wz + faces[d].z1;
            x2 = wx + faces[d].x2;
            y2 = wy + faces[d].y2;
            z2 = wz + faces[d].z2;
            x3 = wx + faces[d].x3;
            y3 = wy + faces[d].y3;
            z3 = wz + faces[d].z3;
            x4 = wx + faces[d].x4;
            y4 = wy + faces[d].y4;
            z4 = wz + faces[d].z4;

            data1 = Block.packData((byte)dir, 0, 15);
            data2 = Block.packData((byte)dir, 0, 15);
            data3 = Block.packData((byte)dir, 0, 15);
            data4 = Block.packData((byte)dir, 0, 15);


            // add vertices

            tempVertices[0] = new BlockVertex(x1, y1, z1, u, v, data1);
            tempVertices[1] = new BlockVertex(x2, y2, z2, u, maxV, data2);
            tempVertices[2] = new BlockVertex(x3, y3, z3, maxU, maxV, data3);
            tempVertices[3] = new BlockVertex(x4, y4, z4, maxU, v, data4);
            vertices.AddRange(c, tempVertices);
            c += 4;
            tempIndices[0] = i;
            tempIndices[1] = (ushort)(i + 1);
            tempIndices[2] = (ushort)(i + 2);
            tempIndices[3] = (ushort)(i + 0);
            tempIndices[4] = (ushort)(i + 2);
            tempIndices[5] = (ushort)(i + 3);
            indices.AddRange(ci, tempIndices);
            i += 4;
            ci += 6;
        }
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