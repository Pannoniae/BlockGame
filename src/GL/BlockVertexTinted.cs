using System.Runtime.InteropServices;
using Molten;

namespace BlockGame.GL;

[StructLayout(LayoutKind.Explicit, Size = 20)]
public readonly struct BlockVertexTinted {
    [FieldOffset(0)]
    public readonly float x;
    [FieldOffset(4)]
    public readonly float y;
    [FieldOffset(8)]
    public readonly float z;
    [FieldOffset(12)]
    public readonly Half u;
    [FieldOffset(14)]
    public readonly Half v;
    [FieldOffset(16)]
    public readonly byte r;
    [FieldOffset(17)]
    public readonly byte g;
    [FieldOffset(18)]
    public readonly byte b;
    [FieldOffset(19)]
    public readonly byte a;
    [FieldOffset(16)]
    public readonly Color c;


    public BlockVertexTinted(float x, float y, float z, float u, float v, byte r, byte g, byte b, byte a) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = (Half)u;
        this.v = (Half)v;
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public BlockVertexTinted(float x, float y, float z, float u, float v, Color c) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = (Half)u;
        this.v = (Half)v;
        this.c = c;
    }

    public BlockVertexTinted(float x, float y, float z, Half u, Half v, Color c) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        this.c = c;
    }

    public BlockVertexTinted(ushort x, ushort y, ushort z, float u, float v, byte r, byte g, byte b, byte a) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = (Half)u;
        this.v = (Half)v;
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public BlockVertexTinted(float x, float y, float z, Half u, Half v, byte r, byte g, byte b, byte a) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }
}