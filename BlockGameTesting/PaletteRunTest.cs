using BlockGame.util;
using BlockGame.world.chunk;

namespace BlockGameTesting;

using NUnit.Framework;

/**
 * getRawBatch/getLightBatch hoist the density switch out of the per-block path for the mesher. They have to
 * agree with the per-element accessors at every palette density, or the world quietly meshes wrong.
 */
[TestFixture]
public class PaletteRunTest {
    private const int CS = 16;

    /** distinct block IDs, to push the palette up through each density tier */
    private static PaletteBlockData make(int distinct, XRandom rnd) {
        var d = new PaletteBlockData(null!, 0);

        for (var y = 0; y < CS; y++) {
            for (var z = 0; z < CS; z++) {
                for (var x = 0; x < CS; x++) {
                    d.fastSet(x, y, z, (ushort)(distinct == 1 ? 1 : rnd.Next(1, distinct + 1)));
                    d.setLight(x, y, z, (byte)(distinct == 1 ? 7 : rnd.Next(0, int.Min(distinct, 256))));
                }
            }
        }

        return d;
    }

    [Test]
    // 4096 pushes the palette to the widest index the codec can ever be (12 bits) - that's the case where
    // its unaligned 4-byte load runs furthest past the last element
    public void runsMatchPerElement([Values(1, 2, 3, 4, 5, 15, 16, 17, 255, 1000, 4096)] int distinct) {
        var d = make(distinct, new XRandom(1337 + distinct));

        Span<uint> blocks = stackalloc uint[CS];
        Span<byte> lights = stackalloc byte[CS];

        for (var y = 0; y < CS; y++) {
            for (var z = 0; z < CS; z++) {
                var coord = (y << 8) + (z << 4);
                d.getRawBatch(coord, blocks);
                d.getLightBatch(coord, lights);

                for (var x = 0; x < CS; x++) {
                    Assert.That(blocks[x], Is.EqualTo(d.getRaw(x, y, z)),
                        $"block mismatch at {x},{y},{z} with {distinct} distinct");
                    Assert.That(lights[x], Is.EqualTo(d.getLight(x, y, z)),
                        $"light mismatch at {x},{y},{z} with {distinct} distinct");
                }
            }
        }
    }

    [Test]
    public void unalignedRunsMatchPerElement() {
        var d = make(9, new XRandom(99));

        Span<uint> blocks = stackalloc uint[5];
        Span<byte> lights = stackalloc byte[5];

        for (var start = 0; start + 5 <= CS * CS * CS; start += 37) {
            d.getRawBatch(start, blocks);
            d.getLightBatch(start, lights);

            for (var i = 0; i < 5; i++) {
                var c = start + i;
                int x = c & 0xF, z = (c >> 4) & 0xF, y = c >> 8;
                Assert.That(blocks[i], Is.EqualTo(d.getRaw(x, y, z)), $"block mismatch at coord {c}");
                Assert.That(lights[i], Is.EqualTo(d.getLight(x, y, z)), $"light mismatch at coord {c}");
            }
        }
    }
}
