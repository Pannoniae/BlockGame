using Silk.NET.Maths;

namespace BlockGame;

public readonly record struct BlockUpdate(Vector3D<int> position, int tick);