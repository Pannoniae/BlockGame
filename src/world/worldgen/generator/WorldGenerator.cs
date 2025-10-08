using BlockGame.world.chunk;

namespace BlockGame.world.worldgen.generator;

public interface WorldGenerator {
    public void setup(int seed);

    public void generate(ChunkCoord coord);

    public void populate(ChunkCoord coord);
}

public static class WorldGenerators {
    public static WorldGenerator create(World world, string? name) {
        return name switch {
            "perlin" => new PerlinWorldGenerator(world),
            "overworld" => new OverworldWorldGenerator(world),
            "simple" => new SimpleWorldGenerator(world),
            "new" => new NewWorldGenerator(world),
            _ => new PerlinWorldGenerator(world)
        };
    }
}