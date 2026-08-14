using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BlockGame.GL.vertexformats;
using BlockGame.render;
using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.chunk;

namespace BlockGameTesting;

/**
 * Mesher tests to see if we fucked it up
 */
[TestFixture]
public class BlockRendererTest {

    [Test]
    public void average2x4MatchesScalar() {
        var rnd = new XRandom(1337);

        for (int draw = 0; draw < 64; draw++) {
            var l0 = (uint)rnd.NextInt64();
            var l1 = (uint)rnd.NextInt64();
            var l2 = (uint)rnd.NextInt64();
            var l3 = (uint)rnd.NextInt64();
            var nibbles = Vector128.Create(l0, l1, l2, l3);

            // opacity flags are 3 bits each (side1, side2, corner), so 8 values per vertex
            for (byte o0 = 0; o0 < 8; o0++) {
                for (byte o1 = 0; o1 < 8; o1++) {
                    for (byte o2 = 0; o2 < 8; o2++) {
                        for (byte o3 = 0; o3 < 8; o3++) {
                            uint packed = (uint)(o0 | (o1 << 8) | (o2 << 16) | (o3 << 24));

                            uint expected = BlockRenderer.average2(l0, o0)
                                            | (uint)(BlockRenderer.average2(l1, o1) << 8)
                                            | (uint)(BlockRenderer.average2(l2, o2) << 16)
                                            | (uint)(BlockRenderer.average2(l3, o3) << 24);

                            if (Avx2.IsSupported) {
                                Assert.That(BlockRenderer.average2x4_avx2(nibbles, packed), Is.EqualTo(expected),
                                    $"avx2 fucked: lights {l0:X8},{l1:X8},{l2:X8},{l3:X8} flags {packed:X8}");
                            }

                            if (Sse41.IsSupported) {
                                Assert.That(BlockRenderer.average2x4_sse(nibbles, packed), Is.EqualTo(expected),
                                    $"sse fucked: lights {l0:X8},{l1:X8},{l2:X8},{l3:X8} flags {packed:X8}");
                            }
                        }
                    }
                }
            }
        }
    }

    /**
     * test the ghetto divpopcnt
     */
    [Test]
    public void divideByThreeApproximationIsExactInRange() {
        for (uint sum = 0; sum <= 60; sum++) {
            var approx = (sum * 0xAAABu) >> 17;
            Assert.That(approx, Is.EqualTo(sum / 3), $"reciprocal /3 wrong at {sum}");
        }
    }

    /** both sides being blocked means the corner can't be seen */
    [Test]
    public void calculateAOFixedSaturatesWhenBothSidesAreBlocked() {
        for (byte flags = 0; flags < 8; flags++) {
            var expected = (flags & 3) == 3 ? 3 : byte.PopCount(flags);
            Assert.That(BlockRenderer.calculateAOFixed(flags), Is.EqualTo(expected), $"flags {flags}");
        }

        // spot check the two that matter: side1+side2 blocked saturates, corner alone does not
        Assert.That(BlockRenderer.calculateAOFixed(0b011), Is.EqualTo(3));
        Assert.That(BlockRenderer.calculateAOFixed(0b100), Is.EqualTo(1));
    }

    [Test]
    public void storeQuadMatchesFieldLayout() {
        var rnd = new XRandom(9001);
        Span<BlockVertexPacked> got = stackalloc BlockVertexPacked[4];

        Span<ushort> xs = stackalloc ushort[4];
        Span<ushort> ys = stackalloc ushort[4];
        Span<ushort> zs = stackalloc ushort[4];
        Span<ushort> us = stackalloc ushort[4];
        Span<ushort> vs = stackalloc ushort[4];
        Span<byte> ls = stackalloc byte[4];
        Span<uint> cs = stackalloc uint[4];

        for (int iter = 0; iter < 512; iter++) {
            for (int i = 0; i < 4; i++) {
                xs[i] = (ushort)rnd.Next(65536);
                ys[i] = (ushort)rnd.Next(65536);
                zs[i] = (ushort)rnd.Next(65536);
                us[i] = (ushort)rnd.Next(65536);
                vs[i] = (ushort)rnd.Next(65536);
                ls[i] = (byte)rnd.Next(256);
                cs[i] = (uint)rnd.NextInt64();
            }

            var A = Vector128.Create(xs[0] | (uint)ys[0] << 16, xs[1] | (uint)ys[1] << 16,
                xs[2] | (uint)ys[2] << 16, xs[3] | (uint)ys[3] << 16);
            var B = Vector128.Create(zs[0] | (uint)us[0] << 16, zs[1] | (uint)us[1] << 16,
                zs[2] | (uint)us[2] << 16, zs[3] | (uint)us[3] << 16);
            var C = Vector128.Create(vs[0] | (uint)ls[0] << 16, vs[1] | (uint)ls[1] << 16,
                vs[2] | (uint)ls[2] << 16, vs[3] | (uint)ls[3] << 16);
            var D = Vector128.Create(cs[0], cs[1], cs[2], cs[3]);

            // shift 1 is the AO winding flip, which rotates which slot each vertex lands in
            for (int shift = 0; shift < 2; shift++) {
                got.Clear();
                BlockRenderer.storeQuad(ref got[0], A, B, C, D, shift);

                for (int i = 0; i < 4; i++) {
                    var v = got[(i + shift) & 3];
                    Assert.That(v.x, Is.EqualTo(xs[i]), $"x, vertex {i}, shift {shift}");
                    Assert.That(v.y, Is.EqualTo(ys[i]), $"y, vertex {i}, shift {shift}");
                    Assert.That(v.z, Is.EqualTo(zs[i]), $"z, vertex {i}, shift {shift}");
                    Assert.That(v.u, Is.EqualTo(us[i]), $"u, vertex {i}, shift {shift}");
                    Assert.That(v.v, Is.EqualTo(vs[i]), $"v, vertex {i}, shift {shift}");
                    Assert.That(v.light, Is.EqualTo(ls[i]), $"light, vertex {i}, shift {shift}");
                    Assert.That(v.cu, Is.EqualTo(cs[i]), $"colour, vertex {i}, shift {shift}");
                    Assert.That(v.unused, Is.EqualTo(0), $"padding, vertex {i}, shift {shift}");
                }
            }
        }
    }

