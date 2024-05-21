using System.Runtime.CompilerServices;
using Silk.NET.Maths;

namespace BlockGame;

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
}

/// <summary>
/// North = +Z
/// South = -Z
/// West = -X
/// East = +X
/// Doubles as a normal too
/// </summary>
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

    public static Vector3D<int>[] directions => [WEST, EAST, SOUTH, NORTH, DOWN, UP];
    public static Vector3D<int>[] directionsWaterSpread => [WEST, EAST, SOUTH, NORTH, DOWN];
    public static Vector3D<int>[] directionsHorizontal => [WEST, EAST, SOUTH, NORTH];
    public static Vector3D<int>[] directionsDiag => [WEST, EAST, SOUTH, NORTH, DOWN, UP, WEST + SOUTH, WEST + NORTH, EAST + SOUTH, EAST + NORTH];
    public static Vector3D<int>[] directionsSelf => [WEST, EAST, SOUTH, NORTH, DOWN, UP, SELF];

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
    NONE = 12
}