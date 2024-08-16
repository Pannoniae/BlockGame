using Molten;

namespace BlockGame;

public readonly record struct BlockUpdate(Vector3I position, int tick) {
    public readonly Vector3I position = position;
    public readonly int tick = tick;
};