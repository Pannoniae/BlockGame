using Silk.NET.OpenGL;

namespace BlockGame;

public class FloatVAO {
    public Shader shader;

    public uint handle;

    public uint vbo;

    public uint count;

    public GL GL;

    public FloatVAO(Shader shader) {
        unsafe {
            GL = Game.instance.GL;
            this.shader = shader;
            handle = GL.GenVertexArray();
            GL.BindVertexArray(handle);
        }
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
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, (void*)0);
            GL.EnableVertexAttribArray(0);
        }
    }

    public void bind() {
        GL.BindVertexArray(handle);
        shader.use();
    }

    public void render() {
        GL.DrawArrays(PrimitiveType.Triangles, 0, count);
    }
}