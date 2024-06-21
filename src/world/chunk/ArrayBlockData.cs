using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlockGame.util;

namespace BlockGame;

public sealed class ArrayBlockData : BlockData, IDisposable {

    private static FixedArrayPool<ushort> blockPool = new(Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE);
    private static FixedArrayPool<byte> lightPool = new(Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE);

    public ushort[] blocks;

    /// <summary>
    /// Skylight is on the lower 4 bits, blocklight is on the upper 4 bits.
    /// Stored in YZX order.
    /// </summary>
    public byte[] light;

    public int blockCount;
    public int translucentCount;
    public int fullBlockCount;
    public int randomTickCount;

    /// <summary>
    /// Has the block storage been initialized?
    /// </summary>
    public bool inited;

    public Chunk chunk;
    public ChunkSection section;

    // YZX because the internet said so
    public ushort this[int x, int y, int z] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => !inited ? (ushort)0 : Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(blocks), y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x);
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            if (!inited && value != 0) {
                init();
            }
            ref var blockRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(blocks), y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x);
            var old = blockRef;
            blockRef = value;
            if (value != 0 && old == 0) {
                blockCount++;
                if (Blocks.get(value).randomTick) {
                    randomTickCount++;
                }
                if (Blocks.isFullBlock(value)) {
                    fullBlockCount++;
                }
                if (Blocks.isTranslucent(value)) {
                    translucentCount++;
                }
            }
            else if (value == 0 && old != 0) {
                blockCount--;
                if (Blocks.get(old).randomTick) {
                    randomTickCount--;
                }
                if (Blocks.isFullBlock(old)) {
                    fullBlockCount--;
                }
                if (Blocks.isTranslucent(old)) {
                    translucentCount--;
                }
            }
        }
    }

    public ArrayBlockData(Chunk chunk, ChunkSection section) {
        this.chunk = chunk;
        this.section = section;
        inited = false;
    }

    public void init() {
        blocks = blockPool.grab();
        light = lightPool.grab();
        Array.Clear(blocks);
        // fill it with empty
        Array.Clear(light);
        inited = true;

        // if we are already lighted, we can light the section (don't do it during worldgen - it will light the entire chunk full of ground
        if (chunk.status >= ChunkStatus.LIGHTED) {
            chunk.lightSection(section);
        }
    }

    public void loadInit() {
        blocks = blockPool.grab();
        light = lightPool.grab();
        Array.Clear(blocks);
        // fill it with empty
        Array.Clear(light);
        inited = true;
    }

    public bool isEmpty() {
        return blockCount == 0;
    }

    public bool hasRandomTickingBlocks() {
        return randomTickCount > 0;
    }

    public bool isFull() {
        return fullBlockCount == Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE;
    }

    public bool hasTranslucentBlocks() {
        return translucentCount > 0;
    }

    // cleanup
    public void Dispose() {
        blockPool.putBack(blocks);
        lightPool.putBack(light);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte getLight(int x, int y, int z) {
        return !inited ? (byte)15 : Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(light), y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void setLight(int x, int y, int z, byte value) {
        Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(light), y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x) = value;
    }

    public byte skylight(int x, int y, int z) {
        var value = !inited ? (byte)15 : Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(light), y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x);
        return (byte)(value & 0xF);
    }

    public byte blocklight(int x, int y, int z) {
        var value = !inited ? (byte)0 : Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(light), y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x);
        return (byte)((value & 0xF0) >> 4);
    }

    public void setSkylight(int x, int y, int z, byte val) {
        if (!inited) {
            init();
        }
        ref var value = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(light), y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x);
        var blocklight = (byte)((value & 0xF0) >> 4);
        // pack it back inside
        value = (byte)(blocklight << 4 | val);
    }

    public void setBlocklight(int x, int y, int z, byte val) {
        if (!inited) {
            init();
        }
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