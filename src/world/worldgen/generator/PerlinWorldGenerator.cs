using BlockGame.util;
using BlockGame.util.meth.noise;
using BlockGame.world.chunk;

namespace BlockGame.world.worldgen.generator;

public partial class PerlinWorldGenerator : WorldGenerator {
    public World world;

    public FastNoiseLite lowNoise;
    public FastNoiseLite highNoise;
    public FastNoiseLite selectorNoise;
    public FastNoiseLite auxNoise;

    public FastNoiseLite foliageNoise;
    public FastNoiseLite temperatureNoise;
    public FastNoiseLite humidityNoise;
    public FastNoiseLite weirdnessNoise;

    public XRandom random;

    public PerlinWorldGenerator(World world) {
        this.world = world;
    }

    public void setup(int seed) {
        random = new XRandom(seed);
        lowNoise = new FastNoiseLite(seed);
        //lowNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
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
        
        weirdnessNoise = new FastNoiseLite(random.Next(seed));
        weirdnessNoise.SetFrequency(1f);
        //weirdnessNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
    }

    // 2D
    public float getNoise2D(FastNoiseLite noise, double x, double z, int octaves, float falloff) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(falloff, "Falloff must be positive");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(octaves, "Octaves must be at least 1");

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
        
        var ampl = 1f / falloff;
        // we're given a number like 2, which means each octave has half the influence of the previous
        //falloff = 1f / falloff;

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

        //Console.Out.WriteLine("initialInfluence: " + initialInfluence);

        for (int i = 0; i < octaves; i++) {
            result += amplitude * noise.GetNoise((float)(x * frequency),
                (float)(y * frequency),
                (float)(z * frequency));

            // Each successive octave has influence 1/f of the previous
            amplitude *= ampl;

            //Console.Out.WriteLine($"octave {i}: freq {frequency}, amp {amplitude}");

            // Each octave doubles the frequency (halves the period)
            frequency *= falloff;
        }

        return result;
    }
    
    public float getNoise3D(OpenSimplex2S_Exp noise, double x, double y, double z, int octaves, float falloff) {
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
                result += (float)noise.noise3_XZBeforeY((float)(x * frequency),
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

        //Console.Out.WriteLine("initialInfluence: " + initialInfluence);

        for (int i = 0; i < octaves; i++) {
            result += amplitude * (float)noise.noise3_XZBeforeY((float)(x * frequency),
                (float)(y * frequency),
                (float)(z * frequency));

            // Each successive octave has influence 1/f of the previous
            amplitude *= ampl;

            //Console.Out.WriteLine($"octave {i}: freq {frequency}, amp {amplitude}");

            // Each octave doubles the frequency (halves the period)
            frequency *= falloff;
        }

        return result;
    }
    
    public double getNoise3Dfbm3(OpenSimplex2S_Exp noise, double x, double y, double z, int octaves, double lacunarity, double persistence) {
        double sum = 0;
        double amplitude = 1;
        double frequency = 1;
        double maxValue = 0;
    
        for (int i = 0; i < octaves; i++) {
            sum += noise.noise3_XYBeforeZ(x * frequency, y * frequency, z * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }
    
        return sum / maxValue;
    }


    /// <summary>
    /// Run getNoise3D on the entire chunk. This is more efficient in theory:tm:
    /// The size determines the buffer's size and the scale determines the scale of the noise. (bigger = bigger terrain features)
    /// </summary>
    public void getNoise3DRegion(float[] buffer, FastNoiseLite noise, ChunkCoord coord, double xScale, double yScale,
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
    
    public void getNoise3DRegion(float[] buffer, OpenSimplex2S_Exp noise, ChunkCoord coord, double xScale, double yScale,
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
    
    public void getNoise3DRegionfbm3(float[] buffer, OpenSimplex2S_Exp noise, ChunkCoord coord, double xScale, double yScale,
        double zScale, int octaves,
        double lacunarity, double persistence) {
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

                    buffer[getIndex(nx, ny, nz)] = (float)getNoise3Dfbm3(
                        noise,
                        x * xScale,
                        y * yScale,
                        z * zScale,
                        octaves,
                        lacunarity, persistence
                    );
                }
            }
        }
    }

    public void getNoise2DRegion(float[] buffer, FastNoiseLite noise, ChunkCoord coord, double xScale, double zScale,
        double frequency, int octaves,
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

    /// <summary>
    /// get noise information for debugging at a specific world position
    /// </summary>
    public Dictionary<string, float> getNoiseInfoAtBlock(int x, int y, int z) {
        var info = new Dictionary<string, float> {
            // sample all noises at this position using the same parameters as worldgen
            ["lowNoise"] = getNoise3D(lowNoise, x * LOW_FREQUENCY, y * LOW_FREQUENCY * Y_DIVIDER, z * LOW_FREQUENCY, 12, 2f),
            ["highNoise"] = getNoise3D(highNoise, x * HIGH_FREQUENCY, y * HIGH_FREQUENCY, z * HIGH_FREQUENCY, 12, Meth.phiF),
            ["weirdnessNoise"] = getNoise3D(weirdnessNoise, x * WEIRDNESS_FREQUENCY, y * WEIRDNESS_FREQUENCY, z * WEIRDNESS_FREQUENCY, 4, Meth.phiF),
            ["selectorNoise"] = getNoise3D(selectorNoise, x * SELECTOR_FREQUENCY, y * SELECTOR_FREQUENCY, z * SELECTOR_FREQUENCY, 4, Meth.etaF),
            ["auxNoise"] = getNoise3D(auxNoise, x * BLOCK_VARIATION_FREQUENCY, 128, z * BLOCK_VARIATION_FREQUENCY, 1, 1),
            ["foliageNoise"] = getNoise2D(foliageNoise, x * FOLIAGE_FREQUENCY, z * FOLIAGE_FREQUENCY, 2, 2),
            ["temperatureNoise"] = temperatureNoise.GetNoise(x, z),
            ["humidityNoise"] = humidityNoise.GetNoise(x, z)
        };

        // calculate final density using the same method as worldgen
        float low = info["lowNoise"];
        float high = info["highNoise"];
        float selector = info["selectorNoise"];
        float weirdness = info["weirdnessNoise"];
        
        info["airBias"] = calculateAirBias(low, high, weirdness, y);
        info["finalDensity"] = calculateDensity(low, high, selector, weirdness, y);
        
        return info;
    }
}