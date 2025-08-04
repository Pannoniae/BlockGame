using System.Runtime.InteropServices;
using Molten;

namespace BlockGame.GL;

[StructLayout(LayoutKind.Explicit, Size = 20)]
public struct BlockVertexTinted {
    [FieldOffset(0)] public float x;
    [FieldOffset(4)] public float y;
    [FieldOffset(8)] public float z;
    
    [FieldOffset(12)] public Half u;
    [FieldOffset(14)] public Half v;
    
    [FieldOffset(16)] public byte r;
    [FieldOffset(17)] public byte g;
    [FieldOffset(18)] public byte b;
    [FieldOffset(19)] public byte a;
    [FieldOffset(16)] public Color c;


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

    public BlockVertexTinted(float x, float y, float z, Half u, Half v) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        r = 255;
        g = 255;
        b = 255;
        a = 255;
    }

    public BlockVertexTinted(float x, float y, float z, float u, float v) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = (Half)u;
        this.v = (Half)v;
        r = 255;
        g = 255;
        b = 255;
        a = 255;
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

    public BlockVertexTinted(float x, float y, float z, Half u, Half v, float r, float g, float b, float a) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        this.r = (byte)(r * 255);
        this.g = (byte)(g * 255);
        this.b = (byte)(b * 255);
        this.a = (byte)(a * 255);
    }
}