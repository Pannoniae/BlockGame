using System.Runtime.CompilerServices;

namespace BlockGame;

public class ArrayBlockData : BlockData {
    public ushort[] blocks = new ushort[Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE];

    // YZX because the internet said so
    public ushort this[int x, int y, int z] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => blocks[y * Chunk.CHUNKSIZESQ +
                      z * Chunk.CHUNKSIZE +
                      x];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => blocks[y * Chunk.CHUNKSIZESQ +
                      z * Chunk.CHUNKSIZE +
                      x] = value;
    }
}