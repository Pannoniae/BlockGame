using System.Runtime.InteropServices;
using Silk.NET.OpenGL;

namespace BlockGame;


public class BlockVAO {
    public uint handle;
    public uint vbo;
    public uint ibo;
    public uint count;

    public BTexture2D blockTexture;

    public GL GL;

    public BlockVAO() {
        GL = Game.instance.GL;
        handle = GL.GenVertexArray();
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

    public void upload(Span<BlockVertex> data, Span<ushort> indices) {
        unsafe {
            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            count = (uint)indices.Length;
            fixed (BlockVertex* d = data) {
                GL.BufferData(BufferTargetARB.ArrayBuffer, (uint)(data.Length * sizeof(BlockVertex)), d,
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
            // 24 bytes in total, 3*4 for pos, 2*4 for uv, 4 bytes for data
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.UnsignedShort, true, 6 * sizeof(ushort), (void*)0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.UnsignedShort, true, 6 * sizeof(ushort), (void*)(0 + 3 * sizeof(ushort)));
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
}

[StructLayout(LayoutKind.Sequential)]
public struct BlockVertex {
    public ushort x;
    public ushort y;
    public ushort z;
    public ushort u;
    public ushort v;

    /// <summary>
    /// from least significant:
    /// first 3 bits are side (see Direction enum)
    /// </summary>
    public ushort d;

    public BlockVertex(float x, float y, float z, ushort u, ushort v, ushort d) {
        // we receive a float from 0 to 16.
        // we convert it to a normalised float from 0 to 1 converted to an ushort
        this.x = (ushort)(x / 16f * ushort.MaxValue);
        this.y = (ushort)(y / 16f * ushort.MaxValue);
        this.z = (ushort)(z / 16f * ushort.MaxValue);
        this.u = u;
        this.v = v;
        this.d = d;
    }

    public BlockVertex(ushort x, ushort y, ushort z, ushort u, ushort v, ushort d) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        this.d = d;
    }
}