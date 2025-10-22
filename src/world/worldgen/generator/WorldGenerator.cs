using BlockGame.util;
using BlockGame.world.chunk;

namespace BlockGame.world.worldgen.generator;

public interface WorldGenerator {
    public void setup(XRandom random, int seed);

    public void generate(ChunkCoord coord);

    public void surface(ChunkCoord coord);
}

public static class WorldGenerators {
    public static readonly string[] all = ["v2", "new", "perlin", "overworld", "simple"];

    public static WorldGenerator create(World world, string? name) {
        return name switch {
            "v2" => new NewWorldGenerator(world, true),
            "new" => new NewWorldGenerator(world, false),
            "perlin" => new PerlinWorldGenerator(world),
            "overworld" => new OverworldWorldGenerator(world),
            "simple" => new SimpleWorldGenerator(world),

            _ => new PerlinWorldGenerator(world)
        };
    }
}