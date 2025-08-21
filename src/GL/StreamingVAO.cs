using Silk.NET.OpenGL.Legacy;
using Silk.NET.OpenGL.Legacy;
using PrimitiveType = Silk.NET.OpenGL.Legacy.PrimitiveType;

namespace BlockGame.GL;

/// <summary>
/// A VAO you can stream things into.
/// </summary>
public class StreamingVAO<T> where T : unmanaged {
    public uint VAOHandle;
    public uint vbo;
    public uint count;

    public Silk.NET.OpenGL.Legacy.GL GL;

    public StreamingVAO() {
        GL = Game.GL;
        VAOHandle = GL.GenVertexArray();
    }

    public void setSize(int size) {
        unsafe {
            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            GL.BufferStorage(BufferStorageTarget.ArrayBuffer, (uint)(size * sizeof(T)), (void*)0,
                BufferStorageMask.DynamicStorageBit);
        }
        format();
    }

    public void upload(Span<T> data) {
        unsafe {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            Game.GL.InvalidateBufferData(vbo);
            count = (uint)data.Length;
            fixed (T* d = data) {
                GL.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (uint)(data.Length * sizeof(T)), d);
            }
        }

        //format();
    }

    public void format() {
        // 18 bytes in total, 3*4 for pos, 2*2 for uv, 2 bytes for data
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        GL.VertexAttribFormat(0, 3, VertexAttribType.Float, false, 0);
        GL.VertexAttribFormat(1, 2, VertexAttribType.HalfFloat, false, 0 + 6 * sizeof(ushort));
        GL.VertexAttribFormat(2, 4, VertexAttribType.UnsignedByte, true, 0 + 8 * sizeof(ushort));

        GL.VertexAttribBinding(0, 0);
        GL.VertexAttribBinding(1, 0);
        GL.VertexAttribBinding(2, 0);

        GL.BindVertexBuffer(0, vbo, 0, 10 * sizeof(ushort));
    }

    public void bind() {
        GL.BindVertexArray(VAOHandle);
    }

    public uint render() {
        unsafe {
            GL.DrawElements(PrimitiveType.Triangles, (uint)(count * 1.5), DrawElementsType.UnsignedShort, (void*)0);
            return count;
        }
    }
}