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

    public static string getTooltip(string name) {
        return name switch {
            "perlin" => "Chaotic floating islands and fragmented terrain.",
            "overworld" => "Old test generator with a boring landscape.",
            "simple" => "(Mostly) flat testing world. Good for testing and building.",
            "new" => "Varied and interesting terrain with mountains, plains and plenty of space for building.",
            _ => "If you see this, you probably deserve a cookie!"
        };
    }

    public static string[] getAllTooltips(params ReadOnlySpan<string> names) {
        var tooltips = new string[names.Length];
        for (int i = 0; i < names.Length; i++) {
            string name = names[i].ToLower();

            // custom name!
            if (names[i] == "Default") {
                name = "new";
            }
            tooltips[i] = getTooltip(name);
        }
        return tooltips;
    }
}