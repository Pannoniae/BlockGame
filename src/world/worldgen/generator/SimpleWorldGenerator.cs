using BlockGame.util;

namespace BlockGame.world.worldgen.generator;

public partial class SimpleWorldGenerator : WorldGenerator {

    public World world;

    public FastNoiseLite noise;
    public FastNoiseLite noise2;
    public FastNoiseLite treenoise;

    public XRandom random;

    public SimpleWorldGenerator(World world) {
        this.world = world;
    }

    public void setup(int seed) {
        random = new XRandom(seed);
        noise = new FastNoiseLite(seed);
        noise2 = new FastNoiseLite(random.Next(seed));
        treenoise = new FastNoiseLite(random.Next(seed));
        noise.SetFrequency(0.02f);
        noise2.SetFrequency(0.05f);
        treenoise.SetFrequency(1f);
    }

    public float getNoise(int x, int z) {
        // we want to have multiple octaves
        return (8f * noise.GetNoise(1 / 8f * x, 1 / 8f * z)
                + 4f * noise.GetNoise(1 / 4f * x, 1 / 4f * z)
                + 2f * noise.GetNoise(1 / 2f * x, 1 / 2f * z)
                + 1f * noise.GetNoise(1 * x, 1 * z)
                + 0.5f * noise.GetNoise(2 * x, 2 * z)
                + 1 / 4f * noise.GetNoise(4 * x, 4 * z)) / (8f + 4f + 2f + 1f + 0.5f + 1 / 4f);
    }

    // 3d noise!
    public float getNoise(int x, int y, int z) {
        // we want to have multiple octaves
        return (8f * noise.GetNoise(1 / 8f * x, 1 / 8f * y, 1 / 8f * z)
                + 4f * noise.GetNoise(1 / 4f * x, 1 / 4f * y, 1 / 4f * z)
                + 2f * noise.GetNoise(1 / 2f * x, 1 / 2f * y, 1 / 2f * z)
                + 1f * noise.GetNoise(1 * x, 1 * y, 1 * z)
                + 0.5f * noise.GetNoise(2 * x, 2 * y, 2 * z)
                + 1 / 4f * noise.GetNoise(4 * x, 4 * y, 4 * z)) / (8f + 4f + 2f + 1f + 0.5f + 1 / 4f);
    }

    public float getNoise2(int x, int z) {
        // we want to have multiple octaves
        return noise2.GetNoise(1 * x, 1 * z);
    }

}