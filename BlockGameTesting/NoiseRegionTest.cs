using System.Diagnostics;
using BlockGame.util;
using BlockGame.world.chunk;
using BlockGame.world.worldgen;
using BlockGame.world.worldgen.generator;

namespace BlockGameTesting;

/** SIMD region noise perfbench shh*/
public class NoiseRegionTest {
    const int NX = WorldgenUtil.NOISE_SIZE_X, NY = WorldgenUtil.NOISE_SIZE_Y, NZ = WorldgenUtil.NOISE_SIZE_Z;
    const int N = NX * NY * NZ;

    NewWorldGenerator gen;

    [SetUp]
    public void setup() {
        gen = new NewWorldGenerator(null!, 4);
        gen.setup(new XRandom(1338), 1338);
    }

    [Test]
    public void seams() {
        var a = new float[N];
        var b = new float[N];
        var c = new float[N];
        foreach (var (cx, cz) in new[] { (0, 0), (17, -33), (-1000, 4095), (123456, -98765) }) {
            WorldgenUtil.getNoise3DRegion(a, gen.tn, new ChunkCoord(cx, cz), NewWorldGenerator.LOW_FREQ, NewWorldGenerator.LOW_FREQ * 2, NewWorldGenerator.LOW_FREQ, 8, 1 + Meth.rhoF * 2);
            WorldgenUtil.getNoise3DRegion(b, gen.tn, new ChunkCoord(cx + 1, cz), NewWorldGenerator.LOW_FREQ, NewWorldGenerator.LOW_FREQ * 2, NewWorldGenerator.LOW_FREQ, 8, 1 + Meth.rhoF * 2);
            WorldgenUtil.getNoise3DRegion(c, gen.tn, new ChunkCoord(cx, cz + 1), NewWorldGenerator.LOW_FREQ, NewWorldGenerator.LOW_FREQ * 2, NewWorldGenerator.LOW_FREQ, 8, 1 + Meth.rhoF * 2);
            for (int y = 0; y < NY; y++) {
                for (int z = 0; z < NZ; z++) {
                    Assert.That(b[WorldgenUtil.getIndex(0, y, z)], Is.EqualTo(a[WorldgenUtil.getIndex(NX - 1, y, z)]), $"x seam at chunk ({cx},{cz}) y={y} z={z}");
                }
                for (int x = 0; x < NX; x++) {
                    Assert.That(c[WorldgenUtil.getIndex(x, y, 0)], Is.EqualTo(a[WorldgenUtil.getIndex(x, y, NZ - 1)]), $"z seam at chunk ({cx},{cz}) y={y} x={x}");
                }
            }
        }
    }

    [Test]
    public void sane() {
        var buf = new float[N];
        double sum = 0, sumSq = 0;
        float min = float.MaxValue, max = float.MinValue;
        const int chunks = 64;
        for (int i = 0; i < chunks; i++) {
            WorldgenUtil.getNoise3DRegion(buf, gen.tn, new ChunkCoord(i * 7, -i * 3), NewWorldGenerator.LOW_FREQ, NewWorldGenerator.LOW_FREQ * 2, NewWorldGenerator.LOW_FREQ, 8, 1 + Meth.rhoF * 2);
            for (int k = 0; k < N; k++) {
                var v = buf[k];
                Assert.That(float.IsFinite(v), $"non-finite at chunk {i} idx {k}");
                sum += v;
                sumSq += v * v;
                min = float.Min(min, v);
                max = float.Max(max, v);
            }
        }
        var n = (double)chunks * N;
        var mean = sum / n;
        var std = double.Sqrt(sumSq / n - mean * mean);
        Console.WriteLine($"mean {mean:F4} std {std:F4} min {min:F4} max {max:F4}");
        Assert.That(float.Abs(min), Is.LessThan(1.5f));
        Assert.That(max, Is.LessThan(1.5f));
        Assert.That(float.Abs((float)mean), Is.LessThan(0.05f));
        Assert.That(std, Is.GreaterThan(0.05));
    }

    [Test, Explicit]
    public void bench() {
        var buf = new float[N];
        const int R = 500;
        // warmup
        for (int i = 0; i < 100; i++) {
            WorldgenUtil.getNoise3DRegion(buf, gen.tn, new ChunkCoord(i, i), NewWorldGenerator.LOW_FREQ, NewWorldGenerator.LOW_FREQ * 2, NewWorldGenerator.LOW_FREQ, 8, 1 + Meth.rhoF * 2);
            scalarRegion(buf, gen.tn, new ChunkCoord(i, i), NewWorldGenerator.LOW_FREQ, NewWorldGenerator.LOW_FREQ * 2, NewWorldGenerator.LOW_FREQ, 8, 1 + Meth.rhoF * 2);
        }
        double simd = double.MaxValue, scalar = double.MaxValue;
        for (int round = 0; round < 5; round++) {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < R; i++) WorldgenUtil.getNoise3DRegion(buf, gen.tn, new ChunkCoord(i, -i), NewWorldGenerator.LOW_FREQ, NewWorldGenerator.LOW_FREQ * 2, NewWorldGenerator.LOW_FREQ, 8, 1 + Meth.rhoF * 2);
            simd = double.Min(simd, sw.Elapsed.TotalMilliseconds / R);
            sw.Restart();
            for (int i = 0; i < R; i++) scalarRegion(buf, gen.tn, new ChunkCoord(i, -i), NewWorldGenerator.LOW_FREQ, NewWorldGenerator.LOW_FREQ * 2, NewWorldGenerator.LOW_FREQ, 8, 1 + Meth.rhoF * 2);
            scalar = double.Min(scalar, sw.Elapsed.TotalMilliseconds / R);
        }
        Console.WriteLine($"3D region, 825 pts x 8 oct: scalar {scalar * 1000:F1} us, simd {simd * 1000:F1} us => {scalar / simd:F2}x  (simd={OpenSimplex2.simdSupported})");
    }

    static void scalarRegion(float[] buffer, SimplexNoise noise, ChunkCoord coord, double xScale, double yScale, double zScale, int octaves, float falloff) {
        int worldX = coord.x * Chunk.CHUNKSIZE, worldZ = coord.z * Chunk.CHUNKSIZE;
        for (int nx = 0; nx < NX; nx++) {
            int x = worldX + nx * WorldgenUtil.NOISE_PER_X;
            for (int nz = 0; nz < NZ; nz++) {
                int z = worldZ + nz * WorldgenUtil.NOISE_PER_Z;
                for (int ny = 0; ny < NY; ny++) {
                    int y = ny * WorldgenUtil.NOISE_PER_Y;
                    buffer[WorldgenUtil.getIndex(nx, ny, nz)] = WorldgenUtil.getNoise3D(noise, x * xScale, y * yScale, z * zScale, octaves, falloff);
                }
            }
        }
    }
}
