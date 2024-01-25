using System.Numerics;
using Silk.NET.OpenGL;

namespace BlockGame;

public class Chunk {
    // later an array
    public int block;

    public uint vao;
    public uint vbo;
    public uint ebo;

    public Shader shader;
    public readonly GL GL;

    private uint count;

    public Chunk() {
        GL = Game.instance.GL;
        shader = new Shader(GL, "shader.vert", "shader.frag");
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
                GL.BufferData(BufferTargetARB.ArrayBuffer, (uint)(vertices.Length * sizeof(float)), data, BufferUsageARB.StreamDraw);
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
                GL.BufferData(BufferTargetARB.ElementArrayBuffer, (uint)(indices.Length * sizeof(uint)), data2, BufferUsageARB.StreamDraw);
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
            shader.setUniform("uModel", Matrix4x4.Identity);
            shader.setUniform("uView", Game.instance.camera.getViewMatrix());
            shader.setUniform("uProjection", Game.instance.camera.getProjectionMatrix());
            shader.setUniform("uColor", new Vector4(0.6f, 0.2f, 0.2f, 1));
            GL.DrawElements(PrimitiveType.Triangles, count, DrawElementsType.UnsignedInt, (void*)0);
            shader.setUniform("uColor", new Vector4(1f, 0.2f, 0.2f, 1));
            //GL.DrawElements(PrimitiveType.Lines, count, DrawElementsType.UnsignedInt, (void*)0);
        }
    }
}