using BlockGame.util;

namespace BlockGame.world.worldgen.generator;

public partial class OverworldWorldGenerator : WorldGenerator {

    public World world;

    public FastNoiseLite terrainNoise;
    public FastNoiseLite terrainNoise2;
    public FastNoiseLite auxNoise;
    public FastNoiseLite auxNoise2;
    public FastNoiseLite treenoise;

    public XRandom random;

    public OverworldWorldGenerator(World world) {
        this.world = world;
    }

    public void setup(XRandom random, int seed) {
        this.random = random;
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

    public float getNoise2(int x, int z) {
        // we want to have multiple octaves
        return auxNoise.GetNoise(x, z);
    }

}