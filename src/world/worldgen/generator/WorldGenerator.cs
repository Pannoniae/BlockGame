using BlockGame.util;
using BlockGame.world.chunk;

namespace BlockGame.world.worldgen.generator;

public interface WorldGenerator {
    public void setup(XRandom random, int seed);

    public void generate(ChunkCoord coord);

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