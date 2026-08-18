using System.Diagnostics;
using BlockGame.main;
using BlockGame.world;
using BlockGame.world.chunk;
using BlockGame.world.worldgen;

namespace BlockGameTesting;

public class WorldgenParallel {
    [OneTimeSetUp]
    public void registry() {
        TestRegistry.ensure();
    }

    private static World mk(int seed) =>
        new(Side.SERVER, "__par_" + Guid.NewGuid().ToString("N")[..8], seed, generatorName: "v4");

    private static ulong hashChunk(Chunk c) {
        ulong h = 1469598103934665603UL;
        for (int y = 0; y < World.WORLDHEIGHT; y++)
        for (int z = 0; z < Chunk.CHUNKSIZE; z++)
        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            h ^= c.getBlock(x, y, z);
            h *= 1099511628211UL;
        }
        // heightmap + biome data ride along in the same stage
        for (int z = 0; z < Chunk.CHUNKSIZE; z++)
        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            h ^= (ulong)c.heightMap.get(x, z);
            h *= 1099511628211UL;
        }
        return h;
    }

    /** sync path: loadChunk(immediately) generates everything on this thread, one at a time */
    private static World sync(int seed, int r, ChunkStatus status) {
        var w = mk(seed);
        for (int x = -r; x <= r; x++)
        for (int z = -r; z <= r; z++)
            w.loadChunk(new ChunkCoord(x, z), status, true);
        return w;
    }

    /** ticket path: queue everything, drain via updateChunkloading -> pregen batches -> loadChunk finds them present */
    private static World ticketed(int seed, int r, ChunkStatus status) {
        var w = mk(seed);
        for (int x = -r; x <= r; x++)
        for (int z = -r; z <= r; z++)
            w.addToChunkLoadQueue(new ChunkCoord(x, z), status);
        int n = 0;
        // loading: true -> server drains the whole queue, no tick budget
        while (w.chunkLoadQueue.Count > 0) {
            w.updateChunkloading(loading: true, ref n);
        }
        return w;
    }

    [Test]
    public void identicalGenerated() => identical(ChunkStatus.GENERATED);

    [Test, Explicit]
    public void identicalPopulated() => identical(ChunkStatus.POPULATED);

    private static void identical(ChunkStatus status) {
        const int r = 4;
        var a = sync(1338, r, status);
        var b = ticketed(1338, r, status);
        int compared = 0;
        // compare the whole loaded set - the ticket path pregens the 3x3 halo too, both worlds have it
        foreach (var chunk in a.chunkList) {
            var other = b.getChunkMaybe(chunk.coord, out var oc) ? oc : null;
            Assert.That(other, Is.Not.Null, $"chunk {chunk.coord} missing from ticketed world");
            Assert.That(other!.status, Is.EqualTo(chunk.status), $"status differs at {chunk.coord}");
            if (hashChunk(other) != hashChunk(chunk)) {
                for (int y = 0; y < World.WORLDHEIGHT; y++)
                for (int z = 0; z < Chunk.CHUNKSIZE; z++)
                for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                    if (chunk.getBlock(x, y, z) != other.getBlock(x, y, z)) {
                        Assert.Fail($"contents differ at {chunk.coord} first at ({x},{y},{z}): sync={chunk.getBlock(x, y, z)} ticketed={other.getBlock(x, y, z)}");
                    }
                }
                Assert.Fail($"heightmap differs at {chunk.coord}");
            }
            compared++;
        }
        Console.WriteLine($"{compared} chunks identical (workers: {GenJob.pool.workers})");
        Assert.That(compared, Is.GreaterThanOrEqualTo((2 * r + 1) * (2 * r + 1)));
    }

    [Test, Explicit]
    public void bench() {
        const int r = 12;
        // warmup both paths (JIT, pools, palette arrays)
        sync(1, 2, ChunkStatus.POPULATED);
        ticketed(1, 2, ChunkStatus.POPULATED);

        var sw = Stopwatch.StartNew();
        var a = sync(1338, r, ChunkStatus.POPULATED);
        var ts = sw.Elapsed.TotalMilliseconds;
        sw.Restart();
        var b = ticketed(1338, r, ChunkStatus.POPULATED);
        var tt = sw.Elapsed.TotalMilliseconds;
        int n = a.chunkList.Count;
        Console.WriteLine($"{n} chunks to POPULATED: sync {ts:F0} ms ({ts / n * 1000:F0} us/chunk), ticketed+pregen {tt:F0} ms ({tt / n * 1000:F0} us/chunk) => {ts / tt:F2}x  (workers: {GenJob.pool.workers})");
    }
}
