using Silk.NET.OpenGL;
using PrimitiveType = Silk.NET.OpenGL.PrimitiveType;

namespace BlockGame.GL;

public class BlockVAO : VAO {
    public uint handle;
    public uint vbo;
    public uint ibo;
    public uint count;

    public Silk.NET.OpenGL.GL GL;

    public BlockVAO() {
        GL = Game.GL;
        handle = GL.GenVertexArray();
        vbo = GL.GenBuffer();
        ibo = GL.GenBuffer();
    }

    public void upload(float[] data) {
        unsafe {
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
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            count = (uint)data.Length;
            fixed (float* d = data) {
                GL.BufferData(BufferTargetARB.ArrayBuffer, (uint)(data.Length * sizeof(float)), d,
                    BufferUsageARB.DynamicDraw);
            }
        }

        format();
    }

    public void upload(BlockVertexPacked[] data, ushort[] indices) {
        unsafe {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            count = (uint)indices.Length;
            fixed (BlockVertexPacked* d = data) {
                GL.BufferData(BufferTargetARB.ArrayBuffer, (uint)(data.Length * sizeof(BlockVertexPacked)), d,
                    BufferUsageARB.DynamicDraw);
            }

            GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, ibo);
            fixed (ushort* d = indices) {
                GL.BufferData(BufferTargetARB.ElementArrayBuffer, (uint)(indices.Length * sizeof(ushort)), d,
                    BufferUsageARB.DynamicDraw);
            }
        }

        format();
    }

    public void upload(Span<BlockVertexPacked> data, Span<ushort> indices) {
        unsafe {
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            count = (uint)indices.Length;
            fixed (BlockVertexPacked* d = data) {
                GL.BufferData(BufferTargetARB.ArrayBuffer, (uint)(data.Length * sizeof(BlockVertexPacked)), d,
                    BufferUsageARB.DynamicDraw);
            }
            
            GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, ibo);
            fixed (ushort* d = indices) {
                GL.BufferData(BufferTargetARB.ElementArrayBuffer, (uint)(indices.Length * sizeof(ushort)), d,
                    BufferUsageARB.DynamicDraw);
            }
        }

        format();
    }

    public void format() {
        unsafe {
            // 18 bytes in total, 3*4 for pos, 2*2 for uv, 2 bytes for data
            GL.VertexAttribIPointer(0, 3, VertexAttribIType.UnsignedShort, 6 * sizeof(ushort), (void*)0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribIPointer(1, 2, VertexAttribIType.UnsignedShort, 6 * sizeof(ushort), (void*)(0 + 3 * sizeof(ushort)));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribIPointer(2, 1, VertexAttribIType.UnsignedShort, 6 * sizeof(ushort), (void*)(0 + 5 * sizeof(ushort)));
            GL.EnableVertexAttribArray(2);
        }
    }

    public void bind() {
        GL.BindVertexArray(handle);
    }

    // rendering
    public uint render() {
        unsafe {
            GL.DrawElements(PrimitiveType.Triangles, count, DrawElementsType.UnsignedShort, (void*)0);
            return count;
        }
    }

    public void Dispose() {
        GL.DeleteBuffer(vbo);
        GL.DeleteBuffer(ibo);
        GL.DeleteVertexArray(handle);
    }
}