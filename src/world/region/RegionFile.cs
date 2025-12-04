using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlockGame.util;
using BlockGame.util.log;

namespace BlockGame.world.region;

/** Region file format (.xrg) - packs 32x32 chunks (1024 total) into a single file */
public sealed class RegionFile : IDisposable {
    private const int HEADER_SIZE = 8192;
    private const int REGION_SIZE = 32;
    private const int CHUNKS_PER_REGION = REGION_SIZE * REGION_SIZE; // 1024
    private const int MAX_CHUNK_SIZE = 4 * 1024 * 1024; // 4MB sanity limit
    private const float DEFRAG_THRESHOLD = 0.5f; // defrag if 50% waste

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    private struct ChunkEntry {
        [FieldOffset(0)]
        public int offset;
        [FieldOffset(4)]
        public int length;

        [FieldOffset(0)]
        public long packed; // for easy zeroing

    }

    public readonly int rx, rz;
    public readonly string path;

    private FileStream? file;
    private readonly ChunkEntry[] header = new ChunkEntry[CHUNKS_PER_REGION];
    // dirty chunks cached in memory (write-back)
    private readonly XIntMap<byte[]> dirtyChunks = [];
    private bool headerDirty;
    private bool isDisposed;

    // shared lock from RegionManager
    private readonly Lock globalLock;

    public RegionFile(string worldPath, int rx, int rz, Lock globalLock) {
        this.rx = rx;
        this.rz = rz;
        this.path = getRegionPath(worldPath, rx, rz);
        this.globalLock = globalLock;

        // ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);

        // open or create file
        var exists = File.Exists(path);
        file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        if (exists && file.Length >= HEADER_SIZE) {
            loadHeader();
        }
        else {
            // new file, initialise with zeros
            clearHeader();
            writeHeader();
        }
    }

    /** Load header from disk (8KB of offset+length pairs) */
    private void loadHeader() {
        file!.Position = 0;
        Span<byte> buffer = stackalloc byte[HEADER_SIZE];
        file.ReadExactly(buffer);

        for (int i = 0; i < CHUNKS_PER_REGION; i++) {
            header[i].packed = MemoryMarshal.Read<long>(buffer.Slice(i * 8, 8));
        }
    }

    /** Write header to disk (must be called after modifying offsets/lengths) */
    private void writeHeader() {
        file!.Position = 0;
        Span<byte> buffer = stackalloc byte[HEADER_SIZE];
        buffer.Clear();

        for (int i = 0; i < CHUNKS_PER_REGION; i++) {
            MemoryMarshal.Write(buffer.Slice(i * 8, 8), in header[i].packed);
        }

        file.Write(buffer);
        file.Flush();
        headerDirty = false;
    }

    private void clearHeader() {
        Array.Clear(header);
        headerDirty = true;
    }

    /** Convert local chunk coord to index */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int idx(int lx, int lz) {
        return lz * REGION_SIZE + lx;
    }

    /** Write chunk to cache (write-back, no disk I/O) - thread-safe */
    public void writeChunk(int lx, int lz, byte[] data) {
        lock (globalLock) {
            writeChunkUnsafe(lx, lz, data);
        }
    }

    /** Write chunk to cache - must be called with lock held */
    internal void writeChunkUnsafe(int lx, int lz, byte[] data) {
        if (data.Length > MAX_CHUNK_SIZE) {
            Log.warn($"Chunk ({lx},{lz}) in region ({rx},{rz}) is too large ({data.Length} bytes), skipping save");
            return;
        }

        int idx = RegionFile.idx(lx, lz);
        dirtyChunks.Set(idx, data);
    }

    /** Read chunk (check dirty cache first, then disk) - thread-safe */
    public byte[]? readChunk(int lx, int lz) {
        lock (globalLock) {
            return readChunkUnsafe(lx, lz);
        }
    }

    /** Read chunk - must be called with lock held */
    internal byte[]? readChunkUnsafe(int lx, int lz) {
        int idx = RegionFile.idx(lx, lz);

        // check dirty cache first
        if (dirtyChunks.TryGetValue(idx, out var cached)) {
            return cached;
        }

        // read from disk
        int offset = header[idx].offset;
        int length = header[idx].length;

        if (offset == 0 || length == 0) {
            return null; // chunk doesn't exist
        }

        file!.Position = offset;
        byte[] data = new byte[length];
        int bytesRead = file.Read(data, 0, length);

        if (bytesRead != length) {
            Log.warn($"Partial read for chunk ({lx},{lz}) in region ({rx},{rz}): expected {length}, got {bytesRead}");
            return null;
        }

        return data;
    }

    /** Flush dirty chunks to disk - thread-safe */
    public void flush() {
        lock (globalLock) {
            flushUnsafe();
        }
    }

    /** Flush dirty chunks to disk - must be called with lock held */
    internal void flushUnsafe() {
        if (dirtyChunks.Count == 0 && !headerDirty) {
            return;
        }

        float r = wasteRatio();
        if (r > DEFRAG_THRESHOLD) {
            defragAndFlush();
        }
        else {
            flushDirty();
        }
    }

