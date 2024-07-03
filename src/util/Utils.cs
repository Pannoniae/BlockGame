using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Silk.NET.Maths;

namespace BlockGame.util;

public static class Utils {
    public static volatile byte[] waste;

    public static Vector3D<double> copy(Vector3D<double> input) {
        return new Vector3D<double>(input.X, input.Y, input.Z);
    }

    public static Vector3D<float> copy(Vector3D<float> input) {
        return new Vector3D<float>(input.X, input.Y, input.Z);
    }

    public static Vector3D<int> copy(Vector3D<int> input) {
        return new Vector3D<int>(input.X, input.Y, input.Z);
    }

    /// <summary>
    /// Correct mod which works with negative numbers. i.e. -1 mod 3 is 2 and -3 mod 3 is 0.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int mod(int x, int m) {
        int r = x % m;
        return r < 0 ? r + m : r;
    }

    /// <summary>
    /// Test GC, scaled by dt so it's per sec
    /// </summary>
    public static void wasteMemory(double dt, float megs) {
        waste = new byte[(int)(megs * 1024 * 1024 * dt)];
    }
    public static float deg2rad(float degrees) {
        return MathF.PI / 180f * degrees;
    }
    public static float rad2deg(float radians) {
        return 180f / MathF.PI * radians;
    }

    public static Vector3D<int> toBlockPos(this Vector3D<double> currentPos) {
        return new Vector3D<int>((int)Math.Floor(currentPos.X), (int)Math.Floor(currentPos.Y),
            (int)Math.Floor(currentPos.Z));
    }

    public static Half half(float value) {
        // Convert float to half float
        // Minimum exponent for rounding
        const uint MinExp = 0x3880_0000u;
        // Exponent displacement #1
        const uint Exponent126 = 0x3f00_0000u;
        // Exponent mask
        const uint SingleBiasedExponentMask = 0x7F80_0000;
        // Exponent displacement #2
        const uint Exponent13 = 0x0680_0000u;
        // Maximum value that is not Infinity in Half
        const float MaxHalfValueBelowInfinity = 65520.0f;
        // Mask for exponent bits in Half
        const uint ExponentMask = 0x7C00;
        uint bitValue = BitConverter.SingleToUInt32Bits(value);
        // Extract sign bit
        uint sign = (bitValue & 0x8000_0000) >> 16;
        // Clear sign bit
        value = float.Abs(value);
        // Rectify values that are Infinity in Half. (float.Min now emits vminps instruction if one of two arguments is a constant)
        value = float.Min(MaxHalfValueBelowInfinity, value);
        // Rectify lower exponent
        uint exponentOffset0 = BitConverter.SingleToUInt32Bits(float.Max(value, BitConverter.UInt32BitsToSingle(MinExp)));
        // Extract exponent
        exponentOffset0 &= SingleBiasedExponentMask;
        // Add exponent by 13
        exponentOffset0 += Exponent13;
        // Round Single into Half's precision (NaN also gets modified here, just setting the MSB of fraction)
        value += BitConverter.UInt32BitsToSingle(exponentOffset0);
        bitValue = BitConverter.SingleToUInt32Bits(value);
        // Subtract exponent by 126
        bitValue -= Exponent126;
        // Shift bitValue right by 13 bits to match the boundary of exponent part and fraction part.
        uint newExponent = bitValue >> 13;
        // Merge the exponent part with fraction part, and add the exponent part and fraction part's overflow.
        bitValue += newExponent;
        // Merge sign bit and possible NaN exponent
        bitValue |= sign;
        // The final result
        return BitConverter.UInt16BitsToHalf((ushort)bitValue);
    }
}

/// <summary>
/// North = +Z
/// South = -Z
/// West = -X
/// East = +X
/// Doubles as a normal too
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct Direction {

    public static int min = 0;
    public static int max = 6;

    public static readonly Vector3D<int> WEST = new(-1, 0, 0);
    public static readonly Vector3D<int> EAST = new(1, 0, 0);
    public static readonly Vector3D<int> SOUTH = new(0, 0, -1);
    public static readonly Vector3D<int> NORTH = new(0, 0, 1);
    public static readonly Vector3D<int> DOWN = new(0, -1, 0);
    public static readonly Vector3D<int> UP = new(0, 1, 0);
    public static readonly Vector3D<int> SELF = new(0, 0, 0);

    public static Vector3D<int>[] directions = [WEST, EAST, SOUTH, NORTH, DOWN, UP];
    public static Vector3D<int>[] directionsLight = [DOWN, UP, WEST, EAST, SOUTH, NORTH];
    public static Vector3D<int>[] directionsWaterSpread = [WEST, EAST, SOUTH, NORTH, DOWN];
    public static Vector3D<int>[] directionsHorizontal = [WEST, EAST, SOUTH, NORTH];
    public static Vector3D<int>[] directionsDiag = [WEST, EAST, SOUTH, NORTH, DOWN, UP, WEST + SOUTH, WEST + NORTH, EAST + SOUTH, EAST + NORTH];
    public static Vector3D<int>[] directionsAll = new Vector3D<int>[27];
    public static Vector3D<int>[] directionsSelf = [WEST, EAST, SOUTH, NORTH, DOWN, UP, SELF];

    static Direction() {
        // construct 27-box of all directions
        int i = 0;
        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                for (int z = -1; z <= 1; z++) {
                    directionsAll[i] = new Vector3D<int>(x, y, z);
                    // don't forget to increment, you silly you!:P
                    i++;
                }
            }
        }
    }
    public static Vector3D<int> getDirection(RawDirection dir) {
        return dir switch {
            RawDirection.WEST => WEST,
            RawDirection.EAST => EAST,
            RawDirection.SOUTH => SOUTH,
            RawDirection.NORTH => NORTH,
            RawDirection.DOWN => DOWN,
            RawDirection.UP => UP,
            _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null)
        };
    }

}

public enum RawDirection : byte {
    WEST = 0,
    EAST = 1,
    SOUTH = 2,
    NORTH = 3,
    DOWN = 4,
    UP = 5,
    NONE = 13 // 13 is 5 with the 4th bit set to 1
}