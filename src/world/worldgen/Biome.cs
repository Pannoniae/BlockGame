using BlockGame.world.block;
using BlockGame.world.worldgen.generator;

namespace BlockGame.world.worldgen;

public enum BiomeType {
    Ocean,
    Beach,
    Desert,
    Plains,
    Forest,
    Taiga,
    Jungle
}

public static class Biomes {

    /** get biome type from temp/humidity/height */
    public static BiomeType getType(float temp, float hum, int height, float blockVar) {
        // underwater
        if (height < NewWorldGenerator.WATER_LEVEL - 1) {
            return BiomeType.Ocean;
        }

        // beaches
        if (height is > NewWorldGenerator.WATER_LEVEL - 3 and < NewWorldGenerator.WATER_LEVEL + 1) {
            return BiomeType.Beach;
        }

        // above water - use temp/humidity
        // todo make a better plains/forest selection rn its "forest" without trees half the time because foliagen is < the threshold, good enough for now tho
        //  release-deadline-driven design lol
        return (temp, hum) switch {
            ( < -0.5f, > 0.4f) => BiomeType.Taiga,
            ( < -0.5f, _) => BiomeType.Plains, // cold + dry = tundra/plains
            ( > 0.5f, > 0.4f) => BiomeType.Jungle,
            ( > 0.5f, _) => BiomeType.Desert,
            _ => BiomeType.Forest // temperate
        };
    }

    /** get surface blocks for biome */
    public static (ushort top, ushort filler) getBlocks(BiomeType biome, float blockVar) {
        return biome switch {
            BiomeType.Ocean => blockVar > 0
                ? (Block.DIRT.id, Block.DIRT.id)
                : (Block.SAND.id, Block.SAND.id),

            BiomeType.Beach => blockVar > -0.2
                ? (Block.SAND.id, Block.SAND.id)
                : (Block.GRAVEL.id, Block.GRAVEL.id),

            BiomeType.Taiga or BiomeType.Plains => (Block.SNOW_GRASS.id, Block.DIRT.id),
            BiomeType.Desert => (Block.SAND.id, Block.SAND.id),
            BiomeType.Jungle or BiomeType.Forest => (Block.GRASS.id, Block.DIRT.id),
            _ => (Block.GRASS.id, Block.DIRT.id)
        };
    }

    // todo OOP the shit out of these functions later if it gets too messy

    /** tree density multiplier for biome */
    public static float getTreeDensity(BiomeType biome) {
        return biome switch {
            BiomeType.Taiga => 0.8f,
            BiomeType.Jungle => 0.6f,
            BiomeType.Forest => 1.0f,
            _ => 0f
        };
    }

    /** can place grass decoration */
    public static bool canPlaceGrass(BiomeType biome) {
        return biome is BiomeType.Plains or BiomeType.Forest or BiomeType.Taiga;
    }

    /** can place cactus */
    public static bool canPlaceCactus(BiomeType biome) {
        return biome is BiomeType.Desert;
    }

    /** can place ice on water surface */
    public static bool canPlaceIce(BiomeType biome, float temp) {
        return biome is BiomeType.Ocean or BiomeType.Taiga or BiomeType.Beach && temp < -0.3f;
    }
}
