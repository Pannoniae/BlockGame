namespace BlockGame;

public class OverworldWorldGenerator : WorldGenerator {

    public World world;

    public FastNoiseLite noise;
    public FastNoiseLite treenoise;

    public Random random;

    public OverworldWorldGenerator(World world) {
        this.world = world;
    }

    public void setup(int seed) {
        random = new Random(seed);
        noise = new FastNoiseLite(seed);
        treenoise = new FastNoiseLite(random.Next(seed));
        noise.SetFrequency(0.03f);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalLacunarity(2f);
        noise.SetFractalGain(0.5f);
        treenoise.SetFrequency(1f);
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