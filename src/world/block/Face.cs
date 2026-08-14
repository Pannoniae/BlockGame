using System.Runtime.InteropServices;
using BlockGame.util;

namespace BlockGame.world.block;

/// <summary>
/// Represents a block face. If noAO, don't let AO cast on this face.
/// If it's not a full face, it's always drawn to ensure it's drawn even when there's a solid block next to it.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Face {
    public const int MAX_FACES = 12;

    public readonly float x1;
    public readonly float x2;
    public readonly float x3;
    public readonly float x4;
    public readonly float y1;
    public readonly float y2;
    public readonly float y3;
    public readonly float y4;
    public readonly float z1;
    public readonly float z2;
    public readonly float z3;
    public readonly float z4;

    public readonly UVPair min;
    public readonly UVPair max;
    public readonly RawDirection direction;
    public readonly byte flags;

    public Face(float x1, float y1, float z1,
        float x2, float y2, float z2,
        float x3, float y3, float z3,
        float x4, float y4, float z4,
        UVPair min, UVPair max,
        RawDirection direction,
        bool noAO = false,
        bool nonFullFace = false) {
        this.x1 = x1;
        this.x2 = x2;
        this.x3 = x3;
        this.x4 = x4;
        this.y1 = y1;
        this.y2 = y2;
        this.y3 = y3;
        this.y4 = y4;
        this.z1 = z1;
        this.z2 = z2;
        this.z3 = z3;
        this.z4 = z4;
        this.min = min;
        this.max = max;
        this.direction = direction;
        flags = (byte)(nonFullFace.toByte() | noAO.toByte() << 1);
    }

    public bool nonFullFace => (flags & (byte)FaceFlags.NON_FULL_FACE) != 0;
    public bool noAO => (flags & (byte)FaceFlags.NO_AO) != 0;
}
