using BlockGame.util;
using BlockGame.world.chunk;

namespace BlockGame.world.worldgen.generator;

/**
 * Do I look like I know what I'm doing? If you think so, go to the optician plz
 */
public partial class NewWorldGenerator : WorldGenerator {
    public World world;

    public SimplexNoise tn;
    public SimplexNoise t2n;
    public SimplexNoise sn;
    public ExpNoise en;
    public ExpNoise fn;
    public SimplexNoise gn;
    public SimplexNoise mn;
    public SimplexNoise on;
    public SimplexNoise auxn;

    public SimplexNoise foliagen;
    public SimplexNoise tempn;
    public SimplexNoise humn;
    public SimplexNoise wn;

    public XRandom random;

    public NewWorldGenerator(World world) {
        this.world = world;
    }

    public void setup(int seed) {
        random = new XRandom(seed);
        tn = new SimplexNoise(seed);
        t2n = new SimplexNoise(random.Next(seed));
        sn = new SimplexNoise(random.Next(seed));
        var s = random.Next(seed);

        // a noobtrap is making the exp too high, so it's endless plains lol

        en = new ExpNoise(s);
        en.setExp(s, float.E, 0.1f);
        s = random.Next(seed);
        fn = new ExpNoise(s);
        fn.setExp(s, float.E, 0.1f);
        gn = new SimplexNoise(random.Next(seed));
        mn = new SimplexNoise(random.Next(seed));

        on = new SimplexNoise(random.Next(seed));

        auxn = new SimplexNoise(random.Next(seed));

        foliagen = new SimplexNoise(random.Next(seed));
        tempn = new SimplexNoise(random.Next(seed));
        humn = new SimplexNoise(random.Next(seed));

        wn = new SimplexNoise(random.Next(seed));
    }

    public float getNoise2D(SimplexNoise noise, double x, double y, int octaves, float falloff) {
        float result = 0.0f;
        float frequency = 1.0f;
        float amplitude = 1 / falloff;
        float gain = 1 / falloff;

        for (int i = 0; i < octaves; i++) {
            result += amplitude * noise.noise2((float)(x * frequency),
                (float)(y * frequency));
            frequency *= falloff;
            amplitude *= gain;
        }

        return result;
    }

    public float getNoise2D(ExpNoise noise, double x, double y, int octaves, float falloff) {
        float result = 0.0f;
        float frequency = 1.0f;
        float amplitude = 1 / falloff;
        float gain = 1 / falloff;

        for (int i = 0; i < octaves; i++) {
            result += amplitude * noise.noise2((float)(x * frequency),
                (float)(y * frequency));
            frequency *= falloff;
            amplitude *= gain;
        }

        return result;
    }

    public float getNoise3D(SimplexNoise noise, double x, double y, double z, int octaves, float falloff) {
        float result = 0.0f;
        float frequency = 1.0f;
        float amplitude = 1 / falloff;
        var gain = 1 / falloff;

        for (int i = 0; i < octaves; i++) {
            result += amplitude * noise.noise3_XZBeforeY((float)(x * frequency),
                (float)(y * frequency),
                (float)(z * frequency));
            frequency *= falloff;
            amplitude *= gain;
        }

        return result;
    }

    public float getNoise3D(ExpNoise noise, double x, double y, double z, int octaves, float falloff) {
        float result = 0.0f;
        float frequency = 1.0f;
        float amplitude = 1 / falloff;
        var gain = 1 / falloff;

        for (int i = 0; i < octaves; i++) {
            result += amplitude * noise.noise3_XZBeforeY((float)(x * frequency),
                (float)(y * frequency),
                (float)(z * frequency));
            frequency *= falloff;
            amplitude *= gain;
        }

        return result;
    }


    /// <summary>
    /// Run getNoise3D on the entire chunk. This is more efficient in theory:tm:
    /// The size determines the buffer's size and the scale determines the scale of the noise. (bigger = bigger terrain features)
    /// </summary>
    public void getNoise3DRegion(float[] buffer, SimplexNoise noise, ChunkCoord coord, double xScale, double yScale,
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

    public void getNoise2DRegion(float[] buffer, SimplexNoise noise, ChunkCoord coord, double xScale, double zScale,
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

    public void getNoise2DRegion(float[] buffer, ExpNoise noise, ChunkCoord coord, double xScale, double zScale,
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
}