using System.Runtime.InteropServices;
using Molten;

namespace BlockGame;

[StructLayout(LayoutKind.Auto)]
public readonly record struct TickAction(Vector3I pos, Action action, int tick) {
    public readonly Vector3I pos = pos;
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