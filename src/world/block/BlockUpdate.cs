using Molten;

namespace BlockGame;

public readonly record struct BlockUpdate(Vector3I position, int tick) {
    public readonly Vector3I position = position;
    public readonly int tick = tick;
    
    public bool Equals(BlockUpdate other) {
        return position.Equals(other.position) && tick == other.tick;
    }

    public override int GetHashCode() {
        return HashCode.Combine(position, tick);
    }
};