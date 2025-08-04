using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.util;

/// <summary>
/// It's like math but meth ;)
/// </summary>
public static partial class Meth {
    public static volatile byte[] waste;

    public static Vector3D copy(Vector3D input) {
        return new Vector3D(input.X, input.Y, input.Z);
    }

    public static Vector3F copy(Vector3F input) {
        return new Vector3F(input.X, input.Y, input.Z);
    }

    public static Vector3I copy(Vector3I input) {
        return new Vector3I(input.X, input.Y, input.Z);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3D norm(this Vector3D v) {
        
        double lengthSq = double.MultiplyAddEstimate(v.X, v.X, double.MultiplyAddEstimate(v.Y, v.Y, v.Z * v.Z));
        double invLength = double.ReciprocalSqrtEstimate(lengthSq);
        return new Vector3D(v.X * invLength, v.Y * invLength, v.Z * invLength);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void normi(this ref Vector3D v) {
        double lengthSq = double.MultiplyAddEstimate(v.X, v.X, double.MultiplyAddEstimate(v.Y, v.Y, v.Z * v.Z));
        double invLength = double.ReciprocalSqrtEstimate(lengthSq);
        v.X *= invLength;
        v.Y *= invLength;
        v.Z *= invLength;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void normi(this ref Vector3 v) {
        float lengthSq = float.MultiplyAddEstimate(v.X, v.X, float.MultiplyAddEstimate(v.Y, v.Y, v.Z * v.Z));
        float invLength = float.ReciprocalSqrtEstimate(lengthSq);
        v.X *= invLength;
        v.Y *= invLength;
        v.Z *= invLength;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double dot(this Vector3D a, Vector3D b) {
        return double.MultiplyAddEstimate(a.X, b.X, double.MultiplyAddEstimate(a.Y, b.Y, a.Z * b.Z));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float dot(this Vector3 a, Vector3 b) {
        return float.MultiplyAddEstimate(a.X, b.X, float.MultiplyAddEstimate(a.Y, b.Y, a.Z * b.Z));
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
        return float.Pi / 180f * degrees;
    }
    public static float rad2deg(float radians) {
        return 180f / float.Pi * radians;
    }

    /// <summary>
    /// Maps a value from one range to another range.
    /// For example, mapRange(0.75f, 0.5f, 1.0f, 0f, 2f) maps 0.75 from range [0.5-1.0] to range [0-2], returning 1.0
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float mapRange(float value, float fromStart, float fromEnd, float toStart, float toEnd) {
        if (value < fromStart) return toStart;
        if (value > fromEnd) return toEnd;
        float t = (value - fromStart) / (fromEnd - fromStart);
        return toStart + t * (toEnd - toStart);
    }

    /// <summary>
    /// Fade in from 0 to 1 over a range
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float fadeIn(float value, float start, float end) {
        return mapRange(value, start, end, 0f, 1f);
    }

    /// <summary>
    /// Fade out from 1 to 0 over a range
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float fadeOut(float value, float start, float end) {
        return mapRange(value, start, end, 1f, 0f);
    }

    public static Vector3I getRandomCoord(XRandom random, int maxX, int maxY, int maxZ) {
        var randomValue = random.Next(maxX * maxY * maxZ);
        var x = randomValue % maxX;
        var y = randomValue / maxX % maxY;
        var z = randomValue / (maxX * maxY);
        return new Vector3I(x, y, z);
    }

    public static Vector3I getRandomCoord(XRandom random, int maxX, int maxY, int maxZ, int minX, int minY, int minZ) {
        return getRandomCoord(random, maxX - minX, maxY - minY, maxZ - minZ) + new Vector3I(minX, minY, minZ);
    }

    public static float ToDegrees(double radians) {
        return (float)(radians * 180.0 / Math.PI);
    }

    public static float lerp(float start, float end, float amount) {
        return start + (end - start) * Math.Clamp(amount, 0.0f, 1.0f);
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

    public static readonly Vector3I WEST = new(-1, 0, 0);
    public static readonly Vector3I EAST = new(1, 0, 0);
    public static readonly Vector3I SOUTH = new(0, 0, -1);
    public static readonly Vector3I NORTH = new(0, 0, 1);
    public static readonly Vector3I DOWN = new(0, -1, 0);
    public static readonly Vector3I UP = new(0, 1, 0);
    public static readonly Vector3I SELF = new(0, 0, 0);

    public static readonly Vector3I[] directions = [WEST, EAST, SOUTH, NORTH, DOWN, UP];
    public static readonly Vector3I[] directionsLight = [DOWN, UP, WEST, EAST, SOUTH, NORTH];
    public static readonly Vector3I[] directionsWaterSpread = [WEST, EAST, SOUTH, NORTH, DOWN];
    public static readonly Vector3I[] directionsHorizontal = [WEST, EAST, SOUTH, NORTH];
    public static readonly Vector3I[] directionsDiag = [WEST, EAST, SOUTH, NORTH, DOWN, UP, WEST + SOUTH, WEST + NORTH, EAST + SOUTH, EAST + NORTH];
    public static readonly Vector3I[] directionsAll = new Vector3I[27];
    public static readonly Vector3I[] directionsSelf = [WEST, EAST, SOUTH, NORTH, DOWN, UP, SELF];

    static Direction() {
        // construct 27-box of all directions
        int i = 0;
        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                for (int z = -1; z <= 1; z++) {
                    directionsAll[i] = new Vector3I(x, y, z);
                    // don't forget to increment, you silly you!:P
                    i++;
                }
            }
        }
    }
    public static Vector3I getDirection(RawDirection dir) {
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

    public static RawDirection getRawDirection(Vector3I dir) {
        if (dir == WEST) {
            return RawDirection.WEST;
        }
        if (dir == EAST) {
            return RawDirection.EAST;
        }
        if (dir == SOUTH) {
            return RawDirection.SOUTH;
        }
        if (dir == NORTH) {
            return RawDirection.NORTH;
        }
        if (dir == DOWN) {
            return RawDirection.DOWN;
        }
        if (dir == UP) {
            return RawDirection.UP;
        }
        throw new ArgumentException("Invalid direction!");
    }

}

public static class UnsafeListAccessor<T> {
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_items")]
    public static extern ref T[] getItems(List<T> list);

    public static Span<T> AsUnsafeSpanOnBackingArray(List<T> list) {
        return new Span<T>(getItems(list));
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
