using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlockGame.util;

namespace BlockGame;

public sealed class PaletteBlockData : BlockData, IDisposable {
    private static readonly ArrayPool<byte> arrayPool = ArrayPool<byte>.Shared;

    private uint[] palette;
    private ushort[] refCounts; // reference count for each palette entry
    private byte[]? indices;
    private int indicesLength; // actual allocated size for returning to pool
    private int paletteSize;
    private int paletteCapacity;
    private int bitsPerIndex;
    
    // light palette
    private byte[] lightPalette;
    private ushort[] lightRefCounts;
    private byte[]? lightIndices;
    private int lightIndicesLength; // actual allocated size for returning to pool
    private int lightPaletteSize;
    private int lightPaletteCapacity;
    private int lightBitsPerIndex;

    public int blockCount;
    public int translucentCount;
    public int fullBlockCount;
    public int randomTickCount;
    public int renderTickCount;

    /// <summary>
    /// Has the block storage been initialized?
    /// </summary>
    public bool inited { get; set; }

    public Chunk chunk;
    public int yCoord;

    private const int TOTAL_BLOCKS = Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE;
    private const int INITIAL_PALETTE_SIZE = 16;
    private const int MAX_DIRECT_PALETTE_SIZE = 16; // if more than this, switch to global indices
    private const int INITIAL_LIGHT_PALETTE_SIZE = 16;

