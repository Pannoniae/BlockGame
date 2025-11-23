using System.Runtime.InteropServices;
using BlockGame.util;

namespace BlockGame.world.block;

#pragma warning disable CS8618
/// <summary>
/// Represents a block face. If noAO, don't let AO cast on this face.
/// If it's not a full face, it's always drawn to ensure it's drawn even when there's a solid block next to it.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct Face(
    float x1,
    float y1,
    float z1,
    float x2,
    float y2,
    float z2,
    float x3,
    float y3,
    float z3,
    float x4,
    float y4,
    float z4,
    UVPair min,
    UVPair max,
    RawDirection direction,
    bool noAO = false,
    bool nonFullFace = false) {
    public const int MAX_FACES = 12;

    public readonly float x1 = x1;
    public readonly float y1 = y1;
    public readonly float z1 = z1;
    public readonly float x2 = x2;
    public readonly float y2 = y2;
    public readonly float z2 = z2;
    public readonly float x3 = x3;
    public readonly float y3 = y3;
    public readonly float z3 = z3;
    public readonly float x4 = x4;
    public readonly float y4 = y4;
    public readonly float z4 = z4;
    public readonly UVPair min = min;
    public readonly UVPair max = max;
    public readonly RawDirection direction = direction;
    public readonly byte flags = (byte)(nonFullFace.toByte() | noAO.toByte() << 1);

    public bool nonFullFace => (flags & (byte)FaceFlags.NON_FULL_FACE) != 0;
    public bool noAO => (flags & (byte)FaceFlags.NO_AO) != 0;
}