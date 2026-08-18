using System.Runtime.CompilerServices;
using BlockGame.world.worldgen;
using BlockGame.world.worldgen.generator;

namespace BlockGame.world.chunk;

public class BiomeData {
    private const int N = 5;

    private const int TOTAL = N * N;

    public sbyte[] temp = new sbyte[TOTAL];
    public sbyte[] hum = new sbyte[TOTAL];

    private Chunk? chunk;

    public void setChunk(Chunk chunk) {
        this.chunk = chunk;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int idx(int bx, int bz) {
        return bz * N + bx;
    }

    public void set(int bx, int bz, sbyte t, sbyte h) {
        temp[idx(bx, bz)] = t;
        hum[idx(bx, bz)] = h;
    }

    public float getTemp(int x, int z) {
        sample(x, z, out var t, out _);
        return t;
    }

    public float getHum(int x, int z) {
        sample(x, z, out _, out var h);
        return h;
    }

    /**
     * helper so we dont sample the same detail twice...
     */
    public void sample(int x, int z, out float temp, out float hum) {
        var t = bilinear(this.temp, x, z);
        var h = bilinear(this.hum, x, z);

        if (chunk != null && chunk.world.generator is NewWorldGenerator gen) {
            // y pinned to 0 so the value distribution (and therefore the fe() normaliser below) is
            // unchanged from when this was a 3D field
            var detail = WorldgenUtil.getNoise3D(gen.detailn,
                (chunk.worldX + x) * NewWorldGenerator.DETAIL_FREQ, 0,
                (chunk.worldZ + z) * NewWorldGenerator.DETAIL_FREQ, 2, 2f) * NewWorldGenerator.DETAIL_STRENGTH;
            t += detail;
            h += detail;
        }

        // remap with sqrt to normalise the simplex noise and push values toward extremes
        // note: this is a bit inaccurate, especially |x|>0.9 (undercounts) but idk how to do it better
        // without a lookup table. it's NOT gaussian, it's a shorter-tailed distribution... GOOD ENOUGH:TM:
        temp = fe(t * (1 / 0.356f));
        hum = fe(h * (1 / 0.356f));
    }

    public BiomeType getBiome(int x, int z) {
        sample(x, z, out var t, out var h);
        return Biomes.getType(t, h, chunk!.heightMap.get(x, z));
    }

    public static float fe(float x) {
        const float a1 = 0.254829592f;
        const float a2 = -0.284496736f;
        const float a3 = 1.421413741f;
        const float a4 = -1.453152027f;
        const float a5 = 1.061405429f;
        const float p = 0.3275911f;

        int sign = x < 0 ? -1 : 1;

        x = Math.Abs(x);

        // A&S formula 7.1.26
        float t = 1.0f / (1.0f + p * x);
        float y = 1.0f - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * float.Exp(-x * x);

        return sign * y;
    }

    /** bilinear interp from block coords to the 4-block sample grid */
    private static float bilinear(sbyte[] data, int x, int z) {
        int x0 = x >> 2;
        int z0 = z >> 2;
        float fx = (x & 3) * 0.25f;
        float fz = (z & 3) * 0.25f;

        float c00 = data[idx(x0, z0)] * (1 / 127f);
        float c10 = data[idx(x0 + 1, z0)] * (1 / 127f);
        float c01 = data[idx(x0, z0 + 1)] * (1 / 127f);
        float c11 = data[idx(x0 + 1, z0 + 1)] * (1 / 127f);

        float c0 = c00 + (c10 - c00) * fx;
        float c1 = c01 + (c11 - c01) * fx;
        return c0 + (c1 - c0) * fz;
    }
}
