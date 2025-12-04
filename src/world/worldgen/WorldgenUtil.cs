using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using BlockGame.util;
using BlockGame.util.log;
using BlockGame.world.block;
using BlockGame.world.chunk;
using BlockGame.world.worldgen.generator;

namespace BlockGame.world.worldgen;

public class WorldgenUtil {

    public static float[] interps = new float[Chunk.MAXINDEXCOL];

    public static void printNoiseResolution(float freq, int octaves, float falloff = 2f) {
        printNoiseResolution(freq, octaves, falloff, 1 / falloff);
    }

    /**
     * Print noise resolution analysis to console.
     *
     * freq: base frequency (1/scale)
     * octaves: number of octaves
     * lacunarity: frequency multiplier per octave (typically 2.0)
     * gain: amplitude multiplier per octave (typically 0.5)
     */
    public static void printNoiseResolution(float freq, int octaves, float lacunarity = 2f, float gain = 0.5f) {
        // base wavelength (feature size of first octave)
        float baseWavelength = 1f / freq;

        // highest frequency (last octave)
        float highestFreq = freq * float.Pow(lacunarity, octaves - 1);
        float smallestWavelength = 1f / highestFreq;

        // calculate approximate gradient (change per block)
        // sum all octave contributions
        float totalGradient = 0f;
        for (int i = 0; i < octaves; i++) {
            float octaveFreq = freq * float.Pow(lacunarity, i);
            float octaveAmp = float.Pow(gain, i);
            totalGradient += octaveFreq * octaveAmp;
        }

        // highest octave gradient (most detail)
        float highestOctaveAmp = float.Pow(gain, octaves - 1);
        float highestGradient = highestFreq * highestOctaveAmp;

        // sufficiency check
        bool sufficient = smallestWavelength is >= 1f and <= 4f;
        string msg;
        if (smallestWavelength < 1f) {
            msg = "TOO MANY octaves - aliasing/waste";
        }
        else if (smallestWavelength > 4f) {
            msg = "TOO FEW octaves - missing detail";
        }
        else {
            msg = "GOOD";
        }

        Log.info("=== Noise Resolution Analysis ===");
        Log.info($"Frequency: {freq:F6} (scale: {1f / freq:F2})");
        Log.info($"Octaves: {octaves}, Lacunarity: {lacunarity}, Gain: {gain}");
        Log.info("\n");
        Log.info($"1. Largest feature size: {baseWavelength:F2} blocks");
        Log.info($"2. Smallest feature size: {smallestWavelength:F2} blocks");
        Log.info($"3. Max gradient (change/block): {totalGradient:F4}");
        Log.info($"   Highest octave gradient: {highestGradient:F4}");
        Log.info("\n");
        Log.info($"Octave sufficiency: {msg}");
        Log.info("  (ideal: smallest feature = 1-4 blocks)");
        Log.info("\n");
    }

    public const int NOISE_PER_X = 4;
    public const int NOISE_PER_Y = 4;
    public const int NOISE_PER_Z = 4;
    private const int NOISE_PER_X_MASK = 3;
    private const int NOISE_PER_Y_MASK = 3;
    private const int NOISE_PER_Z_MASK = 3;
    private const int NOISE_PER_X_SHIFT = 2;
    private const int NOISE_PER_Y_SHIFT = 2;
    private const int NOISE_PER_Z_SHIFT = 2;
    private const float NOISE_PER_X_INV = 1f / NOISE_PER_X;
    private const float NOISE_PER_Y_INV = 1f / NOISE_PER_Y;
    private const float NOISE_PER_Z_INV = 1f / NOISE_PER_Z;
    public const int NOISE_SIZE_X = (Chunk.CHUNKSIZE / NOISE_PER_X) + 1;
    public const int NOISE_SIZE_Y = (Chunk.CHUNKSIZE * Chunk.CHUNKHEIGHT) / NOISE_PER_Y + 1;
    public const int NOISE_SIZE_Z = (Chunk.CHUNKSIZE / NOISE_PER_Z) + 1;

    public static void interpolate(World world, float[] buffer, ChunkCoord coord) {
        var chunk = world.getChunk(coord);

        if (Avx512F.IsSupported) {
            interpolateAVX512(chunk, buffer);
        }
        else if (Avx2.IsSupported) {
            interpolateAVX2(chunk, buffer);
        }
        else if (Sse2.IsSupported) {
            interpolateSSE(chunk, buffer);
        }
        else {
            interpolateScalar(world, chunk, buffer);
        }
    }

