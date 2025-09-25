using System.Runtime.InteropServices;
using Molten;

namespace BlockGame.GL.vertexformats;

/**
 * Never think you can replace this with a half float, you'll be burned with texture bugs forever.
 * TODO add fancy packed vertex format, what the cool kids call "vertex pulling" -
 * have the vertices defined as position, extent, UV, and normal for every *face*, and colours can still be per-vertex
 * This can either be yeeted in an SSBO and read in the shader, or we can use mesh shaders? idk
 * I know mesh shaders are fancy compute-driven stuff which can generate vertices from non-1:1 vertex attribute fetching,
 * but I have skill issues so we'll see
 */
[StructLayout(LayoutKind.Explicit, Size = 28)]
public struct EntityVertex {
    [FieldOffset(0)] public float x;
    [FieldOffset(4)] public float y;
    [FieldOffset(8)] public float z;
    
    [FieldOffset(12)] public float u;
    [FieldOffset(16)] public float v;
    
    [FieldOffset(20)] public byte r;
    [FieldOffset(21)] public byte g;
    [FieldOffset(22)] public byte b;
    [FieldOffset(23)] public byte a;
    [FieldOffset(20)] public Color c;

    [FieldOffset(24)]
    public uint normal;
    
    /**
     * Pack the normal's components into a GL_REV_2_10_10_10 format (32 bits)
     * First one goes on the first/lowest 10 bits, etc.
     *
     * All are normalised to the range of -1.0f to 1.0f.
     */
    private static uint pack(float x, float y, float z, float w) {

        // convert to integer ranges: 10-bit [-512,511], 2-bit [-2,1]
        int px = (int)(x * 511.0f);
        int py = (int)(y * 511.0f);
        int pz = (int)(z * 511.0f);
        int pw = (int)(w * 1.0f);

        // pack into uint with proper two's complement handling
        return ((uint)px & 0x3FF) |
               (((uint)py & 0x3FF) << 10) |
               (((uint)pz & 0x3FF) << 20) |
               (((uint)pw & 0x3) << 30);
    }



    public EntityVertex(float x, float y, float z, float u, float v, byte r, byte g, byte b, byte a, float xn, float yn, float zn) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
        this.normal = pack(xn, yn, zn, 0.0f);
    }

    public EntityVertex(float x, float y, float z, float u, float v) {
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

    public EntityVertex(float x, float y, float z, float u, float v, Color c, float xn, float yn, float zn) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        this.c = c;
        this.normal = pack(xn, yn, zn, 0.0f);
    }

    public EntityVertex(float x, float y, float z, float u, float v, Color c) {
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
        this.u = u;
        this.v = v;
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
        this.normal = pack(0.0f, 0.0f, 0.0f, 0.0f);
    }

    public EntityVertex(float x, float y, float z, float u, float v, byte r, byte g, byte b, byte a) {
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

    public EntityVertex(float x, float y, float z, float u, float v, float r, float g, float b, float a) {
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

    public EntityVertex scale(float scale) {
        x *= scale;
        y *= scale;
        z *= scale;
        return this;
    }
}