    /** Flush dirty chunks using smart reuse and reuse existing slots if possible */
    private void flushDirty() {
        foreach (var (idx, data) in dirtyChunks.Pairs) {
            int existingOffset = header[idx].offset;
            int existingLength = header[idx].length;

            if (existingOffset != 0 && data.Length <= existingLength) {
                // reuse existing slot (fits in old space)
                file!.Position = existingOffset;
                file.Write(data);
                header[idx].length = data.Length;
                headerDirty = true;
            }
            else {
                // append to end (doesn't fit or new chunk)
                file!.Position = file.Length;
                int newOffset = (int)file.Position;
                file.Write(data);
                header[idx].offset = newOffset;
                header[idx].length = data.Length;
                headerDirty = true;
            }
        }

        dirtyChunks.Clear();
        file!.Flush();

        // CRITICAL: write header after updating offsets/lengths
        if (headerDirty) {
            writeHeader();
        }
    }

    /** Defrag and flush: read all chunks, rewrite compacted, update header */
    private void defragAndFlush() {
        // 1. Read ALL chunks into memory (dirty + clean)
        var allChunks = new XIntMap<byte[]>();

        // add dirty chunks
        foreach (var (idx, data) in dirtyChunks.Pairs) {
            allChunks.Set(idx, data);
        }

        // add clean chunks from disk
        for (int i = 0; i < CHUNKS_PER_REGION; i++) {
            if (allChunks.ContainsKey(i)) {
                continue; // already have dirty version
            }

            int offset = header[i].offset;
            int length = header[i].length;

            if (offset != 0 && length != 0) {
                file!.Position = offset;
                byte[] data = new byte[length];
                file.ReadExactly(data, 0, length);
                allChunks.Set(i, data);
            }
        }

        // 2. Use temp file for atomic defrag
        string tempPath = path + ".tmp";
        using (var tempFile = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None)) {
            // skip header for now
            tempFile.Position = HEADER_SIZE;

            // 3. Write chunks sequentially (no gaps)
            clearHeader();
            foreach (var (idx, data) in allChunks.Pairs) {
                int writePos = (int)tempFile.Position;
                tempFile.Write(data);
                header[idx].offset = writePos;
                header[idx].length = data.Length;
            }

            // 4. Write header at start
            tempFile.Position = 0;
            Span<byte> buffer = stackalloc byte[HEADER_SIZE];
            buffer.Clear();

            for (int i = 0; i < CHUNKS_PER_REGION; i++) {
                MemoryMarshal.Write(buffer.Slice(i * 8, 8), in header[i].packed);
            }

            tempFile.Write(buffer);
            tempFile.Flush();
        }

        // 5. Atomic replace: close old file, rename temp to real
        file!.Dispose();

        // windows requires explicit delete before replace
        if (File.Exists(path)) {
            File.Delete(path);
        }

        File.Move(tempPath, path);

        // reopen file
        file = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        dirtyChunks.Clear();
        headerDirty = false;
    }

    /** Calculate waste ratio (1.0 = 100% waste, 0.0 = no waste) */
    private float wasteRatio() {
        if (file!.Length <= HEADER_SIZE) {
            return 0f;
        }

        long used = HEADER_SIZE;
        for (int i = 0; i < CHUNKS_PER_REGION; i++) {
            // check dirty chunks first
            if (dirtyChunks.TryGetValue(i, out var cached)) {
                used += cached.Length;
            }
            else if (header[i].length > 0) {
                used += header[i].length;
            }
        }

        long total = file.Length;
        return 1f - (float)used / total;
    }

    /** Delete chunk from region - thread-safe */
    public void deleteChunk(int lx, int lz) {
        lock (globalLock) {
            deleteChunkUnsafe(lx, lz);
        }
    }

    /** Delete chunk from region - must be called with lock held */
    internal void deleteChunkUnsafe(int lx, int lz) {
        int idx = RegionFile.idx(lx, lz);
        dirtyChunks.Remove(idx);
        header[idx].offset = 0;
        header[idx].length = 0;
        headerDirty = true;
    }

    /** Check if chunk exists (in cache or on disk) - thread-safe */
    public bool hasChunk(int localX, int localZ) {
        lock (globalLock) {
            return hasChunkUnsafe(localX, localZ);
        }
    }

    /** Check if chunk exists - must be called with lock held */
    internal bool hasChunkUnsafe(int localX, int localZ) {
        int idx = RegionFile.idx(localX, localZ);
        return dirtyChunks.ContainsKey(idx) || (header[idx].offset != 0 && header[idx].length != 0);
    }

    public void Dispose() {
        lock (globalLock) {
            DisposeUnsafe();
        }
    }

    /** Dispose - must be called with lock held (for RegionManager eviction) */
    internal void DisposeUnsafe() {
        if (isDisposed) return;
        isDisposed = true;

        flushUnsafe();
        file?.Dispose();
    }

    /** Get region file path for world */
    public static string getRegionPath(string worldPath, int rx, int rz) {
        return $"{worldPath}/region/r.{rx}.{rz}.xrg";
    }
}