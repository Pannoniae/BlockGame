namespace BlockGame;

public class TechDemoWorldGenerator : WorldGenerator {

    public FastNoiseLite noise;
    public World world;

    public TechDemoChunkGenerator chunkGenerator;


    public TechDemoWorldGenerator(World world) {
        this.world = world;
        chunkGenerator = new TechDemoChunkGenerator(this);
    }

    public void setup(int seed) {
        noise = new FastNoiseLite(seed);
        noise.SetFrequency(0.003f);
    }

    public void generate(ChunkCoord coord) {
        chunkGenerator.generate(coord);
    }

    public void populate(ChunkCoord coord) {
        chunkGenerator.populate(coord);
    }
}