using System.Numerics;
using System.Runtime.InteropServices;
using BlockGame.util;
using Molten;

namespace BlockGame.GL;

[StructLayout(LayoutKind.Explicit, Size = 16)]
public struct VertexTinted {
    [FieldOffset(0)]
    public float x;
    [FieldOffset(4)]
    public float y;
    [FieldOffset(8)]
    public float z;
    [FieldOffset(12)]
    public byte r;
    [FieldOffset(13)]
    public byte g;
    [FieldOffset(14)]
    public byte b;
    [FieldOffset(15)]
    public byte a;
    [FieldOffset(12)]
    public Color c;


    public VertexTinted(float x, float y, float z, byte r, byte g, byte b, byte a) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public VertexTinted(float x, float y, float z, Color c) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.c = c;
    }

    public VertexTinted(ushort x, ushort y, ushort z, byte r, byte g, byte b, byte a) {
        this.x = x;
        this.y = y;
        this.z = z;
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public VertexTinted(Vector3 pos, Color4b tint) {
        x = pos.X;
        y = pos.Y;
        z = pos.Z;
        r = tint.R;
        g = tint.G;
        b = tint.B;
        a = tint.A;
    }
}