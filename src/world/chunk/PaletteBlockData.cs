using System.Numerics;
using System.Runtime.CompilerServices;
using BlockGame.net.packet;
using BlockGame.util;
using BlockGame.world.block;

namespace BlockGame.world.chunk;

public sealed class PaletteBlockData : BlockData, IDisposable {
    public static readonly VariableArrayPool<byte> arrayPool = new();
    public static readonly VariableArrayPool<ushort> arrayPoolUS = new();
    public static readonly VariableArrayPool<uint> arrayPoolU = new();

    private uint[] vertices;
    private byte[]? indices;
    private ushort[] blockRefs;
    private int count;
    private int vertCount;
    private int vertCapacity;
    private int density;
    
    // light vertices
    private byte[] lightVertices;
    private byte[]? lightIndices;
    private ushort[] lightRefs;
    private int lightCount;
    private int lightVertCount;
    private int lightVertCapacity;
    private int lightDensity;

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
    private const int INITIAL_SIZE = 2;
    private const int SMALL_ARRAY = 16;
    private const int INITIAL_LIGHT_SIZE = 2;

    // YZX because the internet said so
    public ushort this[int x, int y, int z] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            var index = getIndex(x, y, z);
            return vertices[index].getID();
        }
        set {
            var coord = (y << 8) + (z << 4) + x;
            var oldIdx = getIndexRaw(coord);
            var oldValue = vertices[oldIdx];
            var oldID = oldValue.getID();
            
            var newBlock = value;
            var newIdx = get(newBlock);
            
            decrefcount(blockRefs, oldIdx);
            increfcount(blockRefs, newIdx);
            
            setIndexRaw(coord, newIdx);
            
            updateCounts(oldID, value, x, y, z);
            
            tryCompact();
        }
    }

    public uint getRaw(int x, int y, int z) {
        var index = getIndex(x, y, z);
        return vertices[index];
    }

    public void setRaw(int x, int y, int z, uint value) {
        var coord = (y << 8) + (z << 4) + x;
        var oldIndex = getIndexRaw(coord);
        var oldValue = vertices[oldIndex];
        var oldID = oldValue.getID();
        
        var newIndex = get(value);
        
        decrefcount(blockRefs, oldIndex);
        increfcount(blockRefs, newIndex);
        
        setIndexRaw(coord, newIndex);
        
        var newID = value.getID();
        updateCounts(oldID, newID, x, y, z);
        
        tryCompact();
    }

    public byte getMetadata(int x, int y, int z) {
        var index = getIndex(x, y, z);
        return vertices[index].getMetadata();
    }

    public void setMetadata(int x, int y, int z, byte val) {
        var coord = (y << 8) + (z << 4) + x;
        var oldIndex = getIndexRaw(coord);
        var oldValue = vertices[oldIndex];
        
        var newValue = oldValue.setMetadata(val);
        
        var newIdx = get(newValue);
        
        decrefcount(blockRefs, oldIndex);
        increfcount(blockRefs, newIdx);
        
        setIndexRaw(coord, newIdx);
        
        tryCompact();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int getIndex(int x, int y, int z) {
        return getIndexRaw((y << 8) + (z << 4) + x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int getIndexRaw(int coord) {
        return getIndexRaw(coord, indices, density);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void setIndexRaw(int coord, int index) {
        setIndexRaw(coord, index, indices, density);
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int getLightIndexRaw(int blockCoord) {
        return getIndexRaw(blockCoord, lightIndices, lightDensity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void setLightIndexRaw(int blockCoord, int index) {
        setIndexRaw(blockCoord, index, lightIndices, lightDensity);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int getIndexRaw(int coord, byte[]? src, int bits) {
        switch (bits) {
            case 0:
                return 0;
            case 1:
                return (src![coord >> 3] >> (coord & 7)) & 1;
            case 2:
                return (src![coord >> 2] >> ((coord & 3) << 1)) & 3;
            case 4:
                return (src![coord >> 1] >> ((coord & 1) << 2)) & 15;
            case 8:
                return src![coord];
        }

        var bitIndex = coord * bits;
        var i = bitIndex >> 3;
        var bitOffset = bitIndex & 7;
        
        var result = 0;
        var rem = bits;
        
        while (rem > 0) {
            var theseBits = Math.Min(8 - bitOffset, rem);
            var mask = (1 << theseBits) - 1;
            var val = (src![i] >> bitOffset) & mask;
            
            result |= val << (bits - rem);
            
            rem -= theseBits;
            bitOffset = 0;
            i++;
        }
        
        return result;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void setIndexRaw(int coord, int index, byte[] dest, int bits) {
        switch (bits) {
            case 0: 
                return;
            case 1: 
                dest[coord >> 3] = (byte)((dest[coord >> 3] & ~(1 << (coord & 7))) | ((index & 1) << (coord & 7))); 
                return;
            case 2: 
                dest[coord >> 2] = (byte)((dest[coord >> 2] & ~(3 << ((coord & 3) << 1))) | ((index & 3) << ((coord & 3) << 1))); 
                return;
            case 4: 
                dest[coord >> 1] = (byte)((dest[coord >> 1] & ~(15 << ((coord & 1) << 2))) | ((index & 15) << ((coord & 1) << 2)));
                return;
            case 8: 
                dest[coord] = (byte)index; 
                return;
        }
        
        var bitIndex = coord * bits;
        var i = bitIndex >> 3;
        var bitOffset = bitIndex & 7;
        
        var rem = bits;
        
        while (rem > 0) {
            var theseBits = Math.Min(8 - bitOffset, rem);
            var mask = (1 << theseBits) - 1;
            var val = (index >> (bits - rem)) & mask;
            
            dest[i] = (byte)((dest[i] & ~(mask << bitOffset)) | (val << bitOffset));
            
            rem -= theseBits;
            bitOffset = 0;
            i++;
        }
    }

    private int get(uint blockValue) {
        // todo maybe use a dict for this? we just search linearly for now
        for (int i = 0; i < vertCount; i++) {
            if (vertices[i] == blockValue) {
                return i;
            }
        }
        
        // too big, grow
        if (vertCount >= vertCapacity) {
            grow();
        }
        
        vertices[vertCount] = blockValue;
        blockRefs[vertCount] = 0; // will be incremented by the caller!
        vertCount++;
        
        // check if we need to resize
        var newBits = bitsPerIdx(vertCount);
        if (newBits != density) {
            resizeIndices(newBits, ref indices, ref count, ref density);
        }
        
        return vertCount - 1;
    }
    
    private int getLight(byte lightValue) {
        for (int i = 0; i < lightVertCount; i++) {
            if (lightVertices[i] == lightValue) {
                return i;
            }
        }
        
        if (lightVertCount >= lightVertCapacity) {
            growLight();
        }
        
        lightVertices[lightVertCount] = lightValue;
        lightRefs[lightVertCount] = 0; // will be incremented by caller!
        lightVertCount++;
        
        // check if we need to resize
        var newBits = bitsPerIdx(lightVertCount);
        if (newBits != lightDensity) {
            resizeIndices(newBits, ref lightIndices, ref lightCount, ref lightDensity);
        }
        
        return lightVertCount - 1;
    }

    private void grow() {
        grow(arrayPoolU, ref vertices, ref blockRefs, ref vertCapacity, vertCount);
    }
    
    private void growLight() {
        grow(arrayPool, ref lightVertices, ref lightRefs, ref lightVertCapacity, lightVertCount);
    }
    
    private static void grow<T>(VariableArrayPool<T> pool, ref T[] verticesArray, ref ushort[] refsArray, 
                               ref int capacity, int count) {
        var newCapacity = capacity * 2;
        var newVertices = pool.grab(newCapacity);
        var newRefCounts = arrayPoolUS.grab(newCapacity);
        
        Array.Copy(verticesArray, newVertices, count);
        Array.Copy(refsArray, newRefCounts, count);
        
        if (verticesArray != null) {
            pool.putBack(verticesArray);
        }
        
        if (refsArray != null) {
            arrayPoolUS.putBack(refsArray);
        }
        
        verticesArray = newVertices;
        refsArray = newRefCounts;
        capacity = newCapacity;
    }


    private void tryCompact(bool isLight = false) {
        var count = isLight ? lightVertCount : vertCount;
        var refs = isLight ? lightRefs : blockRefs;
        
        if (count <= SMALL_ARRAY) {
            return;
        }

        var unused = 0;
        for (int i = 0; i < count; i++) {
            if (refs[i] == 0) {
                unused++;
            }
        }
        
        if (unused >= count / 4) {
            if (isLight) compactLight(); else compact();
        }
    }

    private void compact() {
        compact(vertices, blockRefs, ref vertCount, ref indices, ref count, ref density, "vertices");
    }
    
    private void compactLight() {
        compact(lightVertices, lightRefs, ref lightVertCount, ref lightIndices, ref lightCount, ref lightDensity, "light vertices");
    }
    
    private static void compact<T>(T[] verticesArray, ushort[] refsArray, ref int count, 
                                  ref byte[]? indicesArray, ref int indicesLength, ref int bits, 
                                  string errorName) {
        Span<int> remapping = count <= 1024 
            ? stackalloc int[count] 
            : GC.AllocateUninitializedArray<int>(count);
            
        var newSize = 0;
        
        // build remapping table
        for (int i = 0; i < count; i++) {
            if (refsArray[i] > 0) {
                remapping[i] = newSize;
                if (newSize != i) {
                    verticesArray[newSize] = verticesArray[i];
                    refsArray[newSize] = refsArray[i];
                }
                newSize++;
            } else {
                remapping[i] = -1; // unused entry
            }
        }
        
        if (newSize == count) {
            return; // nothing to compact
        }

        count = newSize;
        
        // update all indices in the chunk
        for (int i = 0; i < TOTAL_BLOCKS; i++) {
            var oldIndex = getIndexRaw(i, indicesArray, bits);
            var newIndex = remapping[oldIndex];
            if (newIndex == -1) {
                SkillIssueException.throwNew($"Found reference to unused {errorName} entry");
            }
            setIndexRaw(i, newIndex, indicesArray, bits);
        }
        
        // check if we can reduce bits per index
        var newBits = bitsPerIdx(newSize);
        if (newBits < bits) {
            resizeIndices(newBits, ref indicesArray, ref indicesLength, ref bits);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int bitsPerIdx(int size) {
        return size <= 1 ? 0 : 32 - BitOperations.LeadingZeroCount((uint)(size - 1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int getIndicesSize(int bits) {
        return bits == 0 ? 0 : (TOTAL_BLOCKS * bits + 7) >> 3; // ceiling division by 8
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void increfcount(ushort[] refCounts, int index) {
        if (refCounts[index] < ushort.MaxValue) {
            refCounts[index]++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void decrefcount(ushort[] refCounts, int index) {
        if (refCounts[index] > 0) {
            refCounts[index]--;
        }
    }

    
    private static void resizeIndices(int newBits, ref byte[]? indices, ref int indicesLength, ref int bits) {
        if (newBits == bits) return;
        
        var oldBits = bits;
        
        // if growing from 0 bits, allocate indices array
        if (oldBits == 0) {
            indicesLength = getIndicesSize(newBits);
            indices = arrayPool.grab(indicesLength);
            Array.Clear(indices, 0, indicesLength);
            bits = newBits;
            return;
        }
        
        // if shrinking to 0 bits, deallocate indices array
        if (newBits == 0) {
            if (indices != null) {
                arrayPool.putBack(indices);
                indices = null;
                indicesLength = 0;
            }
            bits = newBits;
            return;
        }
        
        // repack indices
        var oldIndices = indices;
        var oldIndicesLength = indicesLength;
        indicesLength = getIndicesSize(newBits);
        indices = arrayPool.grab(indicesLength);
        Array.Clear(indices, 0, indicesLength);
        
        for (int i = 0; i < TOTAL_BLOCKS; i++) {
            var oldIndex = getIndexRaw(i, oldIndices, oldBits);
            setIndexRaw(i, oldIndex, indices, newBits);
        }
        
        if (oldIndices != null) {
            arrayPool.putBack(oldIndices);
        }
        
        bits = newBits;
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
        return vertices[index].getID();
    }

    /// <summary>
    /// Your responsibility to update the counts after a batch of changes.
    /// </summary>
    public void fastSet(int x, int y, int z, ushort val) {
        var coord = (y << 8) + (z << 4) + x;
        var oldIndex = getIndexRaw(coord);
        var newIndex = get(val);
        
        decrefcount(blockRefs, oldIndex);
        increfcount(blockRefs, newIndex);
        
        setIndexRaw(coord, newIndex);
    }

    /// <summary>
    /// Kind of like <see cref="fastSet"/>, but doesn't check if the block data is initialized. I've warned you.
    /// </summary>
    public void fastSetUnsafe(int x, int y, int z, ushort val) {
        var coord = (y << 8) + (z << 4) + x;
        var oldIndex = getIndexRaw(coord);
        var newIndex = get(val);
        
        decrefcount(blockRefs, oldIndex);
        increfcount(blockRefs, newIndex);
        
        setIndexRaw(coord, newIndex);
    }

    public PaletteBlockData(Chunk chunk, int yCoord) {
        this.chunk = chunk;
        this.yCoord = yCoord;
        
        init();
    }

    public void init() {
        vertCapacity = INITIAL_SIZE;
        vertices = arrayPoolU.grab(vertCapacity);
        blockRefs = arrayPoolUS.grab(vertCapacity);
        
        
        vertices[0] = 0;
        blockRefs[0] = TOTAL_BLOCKS; // all blocks start as air
        vertCount = 1;
        density = 0;
        
        // initialize light vertices
        lightVertCapacity = INITIAL_LIGHT_SIZE;
        lightVertices = arrayPool.grab(lightVertCapacity);
        lightRefs = arrayPoolUS.grab(lightVertCapacity);
        
        lightVertices[0] = 0;
        lightRefs[0] = TOTAL_BLOCKS; // all start as no light
        lightVertCount = 1;
        lightDensity = 0;
        
        inited = true;
    }

    public void loadInit() {
        // inited will be set by setSerializationData after arrays are initialized
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
        Array.Clear(blockRefs, 0, vertCount);
        Array.Clear(lightRefs, 0, lightVertCount);

        for (int i = 0; i < TOTAL_BLOCKS; i++) {
            int x = i & 0xF;
            int z = (i >> 4) & 0xF;
            int y = i >> 8;
            
            var index = getIndexRaw(i);
            var blockID = vertices[index].getID();
            
            blockRefs[index]++;
            
            var lightIndex = getLightIndexRaw(i);
            lightRefs[lightIndex]++;
            
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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte getLight(int x, int y, int z) {
        var coord = (y << 8) + (z << 4) + x;
        var lightIndex = getLightIndexRaw(coord);
        return lightVertices[lightIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void setLight(int x, int y, int z, byte val) {
        var coord = (y << 8) + (z << 4) + x;
        var oldIndex = getLightIndexRaw(coord);
        var newIndex = getLight(val);
        
        decrefcount(lightRefs, oldIndex);
        increfcount(lightRefs, newIndex);
        
        setLightIndexRaw(coord, newIndex);
        
        tryCompact(true);
    }

    public byte skylight(int x, int y, int z) {
        var coord = (y << 8) + (z << 4) + x;
        var lightIndex = getLightIndexRaw(coord);
        var value = lightVertices[lightIndex];
        return (byte)(value & 0xF);
    }

    public byte blocklight(int x, int y, int z) {
        var coord = (y << 8) + (z << 4) + x;
        var lightIndex = getLightIndexRaw(coord);
        var value = lightVertices[lightIndex];
        return (byte)((value >> 4) & 0xF);
    }

    public void setSkylight(int x, int y, int z, byte val) {
        var coord = (y << 8) + (z << 4) + x;
        var oldIndex = getLightIndexRaw(coord);
        var oldValue = lightVertices[oldIndex];
        var blocklight = (byte)((oldValue >> 4) & 0xF);
        var newValue = (byte)((blocklight << 4) | val);
        
        var newIndex = getLight(newValue);
        decrefcount(lightRefs, oldIndex);
        increfcount(lightRefs, newIndex);
        setLightIndexRaw(coord, newIndex);
        
        tryCompact(true);
    }

    public void setBlocklight(int x, int y, int z, byte val) {
        var coord = (y << 8) + (z << 4) + x;
        var oldIndex = getLightIndexRaw(coord);
        var oldValue = lightVertices[oldIndex];
        var skylight = (byte)(oldValue & 0xF);
        var newValue = (byte)((val << 4) | skylight);
        
        var newIndex = getLight(newValue);
        decrefcount(lightRefs, oldIndex);
        increfcount(lightRefs, newIndex);
        setLightIndexRaw(coord, newIndex);
        
        tryCompact(true);
    }

    // methods for serialization compatibility with WorldIO
    public void getSerializationBlocks(uint[] blocks) {
        for (int i = 0; i < TOTAL_BLOCKS; i++) {
            var index = getIndexRaw(i);
            blocks[i] = vertices[index];
        }
    }
    
    /**
     * Need a 4096 length byte array to write into!
     */
    public void getSerializationLight(byte[] light) {
        for (int i = 0; i < TOTAL_BLOCKS; i++) {
            var lightIndex = getLightIndexRaw(i);
            light[i] = lightVertices[lightIndex];
        }
    }

    public void setSerializationData(uint[] blocks, byte[] lightData) {
        
        // if old ones exist, dispose
        ReleaseUnmanagedResources();
        
        // initialize arrays
        vertCapacity = INITIAL_SIZE;
        vertices = arrayPoolU.grab(vertCapacity);
        blockRefs = arrayPoolUS.grab(vertCapacity);
        
        lightVertCapacity = INITIAL_LIGHT_SIZE;
        lightVertices = arrayPool.grab(lightVertCapacity);
        lightRefs = arrayPoolUS.grab(lightVertCapacity);
        
        // reset counters
        vertices[0] = 0; // air block
        blockRefs[0] = 0; // will be set correctly by palette loading
        vertCount = 1;
        density = 0;
        
        lightVertices[0] = 0x00; // no light
        lightRefs[0] = 0; // will be set correctly by palette loading
        lightVertCount = 1;
        lightDensity = 0;
        
        // allocate initial indices
        count = getIndicesSize(density);
        if (count > 0) {
            indices = arrayPool.grab(count);
            Array.Clear(indices, 0, count);
        }
        else {
            indices = null;
        }
        
        // allocate initial light indices
        lightCount = getIndicesSize(lightDensity);
        if (lightCount > 0) {
            lightIndices = arrayPool.grab(lightCount);
            Array.Clear(lightIndices, 0, lightCount);
        }
        else {
            lightIndices = null;
        }


        // load block data
        for (int i = 0; i < blocks.Length; i++) {
            var index = get(blocks[i]);
            setIndexRaw(i, index);
        }
        
        // load light data
        for (int i = 0; i < lightData.Length && i < TOTAL_BLOCKS; i++) {
            var lightIndex = getLight(lightData[i]);
            setLightIndexRaw(i, lightIndex);
        }
        
        inited = true;
        refreshCounts();
    }

    // cleanup
    private void ReleaseUnmanagedResources() {
        if (indices != null) {
            arrayPool.putBack(indices);
            indices = null;
        }

        if (lightIndices != null) {
            arrayPool.putBack(lightIndices);
            lightIndices = null;
        }

        if (vertices != null) {
            arrayPoolU.putBack(vertices);
            vertices = null;
        }

        if (blockRefs != null) {
            arrayPoolUS.putBack(blockRefs);
            blockRefs = null;
        }

        if (lightVertices != null) {
            arrayPool.putBack(lightVertices);
            lightVertices = null;
        }

        if (lightRefs != null) {
            arrayPoolUS.putBack(lightRefs);
            lightRefs = null;
        }

        // reset state to prevent access to disposed arrays
        inited = false;
    }

    public void Dispose() {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~PaletteBlockData() {
        ReleaseUnmanagedResources();
    }
    
    public ChunkDataPacket.SubChunkData write(byte y) {
        return new ChunkDataPacket.SubChunkData {
            y = y,
            vertices = vertices,
            blockRefs = blockRefs,
            indices = indices,
            count = count,
            vertCount = vertCount,
            density = density,
            lightVertices = lightVertices,
            lightRefs = lightRefs,
            lightIndices = lightIndices,
            lightCount = lightCount,
            lightVertCount = lightVertCount,
            lightDensity = lightDensity,
            blockCount = blockCount,
            translucentCount = translucentCount,
            fullBlockCount = fullBlockCount,
            randomTickCount = randomTickCount,
            renderTickCount = renderTickCount
        };
    }
    
    public void read(ChunkDataPacket.SubChunkData data) {
        // dispose old arrays if they exist (to prevent a nice memory leak)
        if (inited) {
            ReleaseUnmanagedResources();
        }

        vertices = data.vertices;
        blockRefs = data.blockRefs;
        indices = data.indices;
        count = data.count;
        vertCount = data.vertCount;
        density = data.density;
        lightVertices = data.lightVertices;
        lightRefs = data.lightRefs;
        lightIndices = data.lightIndices;
        lightCount = data.lightCount;
        lightVertCount = data.lightVertCount;
        lightDensity = data.lightDensity;
        blockCount = data.blockCount;
        translucentCount = data.translucentCount;
        fullBlockCount = data.fullBlockCount;
        vertCapacity = vertices.Length;
        lightVertCapacity = lightVertices.Length;
        randomTickCount = data.randomTickCount;
        renderTickCount = data.renderTickCount;
        inited = true;
    }
}