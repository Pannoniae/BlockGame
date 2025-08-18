using Silk.NET.OpenGL.Legacy;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.GL;

public class FloatVAO {
    public uint handle;

    public uint vbo;

    public uint count;

    public Silk.NET.OpenGL.Legacy.GL GL;

    public FloatVAO() {
        GL = Game.GL;
        handle = GL.GenVertexArray();
        GL.BindVertexArray(handle);
    }

    public void upload(float[] data) {
        unsafe {
            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            count = (uint)data.Length;
            fixed (float* d = data) {
                GL.BufferData(BufferTargetARB.ArrayBuffer, (uint)(data.Length * sizeof(float)), d,
                    BufferUsageARB.DynamicDraw);
            }
        }

        format();
    }

    public void upload(Span<float> data) {
        unsafe {
            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            count = (uint)data.Length;
            fixed (float* d = data) {
                GL.BufferData(BufferTargetARB.ArrayBuffer, (uint)(data.Length * sizeof(float)), d,
                    BufferUsageARB.DynamicDraw);
            }
        }

        format();
    }

    public void format() {
        unsafe {
            GL.BindVertexBuffer(0, vbo, 0, sizeof(float) * 3);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribFormat(0, 3, VertexAttribType.Float, false, 0);
            GL.VertexAttribBinding(0, 0);
            
        }
    }

    public void bind() {
        GL.BindVertexArray(handle);
    }

    public void render() {
        GL.DrawArrays(PrimitiveType.Triangles, 0, count);
    }
}