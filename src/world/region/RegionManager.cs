using BlockGame.util;
using BlockGame.world.chunk;

namespace BlockGame.world.region;

public sealed class RegionManager : IDisposable {
    public const int MAX_CACHED_REGIONS = 32;

    public readonly string worldPath;
    private readonly XLongMap<RegionFile> cache = [];
    private readonly LinkedList<RegionCoord> lruList = [];
    private readonly XLongMap<LinkedListNode<RegionCoord>> lruNodes = [];
    private bool isDisposed;

    /** covers cache, lruList, lruNodes and the refcounts. NEVER held across IO. */
    private readonly Lock cacheLock = new();

    public RegionManager(string worldPath) {
        this.worldPath = worldPath;
    }

    // ---- abstract it so we don't actually hand out RegionFiles for people do bad things with ----

    public bool writeChunk(ChunkCoord chunk, byte[] data) {
        var local = getLocalCoord(chunk);
        var region = pin(getRegionCoord(chunk));
        try {
            lock (region.@lock) {
                return region.writeChunkUnsafe(local.x, local.z, data);
            }
        }
        finally {
            unpin(region);
        }
    }

    public byte[]? readChunk(ChunkCoord chunk) {
        var local = getLocalCoord(chunk);
        var region = pin(getRegionCoord(chunk));
        try {
            lock (region.@lock) {
                return region.readChunkUnsafe(local.x, local.z);
            }
        }
        finally {
            unpin(region);
        }
    }

    public bool hasChunk(ChunkCoord chunk) {
        var local = getLocalCoord(chunk);
        var region = pin(getRegionCoord(chunk));
        try {
            lock (region.@lock) {
                return region.hasChunkUnsafe(local.x, local.z);
            }
        }
        finally {
            unpin(region);
        }
    }

    public void deleteChunk(ChunkCoord chunk) {
        var local = getLocalCoord(chunk);
        var region = pin(getRegionCoord(chunk));
        try {
            lock (region.@lock) {
                region.deleteChunkUnsafe(local.x, local.z);
            }
        }
        finally {
            unpin(region);
        }
    }

    // ---- cache ----

    /** Get or create a region and pin it so it can't be evicted out from under the caller. */
    private RegionFile pin(RegionCoord coord) {
        lock (cacheLock) {
            if (cache.TryGetValue(coord.toLong(), out var region)) {
                touch(coord);
                region.refs++;
                return region;
            }

            region = new RegionFile(worldPath, coord.x, coord.z);
            cache.Add(coord.toLong(), region);

            var node = lruList.AddFirst(coord);
            lruNodes.Add(coord.toLong(), node);

            region.refs++;

            // evict oldest if cache is full
            if (cache.Count > MAX_CACHED_REGIONS) {
                evictOldest();
            }

            return region;
        }
    }

    private void unpin(RegionFile region) {
        lock (cacheLock) {
            region.refs--;
        }
    }

    /** Move region to front of LRU (most recently used) */
    private void touch(RegionCoord key) {
        if (lruNodes.TryGetValue(key.toLong(), out var node)) {
            lruList.Remove(node);
            lruList.AddFirst(node); // reuse node, no allocation
        }
    }

    /**
     * Evict the least recently used UNPINNED region.
     */
    private void evictOldest() {
        var node = lruList.Last;

        while (node != null) {
            var prev = node.Previous;
            var coord = node.Value;

            if (cache.TryGetValue(coord.toLong(), out var region) && region.refs == 0) {
                lruList.Remove(node);
                lruNodes.Remove(coord.toLong());
                cache.Remove(coord.toLong());

                lock (region.@lock) {
                    region.DisposeUnsafe();
                }
                return;
            }

            node = prev;
        }
    }

    /** Flush all dirty regions (autosave/world close) */
    public void flushAll() {
        foreach (var region in snapshot()) {
            lock (region.@lock) {
                region.flushUnsafe();
            }
        }
    }

    /** Close all regions (call on world close) */
    public void closeAll() {
        foreach (var region in snapshot()) {
            lock (region.@lock) {
                region.DisposeUnsafe();
            }
        }

        lock (cacheLock) {
            cache.Clear();
            lruList.Clear();
            lruNodes.Clear();
        }
    }
    private RegionFile[] snapshot() {
        lock (cacheLock) {
            var a = new RegionFile[cache.Count];
            var i = 0;
            foreach (var region in cache) {
                a[i++] = region;
            }
            return a;
        }
    }

    /** Get region coords from chunk coords */
    public static RegionCoord getRegionCoord(ChunkCoord chunk) {
        return new RegionCoord(chunk.x >> 5, chunk.z >> 5);
    }

    /** Get local coords within region */
    public static LocalRegionCoord getLocalCoord(ChunkCoord chunk) {
        return new LocalRegionCoord(chunk.x & 31, chunk.z & 31);
    }

    public void Dispose() {
        if (isDisposed) {
            return;
        }

        isDisposed = true;

        closeAll();
    }
}
