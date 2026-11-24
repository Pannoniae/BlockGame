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

    /**
     * True if the temperature and humidity this generator puts into BiomeData are the final values in [-1, 1] as opposed to simplexslop.
     * The stock generators store raw simplex noise, which BiomeData.sample() has to normalise before use.
     */
    public bool linearClimate => false;
}

// todo make this an actual registry?
public static class WorldGenerators {
    private static readonly Dictionary<string, Func<World, WorldGenerator>> factories = [];

    public static readonly List<string> all = [];

    static WorldGenerators() {
        register("v4", world => new NewWorldGenerator(world, 4));
        register("v3", world => new NewWorldGenerator(world, 3));
        register("v2", world => new NewWorldGenerator(world, 2));
        register("new", world => new NewWorldGenerator(world, 1));
        register("perlin", world => new PerlinWorldGenerator(world));
        register("overworld", world => new OverworldWorldGenerator(world));
        register("simple", world => new SimpleWorldGenerator(world));
        register("flat", world => new FlatWorldGenerator(world));
    }

    /**
     * Register a generator under a name.
     * The factory is called more than once per world (one instance per worldgen job plus one for the world itself),
     * so anything expensive should be shared between instances.
     * The displayname and tooltip come from the "generator.{name}" and "generator.{name}.tooltip" Loc keys.
     */
    public static void register(string name, Func<World, WorldGenerator> factory) {
        if (!factories.TryAdd(name, factory)) {
            InputException.throwNew($"World generator '{name}' is already registered!");
        }

        all.Add(name);
    }

    public static WorldGenerator create(World world, string? name) {
        return name != null && factories.TryGetValue(name, out var factory) ? factory(world) : new PerlinWorldGenerator(world);
    }
}