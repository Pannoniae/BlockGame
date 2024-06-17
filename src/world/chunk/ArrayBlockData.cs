using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlockGame.util;

namespace BlockGame;

public sealed class ArrayBlockData : BlockData, IDisposable {

    private static FixedArrayPool<ushort> blockPool = new(16 * 16 * 16);
    private static FixedArrayPool<byte> lightPool = new(16 * 16 * 16);

    public ushort[] blocks;

    /// <summary>
    /// Skylight is on the lower 4 bits, blocklight is on the upper 4 bits.
    /// Stored in YZX order.
    /// </summary>
    public byte[] light;

    public int blockCount;
    public int randomTickCount;

    /// <summary>
    /// Has the block storage been initialized?
    /// </summary>
    public bool inited;

    public Chunk chunk;

    // YZX because the internet said so
    public ushort this[int x, int y, int z] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(blocks), y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            var old = Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(blocks), y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x);
            Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(blocks), y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x) = value;
            if (value != 0 && old == 0) {
                blockCount++;
                if (Blocks.get(value).randomTick) {
                    randomTickCount++;
                }
            }
            else if (value == 0 && old != 0) {
                blockCount--;
                if (Blocks.get(old).randomTick) {
                    randomTickCount--;
                }
            }
            if (blockCount > 0 && !inited) {
                init();
            }
        }
    }

    public ArrayBlockData(Chunk chunk) {
        this.chunk = chunk;
        inited = false;
        init();
    }

    public void init() {
        blocks = blockPool.grab();
        light = lightPool.grab();
        Array.Clear(blocks);
        Array.Clear(light);
        inited = true;
    }

    public bool isEmpty() {
        return blockCount == 0;
    }

    public bool hasRandomTickingBlocks() {
        return randomTickCount != 0;
    }

    // cleanup
    public void Dispose() {
        blockPool.putBack(blocks);
        lightPool.putBack(light);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte getLight(int x, int y, int z) {
        return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(light), y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void setLight(int x, int y, int z, byte value) {
        Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(light), y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x) = value;
    }

    public byte skylight(int x, int y, int z) {
        var value = Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(light), y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x);
        return (byte)(value & 0xF);
    }

    public byte blocklight(int x, int y, int z) {
        var value = Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(light), y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x);
        return (byte)((value & 0xF0) >> 4);
    }

    public void setSkylight(int x, int y, int z, byte val) {
        ref var value = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(light), y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x);
        var blocklight = (byte)((value & 0xF0) >> 4);
        // pack it back inside
        value = (byte)(blocklight << 4 | val);
    }

    public void setBlocklight(int x, int y, int z, byte val) {
        ref var value = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(light), y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x);
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