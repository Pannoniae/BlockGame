using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;

namespace BlockGame;

public class Chunk {
    public int[,,] block;

    public int chunkX;
    public int chunkY;
    public int chunkZ;

    public uint vao;
    public uint vbo;
    public uint ebo;

    public int uModel;
    public int uView;
    public int uProjection;
    public int uColor;

    public Shader shader;
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
        uColor = shader.getUniformLocation("uColor");
    }

    public void meshChunk() {
        unsafe {
            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            List<float> chunkVertices = new List<float>(CHUNKSIZE * CHUNKSIZE * CHUNKSIZE * 12);
            for (int x = 0; x < CHUNKSIZE; x++) {
                for (int y = 0; y < CHUNKSIZE; y++) {
                    for (int z = 0; z < CHUNKSIZE; z++) {
                        if (block[x, y, z] != 0) {
                            var wpos = world.toWorldPos(chunkX, chunkY, chunkZ, x, y, z);
                            int wx = wpos.X;
                            int wy = wpos.Y;
                            int wz = wpos.Z;

                            float xmin = x - 0.5f;
                            float ymin = y - 0.5f;
                            float zmin = z - 0.5f;
                            float xmax = x + 0.5f;
                            float ymax = y + 0.5f;
                            float zmax = z + 0.5f;

                            if (!world.isBlock(wx, wy - 1, wz)) {
                                float[] verticesBottom = [
                                    // bottom
                                    xmin, ymin, zmin,
                                    xmin, ymin, zmax,
                                    xmax, ymin, zmin,

                                    xmax, ymin, zmax,
                                    xmax, ymin, zmin,
                                    xmin, ymin, zmax
                                ];
                                chunkVertices.AddRange(verticesBottom);
                            }

                            if (!world.isBlock(wx, wy + 1, wz)) {
                                float[] verticesTop = [
                                    // top
                                    xmin, ymax, zmin,
                                    xmin, ymax, zmax,
                                    xmax, ymax, zmin,

                                    xmax, ymax, zmax,
                                    xmax, ymax, zmin,
                                    xmin, ymax, zmax,
                                ];
                                chunkVertices.AddRange(verticesTop);
                            }

                            if (!world.isBlock(wx, wy, wz - 1)) {
                                float[] verticesSouth = [
                                    // south
                                    xmax, ymin, zmin,
                                    xmax, ymax, zmin,
                                    xmin, ymin, zmin,

                                    xmin, ymax, zmin,
                                    xmin, ymin, zmin,
                                    xmax, ymax, zmin,
                                ];
                                chunkVertices.AddRange(verticesSouth);
                            }

                            if (!world.isBlock(wx, wy, wz + 1)) {
                                float[] verticesNorth = [
                                    // north
                                    xmax, ymin, zmax,
                                    xmax, ymax, zmax,
                                    xmin, ymin, zmax,

                                    xmin, ymax, zmax,
                                    xmin, ymin, zmax,
                                    xmax, ymax, zmax,
                                ];
                                chunkVertices.AddRange(verticesNorth);
                            }

                            if (!world.isBlock(wx - 1, wy, wz)) {
                                float[] verticesWest = [
                                    // west
                                    xmin, ymin, zmin,
                                    xmin, ymax, zmin,
                                    xmin, ymin, zmax,

                                    xmin, ymax, zmax,
                                    xmin, ymin, zmax,
                                    xmin, ymax, zmin,
                                ];
                                chunkVertices.AddRange(verticesWest);
                            }

                            if (!world.isBlock(wx + 1, wy, wz)) {
                                float[] verticesEast = [
                                    // east
                                    xmax, ymin, zmin,
                                    xmax, ymax, zmin,
                                    xmax, ymin, zmax,

                                    xmax, ymax, zmax,
                                    xmax, ymin, zmax,
                                    xmax, ymax, zmin,
                                ];
                                chunkVertices.AddRange(verticesEast);
                            }
                        }
                    }
                }
            }

            var finalVertices = CollectionsMarshal.AsSpan(chunkVertices);
            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            fixed (float* data = finalVertices) {
                GL.BufferData(BufferTargetARB.ArrayBuffer, (uint)(finalVertices.Length * sizeof(float)), data,
                    BufferUsageARB.DynamicDraw);
            }

            count = (uint)finalVertices.Length;
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
            GL.EnableVertexAttribArray(0);
        }
    }

    public void drawChunk() {
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
        GL.BindVertexArray(vao);
        shader.use();
        shader.setUniform(uModel, Matrix4x4.CreateWorld(new Vector3(chunkX * 16f, chunkY * 16f, chunkZ * 16f), -Vector3.UnitZ, Vector3.UnitY));
        shader.setUniform(uView, Game.instance.camera.getViewMatrix());
        shader.setUniform(uProjection, Game.instance.camera.getProjectionMatrix());
        shader.setUniform(uColor, new Vector4(0.6f, 0.2f, 0.2f, 1));
        GL.DrawArrays(PrimitiveType.Triangles, 0, count);
        shader.setUniform(uColor, new Vector4(1f, 0.2f, 0.2f, 1));
        //GL.DrawArrays(PrimitiveType.Lines, 0, count);
    }

    public void meshBlock() {
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
    }
}


/// <summary>
/// North = +Z
/// South = -Z
/// West = -X
/// East = +X
/// </summary>
public enum Direction {
    NORTH,
    SOUTH,
    WEST,
    EAST,
    UP,
    DOWN
}