using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;

namespace BlockGame;

public class Chunk {
    public int[,,] block;

    public int chunkX;
    public int chunkY;
    public int chunkZ;

    public BlockVAO vao;

    public Shader shader;

    public int uModel;


    public readonly GL GL;

    private uint count;
    private World world;
    public const int CHUNKSIZE = 16;

    public Chunk(World world, Shader shader, int xpos, int ypos, int zpos) {
        chunkX = xpos;
        chunkY = ypos;
        chunkZ = zpos;
        this.world = world;
        this.shader = shader;
        GL = Game.instance.GL;

        block = new int[CHUNKSIZE, CHUNKSIZE, CHUNKSIZE];

        uModel = shader.getUniformLocation("uModel");
    }

    public void meshChunk() {
        vao = new BlockVAO();

        List<BlockVertex> chunkVertices = new List<BlockVertex>(CHUNKSIZE * CHUNKSIZE * CHUNKSIZE * 6);
        for (int x = 0; x < CHUNKSIZE; x++) {
            for (int y = 0; y < CHUNKSIZE; y++) {
                for (int z = 0; z < CHUNKSIZE; z++) {
                    if (block[x, y, z] != 0) {
                        var wpos = world.toWorldPos(chunkX, chunkY, chunkZ, x, y, z);
                        int wx = wpos.X;
                        int wy = wpos.Y;
                        int wz = wpos.Z;


                        Block b = Blocks.get(world.getBlock(wx, wy, wz));

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


                        float xmin = x;
                        float ymin = y;
                        float zmin = z;
                        float xmax = x + 1f;
                        float ymax = y + 1f;
                        float zmax = z + 1f;

                        if (!world.isBlock(wx - 1, wy, wz)) {
                            var data = packData((byte)RawDirection.WEST);
                            BlockVertex[] verticesWest = [
                                // west
                                new BlockVertex(xmin, ymax, zmax, westU, westV, data),
                                new BlockVertex(xmin, ymin, zmax, westU, westMaxV, data),
                                new BlockVertex(xmin, ymin, zmin, westMaxU, westMaxV, data),

                                new BlockVertex(xmin, ymax, zmax, westU, westV, data),
                                new BlockVertex(xmin, ymin, zmin, westMaxU, westMaxV, data),
                                new BlockVertex(xmin, ymax, zmin, westMaxU, westV, data),
                            ];
                            chunkVertices.AddRange(verticesWest);
                        }

                        if (!world.isBlock(wx + 1, wy, wz)) {
                            var data = packData((byte)RawDirection.EAST);
                            BlockVertex[] verticesEast = [
                                // east
                                new BlockVertex(xmax, ymax, zmin, eastU, eastV, data),
                                new BlockVertex(xmax, ymin, zmin, eastU, eastMaxV, data),
                                new BlockVertex(xmax, ymin, zmax, eastMaxU, eastMaxV, data),
                                new BlockVertex(xmax, ymax, zmin, eastU, eastV, data),
                                new BlockVertex(xmax, ymin, zmax, eastMaxU, eastMaxV, data),
                                new BlockVertex(xmax, ymax, zmax, eastMaxU, eastV, data),
                            ];
                            chunkVertices.AddRange(verticesEast);
                        }

                        if (!world.isBlock(wx, wy, wz - 1)) {
                            var data = packData((byte)RawDirection.SOUTH);
                            BlockVertex[] verticesSouth = [
                                // south
                                new BlockVertex(xmin, ymax, zmin, southU, southV, data),
                                new BlockVertex(xmin, ymin, zmin, southU, southMaxV, data),
                                new BlockVertex(xmax, ymin, zmin, southMaxU, southMaxV, data),

                                new BlockVertex(xmin, ymax, zmin, southU, southV, data),
                                new BlockVertex(xmax, ymin, zmin, southMaxU, southMaxV, data),
                                new BlockVertex(xmax, ymax, zmin, southMaxU, southV, data),
                            ];
                            chunkVertices.AddRange(verticesSouth);
                        }

                        if (!world.isBlock(wx, wy, wz + 1)) {
                            var data = packData((byte)RawDirection.NORTH);
                            BlockVertex[] verticesNorth = [
                                // north
                                new BlockVertex(xmax, ymax, zmax, northU, northV, data),
                                new BlockVertex(xmax, ymin, zmax, northU, northMaxV, data),
                                new BlockVertex(xmin, ymin, zmax, northMaxU, northMaxV, data),

                                new BlockVertex(xmax, ymax, zmax, northU, northV, data),
                                new BlockVertex(xmin, ymin, zmax, northMaxU, northMaxV, data),
                                new BlockVertex(xmin, ymax, zmax, northMaxU, northV, data),
                            ];
                            chunkVertices.AddRange(verticesNorth);
                        }

                        if (!world.isBlock(wx, wy - 1, wz)) {
                            var data = packData((byte)RawDirection.DOWN);
                            BlockVertex[] verticesBottom = [
                                // bottom
                                new BlockVertex(xmin, ymin, zmin, bottomU, bottomV, data),
                                new BlockVertex(xmin, ymin, zmax, bottomU, bottomMaxV, data),
                                new BlockVertex(xmax, ymin, zmax, bottomMaxU, bottomMaxV, data),

                                new BlockVertex(xmin, ymin, zmin, bottomU, bottomV, data),
                                new BlockVertex(xmax, ymin, zmax, bottomMaxU, bottomMaxV, data),
                                new BlockVertex(xmax, ymin, zmin, bottomMaxU, bottomV, data),
                            ];
                            chunkVertices.AddRange(verticesBottom);
                        }

                        if (!world.isBlock(wx, wy + 1, wz)) {
                            var data = packData((byte)RawDirection.UP);
                            BlockVertex[] verticesTop = [
                                // top
                                new BlockVertex(xmin, ymax, zmax, topU, topV, data),
                                new BlockVertex(xmin, ymax, zmin, topU, topMaxV, data),
                                new BlockVertex(xmax, ymax, zmin, topMaxU, topMaxV, data),

                                new BlockVertex(xmin, ymax, zmax, topU, topV, data),
                                new BlockVertex(xmax, ymax, zmin, topMaxU, topMaxV, data),
                                new BlockVertex(xmax, ymax, zmax, topMaxU, topV, data),
                            ];
                            chunkVertices.AddRange(verticesTop);
                        }
                    }
                }
            }
        }

        var finalVertices = CollectionsMarshal.AsSpan(chunkVertices);
        vao.upload(finalVertices);
    }

