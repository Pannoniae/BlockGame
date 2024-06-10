using Silk.NET.Maths;

namespace BlockGame;

public readonly record struct BlockUpdate(Vector3D<int> position, int tick) {
    public readonly Vector3D<int> position = position;
    public readonly int tick = tick;
};