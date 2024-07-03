using Silk.NET.OpenGL;
using TrippyGL;
using PrimitiveType = Silk.NET.OpenGL.PrimitiveType;

namespace BlockGame;


/// <summary>
/// BlockVAO but we use separated vertex attribute format/bindings
/// </summary>
public sealed class SharedBlockVAO : VAO {
    public uint VAOHandle;
    public uint vbo;
    public uint ibo;
    public uint count;

    public GL GL;

    public SharedBlockVAO() {
        GL = Game.GL;
        VAOHandle = GL.GenVertexArray();
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

    public void upload(BlockVertexPacked[] data, ushort[] indices) {
        unsafe {
            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            count = (uint)indices.Length;
            fixed (BlockVertexPacked* d = data) {
                GL.BufferData(BufferTargetARB.ArrayBuffer, (uint)(data.Length * sizeof(BlockVertexPacked)), d,
                    BufferUsageARB.DynamicDraw);
            }

            ibo = GL.GenBuffer();
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
            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            count = (uint)indices.Length;
            fixed (BlockVertexPacked* d = data) {
                GL.BufferData(BufferTargetARB.ArrayBuffer, (uint)(data.Length * sizeof(BlockVertexPacked)), d,
                    BufferUsageARB.DynamicDraw);
            }

            ibo = GL.GenBuffer();
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
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);

            GL.VertexAttribIFormat(0, 3, VertexAttribIType.UnsignedShort, 0);
            GL.VertexAttribIFormat(1, 2, VertexAttribIType.UnsignedShort, 0 + 3 * sizeof(ushort));
            GL.VertexAttribIFormat(2, 1, VertexAttribIType.UnsignedShort, 0 + 5 * sizeof(ushort));

            GL.VertexAttribBinding(0, 0);
            GL.VertexAttribBinding(1, 0);
            GL.VertexAttribBinding(2, 0);

            GL.BindVertexBuffer(0, vbo, 0, 6 * sizeof(ushort));

        }
    }

    public void bind() {
        GL.BindVertexArray(VAOHandle);
    }

    public uint render() {
        unsafe {
            GL.DrawElements(PrimitiveType.Triangles, count, DrawElementsType.UnsignedShort, (void*)0);
            return count;
        }
    }

    public void Dispose() {
        GL.DeleteBuffer(vbo);
        GL.DeleteBuffer(ibo);
        GL.DeleteVertexArray(VAOHandle);
    }
}