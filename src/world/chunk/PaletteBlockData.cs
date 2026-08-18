using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

    private byte[]? skyChannel;
    private byte[]? blockChannel;
    private byte skyValue;
    private byte blockValue;

    private const int LIGHT_PLANE_BYTES = TOTAL_BLOCKS / 2;

    public int blockCount;
    public int translucentCount;
    public int fullBlockCount;
    public int randomTickCount;
    public int renderTickCount;
    public int lightSourceCount;

    /// <summary>
    /// Has the block storage been initialized?
    /// </summary>
    public bool inited { get; set; }

    public Chunk chunk;
    public int yCoord;

    private const int TOTAL_BLOCKS = Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE;
    private const int TOTAL_BIOMES = Chunk.BIOMESIZE * Chunk.BIOMESIZE * Chunk.BIOMESIZE;
    private const int INITIAL_SIZE = 2;

    /** Pan can't write memory-safe code WHATSOEVER so you know what, let's just oversize the array so we don't crash out */
    private const int INDEXSLOP = 4;
    private const int SMALL_ARRAY = 16;

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
            
            var freed = decrefcount(blockRefs, oldIdx);
            increfcount(blockRefs, newIdx);
            
            setIndexRaw(coord, newIdx);
            
            updateCounts(oldID, value, x, y, z);
            
            if (freed) {
                tryCompact();
            }
        }
    }

    public uint getRaw(int x, int y, int z) {
        var index = getIndex(x, y, z);
        return vertices[index];
    }

    /**
     * Decode dst.Length consecutive block coords starting at `coord` in (YZX) order.
     *
     * @param coord The starting block coordinate (0-4095) to read from.
     * @param dst The destination span to write the decoded block values into. Must be at least 1 element long.
     */
    public void getRawBatch(int coord, Span<uint> dst) {
        var pal = vertices;
        var src = indices;
        var n = dst.Length;

        // unaligned or exotic packing just do it the slow way
        if (density is not (0 or 1 or 2 or 4 or 8) || (coord & 7) != 0 || (n & 7) != 0) {
            for (var i = 0; i < n; i++) {
                dst[i] = pal[getIndexRaw(coord + i, src, density)];
            }
            return;
        }

        switch (density) {
            case 0:
                dst.Fill(pal[0]);
                return;
            case 8:
                for (var i = 0; i < n; i++) {
                    dst[i] = pal[src![coord + i]];
                }
                return;
            case 4: {
                var b = coord >> 1;
                for (var i = 0; i < n; i += 2, b++) {
                    var p = src![b];
                    dst[i] = pal[p & 15];
                    dst[i + 1] = pal[p >> 4];
                }
                return;
            }
            case 2: {
                var b = coord >> 2;
                for (var i = 0; i < n; i += 4, b++) {
                    var p = src![b];
                    dst[i] = pal[p & 3];
                    dst[i + 1] = pal[(p >> 2) & 3];
                    dst[i + 2] = pal[(p >> 4) & 3];
                    dst[i + 3] = pal[p >> 6];
                }
                return;
            }
            default: { // 1
                var b = coord >> 3;
                for (var i = 0; i < n; i += 8, b++) {
                    var p = src![b];
                    dst[i] = pal[p & 1];
                    dst[i + 1] = pal[(p >> 1) & 1];
                    dst[i + 2] = pal[(p >> 2) & 1];
                    dst[i + 3] = pal[(p >> 3) & 1];
                    dst[i + 4] = pal[(p >> 4) & 1];
                    dst[i + 5] = pal[(p >> 5) & 1];
                    dst[i + 6] = pal[(p >> 6) & 1];
                    dst[i + 7] = pal[p >> 7];
                }
                return;
            }
        }
    }

    // ---- light plane primitives -------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte getNibble(byte[] plane, int coord) {
        return (byte)((plane[coord >> 1] >> ((coord & 1) << 2)) & 0xF);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void setNibble(byte[] plane, int coord, byte val) {
        ref var b = ref plane[coord >> 1];
        var sh = (coord & 1) << 2;
        b = (byte)((b & ~(0xF << sh)) | (val << sh));
    }

    /** one value -> actual array */
    private static byte[] explode(byte uniform) {
        var p = arrayPool.grab(LIGHT_PLANE_BYTES);
        p.AsSpan(0, LIGHT_PLANE_BYTES).Fill((byte)(uniform | (uniform << 4)));
        return p;
    }

    private static bool isUniform(byte[] plane, out byte val) {
        var first = plane[0];
        val = (byte)(first & 0xF);
        if ((first >> 4) != val) {
            return false;
        }

        return !plane.AsSpan(0, LIGHT_PLANE_BYTES).ContainsAnyExcept(first);
    }

    /**
     * Decode dst.Length consecutive light bytes (blocklight in the high nibble, skylight in the low)
     * starting at `coord`.
     */
    public void getLightBatch(int coord, Span<byte> dst) {
        var sky = skyChannel;
        var bl = blockChannel;
        var n = dst.Length;

        if (sky == null && bl == null) {
            dst.Fill((byte)((blockValue << 4) | skyValue));
            return;
        }

        // both planes real
        if (sky != null && bl != null) {
            if ((coord & 1) == 0 && (n & 1) == 0) {
                var b = coord >> 1;
                for (var i = 0; i < n; i += 2, b++) {
                    int s = sky[b], k = bl[b];
                    dst[i] = (byte)(((k & 0xF) << 4) | (s & 0xF));
                    dst[i + 1] = (byte)((k & 0xF0) | (s >> 4));
                }

                return;
            }

            for (var i = 0; i < n; i++) {
                var c = coord + i;
                dst[i] = (byte)((getNibble(bl, c) << 4) | getNibble(sky, c));
            }

            return;
        }

        if (bl == null) {
            var hi = (byte)(blockValue << 4);
            if ((coord & 1) == 0 && (n & 1) == 0) {
                var b = coord >> 1;
                for (var i = 0; i < n; i += 2, b++) {
                    int s = sky![b];
                    dst[i] = (byte)(hi | (s & 0xF));
                    dst[i + 1] = (byte)(hi | (s >> 4));
                }

                return;
            }

            for (var i = 0; i < n; i++) {
                dst[i] = (byte)(hi | getNibble(sky!, coord + i));
            }

            return;
        }

        {
            var lo = skyValue;
            if ((coord & 1) == 0 && (n & 1) == 0) {
                var b = coord >> 1;
                for (var i = 0; i < n; i += 2, b++) {
                    int k = bl[b];
                    dst[i] = (byte)(((k & 0xF) << 4) | lo);
                    dst[i + 1] = (byte)((k & 0xF0) | lo);
                }

                return;
            }

            for (var i = 0; i < n; i++) {
                dst[i] = (byte)((getNibble(bl, coord + i) << 4) | lo);
            }
        }
    }

    public void setRaw(int x, int y, int z, uint value) {
        var coord = (y << 8) + (z << 4) + x;
        var oldIndex = getIndexRaw(coord);
        var oldValue = vertices[oldIndex];
        var oldID = oldValue.getID();
        
        var newIndex = get(value);
        
        var freed = decrefcount(blockRefs, oldIndex);
        increfcount(blockRefs, newIndex);
        
        setIndexRaw(coord, newIndex);
        
        var newID = value.getID();
        updateCounts(oldID, newID, x, y, z);
        
        if (freed) {
            tryCompact();
        }
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
        
        var freed = decrefcount(blockRefs, oldIndex);
        increfcount(blockRefs, newIdx);
        
        setIndexRaw(coord, newIdx);
        
        if (freed) {
            tryCompact();
        }
    }

    private int getIndex(int x, int y, int z) {
        return getIndexRaw((y << 8) + (z << 4) + x);
    }

    private int getIndexRaw(int coord) {
        return getIndexRaw(coord, indices, density);
    }

    private void setIndexRaw(int coord, int index) {
        setIndexRaw(coord, index, indices, density);
    }
    


    /**
     * The trick is that one unaligned uint load covers EVERY bit width because bitsPerIdx tops out at 12 which is 12 + 7 = 19 bits and
     * always fits in the 32 bits we read. So we don't need to do the switchcaseslop anymore, we just "adjust" the result to clip it in the end.
     * Yes this will overread, hence the <see cref="INDEXSLOP"/> constant
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int getIndexRaw(int coord, byte[]? src, int bits) {
        if (bits == 0) {
            return 0;
        }

        var bitIndex = coord * bits;
        ref var b = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(src!), bitIndex >> 3);
        return (int)((Unsafe.ReadUnaligned<uint>(ref b) >> (bitIndex & 7)) & ((1u << bits) - 1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void setIndexRaw(int coord, int index, byte[]? dest, int bits) {
        if (bits == 0) {
            return;
        }

        var bitIndex = coord * bits;
        ref var b = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(dest!), bitIndex >> 3);
        var sh = bitIndex & 7;
        var mask = ((1u << bits) - 1) << sh;
        var v = Unsafe.ReadUnaligned<uint>(ref b);
        Unsafe.WriteUnaligned(ref b, (v & ~mask) | ((uint)index << sh));
    }

    private int lastidx;

    private int get(uint blockValue) {
        if (lastidx < vertCount && vertices[lastidx] == blockValue) {
            return lastidx;
        }

        // todo maybe use a dict for this? we just search linearly for now
        for (int i = 0; i < vertCount; i++) {
            if (vertices[i] == blockValue) {
                lastidx = i;
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
        
        lastidx = vertCount - 1;
        return lastidx;
    }
    

    private void grow() {
        grow(arrayPoolU, ref vertices, ref blockRefs, ref vertCapacity, vertCount);
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


    private void tryCompact() {
        if (vertCount <= SMALL_ARRAY) {
            return;
        }

        var unused = 0;
        for (int i = 0; i < vertCount; i++) {
            if (blockRefs[i] == 0) {
                unused++;
            }
        }

        if (unused >= vertCount / 4) {
            compact();
        }
    }

    private void compact() {
        compact(vertices, blockRefs, ref vertCount, ref indices, ref count, ref density, "vertices");
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
    private static byte[] grabIndices(int len) {
        var a = arrayPool.grab(len + INDEXSLOP);
        Array.Clear(a, 0, len + INDEXSLOP);
        return a;
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

    /** true if this brought the count to zero - the only event that can make compaction worthwhile */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool decrefcount(ushort[] refCounts, int index) {
        if (refCounts[index] > 0) {
            return --refCounts[index] == 0;
        }
        return false;
    }

    
    private static void resizeIndices(int newBits, ref byte[]? indices, ref int indicesLength, ref int bits) {
        if (newBits == bits) {
            return;
        }

        var oldBits = bits;
        
        // if growing from 0 bits, allocate indices array
        if (oldBits == 0) {
            indicesLength = getIndicesSize(newBits);
            indices = grabIndices(indicesLength);
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
        indices = grabIndices(indicesLength);

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

        var oldEmits = Block.lightLevel[oldID] > 0;
        var emits = Block.lightLevel[newID] > 0;
        if (!oldEmits && emits) {
            lightSourceCount++;
        }
        else if (oldEmits && !emits) {
            lightSourceCount--;
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

        skyChannel = null;
        blockChannel = null;
        skyValue = 0;
        blockValue = 0;
        
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

    public bool hasLightSources() {
        return lightSourceCount > 0;
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
        lightSourceCount = 0;

        // rebuild reference counts
        Array.Clear(blockRefs, 0, vertCount);

        for (int i = 0; i < TOTAL_BLOCKS; i++) {
            int x = i & 0xF;
            int z = (i >> 4) & 0xF;
            int y = i >> 8;
            
            var index = getIndexRaw(i);
            var blockID = vertices[index].getID();
            
            blockRefs[index]++;

            if (blockID != 0) {
                blockCount++;
            }

            if (Block.randomTick[blockID]) {
                randomTickCount++;
            }
            
            if (Block.renderTick[blockID]) {
                renderTickCount++;
            }

            if (Block.lightLevel[blockID] > 0) {
                lightSourceCount++;
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
        var s = skyChannel == null ? skyValue : getNibble(skyChannel, coord);
        var b = blockChannel == null ? blockValue : getNibble(blockChannel, coord);
        return (byte)((b << 4) | s);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void setLight(int x, int y, int z, byte val) {
        setSkylight(x, y, z, (byte)(val & 0xF));
        setBlocklight(x, y, z, (byte)(val >> 4));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte skylight(int x, int y, int z) {
        var coord = (y << 8) + (z << 4) + x;
        return skyChannel == null ? skyValue : getNibble(skyChannel, coord);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte blocklight(int x, int y, int z) {
        var coord = (y << 8) + (z << 4) + x;
        return blockChannel == null ? blockValue : getNibble(blockChannel, coord);
    }

    public void setSkylight(int x, int y, int z, byte val) {
        var coord = (y << 8) + (z << 4) + x;
        var p = skyChannel;
        if (p == null) {
            // still uniform!
            if (val == skyValue) {
                return;
            }

            p = skyChannel = explode(skyValue);
        }

        setNibble(p, coord, val);
    }

    public void setBlocklight(int x, int y, int z, byte val) {
        var coord = (y << 8) + (z << 4) + x;
        var p = blockChannel;
        if (p == null) {
            if (val == blockValue) {
                return;
            }

            p = blockChannel = explode(blockValue);
        }

        setNibble(p, coord, val);
    }

    // methods for serialization compatibility with WorldIO
    public void getSerializationBlocks(uint[] blocks) {
        for (int i = 0; i < TOTAL_BLOCKS; i++) {
            var index = getIndexRaw(i);
            blocks[i] = vertices[index];
        }
    }

    // direct access to internal palette for optimised serialisation
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint[] skillIssueVertices() => vertices;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort[] skillIssueBlockRefs() => blockRefs;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int skillIssueVertCount() => vertCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int skillIssueIndexRaw(int coord) => getIndexRaw(coord);

    /**
     * As you can see even the "direct" access methods are not really direct because we suck
     */
    public void skillIssueLightPlanes(out byte[]? sky, out byte[]? block, out byte usky, out byte ubl) {
        if (skyChannel != null && isUniform(skyChannel, out var us)) {
            arrayPool.putBack(skyChannel);
            skyChannel = null;
            skyValue = us;
        }

        if (blockChannel != null && isUniform(blockChannel, out var ub)) {
            arrayPool.putBack(blockChannel);
            blockChannel = null;
            blockValue = ub;
        }

        sky = skyChannel;
        block = blockChannel;
        usky = skyValue;
        ubl = blockValue;
    }

    public void setLightPlanes(byte[]? sky, byte[]? block, byte usky, byte ubl) {
        if (skyChannel != null) {
            arrayPool.putBack(skyChannel);
        }

        if (blockChannel != null) {
            arrayPool.putBack(blockChannel);
        }

        skyChannel = sky;
        blockChannel = block;
        skyValue = usky;
        blockValue = ubl;
    }

    /**
     * Need a 4096 length byte array to write into!
     */
    public void getSerializationLight(byte[] light) {
        getLightBatch(0, light.AsSpan(0, TOTAL_BLOCKS));
    }

    public void loadLight(ReadOnlySpan<byte> light) {
        if (skyChannel != null) {
            arrayPool.putBack(skyChannel);
            skyChannel = null;
        }

        if (blockChannel != null) {
            arrayPool.putBack(blockChannel);
            blockChannel = null;
        }

        if (light.Length < TOTAL_BLOCKS) {
            skyValue = 0;
            blockValue = 0;
            return;
        }

        skyValue = (byte)(light[0] & 0xF);
        blockValue = (byte)(light[0] >> 4);

        bool skyu = true, blu = true;
        for (int i = 1; i < TOTAL_BLOCKS; i++) {
            var v = light[i];
            if ((v & 0xF) != skyValue) {
                skyu = false;
            }

            if ((v >> 4) != blockValue) {
                blu = false;
            }

            if (!skyu && !blu) {
                break;
            }
        }

        if (!skyu) {
            var sp = arrayPool.grab(LIGHT_PLANE_BYTES);
            for (int i = 0, b = 0; i < TOTAL_BLOCKS; i += 2, b++) {
                sp[b] = (byte)((light[i] & 0xF) | ((light[i + 1] & 0xF) << 4));
            }

            skyChannel = sp;
        }

        if (!blu) {
            var bp = arrayPool.grab(LIGHT_PLANE_BYTES);
            for (int i = 0, b = 0; i < TOTAL_BLOCKS; i += 2, b++) {
                bp[b] = (byte)((light[i] >> 4) | ((light[i + 1] >> 4) << 4));
            }

            blockChannel = bp;
        }
    }

    public void setSerializationData(uint[] blocks, byte[] lightData) {

        // if old ones exist, dispose
        ReleaseUnmanagedResources();

        // initialize arrays
        vertCapacity = INITIAL_SIZE;
        vertices = arrayPoolU.grab(vertCapacity);
        blockRefs = arrayPoolUS.grab(vertCapacity);

        // reset counters
        vertices[0] = 0; // air block
        blockRefs[0] = 0; // will be set correctly by palette loading
        vertCount = 1;
        density = 0;

        // allocate initial indices
        count = getIndicesSize(density);
        indices = count > 0 ? grabIndices(count) : null;

        // load block data
        for (int i = 0; i < blocks.Length; i++) {
            var index = get(blocks[i]);
            setIndexRaw(i, index);
        }

        // load light data
        loadLight(lightData);

        inited = true;
        refreshCounts();
    }

    public void loadFromPaletteWithPlanes(uint[] paletteBlocks, int paletteSize, byte[] paletteIndices,
        byte[]? sky, byte[]? block, byte uSky, byte uBlk) {
        loadBlockPalette(paletteBlocks, paletteSize, paletteIndices);

        skyValue = uSky;
        blockValue = uBlk;
        skyChannel = copyPlane(sky);
        blockChannel = copyPlane(block);

        inited = true;
        refreshCounts();
    }

    private static byte[]? copyPlane(byte[]? src) {
        if (src == null || src.Length < LIGHT_PLANE_BYTES) {
            return null;
        }

        var p = arrayPool.grab(LIGHT_PLANE_BYTES);
        src.AsSpan(0, LIGHT_PLANE_BYTES).CopyTo(p);
        return p;
    }

    /** Load directly from NBT palette */
    public void loadFromPalette(uint[] paletteBlocks, int paletteSize, byte[] paletteIndices, byte[] lightPalette, int lightPaletteSize, byte[] lightIndices) {
        loadBlockPalette(paletteBlocks, paletteSize, paletteIndices);

        var flat = arrayPool.grab(TOTAL_BLOCKS);
        var n = int.Min(lightIndices.Length, TOTAL_BLOCKS);
        for (int i = 0; i < n; i++) {
            var idx = lightIndices[i];
            flat[i] = idx < lightPaletteSize ? lightPalette[idx] : (byte)0;
        }

        if (n < TOTAL_BLOCKS) {
            Array.Clear(flat, n, TOTAL_BLOCKS - n);
        }

        loadLight(flat.AsSpan(0, TOTAL_BLOCKS));
        arrayPool.putBack(flat);
        arrayPool.putBack(lightPalette);

        inited = true;
        refreshCounts();
    }

    private void loadBlockPalette(uint[] paletteBlocks, int paletteSize, byte[] paletteIndices) {
        // return old arrays to pool
        ReleaseUnmanagedResources();

        // take ownership of passed arrays
        vertices = paletteBlocks;
        vertCapacity = paletteBlocks.Length;
        vertCount = paletteSize;
        blockRefs = arrayPoolUS.grab(vertCapacity);

        // count references by scanning indices
        Array.Clear(blockRefs, 0, paletteSize);
        foreach (int paletteIdx in paletteIndices) {
            blockRefs[paletteIdx]++;
        }

        // calculate density and allocate indices arrays
        density = bitsPerIdx(vertCount);
        count = getIndicesSize(density);
        this.indices = count > 0 ? grabIndices(count) : null;

        // copy indices (skip if density is 0, means palette size 1)
        if (indices != null) {
            for (int i = 0; i < paletteIndices.Length; i++) {
                setIndexRaw(i, paletteIndices[i]);
            }
        }

    }

    // cleanup
    private void ReleaseUnmanagedResources() {
        if (indices != null) {
            arrayPool.putBack(indices);
            indices = null;
        }

        if (vertices != null) {
            arrayPoolU.putBack(vertices);
            vertices = null;
        }

        if (blockRefs != null) {
            arrayPoolUS.putBack(blockRefs);
            blockRefs = null;
        }

        if (skyChannel != null) {
            arrayPool.putBack(skyChannel);
            skyChannel = null;
        }

        if (blockChannel != null) {
            arrayPool.putBack(blockChannel);
            blockChannel = null;
        }

        skyValue = 0;
        blockValue = 0;

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
        skillIssueLightPlanes(out var sky, out var blk, out var uSky, out var uBlk);
        return new ChunkDataPacket.SubChunkData {
            y = y,
            vertices = vertices,
            blockRefs = blockRefs,
            indices = indices,
            count = count,
            vertCount = vertCount,
            density = density,
            skyChannel = sky,
            blockChannel = blk,
            skyValue = uSky,
            blockValue = uBlk,
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
        count = data.count;
        if (data.indices != null) {
            indices = grabIndices(count);
            data.indices.AsSpan(0, count).CopyTo(indices);
        }
        else {
            indices = null;
        }

        vertCount = data.vertCount;
        density = data.density;
        skyChannel = data.skyChannel;
        blockChannel = data.blockChannel;
        skyValue = data.skyValue;
        blockValue = data.blockValue;
        blockCount = data.blockCount;
        translucentCount = data.translucentCount;
        fullBlockCount = data.fullBlockCount;
        vertCapacity = vertices.Length;
        randomTickCount = data.randomTickCount;
        renderTickCount = data.renderTickCount;
        inited = true;
    }
}