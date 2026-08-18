using BlockGame.world;
using BlockGame.world.chunk;

namespace BlockGameTesting;

/**
 * The server ships a chunk at LIGHTED, which only requires ring-1 to be POPULATED.
 * Ore veins walk further than that. Does a later populate mutate an already-shipped chunk?
 */
[Explicit]
public class WorldgenStaleChunk {

    [OneTimeSetUp]
    public void Registry() => TestRegistry.ensure();

    private static ulong hashChunk(Chunk c) {
        ulong h = 1469598103934665603UL;
        for (int y = 0; y < World.WORLDHEIGHT; y++)
        for (int z = 0; z < Chunk.CHUNKSIZE; z++)
        for (int x = 0; x < Chunk.CHUNKSIZE; x++) { h ^= c.getBlock(x, y, z); h *= 1099511628211UL; }
        return h;
    }

    /** which ring of later-populated chunks actually reaches back into (0,0)? */
    [Test]
    public void HowFarDoesTheSpillReach() {
        var hits = new int[6];
        for (int seed = 1000; seed < 1030; seed++) {
            var w = new World(Side.SERVER, "__reach_" + Guid.NewGuid().ToString("N")[..8], seed,
                generatorName: "v4");
            var a = new ChunkCoord(0, 0);
            w.loadChunk(a, ChunkStatus.LIGHTED, true);
            var chunk = w.getChunk(a);
            var prev = hashChunk(chunk);

            for (int ring = 2; ring <= 5; ring++) {
                for (int x = -ring; x <= ring; x++)
                for (int z = -ring; z <= ring; z++) {
                    if (Math.Max(Math.Abs(x), Math.Abs(z)) != ring) continue;
                    w.loadChunk(new ChunkCoord(x, z), ChunkStatus.POPULATED, true);
                }
                var now = hashChunk(chunk);
                if (now != prev) hits[ring]++;
                prev = now;
            }
        }
        for (int ring = 2; ring <= 5; ring++) {
            Console.WriteLine($"ring {ring}: mutated (0,0) in {hits[ring]}/30 seeds");
        }
    }

    [Test]
    public void ShippedChunkMutatedLater() {
        int diverged = 0, total = 0;

        for (int seed = 1000; seed < 1010; seed++) {
            var w = new World(Side.SERVER, "__stale_" + Guid.NewGuid().ToString("N")[..8], seed,
                generatorName: "v4");
            var a = new ChunkCoord(0, 0);

            // drive (0,0) to LIGHTED exactly as the server does before sendChunk()
            w.loadChunk(a, ChunkStatus.LIGHTED, true);
            var chunk = w.getChunk(a);
            Assert.That(chunk.status, Is.GreaterThanOrEqualTo(ChunkStatus.LIGHTED));

            // this is the snapshot the client gets
            var shipped = hashChunk(chunk);
            var snap = new ushort[World.WORLDHEIGHT * 256];
            for (int y = 0; y < World.WORLDHEIGHT; y++)
            for (int z = 0; z < Chunk.CHUNKSIZE; z++)
            for (int x = 0; x < Chunk.CHUNKSIZE; x++) { snap[(y << 8) + (z << 4) + x] = chunk.getBlock(x, y, z); }

            // player walks on: rings 2..4 populate afterwards
            for (int x = -4; x <= 4; x++)
            for (int z = -4; z <= 4; z++) {
                if (Math.Max(Math.Abs(x), Math.Abs(z)) < 2) continue;
                w.loadChunk(new ChunkCoord(x, z), ChunkStatus.POPULATED, true);
            }

            var after = hashChunk(chunk);
            total++;
            if (shipped != after) {
                diverged++;
                // which blocks moved, and how far into the chunk?
                var changes = new Dictionary<string, int>();
                int n = 0, minY = 999, maxY = -1;
                for (int y = 0; y < World.WORLDHEIGHT; y++)
                for (int z = 0; z < Chunk.CHUNKSIZE; z++)
                for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                    var now = chunk.getBlock(x, y, z);
                    if (now != snap[(y << 8) + (z << 4) + x]) {
                        n++;
                        minY = Math.Min(minY, y);
                        maxY = Math.Max(maxY, y);
                        var key = $"{BlockGame.world.block.Block.get(snap[(y << 8) + (z << 4) + x])?.name} -> {BlockGame.world.block.Block.get(now)?.name}";
                        changes[key] = changes.GetValueOrDefault(key) + 1;
                    }
                }
                var top = changes.OrderByDescending(kv => kv.Value).Take(4)
                    .Select(kv => $"{kv.Key} x{kv.Value}");
                Console.WriteLine($"seed {seed}: {n} blocks changed, y {minY}..{maxY} | {string.Join(", ", top)}");
            }
            else {
                Console.WriteLine($"seed {seed}: stable");
            }
        }

        Console.WriteLine($"{diverged}/{total} seeds diverged");
        Assert.That(diverged, Is.Zero, "server silently rewrote blocks in an already-shipped chunk");
    }
}
