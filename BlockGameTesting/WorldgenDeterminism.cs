using BlockGame.main;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.block.entity;
using BlockGame.world.chunk;
using BlockGame.world.entity;
using BlockGame.world.item;

namespace BlockGameTesting;

/** does worldgen depend on the order chunks get populated in? */
[Explicit]
public class WorldgenDeterminism {
    [OneTimeSetUp]
    public void Registry() {
        TestRegistry.ensure();
    }

    private static World makeWorld(int seed) =>
        new(Side.SERVER, "__det_" + Guid.NewGuid().ToString("N")[..8], seed, generatorName:"v4");

    private static ushort[] getBlocks(Chunk c) {
        var blocks = new ushort[World.WORLDHEIGHT * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE];
        int i = 0;
        for (int y = 0; y < World.WORLDHEIGHT; y++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                    blocks[i++] = c.getBlock(x, y, z);
                }
            }
        }

        return blocks;
    }

    /** fnv-1a over the block ids */
    private static ulong hashChunk(Chunk c) {
        ulong h = 14695981039346656037UL;
        foreach (var block in getBlocks(c)) {
            h ^= block;
            h *= 1099511628211UL;
        }

        return h;
    }

    private static int countDiffs(ushort[] a, ushort[] b) {
        int n = 0;
        for (int i = 0; i < a.Length; i++) {
            if (a[i] != b[i]) {
                n++;
            }
        }

        return n;
    }

    [Test]
    public void SameSeedSameOrder() {
        var target = new ChunkCoord(0, 0);
        var a = makeWorld(1338);
        var b = makeWorld(1338);
        for (int x = -3; x <= 3; x++) {
            for (int z = -3; z <= 3; z++) {
                a.loadChunk(new ChunkCoord(x, z), ChunkStatus.POPULATED, true);
                b.loadChunk(new ChunkCoord(x, z), ChunkStatus.POPULATED, true);
            }
        }

        var ha = hashChunk(a.getChunk(target));
        var hb = hashChunk(b.getChunk(target));
        Console.WriteLine($"same order: A={ha:X16} B={hb:X16}");
        Assert.That(ha, Is.EqualTo(hb));
    }

    [Test]
    public void SameSeedDifferentOrder() {
        var target = new ChunkCoord(0, 0);
        var a = makeWorld(1338);
        for (int x = -3; x <= 3; x++) {
            for (int z = -3; z <= 3; z++) {
                a.loadChunk(new ChunkCoord(x, z), ChunkStatus.POPULATED, true);
            }
        }

        // reverse order: neighbours get populated before (0,0)
        var b = makeWorld(1338);
        for (int x = 3; x >= -3; x--) {
            for (int z = 3; z >= -3; z--) {
                b.loadChunk(new ChunkCoord(x, z), ChunkStatus.POPULATED, true);
            }
        }

        var ha = hashChunk(a.getChunk(target));
        var hb = hashChunk(b.getChunk(target));
        Console.WriteLine($"fwd order: {ha:X16}");
        Console.WriteLine($"rev order: {hb:X16}");
        Console.WriteLine(ha == hb ? "DETERMINISTIC" : "*** ORDER-DEPENDENT ***");
        Assert.That(ha, Is.EqualTo(hb), "chunk contents depend on generation order");
    }

    /** generate ONLY (0,0) (plus whatever it pulls in) vs a big region first */
    [Test]
    public void IsolatedVsRegion() {
        var target = new ChunkCoord(0, 0);
        var a = makeWorld(1338);
        a.loadChunk(target, ChunkStatus.POPULATED, true);

        var b = makeWorld(1338);
        for (int x = -6; x <= 6; x++) {
            for (int z = -6; z <= 6; z++) {
                b.loadChunk(new ChunkCoord(x, z), ChunkStatus.POPULATED, true);
            }
        }

        var ha = hashChunk(a.getChunk(target));
        var hb = hashChunk(b.getChunk(target));
        Console.WriteLine($"isolated: {ha:X16}");
        Console.WriteLine($"region:   {hb:X16}");
        Console.WriteLine(ha == hb ? "DETERMINISTIC" : "*** CONTEXT-DEPENDENT ***");
        Assert.That(ha, Is.EqualTo(hb));
    }

    /** same seed, forward vs reverse populate order - how many blocks actually move? */
    [Test]
    public void HowMuchDrift() {
        int worst = 0, total = 0;
        for (int seed = 2000; seed < 2010; seed++) {
            var a = makeWorld(seed);
            for (int x = -4; x <= 4; x++) {
                for (int z = -4; z <= 4; z++) {
                    a.loadChunk(new ChunkCoord(x, z), ChunkStatus.POPULATED, true);
                }
            }

            var b = makeWorld(seed);
            for (int x = 4; x >= -4; x--) {
                for (int z = 4; z >= -4; z--) {
                    b.loadChunk(new ChunkCoord(x, z), ChunkStatus.POPULATED, true);
                }
            }

            var blocksA = getBlocks(a.getChunk(new ChunkCoord(0, 0)));
            var blocksB = getBlocks(b.getChunk(new ChunkCoord(0, 0)));
            var d = countDiffs(blocksA, blocksB);
            total += d;
            worst = Math.Max(worst, d);
            Console.WriteLine($"seed {seed}: {d} of {blocksA.Length} blocks differ ({d * 100.0 / blocksA.Length:F2}%)");
        }

        Console.WriteLine($"avg {total / 10.0:F0} blocks, worst {worst}");
    }
}
