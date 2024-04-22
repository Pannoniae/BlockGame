using System.Runtime.CompilerServices;

namespace BlockGame;

public class EmptyBlockData : BlockData {

    public ushort this[int x, int y, int z] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => 0;
        set { }
    }
}