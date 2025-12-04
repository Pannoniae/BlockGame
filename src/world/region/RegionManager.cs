using BlockGame.util;
using BlockGame.world.chunk;

namespace BlockGame.world.region;

/** Manages region files with LRU caching */
public sealed class RegionManager : IDisposable {
    public const int MAX_CACHED_REGIONS = 32;

    public readonly string worldPath;
    private readonly XLongMap<RegionFile> cache = [];
    private readonly LinkedList<RegionCoord> lruList = [];
    private readonly XLongMap<LinkedListNode<RegionCoord>> lruNodes = [];
    private bool isDisposed;

    // global lock protects cache, LRU, and all RegionFile operations
    private readonly Lock globalLock = new();

    public RegionManager(string worldPath) {
        this.worldPath = worldPath;
    }

    /** Get or create region file (with LRU eviction) */
    public RegionFile getRegion(RegionCoord coord) {
        lock (globalLock) {
            // cache hit - move to front
            if (cache.TryGetValue(coord.toLong(), out var region)) {
                touch(coord);
                return region;
            }

            // cache miss - load/create region
            region = new RegionFile(worldPath, coord.x, coord.z, globalLock);
            cache.Add(coord.toLong(), region);

            // add to LRU front
            var node = lruList.AddFirst(coord);
            lruNodes.Add(coord.toLong(), node);

            // evict oldest if cache is full
            if (cache.Count > MAX_CACHED_REGIONS) {
                evictOldest();
            }

            return region;
        }
    }

    /** Move region to front of LRU (most recently used) */
    private void touch(RegionCoord key) {
        if (lruNodes.TryGetValue(key.toLong(), out var node)) {
            lruList.Remove(node);
            lruList.AddFirst(node); // reuse node, no allocation
        }
    }

    /** Evict least recently used region */
    private void evictOldest() {
        if (lruList.Last == null) return;

        var oldest = lruList.Last.Value;
        lruList.RemoveLast();
        lruNodes.Remove(oldest.toLong());

        if (cache.TryGetValue(oldest.toLong(), out var region)) {
            region.DisposeUnsafe(); // flushes before closing (lock already held)
            cache.Remove(oldest.toLong());
        }
    }

    /** Flush all dirty regions (autosave/world close) */
    public void flushAll() {
        lock (globalLock) {
            foreach (var region in cache) {
                region.flushUnsafe();
            }
        }
    }

    /** Close all regions (call on world close) */
    public void closeAll() {
        lock (globalLock) {
            foreach (var region in cache) {
                region.DisposeUnsafe();
            }

            cache.Clear();
            lruList.Clear();
            lruNodes.Clear();
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
        if (isDisposed) return;
        isDisposed = true;

        closeAll();
    }
}