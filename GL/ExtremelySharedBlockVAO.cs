using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using TrippyGL;
using Buffer = System.Buffer;
using PrimitiveType = Silk.NET.OpenGL.PrimitiveType;

namespace BlockGame;

/// <summary>
/// SharedBlockVAO but we only use one VAO / vertex format then just rebind the vertex/index buffer
/// It also uses only one buffer now instead of two
/// </summary>
public class ExtremelySharedBlockVAO : VAO {
    public uint VAOHandle;
    public uint buffer;
    public uint count;

    /// in bytes
    public uint indexOffset;

    public Texture2D blockTexture;

    public GL GL;

    public ExtremelySharedBlockVAO(uint VAOHandle) {
        this.VAOHandle = VAOHandle;
        GL = Game.GL;
        blockTexture = Game.instance.blockTexture;
    }

    public void upload(float[] data) {
        unsafe {
            buffer = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffer);
            count = (uint)data.Length;
            fixed (float* d = data) {
                GL.BufferStorage(BufferStorageTarget.ArrayBuffer, (uint)(data.Length * sizeof(float)), d,
                    BufferStorageMask.None);
            }
        }

        format();
    }

    public void upload(Span<float> data) {
        unsafe {
            buffer = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffer);
            count = (uint)data.Length;
            fixed (float* d = data) {
                GL.BufferStorage(BufferStorageTarget.ArrayBuffer, (uint)(data.Length * sizeof(float)), d,
                    BufferStorageMask.None);
            }
        }

        format();
    }

    public void upload(BlockVertex[] data, ushort[] indices) {
        unsafe {
            buffer = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffer);
            count = (uint)indices.Length;
            var vertexSize = (uint)(data.Length * sizeof(BlockVertex));
            var indexSize = (uint)(indices.Length * sizeof(ushort));
            // index buffer comes after the vertex data
            indexOffset = vertexSize;
            fixed (BlockVertex* d = data) {
                GL.BufferStorage(BufferStorageTarget.ArrayBuffer, vertexSize + indexSize, d,
                    BufferStorageMask.None);
            }

            GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, buffer);
            fixed (ushort* d = indices) {
                GL.BufferSubData(BufferTargetARB.ElementArrayBuffer, (nint)indexOffset, indexSize, d);
            }
        }

        format();
    }

    public void upload(Span<BlockVertex> data, Span<ushort> indices) {
        unsafe {
            buffer = GL.GenBuffer();
            count = (uint)indices.Length;
            var vertexSize = (uint)(data.Length * sizeof(BlockVertex));
            var indexSize = (uint)(indices.Length * sizeof(ushort));
            // index buffer comes after the vertex data
            indexOffset = vertexSize;
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, buffer);
            fixed (BlockVertex* b = data) {
                GL.BufferStorage(BufferStorageTarget.ArrayBuffer, vertexSize + indexSize, b,
                    BufferStorageMask.DynamicStorageBit);
            }
            GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, buffer);
            fixed (ushort* u = indices) {
                GL.BufferSubData(BufferTargetARB.ElementArrayBuffer, (nint)indexOffset, indexSize, u);
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

            GL.VertexAttribFormat(0, 3, VertexAttribType.Float, false, 0);
            GL.VertexAttribFormat(1, 2, VertexAttribType.HalfFloat, false, 0 + 6 * sizeof(ushort));
            GL.VertexAttribIFormat(2, 1, VertexAttribIType.UnsignedShort, 0 + 8 * sizeof(ushort));

            GL.VertexAttribBinding(0, 0);
            GL.VertexAttribBinding(1, 0);
            GL.VertexAttribBinding(2, 0);

        }
    }

    public void bindVAO() {
        GL.BindVertexArray(VAOHandle);
    }

    public void bind() {
        GL.BindVertexBuffer(0, buffer, 0, 9 * sizeof(ushort));
        GL.BindBuffer(GLEnum.ElementArrayBuffer, buffer);
    }

    public uint render() {
        unsafe {
            GL.DrawElements(PrimitiveType.Triangles, count, DrawElementsType.UnsignedShort, (void*)indexOffset);
            return count;
        }
    }

}