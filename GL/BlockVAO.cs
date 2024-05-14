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

    public Texture2D blockTexture;

    public GL GL;

    public BlockVAO() {
        GL = Game.GL;
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
            // 18 bytes in total, 3*4 for pos, 2*2 for uv, 2 bytes for data
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 9 * sizeof(ushort), (void*)0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.HalfFloat, false, 9 * sizeof(ushort), (void*)(0 + 6 * sizeof(ushort)));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribIPointer(2, 1, VertexAttribIType.UnsignedShort, 9 * sizeof(ushort), (void*)(0 + 8 * sizeof(ushort)));
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

[StructLayout(LayoutKind.Sequential, Size = 18)]
public readonly struct BlockVertex : IVertex {
    public readonly float x;
    public readonly float y;
    public readonly float z;
    public readonly Half u;
    public readonly Half v;

    /// <summary>
    /// from least significant:
    /// first 3 bits are side (see Direction enum)
    /// next 2 bits are AO
    /// </summary>
    public readonly ushort d;

    public BlockVertex(float x, float y, float z, Half u, Half v, ushort d) {
        // we receive a float from 0 to 16.
        // we convert it to a normalised float from 0 to 1 converted to an ushort
        //this.x = (ushort)(x / 16f * ushort.MaxValue);
        //this.y = (ushort)(y / 16f * ushort.MaxValue);
        //this.z = (ushort)(z / 16f * ushort.MaxValue);
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        this.d = d;
    }

    public BlockVertex(ushort x, ushort y, ushort z, Half u, Half v, ushort d) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        this.d = d;
    }

    public void WriteAttribDescriptions(Span<VertexAttribDescription> descriptions) {
        descriptions[0] = new VertexAttribDescription(AttributeType.FloatVec3, false, AttributeBaseType.Float);
        descriptions[1] = new VertexAttribDescription(AttributeType.FloatVec2, false, AttributeBaseType.HalfFloat);
        descriptions[2] = new VertexAttribDescription(AttributeType.UnsignedInt, false, AttributeBaseType.UnsignedShort);
    }

    public int AttribDescriptionCount => 3;
}