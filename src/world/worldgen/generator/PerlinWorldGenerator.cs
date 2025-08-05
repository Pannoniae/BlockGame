using BlockGame.util;

namespace BlockGame;

public partial class PerlinWorldGenerator : WorldGenerator {
    public World world;

    public FastNoiseLite lowNoise;
    public FastNoiseLite highNoise;
    public FastNoiseLite selectorNoise;
    public FastNoiseLite auxNoise;

    public FastNoiseLite foliageNoise;
    public FastNoiseLite temperatureNoise;
    public FastNoiseLite humidityNoise;

    public XRandom random;

    public PerlinWorldGenerator(World world) {
        this.world = world;
    }

    public void setup(int seed) {
        random = new XRandom(seed);
        lowNoise = new FastNoiseLite(seed);
        lowNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
        lowNoise.SetFrequency(1f);
        highNoise = new FastNoiseLite(random.Next(seed));
        highNoise.SetFrequency(1f);
        //lowNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
        // todo
        //highNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
        selectorNoise = new FastNoiseLite(random.Next(seed));
        selectorNoise.SetFrequency(1f);
        

        // DONT UNCOMMENT THIS
        // yes the selector is broken, trying to use the valuecubic interpolation with opensimplex noise
        // however this makes it wildly swing to 1 and -1 which is exactly what we want lol
        // so the terrain isn't a hilly mess but either flat or just straight-up shattered cliffs and stuff
        //selectorNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);

        auxNoise = new FastNoiseLite(random.Next(seed));
        auxNoise.SetFrequency(1f);
        //auxNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);

        foliageNoise = new FastNoiseLite(random.Next(seed));
        //foliageNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
        foliageNoise.SetFrequency(1f);
        temperatureNoise = new FastNoiseLite(random.Next(seed));
        //temperatureNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
        temperatureNoise.SetFrequency(1f);
        humidityNoise = new FastNoiseLite(random.Next(seed));
        //humidityNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
        humidityNoise.SetFrequency(1f);
    }

    // 2D
    public float getNoise2D(FastNoiseLite noise, double x, double z, int octaves, float falloff) {
        if (falloff <= 0.0f) {
            throw new ArgumentException("Falloff must be positive");
        }

        if (octaves <= 0) {
            throw new ArgumentException("Octaves must be at least 1");
        }

        float result;
        float frequency;

        // Special case when falloff = 1
        if (Math.Abs(falloff - 1.0f) < 0.0001f) {
            result = 0.0f;
            for (int i = 0; i < octaves; i++) {
                frequency = (float)Math.Pow(2, i);
                result += noise.GetNoise((float)(x * frequency),
                    (float)(z * frequency)) / octaves;
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
            result += amplitude * noise.GetNoise((float)(x * frequency),
                (float)(z * frequency));
            amplitude /= falloff;
            frequency *= 2.0f;
        }

        return result;
    }

    public float getNoise3DFBm(FastNoiseLite noise, double x, double y, double z, int octaves, float falloff) {
        float result = 0.0f;
        float frequency = 1.0f;
        float amplitude = 0.5f;
        float gain = 1 / falloff;

        for (int i = 0; i < octaves; i++) {
            result += noise.GetNoise((float)(x * frequency),
                (float)(y * frequency),
                (float)(z * frequency)) / octaves;
            frequency *= falloff;
            amplitude *= gain;
        }

        return result;
    }

    /// TODO also replace fastNoiseLite with a custom noise generator
    /// probably caching the noise values + 
    public float getNoise3D(FastNoiseLite noise, double x, double y, double z, int octaves, float falloff) {
        float result;
        float frequency;

        // Special case when falloff = 1 (all octaves have equal weight)
        if (Math.Abs(falloff - 1.0f) < 0.0001f) {
            result = 0.0f;
            for (int i = 0; i < octaves; i++) {
                frequency = (float)Math.Pow(2, i);
                result += noise.GetNoise((float)(x * frequency),
                    (float)(y * frequency),
                    (float)(z * frequency)) / octaves;
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

        for (int i = 0; i < octaves; i++) {
            result += amplitude * noise.GetNoise((float)(x * frequency),
                (float)(y * frequency),
                (float)(z * frequency));

            // Each successive octave has influence 1/f of the previous
            amplitude /= falloff;

            // Each octave doubles the frequency (halves the period)
            frequency *= 2.0f;
        }

        return result;
    }


    /// <summary>
    /// Run getNoise3D on the entire chunk. This is more efficient in theory:tm:
    /// The size determines the buffer's size and the scale determines the scale of the noise. (bigger = bigger terrain features)
    /// </summary>
    public void getNoise3DRegion(float[] buffer, FastNoiseLite noise, ChunkCoord coord, double xScale, double yScale, double zScale, int octaves,
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

    public void getNoise2DRegion(float[] buffer, FastNoiseLite noise, ChunkCoord coord, double xScale, double zScale, double frequency, int octaves,
        float falloff) {

        // Precalculate world position offsets
        int worldX = coord.x * Chunk.CHUNKSIZE;
        int worldZ = coord.z * Chunk.CHUNKSIZE;

        for (int nx = 0; nx < NOISE_SIZE_X; nx++) {
            int x = worldX + nx * NOISE_PER_X;

            for (int nz = 0; nz < NOISE_SIZE_Z; nz++) {
                int z = worldZ + nz * NOISE_PER_Z;
                float value = getNoise2D(noise, x * frequency * xScale, z * frequency * zScale, octaves, falloff);

                for (int ny = 0; ny < NOISE_SIZE_Y; ny++) {
                    buffer[getIndex(nx, ny, nz)] = value;
                }
            }
        }
    }
}