    // YZX because the internet said so
    public ushort this[int x, int y, int z] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            var index = getIndex(x, y, z);
            return palette[index].getID();
        }
        set {
            var coord = (y << 8) + (z << 4) + x;
            var oldIndex = getIndexRaw(coord);
            var oldValue = palette[oldIndex];
            var oldID = oldValue.getID();
            
            // find or add new value to palette
            var newValue = value;
            var newIndex = findOrAddToPalette(newValue);
            
            // update reference counts
            decrementRefCount(oldIndex);
            incrementRefCount(newIndex);
            
            // update the index
            setIndexRaw(coord, newIndex);
            
            // update counts
            updateCounts(oldID, value, x, y, z);
            
            // try to shrink palette if we have unused entries
            tryCompactPalette();
        }
    }

    public uint getRaw(int x, int y, int z) {
        var index = getIndex(x, y, z);
        return palette[index];
    }

    public void setRaw(int x, int y, int z, uint value) {
        var coord = (y << 8) + (z << 4) + x;
        var oldIndex = getIndexRaw(coord);
        var oldValue = palette[oldIndex];
        var oldID = oldValue.getID();
        
        // find or add new value to palette
        var newIndex = findOrAddToPalette(value);
        
        // update reference counts
        decrementRefCount(oldIndex);
        incrementRefCount(newIndex);
        
        // update the index
        setIndexRaw(coord, newIndex);
        
        // update counts
        var newID = value.getID();
        updateCounts(oldID, newID, x, y, z);
        
        // try to shrink palette if we have unused entries
        tryCompactPalette();
    }

    public byte getMetadata(int x, int y, int z) {
        var index = getIndex(x, y, z);
        return palette[index].getMetadata();
    }

    public void setMetadata(int x, int y, int z, byte val) {
        var coord = (y << 8) + (z << 4) + x;
        var oldIndex = getIndexRaw(coord);
        var oldValue = palette[oldIndex];
        
        // create new value with updated metadata
        var newValue = oldValue.setMetadata(val);
        
        // find or add to palette
        var newIndex = findOrAddToPalette(newValue);
        
        // update reference counts
        decrementRefCount(oldIndex);
        incrementRefCount(newIndex);
        
        // update the index
        setIndexRaw(coord, newIndex);
        
        // try to shrink palette if we have unused entries
        tryCompactPalette();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int getIndex(int x, int y, int z) {
        return getIndexRaw((y << 8) + (z << 4) + x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int getIndexRaw(int blockCoord) {
        if (bitsPerIndex == 0) return 0; // single block type
        
        var bitIndex = blockCoord * bitsPerIndex;
        var byteIndex = bitIndex >> 3; // div 8
        var bitOffset = bitIndex & 7; // mod 8
        
        // handle crossing byte boundaries
        var result = 0;
        var bitsRemaining = bitsPerIndex;
        
        while (bitsRemaining > 0) {
            var bitsInThisByte = Math.Min(8 - bitOffset, bitsRemaining);
            var mask = (1 << bitsInThisByte) - 1;
            var value = (indices![byteIndex] >> bitOffset) & mask;
            
            result |= value << (bitsPerIndex - bitsRemaining);
            
            bitsRemaining -= bitsInThisByte;
            bitOffset = 0;
            byteIndex++;
        }
        
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void setIndexRaw(int blockCoord, int index) {
        if (bitsPerIndex == 0) return; // single block type, nothing to store
        
        var bitIndex = blockCoord * bitsPerIndex;
        var byteIndex = bitIndex >> 3; // div 8
        var bitOffset = bitIndex & 7; // mod 8
        
        // handle crossing byte boundaries
        var bitsRemaining = bitsPerIndex;
        
        while (bitsRemaining > 0) {
            var bitsInThisByte = Math.Min(8 - bitOffset, bitsRemaining);
            var mask = (1 << bitsInThisByte) - 1;
            var value = (index >> (bitsPerIndex - bitsRemaining)) & mask;
            
            // clear the bits we're about to write
            indices![byteIndex] = (byte)(indices[byteIndex] & ~(mask << bitOffset));
            // write the new bits
            indices[byteIndex] = (byte)(indices[byteIndex] | (value << bitOffset));
            
            bitsRemaining -= bitsInThisByte;
            bitOffset = 0;
            byteIndex++;
        }
    }

    private int findOrAddToPalette(uint blockValue) {
        // linear search for now - could optimize with dictionary if needed
        for (int i = 0; i < paletteSize; i++) {
            if (palette[i] == blockValue) {
                return i;
            }
        }
        
        // need to add to palette - grow if necessary
        if (paletteSize >= paletteCapacity) {
            growPalette();
        }
        
        palette[paletteSize] = blockValue;
        refCounts[paletteSize] = 0; // will be incremented by caller
        paletteSize++;
        
        // check if we need to increase bits per index
        var newBitsPerIndex = calculateBitsPerIndex(paletteSize);
        if (newBitsPerIndex != bitsPerIndex) {
            resizeIndices(newBitsPerIndex);
        }
        
        return paletteSize - 1;
    }

    private void growPalette() {
        var newCapacity = paletteCapacity * 2;
        var newPalette = GC.AllocateUninitializedArray<uint>(newCapacity);
        var newRefCounts = GC.AllocateUninitializedArray<ushort>(newCapacity);
        
        Array.Copy(palette, newPalette, paletteSize);
        Array.Copy(refCounts, newRefCounts, paletteSize);
        
        palette = newPalette;
        refCounts = newRefCounts;
        paletteCapacity = newCapacity;
    }

    private void incrementRefCount(int paletteIndex) {
        if (refCounts[paletteIndex] < ushort.MaxValue) {
            refCounts[paletteIndex]++;
        }
    }

    private void decrementRefCount(int paletteIndex) {
        if (refCounts[paletteIndex] > 0) {
            refCounts[paletteIndex]--;
        }
    }

    private void tryCompactPalette() {
        // only compact if we have unused entries and palette is getting large
        if (paletteSize <= MAX_DIRECT_PALETTE_SIZE) return;
        
        var unusedCount = 0;
        for (int i = 0; i < paletteSize; i++) {
            if (refCounts[i] == 0) {
                unusedCount++;
            }
        }
        
        // only compact if we have significant unused entries (>25% waste)
        if (unusedCount < paletteSize / 4) return;
        
        compactPalette();
    }

    private void compactPalette() {
        // use stack allocation for small palettes, heap for large ones
        Span<int> remapping = paletteSize <= 1024 
            ? stackalloc int[paletteSize] 
            : GC.AllocateUninitializedArray<int>(paletteSize);
            
        var newSize = 0;
        
        // build remapping table
        for (int i = 0; i < paletteSize; i++) {
            if (refCounts[i] > 0) {
                remapping[i] = newSize;
                if (newSize != i) {
                    palette[newSize] = palette[i];
                    refCounts[newSize] = refCounts[i];
                }
                newSize++;
            } else {
                remapping[i] = -1; // unused entry
            }
        }
        
        if (newSize == paletteSize) return; // nothing to compact
        
        paletteSize = newSize;
        
        // update all indices in the chunk
        for (int i = 0; i < TOTAL_BLOCKS; i++) {
            var oldIndex = getIndexRaw(i);
            var newIndex = remapping[oldIndex];
            if (newIndex == -1) {
                throw new InvalidOperationException("Found reference to unused palette entry");
            }
            setIndexRaw(i, newIndex);
        }
        
        // check if we can reduce bits per index
        var newBitsPerIndex = calculateBitsPerIndex(paletteSize);
        if (newBitsPerIndex < bitsPerIndex) {
            resizeIndices(newBitsPerIndex);
        }
    }

    private void resizeIndices(int newBitsPerIndex) {
        if (newBitsPerIndex == bitsPerIndex) return;
        
        var oldBitsPerIndex = bitsPerIndex;
        bitsPerIndex = newBitsPerIndex;
        
        // if growing from 0 bits, allocate indices array
        if (oldBitsPerIndex == 0) {
            indicesLength = calculateIndicesSize(newBitsPerIndex);
            indices = arrayPool.Rent(indicesLength);
            Array.Clear(indices, 0, indicesLength);
            return;
        }
        
        // if shrinking to 0 bits, deallocate indices array
        if (newBitsPerIndex == 0) {
            if (indices != null) {
                arrayPool.Return(indices);
                indices = null;
                indicesLength = 0;
            }
            return;
        }
        
        // put back old indices and grab new ones first to avoid peak memory usage
        var oldIndices = indices;
        var oldIndicesLength = indicesLength;
        indicesLength = calculateIndicesSize(newBitsPerIndex);
        indices = arrayPool.Rent(indicesLength);
        Array.Clear(indices, 0, indicesLength);
        
        // copy all indices to new format directly into the pooled array
        for (int i = 0; i < TOTAL_BLOCKS; i++) {
            var oldIndex = getIndexRaw(i, oldIndices, oldBitsPerIndex);
            setIndexRaw(i, oldIndex);
        }
        
        // put back old indices
        if (oldIndices != null) {
            arrayPool.Return(oldIndices);
        }
        
        bitsPerIndex = newBitsPerIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int getIndexRaw(int blockCoord, byte[] sourceArray, int sourceBitsPerIndex) {
        if (sourceBitsPerIndex == 0) return 0;
        
        var bitIndex = blockCoord * sourceBitsPerIndex;
        var byteIndex = bitIndex >> 3;
        var bitOffset = bitIndex & 7;
        
        var result = 0;
        var bitsRemaining = sourceBitsPerIndex;
        
        while (bitsRemaining > 0) {
            var bitsInThisByte = Math.Min(8 - bitOffset, bitsRemaining);
            var mask = (1 << bitsInThisByte) - 1;
            var value = (sourceArray[byteIndex] >> bitOffset) & mask;
            
            result |= value << (sourceBitsPerIndex - bitsRemaining);
            
            bitsRemaining -= bitsInThisByte;
            bitOffset = 0;
            byteIndex++;
        }
        
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int calculateBitsPerIndex(int paletteSize) {
        return paletteSize <= 1 ? 0 : 32 - BitOperations.LeadingZeroCount((uint)(paletteSize - 1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int calculateIndicesSize(int bitsPerIndex) {
        return bitsPerIndex == 0 ? 0 : (TOTAL_BLOCKS * bitsPerIndex + 7) >> 3; // ceiling division by 8
    }

    // Light palette methods
    private void initLightPalette() {
        lightPaletteCapacity = INITIAL_LIGHT_PALETTE_SIZE;
        lightPalette = GC.AllocateUninitializedArray<byte>(lightPaletteCapacity);
        lightRefCounts = GC.AllocateUninitializedArray<ushort>(lightPaletteCapacity);
        
        // initialize with 0x00 (no light)
        lightPalette[0] = 0x00;
        lightRefCounts[0] = (ushort)TOTAL_BLOCKS; // all start as no light
        lightPaletteSize = 1;
        lightBitsPerIndex = 0; // single light value needs 0 bits
    }

    private int findOrAddToLightPalette(byte lightValue) {
        // linear search for light values (small palette)
        for (int i = 0; i < lightPaletteSize; i++) {
            if (lightPalette[i] == lightValue) {
                return i;
            }
        }
        
        // need to add to light palette
        if (lightPaletteSize >= lightPaletteCapacity) {
            growLightPalette();
        }
        
        lightPalette[lightPaletteSize] = lightValue;
        lightRefCounts[lightPaletteSize] = 0; // will be incremented by caller
        lightPaletteSize++;
        
        // check if we need to increase bits per index
        var newBitsPerIndex = calculateBitsPerIndex(lightPaletteSize);
        if (newBitsPerIndex != lightBitsPerIndex) {
            resizeLightIndices(newBitsPerIndex);
        }
        
        return lightPaletteSize - 1;
    }

    private void growLightPalette() {
        var newCapacity = lightPaletteCapacity * 2;
        var newPalette = GC.AllocateUninitializedArray<byte>(newCapacity);
        var newRefCounts = GC.AllocateUninitializedArray<ushort>(newCapacity);
        
        Array.Copy(lightPalette, newPalette, lightPaletteSize);
        Array.Copy(lightRefCounts, newRefCounts, lightPaletteSize);
        
        lightPalette = newPalette;
        lightRefCounts = newRefCounts;
        lightPaletteCapacity = newCapacity;
    }

    private void incrementLightRefCount(int paletteIndex) {
        if (lightRefCounts[paletteIndex] < ushort.MaxValue) {
            lightRefCounts[paletteIndex]++;
        }
    }

    private void decrementLightRefCount(int paletteIndex) {
        if (lightRefCounts[paletteIndex] > 0) {
            lightRefCounts[paletteIndex]--;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int getLightIndexRaw(int blockCoord) {
        if (lightBitsPerIndex == 0) return 0; // single light value
        
        var bitIndex = blockCoord * lightBitsPerIndex;
        var byteIndex = bitIndex >> 3;
        var bitOffset = bitIndex & 7;
        
        var result = 0;
        var bitsRemaining = lightBitsPerIndex;
        
        while (bitsRemaining > 0) {
            var bitsInThisByte = Math.Min(8 - bitOffset, bitsRemaining);
            var mask = (1 << bitsInThisByte) - 1;
            var value = (lightIndices![byteIndex] >> bitOffset) & mask;
            
            result |= value << (lightBitsPerIndex - bitsRemaining);
            
            bitsRemaining -= bitsInThisByte;
            bitOffset = 0;
            byteIndex++;
        }
        
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void setLightIndexRaw(int blockCoord, int index) {
        if (lightBitsPerIndex == 0) return; // single light value, nothing to store
        
        var bitIndex = blockCoord * lightBitsPerIndex;
        var byteIndex = bitIndex >> 3;
        var bitOffset = bitIndex & 7;
        
        var bitsRemaining = lightBitsPerIndex;
        
        while (bitsRemaining > 0) {
            var bitsInThisByte = Math.Min(8 - bitOffset, bitsRemaining);
            var mask = (1 << bitsInThisByte) - 1;
            var value = (index >> (lightBitsPerIndex - bitsRemaining)) & mask;
            
            lightIndices![byteIndex] = (byte)(lightIndices[byteIndex] & ~(mask << bitOffset));
            lightIndices[byteIndex] = (byte)(lightIndices[byteIndex] | (value << bitOffset));
            
            bitsRemaining -= bitsInThisByte;
            bitOffset = 0;
            byteIndex++;
        }
    }

    private void resizeLightIndices(int newBitsPerIndex) {
        if (newBitsPerIndex == lightBitsPerIndex) return;
        
        var oldBitsPerIndex = lightBitsPerIndex;
        lightBitsPerIndex = newBitsPerIndex;
        
        // if growing from 0 bits, allocate indices array
        if (oldBitsPerIndex == 0) {
            lightIndicesLength = calculateIndicesSize(newBitsPerIndex);
            lightIndices = arrayPool.Rent(lightIndicesLength);
            Array.Clear(lightIndices, 0, lightIndicesLength);
            return;
        }
        
        // if shrinking to 0 bits, deallocate indices array
        if (newBitsPerIndex == 0) {
            if (lightIndices != null) {
                arrayPool.Return(lightIndices);
                lightIndices = null;
                lightIndicesLength = 0;
            }
            return;
        }
        
        // repack light indices
        var oldIndices = lightIndices;
        var oldLightIndicesLength = lightIndicesLength;
        lightIndicesLength = calculateIndicesSize(newBitsPerIndex);
        lightIndices = arrayPool.Rent(lightIndicesLength);
        Array.Clear(lightIndices, 0, lightIndicesLength);
        
        for (int i = 0; i < TOTAL_BLOCKS; i++) {
            var oldIndex = getLightIndexRaw(i, oldIndices, oldBitsPerIndex);
            setLightIndexRaw(i, oldIndex);
        }
        
        if (oldIndices != null) {
            arrayPool.Return(oldIndices);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int getLightIndexRaw(int blockCoord, byte[] sourceArray, int sourceBitsPerIndex) {
        if (sourceBitsPerIndex == 0) return 0;
        
        var bitIndex = blockCoord * sourceBitsPerIndex;
        var byteIndex = bitIndex >> 3;
        var bitOffset = bitIndex & 7;
        
        var result = 0;
        var bitsRemaining = sourceBitsPerIndex;
        
        while (bitsRemaining > 0) {
            var bitsInThisByte = Math.Min(8 - bitOffset, bitsRemaining);
            var mask = (1 << bitsInThisByte) - 1;
            var value = (sourceArray[byteIndex] >> bitOffset) & mask;
            
            result |= value << (sourceBitsPerIndex - bitsRemaining);
            
            bitsRemaining -= bitsInThisByte;
            bitOffset = 0;
            byteIndex++;
        }
        
        return result;
    }

    private void updateCounts(ushort oldID, ushort newID) {
        // if old was air, new is not
        if (oldID == 0 && newID != 0) {
            blockCount++;
        }
        else if (oldID != 0 && newID == 0) {
            blockCount--;
        }

        var oldTick = Block.randomTick[oldID];
        var tick = Block.randomTick[newID];
        if (!oldTick && tick) {
            randomTickCount++;
        }
        else if (oldTick && !tick) {
            randomTickCount--;
        }
        
        var oldRenderTick = Block.renderTick[oldID];
        var renderTick = Block.renderTick[newID];
        if (!oldRenderTick && renderTick) {
            renderTickCount++;
        }
        else if (oldRenderTick && !renderTick) {
            renderTickCount--;
        }

        var oldFullBlock = Block.isFullBlock(oldID);
        var fullBlock = Block.isFullBlock(newID);
        if (!oldFullBlock && fullBlock) {
            fullBlockCount++;
        }
        else if (oldFullBlock && !fullBlock) {
            fullBlockCount--;
        }

        var oldTranslucent = Block.isTranslucent(oldID);
        var translucent = Block.isTranslucent(newID);
        if (!oldTranslucent && translucent) {
            translucentCount++;
        }
        else if (oldTranslucent && !translucent) {
            translucentCount--;
        }
    }

    private void updateCounts(ushort oldID, ushort newID, int x, int y, int z) {
        updateCounts(oldID, newID);

        // handle heightmap updates with actual coordinates
        var oldFullBlock = Block.isFullBlock(oldID);
        var fullBlock = Block.isFullBlock(newID);
        if (!oldFullBlock && fullBlock) {
            chunk.addToHeightMap(x, (yCoord << 4) + y, z);
        }
        else if (oldFullBlock && !fullBlock) {
            chunk.removeFromHeightMap(x, (yCoord << 4) + y, z);
        }
    }

    public ushort fastGet(int x, int y, int z) {
        var index = getIndex(x, y, z);
        return palette[index].getID();
    }

    /// <summary>
    /// Your responsibility to update the counts after a batch of changes.
    /// </summary>
    public void fastSet(int x, int y, int z, ushort val) {
        var coord = (y << 8) + (z << 4) + x;
        var oldIndex = getIndexRaw(coord);
        var newIndex = findOrAddToPalette(val);
        
        decrementRefCount(oldIndex);
        incrementRefCount(newIndex);
        
        setIndexRaw(coord, newIndex);
    }

    /// <summary>
    /// Kind of like <see cref="fastSet"/>, but doesn't check if the block data is initialized. I've warned you.
    /// </summary>
    public void fastSetUnsafe(int x, int y, int z, ushort val) {
        var coord = (y << 8) + (z << 4) + x;
        var oldIndex = getIndexRaw(coord);
        var newIndex = findOrAddToPalette(val);
        
        decrementRefCount(oldIndex);
        incrementRefCount(newIndex);
        
        setIndexRaw(coord, newIndex);
    }

    public PaletteBlockData(Chunk chunk, int yCoord) {
        this.chunk = chunk;
        this.yCoord = yCoord;
        
        init();
    }

    public void init() {
        paletteCapacity = INITIAL_PALETTE_SIZE;
        palette = GC.AllocateUninitializedArray<uint>(paletteCapacity);
        refCounts = GC.AllocateUninitializedArray<ushort>(paletteCapacity);
        
        // initialize block palette
        palette[0] = 0;
        refCounts[0] = (ushort)TOTAL_BLOCKS; // all blocks start as air
        paletteSize = 1;
        bitsPerIndex = 0; // single block type needs 0 bits
        
        // initialize light palette
        initLightPalette();
        
        inited = true;
    }

    public void loadInit() {
        inited = true;
    }

    public bool isEmpty() {
        return blockCount == 0;
    }

    public bool hasRandomTickingBlocks() {
        return randomTickCount > 0;
    }

    public bool hasRenderTickingBlocks() {
        return renderTickCount > 0;
    }

    public bool isFull() {
        return fullBlockCount == TOTAL_BLOCKS;
    }

    public bool hasTranslucentBlocks() {
        return translucentCount > 0;
    }

    /// <summary>
    /// After loading, the counters will be gone. This method recalculates all of them.
    /// </summary>
    public void refreshCounts() {
        blockCount = 0;
        translucentCount = 0;
        fullBlockCount = 0;
        randomTickCount = 0;
        renderTickCount = 0;

        // rebuild reference counts
        Array.Clear(refCounts, 0, paletteSize);

        for (int i = 0; i < TOTAL_BLOCKS; i++) {
            int x = i & 0xF;
            int z = (i >> 4) & 0xF;
            int y = i >> 8;
            
            var index = getIndexRaw(i);
            var blockID = palette[index].getID();
            
            refCounts[index]++;
            
            if (blockID != 0) {
                blockCount++;
            }

            if (Block.randomTick[blockID]) {
                randomTickCount++;
            }
            
            if (Block.renderTick[blockID]) {
                renderTickCount++;
            }
            
            if (Block.isFullBlock(blockID)) {
                chunk.addToHeightMap(x, (yCoord << 4) + y, z);
                fullBlockCount++;
            }

            if (Block.isTranslucent(blockID)) {
                translucentCount++;
            }
        }
    }

    // Light methods - using palette
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte getLight(int x, int y, int z) {
        var coord = (y << 8) + (z << 4) + x;
        var lightIndex = getLightIndexRaw(coord);
        return lightPalette[lightIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void setLight(int x, int y, int z, byte val) {
        var coord = (y << 8) + (z << 4) + x;
        var oldIndex = getLightIndexRaw(coord);
        var newIndex = findOrAddToLightPalette(val);
        
        decrementLightRefCount(oldIndex);
        incrementLightRefCount(newIndex);
        
        setLightIndexRaw(coord, newIndex);
    }

    public byte skylight(int x, int y, int z) {
        var coord = (y << 8) + (z << 4) + x;
        var lightIndex = getLightIndexRaw(coord);
        var value = lightPalette[lightIndex];
        return (byte)(value & 0xF);
    }

    public byte blocklight(int x, int y, int z) {
        var coord = (y << 8) + (z << 4) + x;
        var lightIndex = getLightIndexRaw(coord);
        var value = lightPalette[lightIndex];
        return (byte)((value >> 4) & 0xF);
    }

    public void setSkylight(int x, int y, int z, byte val) {
        var coord = (y << 8) + (z << 4) + x;
        var oldIndex = getLightIndexRaw(coord);
        var oldValue = lightPalette[oldIndex];
        var blocklight = (byte)((oldValue >> 4) & 0xF);
        var newValue = (byte)((blocklight << 4) | val);
        
        var newIndex = findOrAddToLightPalette(newValue);
        decrementLightRefCount(oldIndex);
        incrementLightRefCount(newIndex);
        setLightIndexRaw(coord, newIndex);
    }

    public void setBlocklight(int x, int y, int z, byte val) {
        var coord = (y << 8) + (z << 4) + x;
        var oldIndex = getLightIndexRaw(coord);
        var oldValue = lightPalette[oldIndex];
        var skylight = (byte)(oldValue & 0xF);
        var newValue = (byte)((val << 4) | skylight);
        
        var newIndex = findOrAddToLightPalette(newValue);
        decrementLightRefCount(oldIndex);
        incrementLightRefCount(newIndex);
        setLightIndexRaw(coord, newIndex);
    }

    public static byte extractSkylight(byte value) {
        return (byte)(value & 0xF);
    }

    public static byte extractBlocklight(byte value) {
        return (byte)((value >> 4) & 0xF);
    }

    // methods for serialization compatibility with WorldIO
    public void getSerializationBlocks(uint[] blocks) {
        // decompress palette data back to raw format
        for (int i = 0; i < TOTAL_BLOCKS; i++) {
            var index = getIndexRaw(i);
            blocks[i] = palette[index];
        }
    }
    
    /**
     * Need a 4096 length byte array to write into!
     */
    public void getSerializationLight(byte[] light) {
        // decompress light palette back to raw format for serialization
        for (int i = 0; i < TOTAL_BLOCKS; i++) {
            var lightIndex = getLightIndexRaw(i);
            light[i] = lightPalette[lightIndex];
        }
    }

    public void setSerializationData(uint[] blocks, byte[] lightData) {
        // initialize block palette
        paletteCapacity = INITIAL_PALETTE_SIZE;
        palette = GC.AllocateUninitializedArray<uint>(paletteCapacity);
        refCounts = GC.AllocateUninitializedArray<ushort>(paletteCapacity);
        
        // initialize light palette
        initLightPalette();
        
        // convert raw light data to light palette
        for (int i = 0; i < lightData.Length && i < TOTAL_BLOCKS; i++) {
            var lightValue = lightData[i];
            var lightIndex = findOrAddToLightPalette(lightValue);
            incrementLightRefCount(lightIndex);
            setLightIndexRaw(i, lightIndex);
        }
        
        // start with air in palette
        palette[0] = 0;
        refCounts[0] = 0;
        paletteSize = 1;
        bitsPerIndex = 0;
        
        indicesLength = calculateIndicesSize(bitsPerIndex);
        indices = arrayPool.Rent(indicesLength);
        Array.Clear(indices, 0, indicesLength);
        
        for (int i = 0; i < blocks.Length; i++) {
            var value = blocks[i];
            var index = findOrAddToPalette(value);
            setIndexRaw(i, index);
        }
        inited = true;
        
        refreshCounts();
    }

    // cleanup
    private void ReleaseUnmanagedResources() {
        if (indices != null) {
            arrayPool.Return(indices);
            indices = null;
        }

        if (lightIndices != null) {
            arrayPool.Return(lightIndices);
            lightIndices = null;
        }
        
        // palettes and refCounts are not pooled - they're dynamically allocated
    }

    public void Dispose() {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~PaletteBlockData() {
        ReleaseUnmanagedResources();
    }
}