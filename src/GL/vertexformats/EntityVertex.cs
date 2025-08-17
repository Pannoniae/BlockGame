using System.Runtime.InteropServices;
using Molten;

namespace BlockGame.GL.vertexformats;

[StructLayout(LayoutKind.Explicit, Size = 24)]
public struct EntityVertex {
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

    [FieldOffset(20)]
    public uint normal;
    
    /**
     * Pack the normal's components into a GL_REV_2_10_10_10 format (32 bits)
     * First one goes on the first/lowest 10 bits, etc.
     *
     * All are normalised to the range of -1.0f to 1.0f.
     */
    private static uint pack(float x, float y, float z, float w) {
        
        // convert to integer ranges: 10-bit [-512,511], 2-bit [-2,1]
        uint px = (uint)((int)(x * 511.0f) & 0x3FF);
        uint py = (uint)((int)(y * 511.0f) & 0x3FF);
        uint pz = (uint)((int)(z * 511.0f) & 0x3FF);
        uint pw = (uint)((int)(w * 1.0f) & 0x3);
        
        return px | (py << 10) | (pz << 20) | (pw << 30);
    }



    public EntityVertex(float x, float y, float z, float u, float v, byte r, byte g, byte b, byte a, float xn, float yn, float zn) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = (Half)u;
        this.v = (Half)v;
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
        this.normal = pack(xn, yn, zn, 0.0f);
    }

    public EntityVertex(float x, float y, float z, Half u, Half v) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        r = 255;
        g = 255;
        b = 255;
        a = 255;
        this.normal = pack(0.0f, 0.0f, 0.0f, 0.0f);
    }

    public EntityVertex(float x, float y, float z, float u, float v) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = (Half)u;
        this.v = (Half)v;
        r = 255;
        g = 255;
        b = 255;
        a = 255;
        this.normal = pack(0.0f, 0.0f, 0.0f, 0.0f);
    }

    public EntityVertex(float x, float y, float z, float u, float v, Color c, float xn, float yn, float zn) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = (Half)u;
        this.v = (Half)v;
        this.c = c;
        this.normal = pack(xn, yn, zn, 0.0f);
    }

    public EntityVertex(float x, float y, float z, Half u, Half v, Color c) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        this.c = c;
        this.normal = pack(0.0f, 0.0f, 0.0f, 0.0f);
    }

    public EntityVertex(ushort x, ushort y, ushort z, float u, float v, byte r, byte g, byte b, byte a) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = (Half)u;
        this.v = (Half)v;
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
        this.normal = pack(0.0f, 0.0f, 0.0f, 0.0f);
    }

    public EntityVertex(float x, float y, float z, Half u, Half v, byte r, byte g, byte b, byte a) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
        this.normal = pack(0.0f, 0.0f, 0.0f, 0.0f);
    }

    public EntityVertex(float x, float y, float z, Half u, Half v, float r, float g, float b, float a) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        this.r = (byte)(r * 255);
        this.g = (byte)(g * 255);
        this.b = (byte)(b * 255);
        this.a = (byte)(a * 255);
        this.normal = pack(0.0f, 0.0f, 0.0f, 0.0f);
    }
}