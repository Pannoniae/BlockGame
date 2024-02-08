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
    public int uView;
    public int uProjection;
    public int uColor;
    public int blockTexture;

    public readonly GL GL;

    private uint count;
    private World world;
    public const int CHUNKSIZE = 16;

    public Chunk(World world, int xpos, int ypos, int zpos) {
        chunkX = xpos;
        chunkY = ypos;
        chunkZ = zpos;
        this.world = world;
        GL = Game.instance.GL;
        shader = new Shader(GL, "shader.vert", "shader.frag");

        block = new int[CHUNKSIZE, CHUNKSIZE, CHUNKSIZE];
        for (int x = 0; x < CHUNKSIZE; x++) {
            for (int y = 0; y < CHUNKSIZE; y++) {
                for (int z = 0; z < CHUNKSIZE; z++) {
                    block[x, y, z] = 1;
                }
            }
        }

        uModel = shader.getUniformLocation("uModel");
        uView = shader.getUniformLocation("uView");
        uProjection = shader.getUniformLocation("uProjection");
        //uColor = shader.getUniformLocation("uColor");
        blockTexture = shader.getUniformLocation("blockTexture");
    }

    public void meshChunk() {
        vao = new BlockVAO();

        List<float> chunkVertices = new List<float>(CHUNKSIZE * CHUNKSIZE * CHUNKSIZE * 12);
        for (int x = 0; x < CHUNKSIZE; x++) {
            for (int y = 0; y < CHUNKSIZE; y++) {
                for (int z = 0; z < CHUNKSIZE; z++) {
                    if (block[x, y, z] != 0) {
                        var wpos = world.toWorldPos(chunkX, chunkY, chunkZ, x, y, z);
                        int wx = wpos.X;
                        int wy = wpos.Y;
                        int wz = wpos.Z;

                        Block b = Blocks.get(world.getBlock(wx, wy, wz));

                        var west = Block.texCoords(b.uvs[0]);
                        var westU = west.X;
                        var westV = west.Y;
                        var east = Block.texCoords(b.uvs[1]);
                        var eastU = east.X;
                        var eastV = east.Y;
                        var south = Block.texCoords(b.uvs[2]);
                        var southU = south.X;
                        var southV = south.Y;
                        var north = Block.texCoords(b.uvs[3]);
                        var northU = north.X;
                        var northV = north.Y;
                        var bottom = Block.texCoords(b.uvs[4]);
                        var bottomU = bottom.X;
                        var bottomV = bottom.Y;
                        var top = Block.texCoords(b.uvs[5]);
                        var topU = top.X;
                        var topV = top.Y;


                        float xmin = x - 0.5f;
                        float ymin = y - 0.5f;
                        float zmin = z - 0.5f;
                        float xmax = x + 0.5f;
                        float ymax = y + 0.5f;
                        float zmax = z + 0.5f;

                        if (!world.isBlock(wx - 1, wy, wz)) {
                            float[] verticesWest = [
                                // west
                                xmin, ymin, zmin, westU, westV,
                                xmin, ymax, zmin, westU, westV,
                                xmin, ymin, zmax, westU, westV,

                                xmin, ymax, zmax, westU, westV,
                                xmin, ymin, zmax, westU, westV,
                                xmin, ymax, zmin, westU, westV,
                            ];
                            chunkVertices.AddRange(verticesWest);
                        }

                        if (!world.isBlock(wx + 1, wy, wz)) {
                            float[] verticesEast = [
                                // east
                                xmax, ymin, zmin, eastU, eastV,
                                xmax, ymax, zmin, eastU, eastV,
                                xmax, ymin, zmax, eastU, eastV,

                                xmax, ymax, zmax, eastU, eastV,
                                xmax, ymin, zmax, eastU, eastV,
                                xmax, ymax, zmin, eastU, eastV,
                            ];
                            chunkVertices.AddRange(verticesEast);
                        }

                        if (!world.isBlock(wx, wy, wz - 1)) {
                            float[] verticesSouth = [
                                // south
                                xmax, ymin, zmin, southU, southV,
                                xmax, ymax, zmin, eastU, eastV,
                                xmin, ymin, zmin, eastU, eastV,

                                xmin, ymax, zmin, eastU, eastV,
                                xmin, ymin, zmin, eastU, eastV,
                                xmax, ymax, zmin, eastU, eastV,
                            ];
                            chunkVertices.AddRange(verticesSouth);
                        }

                        if (!world.isBlock(wx, wy, wz + 1)) {
                            float[] verticesNorth = [
                                // north
                                xmax, ymin, zmax, northU, northV,
                                xmax, ymax, zmax, northU, northV,
                                xmin, ymin, zmax, northU, northV,

                                xmin, ymax, zmax, northU, northV,
                                xmin, ymin, zmax, northU, northV,
                                xmax, ymax, zmax, northU, northV,
                            ];
                            chunkVertices.AddRange(verticesNorth);
                        }

                        if (!world.isBlock(wx, wy - 1, wz)) {
                            float[] verticesBottom = [
                                // bottom
                                xmin, ymin, zmin, bottomU, bottomV,
                                xmin, ymin, zmax, bottomU, bottomV,
                                xmax, ymin, zmin, bottomU, bottomV,

                                xmax, ymin, zmax, bottomU, bottomV,
                                xmax, ymin, zmin, bottomU, bottomV,
                                xmin, ymin, zmax, bottomU, bottomV,
                            ];
                            chunkVertices.AddRange(verticesBottom);
                        }

                        if (!world.isBlock(wx, wy + 1, wz)) {
                            float[] verticesTop = [
                                // top
                                xmin, ymax, zmin, topU, topV,
                                xmin, ymax, zmax, topU, topV,
                                xmax, ymax, zmin, topU, topV,

                                xmax, ymax, zmax, topU, topV,
                                xmax, ymax, zmin, topU, topV,
                                xmin, ymax, zmax, topU, topV,
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

    public void drawChunk() {
        vao.bind();
        shader.use();
        //GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
        shader.setUniform(uModel, Matrix4x4.CreateTranslation(new Vector3(chunkX * 16f, chunkY * 16f, chunkZ * 16f)));
        shader.setUniform(uView, Game.instance.camera.getViewMatrix());
        shader.setUniform(uProjection, Game.instance.camera.getProjectionMatrix());
        //shader.setUniform(uColor, new Vector4(0.6f, 0.2f, 0.2f, 1));
        shader.setUniform(blockTexture, 0);
        //shader.setUniform(uColor, new Vector4(1f, 0.2f, 0.2f, 1));
        vao.render();
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
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

/// <summary>
/// North = +Z
/// South = -Z
/// West = -X
/// East = +X
/// </summary>
public enum Direction {
    WEST,
    EAST,
    SOUTH,
    NORTH,
    DOWN,
    UP,
}