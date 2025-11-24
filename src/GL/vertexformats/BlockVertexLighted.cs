namespace BlockGame.GL.vertexformats;

using System.Runtime.InteropServices;
using Molten;

[StructLayout(LayoutKind.Explicit, Size = 28)]
//[StructLayout(LayoutKind.Explicit, Size = 32)]
public struct BlockVertexLighted {
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

    [FieldOffset(24)] public byte light;
    [FieldOffset(25)] public byte unused;

    // both AMD and NV likes 4-byte padded vertex sizes. We don't have misaligned starts in the pos/uv/col/light but since the last member is 2 bytes, the next vertex would start at an unaligned offset. So we add 2 bytes of padding here.
    [FieldOffset(26)] public ushort padding;



    public BlockVertexLighted(float x, float y, float z, float u, float v, byte r, byte g, byte b, byte a, byte light) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
        this.light = light;
    }

    public BlockVertexLighted(float x, float y, float z, float u, float v, Color c, byte light) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        this.c = c;
        this.light = light;

    }
}