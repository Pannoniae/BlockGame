using BlockGame.world.chunk;

namespace BlockGame.world.worldgen.generator;

public interface WorldGenerator {
    public void setup(int seed);

    public void generate(ChunkCoord coord);

    public void populate(ChunkCoord coord);
}