namespace BlockGame;

public class OverworldWorldGenerator : WorldGenerator {

    public World world;

    public OverworldChunkGenerator chunkGenerator;

    public FastNoiseLite noise;
    public FastNoiseLite noise2;
    public FastNoiseLite treenoise;

    public Random random;

    public OverworldWorldGenerator(World world) {
        this.world = world;
    }

    public void setup(int seed) {
        chunkGenerator = new OverworldChunkGenerator(this);
        random = new Random(seed);
        noise = new FastNoiseLite(seed);
        noise2 = new FastNoiseLite(random.Next(seed));
        treenoise = new FastNoiseLite(random.Next(seed));
        noise.SetFrequency(0.005f);
        noise2.SetFrequency(0.05f);
        treenoise.SetFrequency(1f);
    }

    public float getNoise(int x, int z) {
        // we want to have multiple octaves
        return (1f * noise.GetNoise(1 * x, 1 * z)
                + 0.5f * noise.GetNoise(2 * x, 2 * z)
                + 1 / 4f * noise.GetNoise(4 * x, 4 * z)
                + 1 / 5f * noise.GetNoise(8 * x, 8 * z)
                + 1 / 16f * noise.GetNoise(16 * x, 16 * z)) / (1f + 0.5f + 1 / 4f + 1 / 8f + 1 / 16f);
    }

    public float getNoise2(int x, int z) {
        // we want to have multiple octaves
        return (1f * noise2.GetNoise(1 * x, 1 * z)
                ) / (1f);
    }

    public void generate(ChunkCoord coord) {
        chunkGenerator.generate(coord);
    }

    public void populate(ChunkCoord coord) {
        chunkGenerator.populate(coord);
    }

}