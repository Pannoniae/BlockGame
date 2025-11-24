using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlockGame.GL.vertexformats;
using BlockGame.main;
using Silk.NET.OpenGL.Legacy;
using PrimitiveType = Silk.NET.OpenGL.Legacy.PrimitiveType;

namespace BlockGame.GL;

/// <summary>
/// SharedBlockVAO but we only use one VAO / vertex format then just rebind the vertex/index buffer
/// It also uses only one buffer now instead of two
/// </summary>
public sealed class ExtremelySharedBlockVAO : VAO {
    public uint VAOHandle;
    public uint buffer;
    public uint count;

    /// in bytes
    public uint indexOffset;


    public readonly Silk.NET.OpenGL.Legacy.GL GL;

    public ExtremelySharedBlockVAO(uint VAOHandle) {
        this.VAOHandle = VAOHandle;
        GL = Game.GL;
        buffer = GL.CreateBuffer();
    }

    public void upload(BlockVertexPacked[] data, ushort[] indices) {
        unsafe {
            count = (uint)indices.Length;
            var vertexSize = (uint)(data.Length * sizeof(BlockVertexPacked));
            var indexSize = (uint)(indices.Length * sizeof(ushort));
            // index buffer comes after the vertex data
            indexOffset = vertexSize;
            fixed (BlockVertexPacked* d = data) {
                GL.NamedBufferStorage(buffer, vertexSize + indexSize, d,
                    BufferStorageMask.None);
            }

            fixed (ushort* d = indices) {
                GL.NamedBufferSubData(buffer, (nint)indexOffset, indexSize, d);
            }
        }

        format();
    }

    public void upload(Span<BlockVertexPacked> data, Span<ushort> indices) {
        unsafe {
            count = (uint)indices.Length;
            var vertexSize = (uint)(data.Length * sizeof(BlockVertexPacked));
            var indexSize = (uint)(indices.Length * sizeof(ushort));
            // index buffer comes after the vertex data
            indexOffset = vertexSize;
            var c = ArrayPool<byte>.Shared.Rent(data.Length * sizeof(BlockVertexPacked) + indices.Length * sizeof(ushort));
            var cs = c.AsSpan();
            var c2 = c.AsSpan(data.Length * sizeof(BlockVertexPacked));
            MemoryMarshal.AsBytes(data).CopyTo(cs);
            MemoryMarshal.AsBytes(indices).CopyTo(c2);
            fixed (byte* ptr = c) {
                GL.NamedBufferStorage(buffer, vertexSize + indexSize, ptr,
                    BufferStorageMask.None);

            }
            ArrayPool<byte>.Shared.Return(c);

        }

        format();
    }

    public void format() {
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
    }

    public void bindVAO() {
        Game.graphics.vao(VAOHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void bind() {
        GL.VertexArrayVertexBuffer(VAOHandle, 0, buffer, 0, 6 * sizeof(ushort));
        GL.VertexArrayElementBuffer(VAOHandle, buffer);
    }

    public uint render() {
        unsafe {
            GL.DrawElements(PrimitiveType.Triangles, count, DrawElementsType.UnsignedInt, (void*)indexOffset);
            return count;
        }
    }

    public void Dispose() {
        GL.DeleteBuffer(buffer);
    }
}