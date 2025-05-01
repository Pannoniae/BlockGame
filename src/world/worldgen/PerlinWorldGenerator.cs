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
        lowNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
        selectorNoise = new FastNoiseLite(random.Next(seed));
        selectorNoise.SetFrequency(1f);

        selectorNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        auxNoise = new FastNoiseLite(random.Next(seed));
        auxNoise.SetFrequency(1f);
        
        foliageNoise = new FastNoiseLite(random.Next(seed));
        foliageNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
        foliageNoise.SetFrequency(1f);
        temperatureNoise = new FastNoiseLite(random.Next(seed));
        temperatureNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
        temperatureNoise.SetFrequency(1f);
        humidityNoise = new FastNoiseLite(random.Next(seed));
        humidityNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
        humidityNoise.SetFrequency(1f);
    }
    
    // 2D
    public float getNoise(FastNoiseLite noise, double x, double z, int octaves, float falloff) {
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

    public float getNoise3D(FastNoiseLite noise, double x, double y, double z, int octaves, float falloff) {
        if (falloff <= 0.0f) {
            throw new ArgumentException("Falloff must be positive");
        }

        if (octaves <= 0) {
            throw new ArgumentException("Octaves must be at least 1");
        }
        
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
}