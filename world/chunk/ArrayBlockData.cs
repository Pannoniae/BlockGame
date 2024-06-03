using System.Runtime.CompilerServices;

namespace BlockGame;

public class ArrayBlockData : BlockData {

    /// <summary>
    /// Skylight is on the lower 4 bits, blocklight is on the upper 4 bits.
    /// Stored in YZX order.
    /// </summary>
    public ArrayBlockDataB light;
    public ArrayBlockDataU blocks;

    public Chunk chunk;

    // YZX because the internet said so
    public ushort this[int x, int y, int z] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => blocks[y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => blocks[y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x] = value;
    }

    public ArrayBlockData(Chunk chunk) {
        this.chunk = chunk;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte getLight(int x, int y, int z) {
        return light[y * Chunk.CHUNKSIZESQ +
                     z * Chunk.CHUNKSIZE +
                     x];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void setLight(int x, int y, int z, byte value) {
        light[y * Chunk.CHUNKSIZESQ +
              z * Chunk.CHUNKSIZE +
              x] = value;
    }

    public byte skylight(int x, int y, int z) {
        var value = light[y * Chunk.CHUNKSIZESQ +
                          z * Chunk.CHUNKSIZE +
                          x];
        return (byte)(value & 0xF);
    }

    public byte blocklight(int x, int y, int z) {
        var value = light[y * Chunk.CHUNKSIZESQ +
                          z * Chunk.CHUNKSIZE +
                          x];
        return (byte)((value & 0xF0) >> 4);
    }

    public void setSkylight(int x, int y, int z, byte val) {
        ref var value = ref light[y * Chunk.CHUNKSIZESQ +
                                  z * Chunk.CHUNKSIZE +
                                  x];
        var blocklight = (byte)((value & 0xF0) >> 4);
        // pack it back inside
        value = (byte)(blocklight << 4 | val);
    }

    public void setBlocklight(int x, int y, int z, byte val) {
        ref var value = ref light[y * Chunk.CHUNKSIZESQ +
                                  z * Chunk.CHUNKSIZE +
                                  x];
        var skylight = (byte)(value & 0xF);
        // pack it back inside
        value = (byte)(val << 4 | skylight);
    }

    public static byte extractSkylight(byte value) {
        return (byte)(value & 0xF);
    }

    public static byte extractBlocklight(byte value) {
        return (byte)((value & 0xF0) >> 4);
    }
}

[InlineArray(Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE)]
public struct ArrayBlockDataB {
    private byte b;
}

[InlineArray(Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE)]
public struct ArrayBlockDataU {
    private ushort u;
}

[InlineArray(Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX)]
public struct NeighbourBlockDataB {
    private byte b;
}

[InlineArray(Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX * Chunk.CHUNKSIZEEX)]
public struct NeighbourBlockDataU {
    private ushort u;
}