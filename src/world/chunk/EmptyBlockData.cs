using System.Runtime.CompilerServices;

namespace BlockGame;

public class EmptyBlockData : BlockData {

    public ushort this[int x, int y, int z] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => 0;
        set { }
    }

    public byte getLight(int x, int y, int z) {
        // only skylight, zero blocklight
        return 15;
    }

    public byte skylight(int x, int y, int z) {
        return 15;
    }

    public byte blocklight(int x, int y, int z) {
        return 0;
    }

    public void setSkylight(int x, int y, int z, byte val) {
    }

    public void setBlocklight(int x, int y, int z, byte val) {
    }
}