using Silk.NET.Maths;

namespace BlockGame;

public static class Utils {

    public static Vector3D<double> copy(Vector3D<double> input) {
        return new Vector3D<double>(input.X, input.Y, input.Z);
    }

    public static Vector3D<float> copy(Vector3D<float> input) {
        return new Vector3D<float>(input.X, input.Y, input.Z);
    }

    public static Vector3D<int> copy(Vector3D<int> input) {
        return new Vector3D<int>(input.X, input.Y, input.Z);
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
    public static readonly Vector3D<int> WEST = new(-1, 0, 0);
    public static readonly Vector3D<int> EAST = new(1, 0, 0);
    public static readonly Vector3D<int> SOUTH = new(0, 0, -1);
    public static readonly Vector3D<int> NORTH = new(0, 0, 1);
    public static readonly Vector3D<int> DOWN = new(0, -1, 0);
    public static readonly Vector3D<int> UP = new(0, 1, 0);
    public static readonly Vector3D<int> SELF = new(0, 0, 0);

    public static Vector3D<int>[] directions => [WEST, EAST, SOUTH, NORTH, DOWN, UP];
    public static Vector3D<int>[] directionsHorizontal => [WEST, EAST, SOUTH, NORTH];
    public static Vector3D<int>[] directionsDiag => [WEST, EAST, SOUTH, NORTH, DOWN, UP, WEST + SOUTH, WEST + NORTH, EAST + SOUTH, EAST + NORTH];
    public static Vector3D<int>[] directionsSelf => [WEST, EAST, SOUTH, NORTH, DOWN, UP, SELF];

}


public enum RawDirection : byte {
    WEST = 0,
    EAST = 1,
    SOUTH = 2,
    NORTH = 3,
    DOWN = 4,
    UP = 5
}
