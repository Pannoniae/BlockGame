using System.Diagnostics;
using BlockGame.world;
using BlockGame.world.chunk;

namespace BlockGameTesting;

/** wall-clock worldgen benchmark over a chunk grid. [Explicit] so it doesn't run in normal test passes. */
[Explicit]
public class WorldgenBench {

    [OneTimeSetUp]
    public void Registry() {
        TestRegistry.ensure();
    }

    private static World mkWorld(string gen, int seed) =>
        new(Side.SERVER, "__bench_" + Guid.NewGuid().ToString("N")[..8], seed, generatorName: gen);

    [Test]
    public void BenchV4() => run("v4", 12);

    [Test]
    public void BenchV3() => run("v3", 12);

    /** split the two stages - terrain (noise + interpolate + surface blocks) vs populate (ores/caves/deco) */
    [Test]
    public void BenchStages() {
        run("v4", 12, ChunkStatus.GENERATED);
        run("v4", 12, ChunkStatus.POPULATED);
    }

    private void run(string gen, int r, ChunkStatus target = ChunkStatus.POPULATED) {
        // warmup
        var w = mkWorld(gen, 1338);
        for (int x = -2; x <= 2; x++)
        for (int z = -2; z <= 2; z++) {
            w.loadChunk(new ChunkCoord(x, z), target, true);
        }

        var world = mkWorld(gen, 1338);
        var sw = Stopwatch.StartNew();
        int n = 0;
        for (int x = -r; x <= r; x++) {
            for (int z = -r; z <= r; z++) {
                world.loadChunk(new ChunkCoord(x, z), target, true);
                n++;
            }
        }
        sw.Stop();

        Console.WriteLine($"=== {gen} {target}: {n} chunks in {sw.Elapsed.TotalMilliseconds:F1} ms " +
                          $"({sw.Elapsed.TotalMilliseconds / n * 1000:F0} us/chunk) ===");
    }
}
