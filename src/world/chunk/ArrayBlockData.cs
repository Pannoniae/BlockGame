using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlockGame.util;

namespace BlockGame;

public sealed class ArrayBlockData : BlockData, IDisposable {


    public static FixedArrayPool<ushort> blockPool = new(Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE);
    public static FixedArrayPool<byte> lightPool = new(Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE);

    public ushort[]? blocks;

    /// <summary>
    /// Skylight is on the lower 4 bits, blocklight is on the upper 4 bits.
    /// Stored in YZX order.
    /// </summary>
    public byte[]? light;

    public int blockCount;
    public int translucentCount;
    public int fullBlockCount;
    public int randomTickCount;

    /// <summary>
    /// Has the block storage been initialized?
    /// </summary>
    public bool inited;

    public Chunk chunk;
    public SubChunk section;

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
            // if old was air, new is not
            if (old == 0 && value != 0) {
                blockCount++;
            }
            else if (old != 0 && value == 0) {
                blockCount--;
            }

            var oldTick = Blocks.get(old).randomTick;
            var tick = Blocks.get(value).randomTick;
            if (!oldTick && tick) {
                randomTickCount++;
            }
            else if (oldTick && !tick) {
                randomTickCount--;
            }

            var oldFullBlock = Blocks.isFullBlock(old);
            var fullBlock = Blocks.isFullBlock(value);
            if (!oldFullBlock && fullBlock) {
                chunk.addToHeightMap(x, section.chunkY * Chunk.CHUNKSIZE + y, z);
                fullBlockCount++;
            }
            else if (oldFullBlock && !fullBlock) {
                chunk.removeFromHeightMap(x, section.chunkY * Chunk.CHUNKSIZE + y, z);
                fullBlockCount--;
            }

            var oldTranslucent = Blocks.isTranslucent(old);
            var translucent = Blocks.isTranslucent(value);
            if (!oldTranslucent && translucent) {
                translucentCount++;
            }
            else if (oldTranslucent && !translucent) {
                translucentCount--;
            }
        }
    }

    /// <summary>
    /// Your reponsibility to update the counts after a batch of changes.
    /// </summary>
    public void fastSet(int x, int y, int z, ushort value) {
        if (!inited && value != 0) {
            init();
        }
        Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(blocks), y * Chunk.CHUNKSIZESQ + z * Chunk.CHUNKSIZE + x) = value;
    }

    public ArrayBlockData(Chunk chunk, SubChunk section) {
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
        return blockCount == 0 || !inited;
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
    private void ReleaseUnmanagedResources() {
        if (blocks != null) {
            blockPool.putBack(blocks);
        }
        if (light != null) {
            lightPool.putBack(light);
        }
    }
    public void Dispose() {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
    ~ArrayBlockData() {
        ReleaseUnmanagedResources();
    }


    /// <summary>
    /// After loading, the counters will be gone. This method recalculates all of them.
    /// </summary>
    public void refreshCounts() {
        blockCount = 0;
        translucentCount = 0;
        fullBlockCount = 0;
        randomTickCount = 0;

        ref var blockArray = ref MemoryMarshal.GetArrayDataReference(blocks);
        for (int i = 0; i < blocks.Length; i++) {
            int x = i & 0xF;
            int z = i >> 4 & 0xF;
            int y = i >> 8;
            var block = blockArray;
            if (block != 0) {
                blockCount++;
            }
            if (Blocks.get(block).randomTick) {
                randomTickCount++;
            }
            if (Blocks.isFullBlock(block)) {
                chunk.addToHeightMap(x, section.chunkY * Chunk.CHUNKSIZE + y, z);
                fullBlockCount++;
            }
            if (Blocks.isTranslucent(block)) {
                translucentCount++;
            }
            blockArray = ref Unsafe.Add(ref blockArray, 1);
        }
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