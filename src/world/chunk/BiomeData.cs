using System.Runtime.CompilerServices;
using BlockGame.world.worldgen;
using BlockGame.world.worldgen.generator;

namespace BlockGame.world.chunk;

public class BiomeData {
    private const int BIOME_X = 4;
    private const int BIOME_Y = 32;
    private const int BIOME_Z = 4;
    private const int TOTAL = BIOME_X * BIOME_Y * BIOME_Z; // 512x

    public readonly sbyte[] hum = new sbyte[TOTAL];
    public readonly sbyte[] temp = new sbyte[TOTAL];
    public readonly sbyte[] age = new sbyte[TOTAL];
    public readonly sbyte[] w = new sbyte[TOTAL];

    private Chunk? chunk;

    public void setChunk(Chunk chunk) {
        this.chunk = chunk;
    }

    /** convert biome coords to array index (YZX) */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int idx(int bx, int by, int bz) {
        return (by << 4) + (bz << 2) + bx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void setHum(int bx, int by, int bz, sbyte value) {
        hum[idx(bx, by, bz)] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void setTemp(int bx, int by, int bz, sbyte value) {
        temp[idx(bx, by, bz)] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void setAge(int bx, int by, int bz, sbyte value) {
        age[idx(bx, by, bz)] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void setW(int bx, int by, int bz, sbyte value) {
        w[idx(bx, by, bz)] = value;
    }


    public float getHum(int x, int y, int z) {
        var value = interpolate(hum, x, y, z);
        return applyDetailAndRemap(value, x, y, z);
    }

    public float getTemp(int x, int y, int z) {
        var value = interpolate(temp, x, y, z);
        return applyDetailAndRemap(value, x, y, z);
    }

    public float getAge(int x, int y, int z) {
        var value = interpolate(age, x, y, z);
        return applyDetailAndRemap(value, x, y, z);
    }

    public float getWeirdness(int x, int y, int z) {
        var value = interpolate(w, x, y, z);
        return applyDetailAndRemap(value, x, y, z);
    }

    private float applyDetailAndRemap(float value, int x, int y, int z) {
        if (chunk == null || chunk.world.generator is not NewWorldGenerator gen) {
            return value;
        }

        int wx = chunk.worldX + x;
        int wz = chunk.worldZ + z;

        var detail = WorldgenUtil.getNoise3D(gen.detailn, wx * NewWorldGenerator.DETAIL_FREQ,
            y * NewWorldGenerator.DETAIL_FREQ, wz * NewWorldGenerator.DETAIL_FREQ, 2, 2f);

        value += detail * NewWorldGenerator.DETAIL_STRENGTH;

        // remap with sqrt to normalise the simplex noise and push values toward extremes
        value = float.Sign(value) * float.Sqrt(float.Abs(value));

        return value;
    }

    /** trilinear interpolation from block coords to biome values */
    private static float interpolate(sbyte[] data, int x, int y, int z) {
        // convert block coords to biome space (each biome point covers 4x4x4 blocks)
        float bx = x * 0.25f;
        float by = y * 0.25f;
        float bz = z * 0.25f;

        int x0 = (int)bx;
        int y0 = (int)by;
        int z0 = (int)bz;

        int x1 = Math.Min(x0 + 1, BIOME_X - 1);
        int y1 = Math.Min(y0 + 1, BIOME_Y - 1);
        int z1 = Math.Min(z0 + 1, BIOME_Z - 1);

        float fx = bx - x0;
        float fy = by - y0;
        float fz = bz - z0;

        float c000 = data[idx(x0, y0, z0)] / 127f;
        float c001 = data[idx(x0, y0, z1)] / 127f;
        float c010 = data[idx(x0, y1, z0)] / 127f;
        float c011 = data[idx(x0, y1, z1)] / 127f;
        float c100 = data[idx(x1, y0, z0)] / 127f;
        float c101 = data[idx(x1, y0, z1)] / 127f;
        float c110 = data[idx(x1, y1, z0)] / 127f;
        float c111 = data[idx(x1, y1, z1)] / 127f;

        float c00 = c000 * (1 - fx) + c100 * fx;
        float c01 = c001 * (1 - fx) + c101 * fx;
        float c10 = c010 * (1 - fx) + c110 * fx;
        float c11 = c011 * (1 - fx) + c111 * fx;

        float c0 = c00 * (1 - fy) + c10 * fy;
        float c1 = c01 * (1 - fy) + c11 * fy;

        return c0 * (1 - fz) + c1 * fz;
    }
}