    private static void interpolateScalar(World world, Chunk chunk, float[] buffer) {
        for (int y = 0; y < Chunk.CHUNKSIZE * Chunk.CHUNKHEIGHT; y++) {
            var y0 = y >> NOISE_PER_Y_SHIFT;
            var y1 = y0 + 1;
            float yd = (y & NOISE_PER_Y_MASK) * NOISE_PER_Y_INV;

            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                var z0 = z >> NOISE_PER_Z_SHIFT;
                var z1 = z0 + 1;
                float zd = (z & NOISE_PER_Z_MASK) * NOISE_PER_Z_INV;

                for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                    // the grid cell that contains the point
                    var x0 = x >> NOISE_PER_X_SHIFT;
                    var x1 = x0 + 1;

                    // the lerp (between 0 and 1)
                    // float xd = (x % NOISE_PER_X) / (float)NOISE_PER_X;
                    float xd = (x & NOISE_PER_X_MASK) * NOISE_PER_X_INV;
                    //var ys = y >> 4;

                    float value;
                    {
                        // the eight corner values from the buffer
                        var c000 = buffer[getIndex(x0, y0, z0)];
                        var c001 = buffer[getIndex(x0, y0, z1)];
                        var c010 = buffer[getIndex(x0, y1, z0)];
                        var c011 = buffer[getIndex(x0, y1, z1)];
                        var c100 = buffer[getIndex(x1, y0, z0)];
                        var c101 = buffer[getIndex(x1, y0, z1)];
                        var c110 = buffer[getIndex(x1, y1, z0)];
                        var c111 = buffer[getIndex(x1, y1, z1)];

                        // Interpolate along x
                        var c00 = lerp(c000, c100, xd);
                        var c01 = lerp(c001, c101, xd);
                        var c10 = lerp(c010, c110, xd);
                        var c11 = lerp(c011, c111, xd);

                        // Interpolate along y
                        var c0 = lerp(c00, c10, yd);
                        var c1 = lerp(c01, c11, yd);

                        // Interpolate along z
                        value = lerp(c0, c1, zd);
                    }

                    // if we haven't initialised the chunk yet, do so
                    // if below sea level, water (or ice if cold)
                    if (value > 0) {
                        chunk.setBlockFast(x, y, z, Block.STONE.id);
                        chunk.addToHeightMap(x, y, z);
                    }
                    else {
                        if (y is < NewWorldGenerator.WATER_LEVEL and >= 40) {
                            // check if water should be ice (frozen)
                            if (y >= NewWorldGenerator.WATER_LEVEL - 1) {
                                var temp = chunk.biomeData.getTemp(x, y, z);
                                if (temp < -0.5f) {
                                    chunk.setBlockFast(x, y, z, Block.ICE.id);
                                    chunk.addToHeightMap(x, y, z);
                                    continue;
                                }
                            }

                            chunk.setBlockFast(x, y, z, Block.WATER.id);
                            chunk.addToHeightMap(x, y, z);
                        }
                    }
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<float> lerp(Vector128<float> a, Vector128<float> b, Vector128<float> t) {
        // a + t * (b - a)
        return Fma.MultiplyAdd(Sse.Subtract(b, a), t, a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<float> lerp(Vector256<float> a, Vector256<float> b, Vector256<float> t) {
        return Fma.IsSupported
            ? Fma.MultiplyAdd(Avx.Subtract(b, a), t, a)
            : Avx.Add(a, Avx.Multiply(t, Avx.Subtract(b, a)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector512<float> lerp(Vector512<float> a, Vector512<float> b, Vector512<float> t) {
        return Avx512F.FusedMultiplyAdd(Avx512F.Subtract(b, a), t, a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<float> broadcastTwoCells(float first, float second) {
        return Vector256.Create(first, first, first, first, second, second, second, second);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector512<float> broadcastFourCells(float a, float b, float c, float d) {
        return Vector512.Create(a, a, a, a, b, b, b, b, c, c, c, c, d, d, d, d);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<float> createXdsVector256(int blockX) {
        return Vector256.Create(
            ((blockX + 0) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 1) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 2) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 3) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 4) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 5) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 6) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 7) & NOISE_PER_X_MASK) * NOISE_PER_X_INV
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector512<float> createXdsVector512(int blockX) {
        return Vector512.Create(
            ((blockX + 0) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 1) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 2) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 3) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 4) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 5) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 6) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 7) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 8) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 9) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 10) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 11) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 12) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 13) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 14) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
            ((blockX + 15) & NOISE_PER_X_MASK) * NOISE_PER_X_INV
        );
    }

    private static void interpolateSSE(Chunk chunk, float[] buffer) {

        for (int y = 0; y < Chunk.CHUNKSIZE * Chunk.CHUNKHEIGHT; y++) {
            var y0 = y >> NOISE_PER_Y_SHIFT;
            var y1 = y0 + 1;
            float yd = (y & NOISE_PER_Y_MASK) * NOISE_PER_Y_INV;
            var yd_vec = Vector128.Create(yd);

            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                var z0 = z >> NOISE_PER_Z_SHIFT;
                var z1 = z0 + 1;
                float zd = (z & NOISE_PER_Z_MASK) * NOISE_PER_Z_INV;
                var zd_vec = Vector128.Create(zd);

                // process 4 x-values at once
                for (int x0 = 0; x0 < NOISE_PER_X; x0++) {
                    // compute x0, x1, xd for 4 consecutive x values
                    var x = x0 << NOISE_PER_X_SHIFT;
                    var x1 = x0 + 1;

                    // xd values: [0.0, 0.25, 0.5, 0.75] or [0.25, 0.5, 0.75, 1.0], etc.
                    var xds = Vector128.Create(
                        ((x + 0) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
                        ((x + 1) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
                        ((x + 2) & NOISE_PER_X_MASK) * NOISE_PER_X_INV,
                        ((x + 3) & NOISE_PER_X_MASK) * NOISE_PER_X_INV
                    );

                    // load 8 corners for x0 (shared by all 4 blocks)
                    var c000 = buffer[getIndex(x0, y0, z0)];
                    var c001 = buffer[getIndex(x0, y0, z1)];
                    var c010 = buffer[getIndex(x0, y1, z0)];
                    var c011 = buffer[getIndex(x0, y1, z1)];
                    var c100 = buffer[getIndex(x1, y0, z0)];
                    var c101 = buffer[getIndex(x1, y0, z1)];
                    var c110 = buffer[getIndex(x1, y1, z0)];
                    var c111 = buffer[getIndex(x1, y1, z1)];

                    // broadcast (same corner for all 4)
                    var c000_vec = Vector128.Create(c000);
                    var c001_vec = Vector128.Create(c001);
                    var c010_vec = Vector128.Create(c010);
                    var c011_vec = Vector128.Create(c011);
                    var c100_vec = Vector128.Create(c100);
                    var c101_vec = Vector128.Create(c101);
                    var c110_vec = Vector128.Create(c110);
                    var c111_vec = Vector128.Create(c111);

                    // interpolate along x
                    var c00 = lerp(c000_vec, c100_vec, xds);
                    var c01 = lerp(c001_vec, c101_vec, xds);
                    var c10 = lerp(c010_vec, c110_vec, xds);
                    var c11 = lerp(c011_vec, c111_vec, xds);

                    // interpolate along y
                    var c0 = lerp(c00, c10, yd_vec);
                    var c1 = lerp(c01, c11, yd_vec);

                    // interpolate along z
                    var values = lerp(c0, c1, zd_vec);

                    // store at the correct location in interps
                    values.StoreUnsafe(ref interps[x + z * Chunk.CHUNKSIZE + y * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE]);
                }
            }
        }

        // now set blocks from interps

        for (int i = 0; i < Chunk.MAXINDEXCOL; i++) {
            float value = interps[i];
            int x = i & 0xF;
            int z = (i >> 4) & 0xF;
            int y = i >> 8;


            if (value > 0) {
                chunk.setBlockFast(x, y, z, Block.STONE.id);
                chunk.addToHeightMap(x, y, z);
            }
            else {
                if (y is < NewWorldGenerator.WATER_LEVEL and >= 40) {
                    // check if water should be ice (frozen)
                    if (y >= NewWorldGenerator.WATER_LEVEL - 1) {
                        var temp = chunk.biomeData.getTemp(x, y, z);
                        if (temp < -0.5f) {
                            chunk.setBlockFast(x, y, z, Block.ICE.id);
                            chunk.addToHeightMap(x, y, z);
                            continue;
                        }
                    }

                    chunk.setBlockFast(x, y, z, Block.WATER.id);
                    chunk.addToHeightMap(x, y, z);
                }
            }
        }
    }

    /**
     * AVX512 trilinear interpolation - processes all 16 blocks in one shot.
     * Two-pass design: SIMD compute phase (high ILP) + scalar placement phase (branchy).
     *
     * 16 blocks span 5 grid cells (4 blocks per cell):
     *   blocks [0..3]   → grid cells [0, 1]
     *   blocks [4..7]   → grid cells [1, 2]
     *   blocks [8..11]  → grid cells [2, 3]
     *   blocks [12..15] → grid cells [3, 4]
     */
    private static void interpolateAVX512(Chunk chunk, float[] buffer) {
        const int width = Chunk.CHUNKSIZE;
        const int slice = width * width;
        const int height = width * Chunk.CHUNKHEIGHT;

        // hoisted constants
        var xts = Vector512.Create(
            0.00f, 0.25f, 0.50f, 0.75f, 0.00f, 0.25f, 0.50f, 0.75f,
            0.00f, 0.25f, 0.50f, 0.75f, 0.00f, 0.25f, 0.50f, 0.75f
        );
        var ztv0 = Vector512.Create(0.00f);
        var ztv1 = Vector512.Create(0.25f);
        var ztv2 = Vector512.Create(0.50f);
        var ztv3 = Vector512.Create(0.75f);
        var c00plo = Vector512.Create(0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3);
        var c00phi = Vector512.Create(1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4);
        var c01plo = Vector512.Create(5, 5, 5, 5, 6, 6, 6, 6, 7, 7, 7, 7, 8, 8, 8, 8);
        var c01phi = Vector512.Create(6, 6, 6, 6, 7, 7, 7, 7, 8, 8, 8, 8, 9, 9, 9, 9);

        for (int y = 0; y < height; y++) {
            int gridY0 = y >> NOISE_PER_Y_SHIFT;
            int gridY1 = gridY0 + 1;
            float yt = (y & NOISE_PER_Y_MASK) * NOISE_PER_Y_INV;
            var ytVec = Vector512.Create(yt);
            int yOffset = y * slice;

            // process 4 z at a time
            for (int z = 0; z < width; z += 4) {
                int gridZ0 = z >> NOISE_PER_Z_SHIFT;

                // load ONCE for all 4 z iterations (they share gridZ0)
                var y0_plane = Vector512.LoadUnsafe(ref buffer[getIndexSpecial(gridY0, gridZ0)]);
                var y1_plane = Vector512.LoadUnsafe(ref buffer[getIndexSpecial(gridY1, gridZ0)]);

                // permute ONCE (same for all 4 z)
                var c00_lo = Avx512F.PermuteVar16x32(y0_plane, c00plo);
                var c00_hi = Avx512F.PermuteVar16x32(y0_plane, c00phi);
                var c01_lo = Avx512F.PermuteVar16x32(y0_plane, c01plo);
                var c01_hi = Avx512F.PermuteVar16x32(y0_plane, c01phi);

                var c10_lo = Avx512F.PermuteVar16x32(y1_plane, c00plo);
                var c10_hi = Avx512F.PermuteVar16x32(y1_plane, c00phi);
                var c11_lo = Avx512F.PermuteVar16x32(y1_plane, c01plo);
                var c11_hi = Avx512F.PermuteVar16x32(y1_plane, c01phi);

                // X and Y lerps (same for all 4 z)
                var c0_xy = lerp(lerp(c00_lo, c00_hi, xts), lerp(c10_lo, c10_hi, xts), ytVec);
                var c1_xy = lerp(lerp(c01_lo, c01_hi, xts), lerp(c11_lo, c11_hi, xts), ytVec);

                // final Z lerps, store 4*64 bytes
                int offset = yOffset + z * width;
                lerp(c0_xy, c1_xy, ztv0).StoreUnsafe(ref interps[offset]);
                lerp(c0_xy, c1_xy, ztv1).StoreUnsafe(ref interps[offset + width]);
                lerp(c0_xy, c1_xy, ztv2).StoreUnsafe(ref interps[offset + (width * 2)]);
                lerp(c0_xy, c1_xy, ztv3).StoreUnsafe(ref interps[offset + (width * 3)]);
            }
        }

        for (int i = 0; i < Chunk.MAXINDEXCOL; i++) {
            float density = interps[i];
            int x = i & 0xF;
            int z = (i >> 4) & 0xF;
            int y = i >> 8;

            if (density > 0) {
                chunk.setBlockFast(x, y, z, Block.STONE.id);
                chunk.addToHeightMap(x, y, z);
            }
            else if (y is < NewWorldGenerator.WATER_LEVEL and >= 40) {
                // frozen water at surface in cold biomes
                if (y >= NewWorldGenerator.WATER_LEVEL - 1) {
                    float temp = chunk.biomeData.getTemp(x, y, z);
                    if (temp < -0.5f) {
                        chunk.setBlockFast(x, y, z, Block.ICE.id);
                        chunk.addToHeightMap(x, y, z);
                        continue;
                    }
                }

                chunk.setBlockFast(x, y, z, Block.WATER.id);
                chunk.addToHeightMap(x, y, z);
            }
        }
    }

    /**
     * AVX2 trilinear interpolation - processes 8 blocks per iteration.
     * Two-pass design: SIMD compute phase (high ILP) + scalar placement phase (branchy).
     *
     * Why left/mid/right? 8 blocks span 2.5 grid cells (4 blocks per cell):
     *   blocks [0..3] interpolate between grid cells [left, mid]
     *   blocks [4..7] interpolate between grid cells [mid, right]
     */
    private static void interpolateAVX2(Chunk chunk, float[] buffer) {
        const int width = Chunk.CHUNKSIZE;
        const int slice = width * width;
        const int height = width * Chunk.CHUNKHEIGHT;

        for (int y = 0; y < height; y++) {
            int gridY0 = y >> NOISE_PER_Y_SHIFT;
            int gridY1 = gridY0 + 1;
            float yt = (y & NOISE_PER_Y_MASK) * NOISE_PER_Y_INV;
            var ytVec = Vector256.Create(yt);
            int yOffset = y * slice;

            for (int z = 0; z < width; z++) {
                int gridZ0 = z >> NOISE_PER_Z_SHIFT;
                int gridZ1 = gridZ0 + 1;
                float zt = (z & NOISE_PER_Z_MASK) * NOISE_PER_Z_INV;
                var ztVec = Vector256.Create(zt);
                int offset = yOffset + z * width;

                // process 8 blocks at once (2 iterations cover all 16 blocks in X)
                for (int gridX = 0; gridX + 1 < NOISE_PER_X; gridX += 2) {
                    int blockX = gridX << NOISE_PER_X_SHIFT;
                    var xts = createXdsVector256(blockX);

                    // need 3 grid cells: [gridX, gridX+1, gridX+2]
                    int left = gridX;
                    int mid = gridX + 1;
                    int right = gridX + 2;

                    // load 12 corner values (4 cube corners * 3 grid cells)
                    float c000_L = buffer[getIndex(left, gridY0, gridZ0)];
                    float c000_M = buffer[getIndex(mid, gridY0, gridZ0)];
                    float c000_R = buffer[getIndex(right, gridY0, gridZ0)];

                    float c001_L = buffer[getIndex(left, gridY0, gridZ1)];
                    float c001_M = buffer[getIndex(mid, gridY0, gridZ1)];
                    float c001_R = buffer[getIndex(right, gridY0, gridZ1)];

                    float c010_L = buffer[getIndex(left, gridY1, gridZ0)];
                    float c010_M = buffer[getIndex(mid, gridY1, gridZ0)];
                    float c010_R = buffer[getIndex(right, gridY1, gridZ0)];

                    float c011_L = buffer[getIndex(left, gridY1, gridZ1)];
                    float c011_M = buffer[getIndex(mid, gridY1, gridZ1)];
                    float c011_R = buffer[getIndex(right, gridY1, gridZ1)];

                    var c00 = lerp(broadcastTwoCells(c000_L, c000_M), broadcastTwoCells(c000_M, c000_R), xts);
                    var c01 = lerp(broadcastTwoCells(c001_L, c001_M), broadcastTwoCells(c001_M, c001_R), xts);
                    var c10 = lerp(broadcastTwoCells(c010_L, c010_M), broadcastTwoCells(c010_M, c010_R), xts);
                    var c11 = lerp(broadcastTwoCells(c011_L, c011_M), broadcastTwoCells(c011_M, c011_R), xts);

                    var c0 = lerp(c00, c10, ytVec);
                    var c1 = lerp(c01, c11, ytVec);
                    var result = lerp(c0, c1, ztVec);

                    result.StoreUnsafe(ref interps[offset + blockX]);
                }
            }
        }

        for (int i = 0; i < Chunk.MAXINDEXCOL; i++) {
            float density = interps[i];
            int x = i & 0xF;
            int z = (i >> 4) & 0xF;
            int y = i >> 8;

            if (density > 0) {
                chunk.setBlockFast(x, y, z, Block.STONE.id);
                chunk.addToHeightMap(x, y, z);
            }
            else if (y is < NewWorldGenerator.WATER_LEVEL and >= 40) {
                // frozen water at surface in cold biomes
                if (y >= NewWorldGenerator.WATER_LEVEL - 1) {
                    float temp = chunk.biomeData.getTemp(x, y, z);
                    if (temp < -0.5f) {
                        chunk.setBlockFast(x, y, z, Block.ICE.id);
                        chunk.addToHeightMap(x, y, z);
                        continue;
                    }
                }

                chunk.setBlockFast(x, y, z, Block.WATER.id);
                chunk.addToHeightMap(x, y, z);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int getIndex(int x, int y, int z) {
        // THE PARENTHESES ARE IMPORTANT
        // otherwise it does 5 * 5 for some reason??? completely useless multiplication
        return x + z * NOISE_SIZE_X + y * (NOISE_SIZE_X * NOISE_SIZE_Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int getIndexSpecial(int y, int z) {
        // THE PARENTHESES ARE IMPORTANT
        // otherwise it does 5 * 5 for some reason??? completely useless multiplication
        return z * NOISE_SIZE_X + y * (NOISE_SIZE_X * NOISE_SIZE_Z);
    }

    private static Vector128<int> getIndex4(int x, int y, int z) {
        return Vector128.Create(x) + Vector128.Create(z) * NOISE_SIZE_X +
               Vector128.Create(y) * NOISE_SIZE_X * NOISE_SIZE_Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float lerp(float a, float b, float t) {
        return a + t * (b - a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float sample2D(float[] buffer, int x, int z) {
        var x0 = x >> NOISE_PER_X_SHIFT;
        var x1 = x0 + 1;
        var z0 = z >> NOISE_PER_Z_SHIFT;
        var z1 = z0 + 1;
        float xd = (x & NOISE_PER_X_MASK) * NOISE_PER_X_INV;
        float zd = (z & NOISE_PER_Z_MASK) * NOISE_PER_Z_INV;

        var v00 = buffer[getIndex(x0, 0, z0)];
        var v01 = buffer[getIndex(x0, 0, z1)];
        var v10 = buffer[getIndex(x1, 0, z0)];
        var v11 = buffer[getIndex(x1, 0, z1)];

        var v0 = lerp(v00, v10, xd);
        var v1 = lerp(v01, v11, xd);
        return lerp(v0, v1, zd);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float sample3D(float[] buffer, int x, int y, int z) {
        var x0 = x >> NOISE_PER_X_SHIFT;
        var x1 = x0 + 1;
        var y0 = y >> NOISE_PER_Y_SHIFT;
        var y1 = y0 + 1;
        var z0 = z >> NOISE_PER_Z_SHIFT;
        var z1 = z0 + 1;
        float xd = (x & NOISE_PER_X_MASK) * NOISE_PER_X_INV;
        float yd = (y & NOISE_PER_Y_MASK) * NOISE_PER_Y_INV;
        float zd = (z & NOISE_PER_Z_MASK) * NOISE_PER_Z_INV;

        var c000 = buffer[getIndex(x0, y0, z0)];
        var c001 = buffer[getIndex(x0, y0, z1)];
        var c010 = buffer[getIndex(x0, y1, z0)];
        var c011 = buffer[getIndex(x0, y1, z1)];
        var c100 = buffer[getIndex(x1, y0, z0)];
        var c101 = buffer[getIndex(x1, y0, z1)];
        var c110 = buffer[getIndex(x1, y1, z0)];
        var c111 = buffer[getIndex(x1, y1, z1)];

        var c00 = lerp(c000, c100, xd);
        var c01 = lerp(c001, c101, xd);
        var c10 = lerp(c010, c110, xd);
        var c11 = lerp(c011, c111, xd);

        var c0 = lerp(c00, c10, yd);
        var c1 = lerp(c01, c11, yd);

        return lerp(c0, c1, zd);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<double> lerp4(Vector256<double> a, Vector256<double> b, Vector256<double> t) {
        return a + t * (b - a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<float> lerp4(Vector128<float> a, Vector128<float> b, Vector128<float> t) {
        return a + t * (b - a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<float> lerp2(Vector128<float> a, Vector128<float> b, Vector128<float> t) {
        return a + t * (b - a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector64<float> lerp2(Vector64<float> a, Vector64<float> b, Vector64<float> t) {
        return a + t * (b - a);
    }

    /** generate deterministic octave offset from seed */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (double x, double y, double z) getOffset(int seed, int octave) {
        int hash = XHash.hash(seed ^ (octave * 1619));
        double x = ((hash & 0x3FF) - 512) * 2.0; // 10 bits, range ~-1024 to 1024
        hash >>= 10;
        double y = ((hash & 0x3FF) - 512) * 2.0;
        hash >>= 10;
        double z = ((hash & 0x3FF) - 512) * 2.0;
        return (x, y, z);
    }

    public static float getNoise2D(SimplexNoise noise, double x, double y, int octaves, float falloff) {
        float result = 0.0f;
        float frequency = 1.0f;
        float amplitude = 1 / falloff;
        float gain = 1 / falloff;

        for (int i = 0; i < octaves; i++) {
            var (ox, oy, _) = getOffset((int)noise.seed, i);
            result += amplitude * noise.noise2((float)(x * frequency + ox),
                (float)(y * frequency + oy));
            frequency *= falloff;
            amplitude *= gain;
        }

        return result;
    }

    public static float getNoise2D(ExpNoise noise, double x, double y, int octaves, float falloff) {
        float result = 0.0f;
        float frequency = 1.0f;
        float amplitude = 1 / falloff;
        float gain = 1 / falloff;

        for (int i = 0; i < octaves; i++) {
            var (ox, oy, _) = getOffset((int)noise.seed, i);
            result += amplitude * noise.noise2((float)(x * frequency + ox),
                (float)(y * frequency + oy));
            frequency *= falloff;
            amplitude *= gain;
        }

        return result;
    }

    public static float getNoise3D(SimplexNoise noise, double x, double y, double z, int octaves, float falloff) {
        float result = 0.0f;
        float frequency = 1.0f;
        float amplitude = 1 / falloff;
        var gain = 1 / falloff;

        for (int i = 0; i < octaves; i++) {
            var (ox, oy, oz) = getOffset((int)noise.seed, i);
            result += amplitude * noise.noise3_XZBeforeY((float)(x * frequency + ox),
                (float)(y * frequency + oy),
                (float)(z * frequency + oz));
            frequency *= falloff;
            amplitude *= gain;
        }

        return result;
    }

    public static float getNoise3D(ExpNoise noise, double x, double y, double z, int octaves, float falloff) {
        float result = 0.0f;
        float frequency = 1.0f;
        float amplitude = 1 / falloff;
        var gain = 1 / falloff;

        for (int i = 0; i < octaves; i++) {
            var (ox, oy, oz) = getOffset((int)noise.seed, i);
            result += amplitude * noise.noise3_XZBeforeY((float)(x * frequency + ox),
                (float)(y * frequency + oy),
                (float)(z * frequency + oz));
            frequency *= falloff;
            amplitude *= gain;
        }

        return result;
    }

    /// <summary>
    /// Run getNoise3D on the entire chunk. This is more efficient in theory:tm:
    /// The size determines the buffer's size and the scale determines the scale of the noise. (bigger = bigger terrain features)
    /// </summary>
    public static void getNoise3DRegion(float[] buffer, SimplexNoise noise, ChunkCoord coord, double xScale, double yScale,
        double zScale, int octaves,
        float falloff) {
        // Precalculate world position offsets
        int worldX = coord.x * Chunk.CHUNKSIZE;
        int worldZ = coord.z * Chunk.CHUNKSIZE;

        for (int nx = 0; nx < NOISE_SIZE_X; nx++) {
            int x = worldX + nx * NOISE_PER_X;

            for (int nz = 0; nz < NOISE_SIZE_Z; nz++) {
                int z = worldZ + nz * NOISE_PER_Z;
                // For 3D noise, sample at each Y level
                for (int ny = 0; ny < NOISE_SIZE_Y; ny++) {
                    int y = ny * NOISE_PER_Y;

                    buffer[getIndex(nx, ny, nz)] = getNoise3D(
                        noise,
                        x * xScale,
                        y * yScale,
                        z * zScale,
                        octaves,
                        falloff
                    );
                }
            }
        }
    }

    public static void getNoise2DRegion(float[] buffer, SimplexNoise noise, ChunkCoord coord, double xScale, double zScale,
        int octaves,
        float falloff) {
        // Precalculate world position offsets
        int worldX = coord.x * Chunk.CHUNKSIZE;
        int worldZ = coord.z * Chunk.CHUNKSIZE;

        for (int nx = 0; nx < NOISE_SIZE_X; nx++) {
            int x = worldX + nx * NOISE_PER_X;

            for (int nz = 0; nz < NOISE_SIZE_Z; nz++) {
                int z = worldZ + nz * NOISE_PER_Z;
                float value = getNoise2D(noise, x * xScale, z * zScale, octaves, falloff);

                for (int ny = 0; ny < NOISE_SIZE_Y; ny++) {
                    buffer[getIndex(nx, ny, nz)] = value;
                }
            }
        }
    }

    public static void getNoise2DRegion(float[] buffer, ExpNoise noise, ChunkCoord coord, double xScale, double zScale,
        int octaves,
        float falloff) {
        // Precalculate world position offsets
        int worldX = coord.x * Chunk.CHUNKSIZE;
        int worldZ = coord.z * Chunk.CHUNKSIZE;

        for (int nx = 0; nx < NOISE_SIZE_X; nx++) {
            int x = worldX + nx * NOISE_PER_X;

            for (int nz = 0; nz < NOISE_SIZE_Z; nz++) {
                int z = worldZ + nz * NOISE_PER_Z;
                float value = getNoise2D(noise, x * xScale, z * zScale, octaves, falloff);

                for (int ny = 0; ny < NOISE_SIZE_Y; ny++) {
                    buffer[getIndex(nx, ny, nz)] = value;
                }
            }
        }
    }

    public static void placeRainforestTree(World world, XRandom random, ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        var x = random.Next(0, Chunk.CHUNKSIZE);
        var z = random.Next(0, Chunk.CHUNKSIZE);
        var y = chunk.heightMap.get(x, z);

        if (y > 120) {
            return;
        }

        // must be on grass or snow grass
        var surface = chunk.getBlock(x, y, z);
        if (surface != Block.GRASS.id && surface != Block.SNOW_GRASS.id) {
            return;
        }

        var xWorld = coord.x * Chunk.CHUNKSIZE + x;
        var zWorld = coord.z * Chunk.CHUNKSIZE + z;

        // if there's stuff in the bounding box, don't place a tree
        for (int yd = 1; yd < 8; yd++) {
            for (int zd = -2; zd <= 2; zd++) {
                for (int xd = -2; xd <= 2; xd++) {
                    if (world.getBlock(xWorld, y + yd, zWorld) != Block.AIR.id) {
                        return;
                    }
                }
            }
        }

        TreeGenerator.placeMahoganyTree(world, random, xWorld, y + 1, zWorld);
    }

    public static void placeTree(World world, XRandom random, ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        var x = random.Next(0, Chunk.CHUNKSIZE);
        var z = random.Next(0, Chunk.CHUNKSIZE);
        var y = chunk.heightMap.get(x, z);

        if (y > 120) {
            return;
        }

        // must be on grass or snow grass
        var surface = chunk.getBlock(x, y, z);
        if (surface != Block.GRASS.id && surface != Block.SNOW_GRASS.id) {
            return;
        }

        var xWorld = coord.x * Chunk.CHUNKSIZE + x;
        var zWorld = coord.z * Chunk.CHUNKSIZE + z;

        // if there's stuff in the bounding box, don't place a tree
        for (int yd = 1; yd < 8; yd++) {
            for (int zd = -2; zd <= 2; zd++) {
                for (int xd = -2; xd <= 2; xd++) {
                    if (world.getBlock(xWorld, y + yd, zWorld) != Block.AIR.id) {
                        return;
                    }
                }
            }
        }

        // 1/15 chance for fancy tree
        if (random.Next(15) == 0) {
            TreeGenerator.placeFancyTree(world, random, xWorld, y + 1, zWorld);
        }
        // 1 / 20 for maple
        else if (random.Next(20) == 0) {
            TreeGenerator.placeMapleTree(world, random, xWorld, y + 1, zWorld);
        }
        else {
            TreeGenerator.placeOakTree(world, random, xWorld, y + 1, zWorld);
        }
    }

    public static void placePineTree(World world, XRandom random, ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        var x = random.Next(0, Chunk.CHUNKSIZE);
        var z = random.Next(0, Chunk.CHUNKSIZE);
        var y = chunk.heightMap.get(x, z);

        if (y > 120) {
            return;
        }

        // must be on grass or snow grass
        var surface = chunk.getBlock(x, y, z);
        if (surface != Block.GRASS.id && surface != Block.SNOW_GRASS.id) {
            return;
        }

        var xWorld = coord.x * Chunk.CHUNKSIZE + x;
        var zWorld = coord.z * Chunk.CHUNKSIZE + z;

        // if there's stuff in the bounding box, don't place a tree
        for (int yd = 1; yd < 8; yd++) {
            for (int zd = -2; zd <= 2; zd++) {
                for (int xd = -2; xd <= 2; xd++) {
                    if (world.getBlock(xWorld + xd, y + yd, zWorld + zd) != Block.AIR.id) {
                        return;
                    }
                }
            }
        }

        TreeGenerator.placePineTree(world, random, xWorld, y + 1, zWorld);
    }

    public static void placeCandyTree(World world, XRandom random, ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        var x = random.Next(0, Chunk.CHUNKSIZE);
        var z = random.Next(0, Chunk.CHUNKSIZE);
        var y = chunk.heightMap.get(x, z);

        if (y > 120) {
            return;
        }

        // must be on grass or snow grass
        var surface = chunk.getBlock(x, y, z);
        if (surface != Block.GRASS.id && surface != Block.SNOW_GRASS.id) {
            return;
        }

        var xWorld = coord.x * Chunk.CHUNKSIZE + x;
        var zWorld = coord.z * Chunk.CHUNKSIZE + z;

        // if there's stuff in the bounding box, don't place a tree
        for (int yd = 1; yd < 8; yd++) {
            for (int zd = -2; zd <= 2; zd++) {
                for (int xd = -2; xd <= 2; xd++) {
                    if (world.getBlock(xWorld, y + yd, zWorld) != Block.AIR.id) {
                        return;
                    }
                }
            }
        }

        TreeGenerator.placeCandyTree(world, random, xWorld, y + 1, zWorld);
    }

    public static float getNoise(FastNoiseLite noise, double x, double z, int octaves, double gain) {
        // we want to have multiple octaves
        float result = 0;
        double amplitude = 0.5;
        double frequency = 1;
        for (int i = 0; i < octaves; i++) {
            result += (float)amplitude * noise.GetNoise(frequency * x, frequency * z);
            frequency *= 1 / gain;
            amplitude *= gain;
        }
        return result;
    }

    public static float getNoise2DVC(SimplexNoise noise, double x, double z, int octaves, float falloff) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(falloff, "Falloff must be positive");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(octaves, "Octaves must be at least 1");

        float result;
        float frequency;

        // Special case when falloff = 1
        if (Math.Abs(falloff - 1.0f) < 0.0001f) {
            result = 0.0f;
            for (int i = 0; i < octaves; i++) {
                frequency = (float)Math.Pow(2, i);
                var (ox, oz, _) = getOffset((int)noise.seed, i);
                result += noise.noise2((float)(x * frequency + ox),
                    (float)(z * frequency + oz)) / octaves;
            }

            return result;
        }

        float n = (float)((falloff - 1) * Math.Pow(falloff, octaves)) /
                  (float)(Math.Pow(falloff, octaves) - 1);
        float initialInfluence = n / falloff;

        result = 0.0f;
        float amplitude = initialInfluence;
        frequency = 1.0f;

        for (int i = 0; i < octaves; i++) {
            var (ox, oz, _) = getOffset((int)noise.seed, i);
            result += amplitude * noise.noise2((float)(x * frequency + ox),
                (float)(z * frequency + oz));
            amplitude /= falloff;
            frequency *= 2.0f;
        }

        return result;
    }

    public static float getNoise3DFBm(SimplexNoise noise, double x, double y, double z, int octaves, float falloff) {
        float result = 0.0f;
        float frequency = 1.0f;
        float amplitude = 1 / falloff;
        var gain = 1 / falloff;

        for (int i = 0; i < octaves; i++) {
            var (ox, oy, oz) = getOffset((int)noise.seed, i);
            result += amplitude * noise.noise3_XZBeforeY((float)(x * frequency + ox),
                (float)(y * frequency + oy),
                (float)(z * frequency + oz));
            frequency *= falloff;
            amplitude *= gain;
        }

        return result;
    }

    /// TODO also replace fastNoiseLite with a custom noise generator
    /// probably caching the noise values +
    public static float getNoise3DCubic(SimplexNoise noise, double x, double y, double z, int octaves, float falloff) {
        float result;
        float frequency;

        var ampl = 1f / falloff;
        // we're given a number like 2, which means each octave has half the influence of the previous
        //falloff = 1f / falloff;

        // Special case when falloff = 1 (all octaves have equal weight)
        if (Math.Abs(falloff - 1.0f) < 0.0001f) {
            result = 0.0f;
            for (int i = 0; i < octaves; i++) {
                frequency = (float)Math.Pow(2, i);
                var (ox, oy, oz) = getOffset((int)noise.seed, i);
                result += noise.noise3_XZBeforeY((float)(x * frequency + ox),
                    (float)(y * frequency + oy),
                    (float)(z * frequency + oz)) / octaves;
            }

            return result;
        }

        // Calculate normalizer as n = ((f - 1) * f^o) / (f^o - 1)
        float n = (float)((falloff - 1) * Math.Pow(falloff, octaves)) /
                  (float)(Math.Pow(falloff, octaves) - 1);

        // Initial influence i = n/f
        float initialInfluence = n / falloff;

        result = 0.0f;
        float amplitude = initialInfluence;
        frequency = 1.0f;

        //Console.Out.WriteLine("initialInfluence: " + initialInfluence);

        for (int i = 0; i < octaves; i++) {
            var (ox, oy, oz) = getOffset((int)noise.seed, i);
            result += amplitude * noise.noise3_XZBeforeY((float)(x * frequency + ox),
                (float)(y * frequency + oy),
                (float)(z * frequency + oz));

            // Each successive octave has influence 1/f of the previous
            amplitude *= ampl;

            //Console.Out.WriteLine($"octave {i}: freq {frequency}, amp {amplitude}");

            // Each octave doubles the frequency (halves the period)
            frequency *= falloff;
        }

        return result;
    }

    public static float getNoise3Dcubic(ExpNoise noise, double x, double y, double z, int octaves, float falloff) {
        float result;
        float frequency;

        var ampl = 1f / falloff;
        // we're given a number like 2, which means each octave has half the influence of the previous
        //falloff = 1f / falloff;

        // Special case when falloff = 1 (all octaves have equal weight)
        if (Math.Abs(falloff - 1.0f) < 0.0001f) {
            result = 0.0f;
            for (int i = 0; i < octaves; i++) {
                frequency = (float)Math.Pow(2, i);
                var (ox, oy, oz) = getOffset((int)noise.seed, i);
                result += (float)noise.noise3_XZBeforeY((float)(x * frequency + ox),
                    (float)(y * frequency + oy),
                    (float)(z * frequency + oz)) / octaves;
            }

            return result;
        }

        // Calculate normalizer as n = ((f - 1) * f^o) / (f^o - 1)
        float n = (float)((falloff - 1) * Math.Pow(falloff, octaves)) /
                  (float)(Math.Pow(falloff, octaves) - 1);

        // Initial influence i = n/f
        float initialInfluence = n / falloff;

        result = 0.0f;
        float amplitude = initialInfluence;
        frequency = 1.0f;

        //Console.Out.WriteLine("initialInfluence: " + initialInfluence);

        for (int i = 0; i < octaves; i++) {
            var (ox, oy, oz) = getOffset((int)noise.seed, i);
            result += amplitude * (float)noise.noise3_XZBeforeY((float)(x * frequency + ox),
                (float)(y * frequency + oy),
                (float)(z * frequency + oz));

            // Each successive octave has influence 1/f of the previous
            amplitude *= ampl;

            //Console.Out.WriteLine($"octave {i}: freq {frequency}, amp {amplitude}");

            // Each octave doubles the frequency (halves the period)
            frequency *= falloff;
        }

        return result;
    }

    public static double getNoise3Dfbm3(ExpNoise noise, double x, double y, double z, int octaves, double lacunarity,
        double persistence) {
        double sum = 0;
        double amplitude = 1;
        double frequency = 1;
        double maxValue = 0;

        for (int i = 0; i < octaves; i++) {
            var (ox, oy, oz) = getOffset((int)noise.seed, i);
            sum += noise.noise3_XYBeforeZ(x * frequency + ox, y * frequency + oy, z * frequency + oz) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return sum / maxValue;
    }

    /**
     * Sample all noise buffers in a generator object using reflection.
     * Used for /noise command debugging.
     */
    public static string sampleBuffers(object generator, int cx, int cy, int cz, int bufferSize, string label) {
        var result = new StringBuilder();
        result.AppendLine($"Noise at chunk-relative ({cx}, {cy}, {cz}) [{label}]:");

        // use reflection to find all float[] buffers
        var fields = generator.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields) {
            if (field.FieldType == typeof(float[])) {
                var buffer = (float[])field.GetValue(generator);
                if (buffer != null && buffer.Length == bufferSize) {
                    var val = sample3D(buffer, cx, cy, cz);
                    // skip if buffer is empty (all zeros)
                    if (val != 0f || Array.Exists(buffer, v => v != 0f)) {
                        result.AppendLine($"  {field.Name,-15}: {val:F4}");
                    }
                }
            }
        }

        return result.ToString();
    }

    /** generate 3D noise for biome data (5x40x5 grid covering chunk + boundaries) */
    public static void getNoise3DRegionBiome(float[] buffer, SimplexNoise noise, ChunkCoord coord,
        double xScale, double yScale, double zScale, int octaves, float falloff,
        int sizeX, int sizeY, int sizeZ) {
        int worldX = coord.x * Chunk.CHUNKSIZE;
        int worldZ = coord.z * Chunk.CHUNKSIZE;

        for (int nx = 0; nx < sizeX; nx++) {
            int x = worldX + nx * 4; // biome grid spacing is 4 blocks

            for (int nz = 0; nz < sizeZ; nz++) {
                int z = worldZ + nz * 4;

                for (int ny = 0; ny < sizeY; ny++) {
                    int y = ny * 4; // biome grid spacing is 4 blocks vertically

                    int idx = (ny * sizeZ + nz) * sizeX + nx;
                    buffer[idx] = getNoise3D(noise, x * xScale, y * yScale, z * zScale, octaves, falloff);
                }
            }
        }
    }
}