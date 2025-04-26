using System.Runtime.CompilerServices;
using Silk.NET.OpenGL;
using PrimitiveType = Silk.NET.OpenGL.PrimitiveType;

namespace BlockGame.GL;

/// <summary>
/// SharedBlockVAO but we only use one VAO / vertex format then just rebind the vertex/index buffer
/// It also uses only one buffer now instead of two
/// </summary>
public sealed class SharedBlockVAO : VAO
{
    public uint VAOHandle;
    public uint buffer;
    public uint count;


    public readonly Silk.NET.OpenGL.GL GL;

    public SharedBlockVAO(uint VAOHandle) {
        this.VAOHandle = VAOHandle;
        GL = Game.GL;
    }

    public void upload(BlockVertexPacked[] data, ushort[] indices) {
        unsafe {
            GL.DeleteBuffer(buffer);
            buffer = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffer);
            count = (uint)indices.Length;
            var vertexSize = (uint)(data.Length * sizeof(BlockVertexPacked));
            fixed (BlockVertexPacked* d = data) {
                GL.BufferStorage(BufferStorageTarget.ArrayBuffer, vertexSize, d,
                    BufferStorageMask.None);
            }
        }

        format();
    }

    public void upload(Span<BlockVertexPacked> data, uint _count) {
        unsafe {
            GL.DeleteBuffer(buffer);
            buffer = GL.GenBuffer();
            count = (uint)(_count * 1.5);
            var vertexSize = (uint)(data.Length * sizeof(BlockVertexPacked));
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffer);
            fixed (BlockVertexPacked* d = data) {
                GL.BufferStorage(BufferStorageTarget.ArrayBuffer, vertexSize, d,
                    BufferStorageMask.None);
            }

        }

        format();
    }

    public void upload(Span<BlockVertexPacked> data, Span<ushort> indices) {
        throw new Exception("this doesn't work!");
    }

    public void format() {
        // 18 bytes in total, 3*4 for pos, 2*2 for uv, 2 bytes for data
        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        GL.VertexAttribIFormat(0, 3, VertexAttribIType.UnsignedShort, 0);
        GL.VertexAttribIFormat(1, 2, VertexAttribIType.UnsignedShort, 0 + 3 * sizeof(ushort));
        GL.VertexAttribFormat(2, 4, VertexAttribType.UnsignedByte, true, 0 + 5 * sizeof(ushort));

        GL.VertexAttribBinding(0, 0);
        GL.VertexAttribBinding(1, 0);
        GL.VertexAttribBinding(2, 0);
    }

    public void bindVAO() {
        GL.BindVertexArray(VAOHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void bind() {
        GL.VertexArrayVertexBuffer(VAOHandle, 0, buffer, 0, 7 * sizeof(ushort));
    }

    public uint render() {
        unsafe {
            GL.DrawElements(PrimitiveType.Triangles, count, DrawElementsType.UnsignedShort, (void*)0);
            return count;
        }
    }

    public void Dispose() {
        GL.DeleteBuffer(buffer);
    }
}