using Silk.NET.OpenGL;

namespace BlockGame;


// 3 floats for position, 2 floats for texcoords
public class BlockVAO {
    public uint handle;
    public uint vbo;
    public uint count;

    public BTexture2D blockTexture;

    public GL GL;

    public BlockVAO() {
        GL = Game.instance.GL;
        handle = GL.GenVertexArray();
        GL.BindVertexArray(handle);
        blockTexture = Game.instance.blockTexture;
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
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(0 + 3 * sizeof(float)));
            GL.EnableVertexAttribArray(1);
        }
    }

    public void bind() {
        blockTexture.bind();
        GL.BindVertexArray(handle);
    }

    public void render() {
        GL.DrawArrays(PrimitiveType.Triangles, 0, count / 5);
    }
}