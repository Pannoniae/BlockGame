using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using TrippyGL;
using AttributeType = TrippyGL.AttributeType;
using PrimitiveType = Silk.NET.OpenGL.PrimitiveType;

namespace BlockGame;

public class BlockVAO : VAO {
    public uint handle;
    public uint vbo;
    public uint ibo;
    public uint count;

    public GL GL;

    public BlockVAO() {
        GL = Game.GL;
        handle = GL.GenVertexArray();
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

[StructLayout(LayoutKind.Sequential, Size = 18)]
public readonly struct BlockVertex : IVertex {
    public readonly float x;
    public readonly float y;
    public readonly float z;
    public readonly Half u;
    public readonly Half v;

    /// <summary>
    /// from least significant:
    /// second byte (8-16) is lighting
    /// first 3 bits are side (see Direction enum)
    /// next 2 bits are AO
    /// next 2 bits are texU == 1 and texV == 1
    /// </summary>
    public readonly ushort d;

    public BlockVertex(float x, float y, float z, float u, float v, ushort d) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = (Half)u;
        this.v = (Half)v;
        this.d = d;
    }

    public BlockVertex(ushort x, ushort y, ushort z, float u, float v, ushort d) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = (Half)u;
        this.v = (Half)v;
        this.d = d;
    }

    public void WriteAttribDescriptions(Span<VertexAttribDescription> descriptions) {
        descriptions[0] = new VertexAttribDescription(AttributeType.FloatVec3, false, AttributeBaseType.Float);
        descriptions[1] = new VertexAttribDescription(AttributeType.FloatVec2, false, AttributeBaseType.HalfFloat);
        descriptions[2] = new VertexAttribDescription(AttributeType.UnsignedInt, false, AttributeBaseType.UnsignedShort);
    }

    public int AttribDescriptionCount => 3;
}

[StructLayout(LayoutKind.Sequential, Size = 12)]
public struct BlockVertexPacked : IVertex {
    public ushort x;
    public ushort y;
    public ushort z;
    public ushort u;
    public ushort v;

    /// <summary>
    /// from least significant:
    /// second byte (8-16) is lighting
    /// first 3 bits are side (see Direction enum)
    /// next 2 bits are AO
    /// next 2 bits are texU == 1 and texV == 1
    /// </summary>
    public ushort d;

    public BlockVertexPacked(float x, float y, float z, float u, float v, ushort d) {
        this.x = (ushort)((x + 16) * 256);
        this.y = (ushort)((y + 16) * 256);
        this.z = (ushort)((z + 16) * 256);
        this.u = (ushort)(u * 32768);
        this.v = (ushort)(v * 32768);
        this.d = d;
    }

    public BlockVertexPacked(ushort x, ushort y, ushort z, ushort u, ushort v, ushort d) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        this.d = d;
    }

    public void WriteAttribDescriptions(Span<VertexAttribDescription> descriptions) {
        descriptions[0] = new VertexAttribDescription(AttributeType.UnsignedIntVec3, false, AttributeBaseType.UnsignedShort);
        descriptions[1] = new VertexAttribDescription(AttributeType.UnsignedIntVec2, false, AttributeBaseType.UnsignedShort);
        descriptions[2] = new VertexAttribDescription(AttributeType.UnsignedInt, false, AttributeBaseType.UnsignedShort);
    }

    public int AttribDescriptionCount => 3;
}