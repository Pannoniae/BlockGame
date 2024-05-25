using System.Runtime.CompilerServices;

namespace BlockGame;

public class ArrayBlockData : BlockData {

    /// <summary>
    /// Skylight is on the lower 4 bits, blocklight is on the upper 4 bits.
    /// Stored in YZX order.
    /// </summary>
    public byte[] light = new byte[Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE];
    public ushort[] blocks = new ushort[Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE];

    public Chunk chunk;

    // YZX because the internet said so
    public ushort this[int x, int y, int z] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ChunkSectionRenderer.access(blocks, y * Chunk.CHUNKSIZESQ +
                                                   z * Chunk.CHUNKSIZE +
                                                   x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => ChunkSectionRenderer.access(blocks, y * Chunk.CHUNKSIZESQ +
                                                   z * Chunk.CHUNKSIZE +
                                                   x, value);
    }

    public ArrayBlockData(Chunk chunk) {
        this.chunk = chunk;
    }

    public byte getLight(int x, int y, int z) {
        var value = ChunkSectionRenderer.access(light, y * Chunk.CHUNKSIZESQ +
                                                       z * Chunk.CHUNKSIZE +
                                                       x);
        return value;
    }

    public byte skylight(int x, int y, int z) {
        var value = ChunkSectionRenderer.access(light, y * Chunk.CHUNKSIZESQ +
                                                       z * Chunk.CHUNKSIZE +
                                                       x);
        return (byte)(value & 0xF);
    }

    public byte blocklight(int x, int y, int z) {
        var value = ChunkSectionRenderer.access(light, y * Chunk.CHUNKSIZESQ +
                                                       z * Chunk.CHUNKSIZE +
                                                       x);
        return (byte)((value & 0xF0) >> 4);
    }

    public void setSkylight(int x, int y, int z, byte val) {
        var value = ChunkSectionRenderer.access(light, y * Chunk.CHUNKSIZESQ +
                                                       z * Chunk.CHUNKSIZE +
                                                       x);
        var blocklight = (byte)((value & 0xF0) >> 4);
        // pack it back inside
        light[y * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE +
              z * Chunk.CHUNKSIZE +
              x] = (byte)(blocklight << 4 | val & 0xF);
    }

    public void setBlocklight(int x, int y, int z, byte val) {
        var value = ChunkSectionRenderer.access(light, y * Chunk.CHUNKSIZESQ +
                                                       z * Chunk.CHUNKSIZE +
                                                       x);
        var skylight = (byte)(value & 0xF);
        // pack it back inside
        light[y * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE +
              z * Chunk.CHUNKSIZE +
              x] = (byte)(val << 4 | skylight & 0xF);
    }

    public static byte extractSkylight(byte value) {
        return (byte)(value & 0xF);
    }

    public static byte extractBlocklight(byte value) {
        return (byte)((value & 0xF0) >> 4);
    }
}