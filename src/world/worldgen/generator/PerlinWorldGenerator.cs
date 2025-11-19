using BlockGame.util;
using BlockGame.util.meth.noise;
using BlockGame.world.chunk;
using BlockGame.world.worldgen.surface;

namespace BlockGame.world.worldgen.generator;

public partial class PerlinWorldGenerator : WorldGenerator {
    public World world;

    public SimplexNoise lowNoise;
    public SimplexNoise highNoise;
    public SimplexNoise selectorNoise;
    public SimplexNoise auxNoise;

    public SimplexNoise foliageNoise;
    public SimplexNoise temperatureNoise;
    public SimplexNoise humidityNoise;
    public SimplexNoise weirdnessNoise;

    public XRandom random;
    private readonly SurfaceGenerator surfacegen;

    public PerlinWorldGenerator(World world) {
        this.world = world;
        surfacegen = new NewSurfaceGenerator(this, world, 1); // old generator, use version 1
    }

    public void setup(XRandom random, int seed) {
        this.random = random;
        lowNoise = new SimplexNoise(seed);
        highNoise = new SimplexNoise(random.Next(seed));
        selectorNoise = new SimplexNoise(random.Next(seed));

        auxNoise = new SimplexNoise(random.Next(seed));

        foliageNoise = new SimplexNoise(random.Next(seed));
        temperatureNoise = new SimplexNoise(random.Next(seed));
        humidityNoise = new SimplexNoise(random.Next(seed));

        weirdnessNoise = new SimplexNoise(random.Next(seed));

        surfacegen.setup(random, seed);
    }

    /// <summary>
    /// get noise information for debugging at a specific world position
    /// </summary>
    public Dictionary<string, float> getNoiseInfoAtBlock(int x, int y, int z) {
        var info = new Dictionary<string, float> {
            // sample all noises at this position using the same parameters as worldgen
            ["lowNoise"] = WorldgenUtil.getNoise3DCubic(lowNoise, x * LOW_FREQUENCY, y * LOW_FREQUENCY * Y_DIVIDER, z * LOW_FREQUENCY, 12, 2f),
            ["highNoise"] = WorldgenUtil.getNoise3DCubic(highNoise, x * HIGH_FREQUENCY, y * HIGH_FREQUENCY, z * HIGH_FREQUENCY, 12, Meth.phiF),
            ["weirdnessNoise"] = WorldgenUtil.getNoise3DCubic(weirdnessNoise, x * WEIRDNESS_FREQUENCY, y * WEIRDNESS_FREQUENCY, z * WEIRDNESS_FREQUENCY, 4, Meth.phiF),
            ["selectorNoise"] = WorldgenUtil.getNoise3DCubic(selectorNoise, x * SELECTOR_FREQUENCY, y * SELECTOR_FREQUENCY, z * SELECTOR_FREQUENCY, 4, Meth.etaF),
            ["auxNoise"] = WorldgenUtil.getNoise3DCubic(auxNoise, x * BLOCK_VARIATION_FREQUENCY, 128, z * BLOCK_VARIATION_FREQUENCY, 1, 1),
            ["foliageNoise"] = WorldgenUtil.getNoise2D(foliageNoise, x * FOLIAGE_FREQUENCY, z * FOLIAGE_FREQUENCY, 2, 2),
            ["temperatureNoise"] = temperatureNoise.noise2(x, z),
            ["humidityNoise"] = humidityNoise.noise2(x, z)
        };

        // calculate final density using the same method as worldgen
        float low = info["lowNoise"];
        float high = info["highNoise"];
        float selector = info["selectorNoise"];
        float weirdness = info["weirdnessNoise"];
        
        info["airBias"] = calculateAirBias(low, high, weirdness, y);
        info["finalDensity"] = calculateDensity(low, high, selector, weirdness, y);
        
        return info;
    }
}