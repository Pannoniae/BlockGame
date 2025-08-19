using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlockGame.util;

namespace BlockGame;

public sealed class ArrayBlockData : BlockData, IDisposable {


    public static FixedArrayPool<uint> blockPool = new(Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE);
    public static FixedArrayPool<byte> lightPool = new(Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE);

    public uint[]? blocks;

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
    public int yCoord;

    // YZX because the internet said so
    public ushort this[int x, int y, int z] {
        
        // TODO removed the inited check, add it back when the unloaded chunk optimisation is implemented properly
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(blocks), (y << 8) + (z << 4) + x).getID();
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            /*if (!inited && value != 0) {
                init();
            }*/
            ref var blockRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(blocks), (y << 8) + (z << 4) + x);
            ushort old = blockRef.getID();
            blockRef = value;
            // if old was air, new is not
            if (old == 0 && value != 0) {
                blockCount++;
            }
            else if (old != 0 && value == 0) {
                blockCount--;
            }

            var oldTick = Block.randomTick[old];
            var tick = Block.randomTick[value];
            if (!oldTick && tick) {
                randomTickCount++;
            }
            else if (oldTick && !tick) {
                randomTickCount--;
            }

            var oldFullBlock = Block.isFullBlock(old);
            var fullBlock = Block.isFullBlock(value);
            if (!oldFullBlock && fullBlock) {
                chunk.addToHeightMap(x, (yCoord << 4) + y, z);
                fullBlockCount++;
            }
            else if (oldFullBlock && !fullBlock) {
                chunk.removeFromHeightMap(x, (yCoord << 4) + y, z);
                fullBlockCount--;
            }

            var oldTranslucent = Block.isTranslucent(old);
            var translucent = Block.isTranslucent(value);
            if (!oldTranslucent && translucent) {
                translucentCount++;
            }
            else if (oldTranslucent && !translucent) {
                translucentCount--;
            }
        }
    }

    public ushort fastGet(int x, int y, int z) {
        return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(blocks), (y << 8) + (z << 4) + x).getID();
    }

    /// <summary>
    /// Your responsibility to update the counts after a batch of changes.
    /// </summary>
    public void fastSet(int x, int y, int z, ushort value) {
        Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(blocks), (y << 8) + (z << 4) + x) = value;
    }
    
    /// <summary>
    /// Kind of like <see cref="fastSet"/>, but doesn't check if the block data is initialized. I've warned you.
    /// </summary>
    public void fastSetUnsafe(int x, int y, int z, ushort value) {
        Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(blocks), (y << 8) + (z << 4) + x) = value;
    }

    public ArrayBlockData(Chunk chunk, int yCoord) {
        this.chunk = chunk;
        this.yCoord = yCoord;
        //inited = false;
        
        // TODO disabled the empty chunk memory optimisation, because it was fucking up the lighting! this needs to be fixed later
        init();
    }
    
    // don't inline this, lots of useless code we don't need in the common case.
    //[MethodImpl(MethodImplOptions.NoInlining)]
    public void init() {
        blocks = blockPool.grab();
        light = lightPool.grab();
        Array.Clear(blocks);
        // fill it with empty
        Array.Fill(light, (byte)0x00);
        inited = true;

        // if we are already lighted, we can light the section (don't do it during worldgen - it will light the entire chunk full of ground
        // todo this will light caves & shit too. fix this so chunk is only lighted once
        //if (chunk.status >= ChunkStatus.LIGHTED) {
        //    
        //}
    }

    // don't need to init - the arrays will be overwritten anyway
    public void loadInit() {
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
        if (!inited) {
            return;
        }
        blockCount = 0;
        translucentCount = 0;
        fullBlockCount = 0;
        randomTickCount = 0;

        ref var blockArray = ref MemoryMarshal.GetArrayDataReference(blocks);
        for (int i = 0; i < blocks.Length; i++) {
            int x = i & 0xF;
            int z = i >> 4 & 0xF;
            int y = i >> 8;
            var block = blockArray.getID();
            if (block != 0) {
                blockCount++;
            }
            if (Block.randomTick[block]) {
                randomTickCount++;
            }
            if (Block.isFullBlock(block)) {
                chunk.addToHeightMap(x, (yCoord << 4) + y, z);
                fullBlockCount++;
            }
            if (Block.isTranslucent(block)) {
                translucentCount++;
            }
            blockArray = ref Unsafe.Add(ref blockArray, 1);
        }
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte getLight(int x, int y, int z) {
        return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(light), (y << 8) + (z << 4) + x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void setLight(int x, int y, int z, byte value) {
        Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(light), (y << 8) + (z << 4) + x) = value;
    }

    public byte skylight(int x, int y, int z) {
        var value = Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(light), (y << 8) + (z << 4) + x);
        return (byte)(value & 0xF);
    }

    public byte blocklight(int x, int y, int z) {
        var value = Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(light), (y << 8) + (z << 4) + x);
        return (byte)((value >> 4) & 0xF);
    }

    public void setSkylight(int x, int y, int z, byte val) {
        ref var value = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(light), (y << 8) + (z << 4) + x);
        var blocklight = (byte)((value >> 4) & 0xF);
        // pack it back inside
        value = (byte)(blocklight << 4 | val);
    }

    public void setBlocklight(int x, int y, int z, byte val) {
        ref var value = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(light), (y << 8) + (z << 4) + x);
        var skylight = (byte)(value & 0xF);
        // pack it back inside
        value = (byte)(val << 4 | skylight);
    }

    public static byte extractSkylight(byte value) {
        return (byte)(value & 0xF);
    }

    public static byte extractBlocklight(byte value) {
        return (byte)((value >> 4) & 0xF);
    }
}