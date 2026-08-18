using BlockGame.main;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.block.entity;
using BlockGame.world.chunk;
using BlockGame.world.entity;
using BlockGame.world.item;

namespace BlockGameTesting;

[Explicit]
public class WorldgenDeterminism {

    [OneTimeSetUp]
    public void Registry() {
        TestRegistry.ensure();
    }

    /** how many blocks differ between two snapshots of the same chunk coord */
    private static int diff(ushort[] a, ushort[] b) {
        int n = 0;
        for (int i = 0; i < a.Length; i++) {
            if (a[i] != b[i]) {
                n++;
            }
        }
        return n;
    }

    private static ushort[] snap(Chunk c) {
        var a = new ushort[World.WORLDHEIGHT * 256];
        for (int y = 0; y < World.WORLDHEIGHT; y++)
        for (int z = 0; z < Chunk.CHUNKSIZE; z++)
        for (int x = 0; x < Chunk.CHUNKSIZE; x++) a[(y << 8) + (z << 4) + x] = c.getBlock(x, y, z);
        return a;
    }

    /** same seed, forward vs reverse populate order - how many blocks actually move? */
    [Test]
    public void HowMuchDrift() {
        int worst = 0, total = 0;
        for (int seed = 2000; seed < 2010; seed++) {
            var a = mk(seed);
            for (int x = -4; x <= 4; x++) for (int z = -4; z <= 4; z++)
                a.loadChunk(new ChunkCoord(x, z), ChunkStatus.POPULATED, true);
            var b = mk(seed);
            for (int x = 4; x >= -4; x--) for (int z = 4; z >= -4; z--)
                b.loadChunk(new ChunkCoord(x, z), ChunkStatus.POPULATED, true);

            var t = new ChunkCoord(0, 0);
            var d = diff(snap(a.getChunk(t)), snap(b.getChunk(t)));
            total += d;
            worst = Math.Max(worst, d);
            Console.WriteLine($"seed {seed}: {d} of 32768 blocks differ ({d / 327.68:F2}%)");
        }
        Console.WriteLine($"avg {total / 10.0:F0} blocks, worst {worst}");
    }

    private static ulong hashChunk(Chunk c) {
        ulong h = 1469598103934665603UL;
        for (int y = 0; y < World.WORLDHEIGHT; y++)
        for (int z = 0; z < Chunk.CHUNKSIZE; z++)
        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            h ^= c.getBlock(x, y, z);
            h *= 1099511628211UL;
        }
        return h;
    }

    private static World mk(int seed) =>
        new(Side.SERVER, "__det_" + Guid.NewGuid().ToString("N")[..8], seed, generatorName: "v4");

    [Test]
    public void SameSeedSameOrder() {
        var target = new ChunkCoord(0, 0);
        var a = mk(1338);
        var b = mk(1338);
        foreach (var w in new[] { a, b }) {
            for (int x = -3; x <= 3; x++)
            for (int z = -3; z <= 3; z++)
                w.loadChunk(new ChunkCoord(x, z), ChunkStatus.POPULATED, true);
        }
        Console.WriteLine($"same order: A={hashChunk(a.getChunk(target)):X16} B={hashChunk(b.getChunk(target)):X16}");
        Assert.That(hashChunk(a.getChunk(target)), Is.EqualTo(hashChunk(b.getChunk(target))));
    }

    [Test]
    public void SameSeedDifferentOrder() {
        var target = new ChunkCoord(0, 0);
        var a = mk(1338);
        for (int x = -3; x <= 3; x++)
        for (int z = -3; z <= 3; z++)
            a.loadChunk(new ChunkCoord(x, z), ChunkStatus.POPULATED, true);

        // reverse order: neighbours get populated before (0,0)
        var b = mk(1338);
        for (int x = 3; x >= -3; x--)
        for (int z = 3; z >= -3; z--)
            b.loadChunk(new ChunkCoord(x, z), ChunkStatus.POPULATED, true);

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
        var a = mk(1338);
        a.loadChunk(target, ChunkStatus.POPULATED, true);

        var b = mk(1338);
        for (int x = -6; x <= 6; x++)
        for (int z = -6; z <= 6; z++)
            b.loadChunk(new ChunkCoord(x, z), ChunkStatus.POPULATED, true);

        var ha = hashChunk(a.getChunk(target));
        var hb = hashChunk(b.getChunk(target));
        Console.WriteLine($"isolated: {ha:X16}");
        Console.WriteLine($"region:   {hb:X16}");
        Console.WriteLine(ha == hb ? "DETERMINISTIC" : "*** CONTEXT-DEPENDENT ***");
        Assert.That(ha, Is.EqualTo(hb));
    }
}