    /**
     * check if the layout match
     */
    [Test]
    public void faceLayoutIsSoA() {
        var f = new Face(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
            default, default, RawDirection.UP);

        ref float p = ref Unsafe.As<Face, float>(ref f);

        // x1 x2 x3 x4
        Assert.That(Unsafe.Add(ref p, 0), Is.EqualTo(1f));
        Assert.That(Unsafe.Add(ref p, 1), Is.EqualTo(4f));
        Assert.That(Unsafe.Add(ref p, 2), Is.EqualTo(7f));
        Assert.That(Unsafe.Add(ref p, 3), Is.EqualTo(10f));
        // y1 y2 y3 y4
        Assert.That(Unsafe.Add(ref p, 4), Is.EqualTo(2f));
        Assert.That(Unsafe.Add(ref p, 5), Is.EqualTo(5f));
        Assert.That(Unsafe.Add(ref p, 6), Is.EqualTo(8f));
        Assert.That(Unsafe.Add(ref p, 7), Is.EqualTo(11f));
        // z1 z2 z3 z4
        Assert.That(Unsafe.Add(ref p, 8), Is.EqualTo(3f));
        Assert.That(Unsafe.Add(ref p, 9), Is.EqualTo(6f));
        Assert.That(Unsafe.Add(ref p, 10), Is.EqualTo(9f));
        Assert.That(Unsafe.Add(ref p, 11), Is.EqualTo(12f));
    }

    [Test]
    public void fillCacheMatchesScalarGather() {
        const int EX = Chunk.CHUNKSIZEEX;
        const int EXSQ = Chunk.CHUNKSIZEEXSQ;

        var blocks = new uint[EX * EX * EX + 4];
        var lights = new byte[EX * EX * EX + 4];

        var rnd = new XRandom(4242);
        for (int i = 0; i < blocks.Length; i++) {
            blocks[i] = (uint)rnd.NextInt64();
            lights[i] = (byte)rnd.Next(256);
        }

        var br = new BlockRenderer();

        ReadOnlySpan<int> coords = [0, 1, 7, 14, 15];

        foreach (var x in coords) {
            foreach (var y in coords) {
                foreach (var z in coords) {
                    int index = (y + 1) * EXSQ + (z + 1) * EX + (x + 1);
                    br.fillCache(ref blocks[index], ref lights[index]);

                    for (int cy = 0; cy < 3; cy++) {
                        for (int cz = 0; cz < 3; cz++) {
                            for (int cx = 0; cx < 3; cx++) {
                                int ci = cy * 9 + cz * 3 + cx;
                                int si = index + (cy - 1) * EXSQ + (cz - 1) * EX + (cx - 1);

                                Assert.That(br.ctx.blockCache[ci], Is.EqualTo(blocks[si]),
                                    $"block at cache {ci}, from ({x},{y},{z})");
                                Assert.That(br.ctx.lightCache[ci], Is.EqualTo(lights[si]),
                                    $"light at cache {ci}, from ({x},{y},{z})");
                            }
                        }
                    }
                }
            }
        }
    }

    /**
     * NONE is 13 and shouldn't be shaded
     */
    [Test]
    public void noneDirectionAliasesToUnshaded() {
        Assert.That((byte)RawDirection.NONE & 0b111, Is.EqualTo((byte)RawDirection.UP));
        Assert.That(Block.a[(byte)RawDirection.NONE & 0b111], Is.EqualTo(1f));
    }
}
