using System.Runtime.InteropServices;
using Molten;

namespace BlockGame.GL;

[StructLayout(LayoutKind.Explicit, Size = 14)]
public struct BlockVertexPacked {
    [FieldOffset(0)] public ushort x;
    [FieldOffset(2)] public ushort y;
    [FieldOffset(4)] public ushort z;
    [FieldOffset(6)] public ushort u;
    [FieldOffset(8)] public ushort v;
    [FieldOffset(10)] public Color c;
    [FieldOffset(10)] public byte r;
    [FieldOffset(11)] public byte g;
    [FieldOffset(12)] public byte b;
    [FieldOffset(13)] public byte a;

    public BlockVertexPacked(float x, float y, float z, float u, float v, byte r, byte g, byte b, byte a) {
        this.x = (ushort)((x + 16) * 256);
        this.y = (ushort)((y + 16) * 256);
        this.z = (ushort)((z + 16) * 256);
        this.u = (ushort)(u * 32768);
        this.v = (ushort)(v * 32768);
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public BlockVertexPacked(float x, float y, float z, float u, float v, Color c) {
        this.x = (ushort)((x + 16) * 256);
        this.y = (ushort)((y + 16) * 256);
        this.z = (ushort)((z + 16) * 256);
        this.u = (ushort)(u * 32768);
        this.v = (ushort)(v * 32768);
        r = c.R;
        g = c.G;
        b = c.B;
        a = c.A;
    }

    public BlockVertexPacked(ushort x, ushort y, ushort z, ushort u, ushort v, Color c) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.u = u;
        this.v = v;
        this.c = c;
    }
}