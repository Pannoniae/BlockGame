using BlockGame.util;

namespace BlockGame;

public partial class PerlinWorldGenerator : WorldGenerator {

    public World world;

    public FastNoiseLite lowNoise;
    public FastNoiseLite highNoise;
    public FastNoiseLite selectorNoise;

    public XRandom random;

    public PerlinWorldGenerator(World world) {
        this.world = world;
    }

    public void setup(int seed) {
        random = new XRandom(seed);
        lowNoise = new FastNoiseLite(seed);
        lowNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
        highNoise = new FastNoiseLite(random.Next(seed));
        lowNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
        selectorNoise = new FastNoiseLite(random.Next(seed));
        lowNoise.SetNoiseType(FastNoiseLite.NoiseType.ValueCubic);
    }

    public float getNoise(FastNoiseLite noise, double x, double z, int octaves, double gain) {
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

    public float get3DNoise(FastNoiseLite noise, int x, int y, int z, int octaves, double gain) {
        // we want to have multiple octaves
        float result = 0;
        double amplitude = 0.5;
        double frequency = 1;
        for (int i = 0; i < octaves; i++) {
            result += (float)amplitude * noise.GetNoise(frequency * x, frequency * y, frequency * z);
            frequency *= 1 / gain;
            amplitude *= gain;
        }
        return result;
    }

}