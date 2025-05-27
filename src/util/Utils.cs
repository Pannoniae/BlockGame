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