    // this will pack the data into the uint
    public uint packData(byte direction) {
        return direction;
    }

    public void drawChunk() {
        vao.bind();

        //GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
        shader.setUniform(uModel, Matrix4x4.CreateTranslation(new Vector3(chunkX * 16f, chunkY * 16f, chunkZ * 16f)));

        vao.render();
        //GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
    }

    /*public void meshBlock() {
        unsafe {
            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
            float[] vertices = [
                0, 0, 0,
                0, 0, 1,
                1, 0, 0,
                1, 0, 1,

                0, 0, 0,
                0, 0, 1,
                0, 1, 0,
                0, 1, 1,

                0, 0, 0,
                1, 0, 0,
                0, 1, 0,
                1, 1, 0,

                0, 1, 0,
                0, 1, 1,
                1, 1, 0,
                1, 1, 1,

                1, 0, 1,
                1, 0, 0,
                1, 1, 1,
                1, 1, 0,

                1, 0, 1,
                0, 0, 1,
                1, 1, 1,
                0, 1, 1,
            ];

            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            fixed (float* data = vertices) {
                GL.BufferData(BufferTargetARB.ArrayBuffer, (uint)(vertices.Length * sizeof(float)), data,
                    BufferUsageARB.StreamDraw);
            }

            uint[] indices = [
                0, 1, 3,
                0, 2, 3,

                4, 5, 7,
                4, 6, 7,

                8, 9, 11,
                8, 10, 11,

                12, 13, 15,
                12, 14, 15,

                16, 17, 19,
                16, 18, 19,

                20, 21, 23,
                20, 22, 23,
            ];
            ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
            fixed (uint* data2 = indices) {
                GL.BufferData(BufferTargetARB.ElementArrayBuffer, (uint)(indices.Length * sizeof(uint)), data2,
                    BufferUsageARB.StreamDraw);
            }

            count = (uint)indices.Length;
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
            GL.EnableVertexAttribArray(0);
        }
    }

    public void drawBlock() {
        unsafe {
            //GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
            GL.BindVertexArray(vao);
            shader.use();
            shader.setUniform(uModel, Matrix4x4.Identity);
            shader.setUniform(uView, Game.instance.camera.getViewMatrix());
            shader.setUniform(uProjection, Game.instance.camera.getProjectionMatrix());
            shader.setUniform(uColor, new Vector4(0.6f, 0.2f, 0.2f, 1));
            GL.DrawElements(PrimitiveType.Triangles, count, DrawElementsType.UnsignedInt, (void*)0);
            shader.setUniform(uColor, new Vector4(1f, 0.2f, 0.2f, 1));
            //GL.DrawElements(PrimitiveType.Lines, count, DrawElementsType.UnsignedInt, (void*)0);
        }
    }*/
}