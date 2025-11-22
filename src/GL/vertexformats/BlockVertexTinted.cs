using System.Runtime.InteropServices;

namespace BlockGame.GL.vertexformats;

[StructLayout(LayoutKind.Explicit, Size = 24)]
public struct BlockVertexTinted {
    [FieldOffset(0)] public float x;
    [FieldOffset(4)] public float y;
    [FieldOffset(8)] public float z;
    
    [FieldOffset(12)] public float u;
    [FieldOffset(16)] public float v;
    
    [FieldOffset(20)] public uint cu;
    [FieldOffset(20)] public Color c;
    [FieldOffset(20)] public byte r;
    [FieldOffset(21)] public byte g;
    [FieldOffset(22)] public byte b;
    [FieldOffset(23)] public byte a;
    


    public BlockVertexTinted(float x, float y, float z, float u, float v, byte r, byte g, byte b, byte a) {
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

    public BlockVertexTinted(float x, float y, float z, Half u, Half v) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = (float)u;
        this.v = (float)v;
        r = 255;
        g = 255;
        b = 255;
        a = 255;
    }

    public BlockVertexTinted(float x, float y, float z, float u, float v) {
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

    public BlockVertexTinted(float x, float y, float z, float u, float v, Color c) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        this.c = c;
    }

    public BlockVertexTinted(float x, float y, float z, Half u, Half v, Color c) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = (float)u;
        this.v = (float)v;
        this.c = c;
    }

    public BlockVertexTinted(ushort x, ushort y, ushort z, float u, float v, byte r, byte g, byte b, byte a) {
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

    public BlockVertexTinted(float x, float y, float z, Half u, Half v, byte r, byte g, byte b, byte a) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = (float)u;
        this.v = (float)v;
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public BlockVertexTinted(float x, float y, float z, Half u, Half v, float r, float g, float b, float a) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = (float)u;
        this.v = (float)v;
        this.r = (byte)(r * 255);
        this.g = (byte)(g * 255);
        this.b = (byte)(b * 255);
        this.a = (byte)(a * 255);
    }
}