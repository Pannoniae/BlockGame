namespace BlockGame;

public class TechDemoWorldGenerator : WorldGenerator {

    public FastNoiseLite noise;
    public World world;


    public TechDemoWorldGenerator(World world) {
        this.world = world;
    }

    public void setup(int seed) {
        noise = new FastNoiseLite(seed);
        noise.SetFrequency(0.003f);
    }

    public void generate(ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        chunk.generator.generate();
    }

    public void populate(ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        chunk.generator.populate();
    }
}