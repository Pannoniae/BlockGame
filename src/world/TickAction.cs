using Silk.NET.Maths;

namespace BlockGame;

public readonly record struct TickAction(Vector3D<int> pos, Action action, int tick) {
    public readonly Vector3D<int> pos = pos;
    public readonly Action action = action;
    public readonly int tick = tick;


    // two tickactions are equal if the positions are equal and the tick is equal
    public bool Equals(TickAction other) {
        return pos.Equals(other.pos) && tick == other.tick;
    }
    public override int GetHashCode() {
        unchecked {
            return pos.GetHashCode() * 397 ^ tick;
        }
    }
};