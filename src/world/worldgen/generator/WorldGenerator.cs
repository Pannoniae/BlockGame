using BlockGame.util;
using BlockGame.world.chunk;

namespace BlockGame.world.worldgen.generator;

public interface WorldGenerator {
    public void setup(XRandom random, int seed);

    /**
     * Terrain for one chunk. Must only touch the chunk it's given - it may be a detached chunk on a worker thread
     * that isn't in the level yet, so no world.getChunk / world.setBlock in here.
     */
    public void generate(Chunk chunk);

    public void surface(ChunkCoord coord);
}

public static class WorldGenerators {
    public static readonly string[] all = ["v4", "v3", "v2", "new", "perlin", "overworld", "simple", "flat"];

    public static WorldGenerator create(World world, string? name) {
        return name switch {
            "v4" => new NewWorldGenerator(world, 4),
            "v3" => new NewWorldGenerator(world, 3),
            "v2" => new NewWorldGenerator(world, 2),
            "new" => new NewWorldGenerator(world, 1),
            "perlin" => new PerlinWorldGenerator(world),
            "overworld" => new OverworldWorldGenerator(world),
            "simple" => new SimpleWorldGenerator(world),
            "flat" => new FlatWorldGenerator(world),
            _ => new PerlinWorldGenerator(world)
        };
    }
}