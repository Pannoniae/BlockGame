using BlockGame.util;

namespace BlockGame.world.worldgen.generator;

public partial class TechDemoWorldGenerator : WorldGenerator {

    public FastNoiseLite noise;
    public World world;
    


    public TechDemoWorldGenerator(World world) {
        this.world = world;
    }

    public void setup(XRandom random, int seed) {
        noise = new FastNoiseLite(seed);
        noise.SetFrequency(0.003f);
    }
}