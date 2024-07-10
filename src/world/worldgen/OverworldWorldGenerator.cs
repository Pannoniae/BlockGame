namespace BlockGame;

public class OverworldWorldGenerator : WorldGenerator {

    public World world;

    public OverworldChunkGenerator chunkGenerator;

    public FastNoiseLite terrainNoise;
    public FastNoiseLite terrainNoise2;
    public FastNoiseLite auxNoise;
    public FastNoiseLite auxNoise2;
    public FastNoiseLite treenoise;

    public Random random;

    public OverworldWorldGenerator(World world) {
        this.world = world;
    }

    public void setup(int seed) {
        chunkGenerator = new OverworldChunkGenerator(this);
        random = new Random(seed);
        terrainNoise = new FastNoiseLite(seed);
        terrainNoise2 = new FastNoiseLite(random.Next(seed));
        auxNoise = new FastNoiseLite(random.Next(seed));
        auxNoise2 = new FastNoiseLite(random.Next(seed));
        treenoise = new FastNoiseLite(random.Next(seed));
        terrainNoise.SetFrequency(0.02f);
        terrainNoise2.SetFrequency(0.02f);
        auxNoise.SetFrequency(0.05f);
        auxNoise2.SetFrequency(0.05f);
        treenoise.SetFrequency(1f);
    }

    public float getNoise(int x, int z) {
        // we want to have multiple octaves
        return (8f * terrainNoise.GetNoise(1 / 8f * x, 1 / 8f * z)
                + 4f * terrainNoise.GetNoise(1 / 4f * x, 1 / 4f * z)
                + 2f * terrainNoise.GetNoise(1 / 2f * x, 1 / 2f * z)
                + 1f * terrainNoise.GetNoise(1 * x, 1 * z)
                + 0.5f * terrainNoise.GetNoise(2 * x, 2 * z)
                + 1 / 4f * terrainNoise.GetNoise(4 * x, 4 * z)) / (8f + 4f + 2f + 1f + 0.5f + 1 / 4f);
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

    // 3d noise!
    public float get3DNoise(int x, int y, int z) {
        // we want to have multiple octaves
        return (8f * terrainNoise.GetNoise(1 / 8f * x, 1 / 8f * y, 1 / 8f * z)
                + 4f * terrainNoise.GetNoise(1 / 4f * x, 1 / 4f * y, 1 / 4f * z)
                + 2f * terrainNoise.GetNoise(1 / 2f * x, 1 / 2f * y, 1 / 2f * z)
                + 1f * terrainNoise.GetNoise(1 * x, 1 * y, 1 * z)
                + 0.5f * terrainNoise.GetNoise(2 * x, 2 * y, 2 * z)
                + 1 / 4f * terrainNoise.GetNoise(4 * x, 4 * y, 4 * z)) / (8f + 4f + 2f + 1f + 0.5f + 1 / 4f);
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

    public float getNoise2(int x, int z) {
        // we want to have multiple octaves
        return auxNoise.GetNoise(x, z);
    }

    public void generate(ChunkCoord coord) {
        chunkGenerator.generate(coord);
    }

    public void populate(ChunkCoord coord) {
        chunkGenerator.populate(coord);
    }

}