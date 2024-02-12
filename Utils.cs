using Silk.NET.Maths;

namespace BlockGame;

public static class Utils {

}

/// <summary>
/// North = +Z
/// South = -Z
/// West = -X
/// East = +X
/// </summary>
public readonly record struct Direction {
    public static readonly Vector3D<int> WEST = new(-1, 0, 0);
    public static readonly Vector3D<int> EAST = new(1, 0, 0);
    public static readonly Vector3D<int> SOUTH = new(0, 0, -1);
    public static readonly Vector3D<int> NORTH = new(0, 0, 1);
    public static readonly Vector3D<int> DOWN = new(0, -1, 0);
    public static readonly Vector3D<int> UP = new(0, 1, 0);

    public static Vector3D<int>[] directions => [WEST, EAST, SOUTH, NORTH, DOWN, UP];
}
