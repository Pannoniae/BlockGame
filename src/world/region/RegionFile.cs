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

    public readonly int rx, rz;
    public readonly string path;

    private FileStream? file;
    // header[i*2] = offset, header[i*2+1] = length
    // todo create struct instead of rawdogging ints...
    private readonly int[] header = new int[CHUNKS_PER_REGION << 1];
    // dirty chunks cached in memory (write-back)
    private readonly Dictionary<int, byte[]> dirtyChunks = new();
    private bool headerDirty;
    private bool isDisposed;

    public RegionFile(string worldPath, int rx, int rz) {
        this.rx = rx;
        this.rz = rz;
        this.path = getRegionPath(worldPath, rx, rz);

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

        for (int i = 0; i < CHUNKS_PER_REGION << 1; i++) {
            header[i] = MemoryMarshal.Read<int>(buffer.Slice(i * 4, 4));
        }
    }

    /** Write header to disk (must be called after modifying offsets/lengths) */
    private void writeHeader() {
        file!.Position = 0;
        Span<byte> buffer = stackalloc byte[HEADER_SIZE];
        buffer.Clear();

        for (int i = 0; i < CHUNKS_PER_REGION << 1; i++) {
            MemoryMarshal.Write(buffer.Slice(i * 4, 4), in header[i]);
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

    /** Write chunk to cache (write-back, no disk I/O) */
    public void writeChunk(int lx, int lz, byte[] data) {
        if (data.Length > MAX_CHUNK_SIZE) {
            Log.warn($"Chunk ({lx},{lz}) in region ({rx},{rz}) is too large ({data.Length} bytes), skipping save");
            return;
        }

        int idx = RegionFile.idx(lx, lz);
        dirtyChunks[idx] = data;
    }

    /** Read chunk (check dirty cache first, then disk) */
    public byte[]? readChunk(int lx, int lz) {
        int idx = RegionFile.idx(lx, lz);

        // check dirty cache first
        if (dirtyChunks.TryGetValue(idx, out var cached)) {
            return cached;
        }

        // read from disk
        int offset = header[idx << 1];
        int length = header[(idx << 1) + 1];

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

    /** Flush dirty chunks to disk */
    public void flush() {
        if (dirtyChunks.Count == 0 && !headerDirty) {
            return;
        }

        float r = wasteRatio();
        if (r > DEFRAG_THRESHOLD) {
            defragAndFlush();
        }
        else {
            flushDirtyChunks();
        }
    }

    /** Flush dirty chunks using smart reuse and reuse existing slots if possible */
    private void flushDirtyChunks() {
        foreach (var (idx, data) in dirtyChunks) {
            int existingOffset = header[idx << 1];
            int existingLength = header[(idx << 1) + 1];

            if (existingOffset != 0 && data.Length <= existingLength) {
                // reuse existing slot (fits in old space)
                file!.Position = existingOffset;
                file.Write(data);
                header[(idx << 1) + 1] = data.Length;
                headerDirty = true;
            }
            else {
                // append to end (doesn't fit or new chunk)
                file!.Position = file.Length;
                int newOffset = (int)file.Position;
                file.Write(data);
                header[idx << 1] = newOffset;
                header[(idx << 1) + 1] = data.Length;
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
        foreach (var (idx, data) in dirtyChunks) {
            allChunks.Set(idx, data);
        }

        // add clean chunks from disk
        for (int i = 0; i < CHUNKS_PER_REGION; i++) {
            if (allChunks.ContainsKey(i)) {
                continue; // already have dirty version
            }

            int offset = header[i << 1];
            int length = header[(i << 1) + 1];

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
                header[idx << 1] = writePos;
                header[(idx << 1) + 1] = data.Length;
            }

            // 4. Write header at start
            tempFile.Position = 0;
            Span<byte> buffer = stackalloc byte[HEADER_SIZE];
            buffer.Clear();

            for (int i = 0; i < CHUNKS_PER_REGION << 1; i++) {
                MemoryMarshal.Write(buffer.Slice(i * 4, 4), in header[i]);
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

        long usedSpace = HEADER_SIZE;
        for (int i = 0; i < CHUNKS_PER_REGION; i++) {
            // check dirty chunks first
            if (dirtyChunks.TryGetValue(i, out var cached)) {
                usedSpace += cached.Length;
            }
            else if (header[(i << 1) + 1] > 0) {
                usedSpace += header[(i << 1) + 1];
            }
        }

        long totalSpace = file.Length;
        return 1f - (float)usedSpace / totalSpace;
    }

    /** Delete chunk from region */
    public void deleteChunk(int lx, int lz) {
        int idx = RegionFile.idx(lx, lz);
        dirtyChunks.Remove(idx);
        header[idx << 1] = 0;
        header[(idx << 1) + 1] = 0;
        headerDirty = true;
    }

    /** Check if chunk exists (in cache or on disk) */
    public bool hasChunk(int localX, int localZ) {
        int idx = RegionFile.idx(localX, localZ);
        return dirtyChunks.ContainsKey(idx) || (header[idx << 1] != 0 && header[(idx << 1) + 1] != 0);
    }

    public void Dispose() {
        if (isDisposed) return;
        isDisposed = true;

        flush();
        file?.Dispose();
    }

    /** Get region file path for world */
    public static string getRegionPath(string worldPath, int rx, int rz) {
        return $"{worldPath}/region/r.{rx}.{rz}.xrg";
    }
}