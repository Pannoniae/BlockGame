using System.Numerics;
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
    public int uMVP;
    public int uCameraPos;
    public int drawDistance;
    public int fogColour;


    public Shader outline;
    private uint outlineVao;
    private uint outlineCount;
    private int outline_uModel;
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
        var tex = Game.instance.blockTexture;
        Game.GD.SetActiveTexture(0);
        GL.BindTexture(TextureTarget.Texture2D, tex.Handle);

        if (fastChunkSwitch) {
            GL.BindVertexArray(chunkVAO);
        }

        var viewProj = world.player.camera.getViewMatrix(interp) * world.player.camera.getProjectionMatrix();
        // gather chunks to render
        chunksToRender.Clear();
        foreach (var chunk in world.chunks.Values) {
            if (chunk.status >= ChunkStatus.MESHED) {
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
        shader.setUniform(blockTexture, 0);
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
        GL.DepthFunc(DepthFunction.Lequal);
        foreach (var chunk in chunksToRender) {
            chunk.drawTransparent(world.player.camera);
        }
        GL.DepthMask(true);
        GL.DepthFunc(DepthFunction.Lequal);
        GL.Enable(EnableCap.CullFace);

    }

    public void meshBlockOutline() {
        unsafe {
            var GL = Game.GL;
            const float OFFSET = 0.005f;
            var minX = 0f - OFFSET;
            var minY = 0f - OFFSET;
            var minZ = 0f - OFFSET;
            var maxX = 1f + OFFSET;
            var maxY = 1f + OFFSET;
            var maxZ = 1f + OFFSET;

            outlineVao = GL.GenVertexArray();
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
            var vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            fixed (float* data = vertices) {
                GL.BufferData(BufferTargetARB.ArrayBuffer, (uint)(vertices.Length * sizeof(float)), data,
                    BufferUsageARB.StreamDraw);
            }

            outlineCount = (uint)vertices.Length / 3;
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
            GL.EnableVertexAttribArray(0);

            outline_uModel = outline.getUniformLocation("uModel");
            outline_uView = outline.getUniformLocation("uView");
            outline_uProjection = outline.getUniformLocation("uProjection");
        }
    }

    public void drawBlockOutline(double interp) {
        var GL = Game.GL;
        var block = Game.instance.targetedPos!.Value;
        GL.BindVertexArray(outlineVao);
        outline.use();
        outline.setUniform(outline_uModel, Matrix4x4.CreateTranslation(block.X, block.Y, block.Z));
        outline.setUniform(outline_uView, world.player.camera.getViewMatrix(interp));
        outline.setUniform(outline_uProjection, world.player.camera.getProjectionMatrix());
        GL.DrawArrays(PrimitiveType.Lines, 0, outlineCount);
    }

    public static void meshBlock(Block block, ref BlockVertex[]? vertices, ref ushort[]? indices) {
        vertices ??= new BlockVertex[24];
        indices ??= new ushort[36];
        ushort i = 0;
        int wx = 0;
        int wy = 0;
        int wz = 0;

        int c = 0;
        int ci = 0;


        Block b = block;

        // calculate texcoords
        var westCoords = b.uvs[0];
        var west = Block.texCoords(westCoords);
        var westMax = Block.texCoords(westCoords.u + 1, westCoords.v + 1);
        var westU = west.X;
        var westV = west.Y;
        var westMaxU = westMax.X;
        var westMaxV = westMax.Y;

        var eastCoords = b.uvs[1];
        var east = Block.texCoords(eastCoords);
        var eastMax = Block.texCoords(eastCoords.u + 1, eastCoords.v + 1);
        var eastU = east.X;
        var eastV = east.Y;
        var eastMaxU = eastMax.X;
        var eastMaxV = eastMax.Y;

        var southCoords = b.uvs[2];
        var south = Block.texCoords(southCoords);
        var southMax = Block.texCoords(southCoords.u + 1, southCoords.v + 1);
        var southU = south.X;
        var southV = south.Y;
        var southMaxU = southMax.X;
        var southMaxV = southMax.Y;

        var northCoords = b.uvs[3];
        var north = Block.texCoords(northCoords);
        var northMax = Block.texCoords(northCoords.u + 1, northCoords.v + 1);
        var northU = north.X;
        var northV = north.Y;
        var northMaxU = northMax.X;
        var northMaxV = northMax.Y;

        var bottomCoords = b.uvs[4];
        var bottom = Block.texCoords(bottomCoords);
        var bottomMax = Block.texCoords(bottomCoords.u + 1, bottomCoords.v + 1);
        var bottomU = bottom.X;
        var bottomV = bottom.Y;
        var bottomMaxU = bottomMax.X;
        var bottomMaxV = bottomMax.Y;

        var topCoords = b.uvs[5];
        var top = Block.texCoords(topCoords);
        var topMax = Block.texCoords(topCoords.u + 1, topCoords.v + 1);
        var topU = top.X;
        var topV = top.Y;
        var topMaxU = topMax.X;
        var topMaxV = topMax.Y;

        float xmin = wx;
        float ymin = wy;
        float zmin = wz;
        float xmax = wx + 1f;
        float ymax = wy + 1f;
        float zmax = wz + 1f;

        var data = Block.packData((byte)RawDirection.WEST, 0);
        BlockVertex[] verticesWest = [
            // west
            new BlockVertex(xmin, ymax, zmax, westU, westV, data),
            new BlockVertex(xmin, ymin, zmax, westU, westMaxV, data),
            new BlockVertex(xmin, ymin, zmin, westMaxU, westMaxV, data),
            new BlockVertex(xmin, ymax, zmin, westMaxU, westV, data),
        ];
        vertices.AddRange(c, verticesWest);
        c += 4;
        ushort[] id = [
            i,
            (ushort)(i + 1),
            (ushort)(i + 2),
            (ushort)(i + 0),
            (ushort)(i + 2),
            (ushort)(i + 3)
        ];
        indices.AddRange(ci, id);
        i += 4;
        ci += 6;
        data = Block.packData((byte)RawDirection.EAST, 0);

        BlockVertex[] verticesEast = [
            // east
            new BlockVertex(xmax, ymax, zmin, eastU, eastV, data),
            new BlockVertex(xmax, ymin, zmin, eastU, eastMaxV, data),
            new BlockVertex(xmax, ymin, zmax, eastMaxU, eastMaxV, data),
            new BlockVertex(xmax, ymax, zmax, eastMaxU, eastV, data),
        ];
        vertices.AddRange(c, verticesEast);
        c += 4;
        id = [
            i,
            (ushort)(i + 1),
            (ushort)(i + 2),
            (ushort)(i + 0),
            (ushort)(i + 2),
            (ushort)(i + 3)
        ];

        indices.AddRange(ci, id);
        i += 4;
        ci += 6;
        data = Block.packData((byte)RawDirection.SOUTH, 0);

        BlockVertex[] verticesSouth = [
            // south
            new BlockVertex(xmin, ymax, zmin, southU, southV, data),
            new BlockVertex(xmin, ymin, zmin, southU, southMaxV, data),
            new BlockVertex(xmax, ymin, zmin, southMaxU, southMaxV, data),
            new BlockVertex(xmax, ymax, zmin, southMaxU, southV, data),
        ];
        vertices.AddRange(i, verticesSouth);
        c += 4;
        id = [
            i,
            (ushort)(i + 1),
            (ushort)(i + 2),
            (ushort)(i + 0),
            (ushort)(i + 2),
            (ushort)(i + 3)
        ];

        indices.AddRange(ci, id);
        i += 4;
        ci += 6;
        data = Block.packData((byte)RawDirection.NORTH, 0);
        BlockVertex[] verticesNorth = [
            // north
            new BlockVertex(xmax, ymax, zmax, northU, northV, data),
            new BlockVertex(xmax, ymin, zmax, northU, northMaxV, data),
            new BlockVertex(xmin, ymin, zmax, northMaxU, northMaxV, data),
            new BlockVertex(xmin, ymax, zmax, northMaxU, northV, data),
        ];
        vertices.AddRange(i, verticesNorth);
        c += 4;
        id = [
            i,
            (ushort)(i + 1),
            (ushort)(i + 2),
            (ushort)(i + 0),
            (ushort)(i + 2),
            (ushort)(i + 3)
        ];
        indices.AddRange(ci, id);
        i += 4;
        ci += 6;
        data = Block.packData((byte)RawDirection.DOWN, 0);
        BlockVertex[] verticesBottom = [
            // bottom
            new BlockVertex(xmin, ymin, zmin, bottomU, bottomV, data),
            new BlockVertex(xmin, ymin, zmax, bottomU, bottomMaxV, data),
            new BlockVertex(xmax, ymin, zmax, bottomMaxU, bottomMaxV, data),
            new BlockVertex(xmax, ymin, zmin, bottomMaxU, bottomV, data),
        ];
        vertices.AddRange(i, verticesBottom);
        c += 4;
        id = [
            i,
            (ushort)(i + 1),
            (ushort)(i + 2),
            (ushort)(i + 0),
            (ushort)(i + 2),
            (ushort)(i + 3)
        ];
        indices.AddRange(ci, id);
        i += 4;
        ci += 6;
        data = Block.packData((byte)RawDirection.UP, 0);

        BlockVertex[] verticesTop = [
            // top
            new BlockVertex(xmin, ymax, zmax, topU, topV, data),
            new BlockVertex(xmin, ymax, zmin, topU, topMaxV, data),
            new BlockVertex(xmax, ymax, zmin, topMaxU, topMaxV, data),
            new BlockVertex(xmax, ymax, zmax, topMaxU, topV, data),
        ];

        vertices.AddRange(i, verticesTop);
        c += 4;
        id = [
            i,
            (ushort)(i + 1),
            (ushort)(i + 2),
            (ushort)(i + 0),
            (ushort)(i + 2),
            (ushort)(i + 3)
        ];

        indices.AddRange(ci, id);
        i += 4;
        ci += 6;
    }
}

public static class ArrayExtensions {
    public static void AddRange<T>(this T[] arr, int index, T[] elements) {
        for (int i = index; i < index + elements.Length; i++) {
            arr[i] = elements[i - index];
        }
    }
}