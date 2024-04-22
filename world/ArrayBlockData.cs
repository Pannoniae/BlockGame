using System.Runtime.CompilerServices;

namespace BlockGame;

public class ArrayBlockData : BlockData {
    public ushort[] blocks = new ushort[Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE];

    // YZX because the internet said so
    public ushort this[int x, int y, int z] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => blocks[y * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE +
                      x * Chunk.CHUNKSIZE +
                      z];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => blocks[y * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE +
                      x * Chunk.CHUNKSIZE +
                      z] = value;
    }
}