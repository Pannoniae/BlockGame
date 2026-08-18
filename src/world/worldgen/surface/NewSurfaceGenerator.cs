using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.chunk;
using BlockGame.world.worldgen.feature;
using BlockGame.world.worldgen.generator;

namespace BlockGame.world.worldgen.surface;

// todo this is a terrible hack to reduce copypasta but it should really be a proper registry or something
public enum TreeType {
    Oak,
    Fancy,
    Maple,
    Pine,
    Candy,
    Palm,
    MahoganySmall,
    MahoganyMedium,
    MahoganyHuge
}

public class NewSurfaceGenerator : SurfaceGenerator {
    public WorldGenerator worldgen;
    public World world;

    public SimplexNoise foliagen;

    public const float FREQFOLIAGE = 1 / 19f;

    private readonly Cave caves = new();
    private readonly Ravine ravines = new();

    public readonly record struct Vein(OreFeature ore, int count, int minY, int maxY) {
        public readonly OreFeature ore = ore;
        public readonly int count = count;
        public readonly int minY = minY;
        public readonly int maxY = maxY;
    }

    public readonly List<Vein> veins;

    /** one tree kind and its share of a biome's rolls */
    public readonly record struct TreeEntry(TreeType type, float weight) {
        public readonly TreeType type = type;
        public readonly float weight = weight;
    }

    public readonly record struct BiomeTrees(int count, TreeEntry[] kinds) {
        // prolly should be a % or a float or sth instead of a for loop but oh well
        public readonly int count = count;
        public readonly TreeEntry[] kinds = kinds;
    }

    private static readonly TreeEntry[] forestTrees = [
        new(TreeType.Oak, 14f), new(TreeType.Fancy, 1f), new(TreeType.Maple, 0.7f)
    ];

    private static readonly TreeEntry[] plainsTrees = [new(TreeType.Oak, 1f), new(TreeType.Candy, 0.05f)];
    private static readonly TreeEntry[] taigaTrees = [new(TreeType.Pine, 1f)];
    private static readonly TreeEntry[] desertTrees = [new(TreeType.Palm, 1f)];

    private static readonly TreeEntry[] jungleTrees = [
        new(TreeType.MahoganySmall, 6f), new(TreeType.MahoganyMedium, 3f), new(TreeType.MahoganyHuge, 1f)
    ];

    private static BiomeTrees getTrees(BiomeType b) {
        return b switch {
            BiomeType.Forest => new BiomeTrees(43, forestTrees),
            BiomeType.Jungle => new BiomeTrees(59, jungleTrees),
            BiomeType.Taiga => new BiomeTrees(34, taigaTrees),
            BiomeType.Plains => new BiomeTrees(2, plainsTrees),
            BiomeType.Desert => new BiomeTrees(1, desertTrees),
            _ => default
        };
    }

    private readonly BiomeType[] biomes = new BiomeType[Chunk.CHUNKSIZE * Chunk.CHUNKSIZE];

    private readonly byte[] height = new byte[Chunk.CHUNKSIZE * Chunk.CHUNKSIZE];

    public NewSurfaceGenerator(WorldGenerator worldgen, World world, int version) {
        this.worldgen = worldgen;
        this.world = world;

        veins = [
            new Vein(new OreFeature(Block.COAL_ORE.id, 16), 16, 0, World.WORLDHEIGHT),
            // copper spawns more on the surface!
            new Vein(new OreFeature(Block.COPPER_ORE.id, 12), 16, World.WORLDHEIGHT / 4,
                (int)(World.WORLDHEIGHT * (3 / 4f))),
            new Vein(new OreFeature(Block.TIN_ORE.id, 8), 12, 0, World.WORLDHEIGHT / 2),
            new Vein(new OreFeature(Block.IRON_ORE.id, 8), 16, 0, World.WORLDHEIGHT / 2),
            new Vein(new OreFeature(Block.GOLD_ORE.id, 8), 4, 0, World.WORLDHEIGHT / 3),
            // cosmetic shit!
            new Vein(new OreFeature(Block.DIAMOND_ORE.id, 8), 2, 0, World.WORLDHEIGHT / 4),
            new Vein(new OreFeature(Block.CINNABAR_ORE.id, 6), 2, 0, World.WORLDHEIGHT / 4),
            // clay in the hills, the rest scattered underground
            new Vein(new OreFeature(Block.CLAY_BLOCK.id, 24, stoneMode:false), 16, 72, World.WORLDHEIGHT),
            new Vein(new OreFeature(Block.DIRT.id, 32, stoneMode:false), 16, 16, World.WORLDHEIGHT - 16),
            new Vein(new OreFeature(Block.GRAVEL.id, 32, stoneMode:false), 8, 16, World.WORLDHEIGHT - 16),
            // sand pockets underground (le funny)
            new Vein(new OreFeature(Block.SAND.id, 32, stoneMode:false), 6, 16, World.WORLDHEIGHT - 16)
        ];
    }

    public void setup(XRandom random, int seed) {
        foliagen = new SimplexNoise(random.Next(seed));
    }

    public void surface(XRandom random, ChunkCoord coord) {
        var chunk = world.getChunk(coord);

        var xWorld = coord.x * Chunk.CHUNKSIZE;
        var zWorld = coord.z * Chunk.CHUNKSIZE;

        foreach (var v in veins) {
            for (int i = 0; i < v.count; i++) {
                var x = xWorld + random.Next(0, Chunk.CHUNKSIZE);
                var z = zWorld + random.Next(0, Chunk.CHUNKSIZE);
                var y = random.Next(v.minY, v.maxY);
                v.ore.place(world, random, x, y, z);
            }
        }

        caves.place(world, coord);
        ravines.place(world, coord);

        for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
            for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                var y = chunk.heightMap.get(x, z);
                while (y > 0 && !Block.fullBlock[chunk.getBlock(x, y, z)]) {
                    y--;
                }

                var i = (z << 4) + x;
                height[i] = y;
                chunk.biomeData.sample(x, z, out var t, out var h);
                biomes[i] = Biomes.getType(t, h, y);
            }
        }

        placeTrees(chunk, random);
        placeGroundCover(chunk, random, coord);
        placeUndergrowth(chunk, random);
    }

    private void placeTrees(Chunk chunk, XRandom random) {
        int max = 0;
        foreach (BiomeType t in biomes) {
            max = int.Max(max, getTrees(t).count);
        }

        var attempts = random.Next(max * 3 / 4, max * 5 / 4 + 1);

        for (int i = 0; i < attempts; i++) {
            var x = random.Next(0, Chunk.CHUNKSIZE);
            var z = random.Next(0, Chunk.CHUNKSIZE);
            var trees = getTrees(biomes[(z << 4) + x]).kinds;
            if (trees == null || trees.Length == 0) {
                continue;
            }

            placeTree(chunk, random, select(random, trees), x, z);
        }
    }

    private static TreeType select(XRandom random, TreeEntry[] trees) {
        float total = 0;
        foreach (var k in trees) {
            total += k.weight;
        }

        var roll = random.NextSingle() * total;
        foreach (var k in trees) {
            roll -= k.weight;
            if (roll <= 0) {
                return k.type;
            }
        }

        return trees[^1].type;
    }

    private bool placeTree(Chunk chunk, XRandom random, TreeType type, int x, int z) {
        var y = height[(z << 4) + x];
        if (y > 120) {
            return false;
        }

        var surface = chunk.getBlock(x, y, z);
        var ok = type == TreeType.Palm
            ? surface == Block.SAND.id
            : surface == Block.GRASS.id || surface == Block.SNOW_GRASS.id;
        if (!ok) {
            return false;
        }

        var (r, h) = getTreeAABB(type);
        var wx = chunk.worldX + x;
        var wz = chunk.worldZ + z;

        for (int yd = 1; yd < h; yd++) {
            for (int zd = -r; zd <= r; zd++) {
                for (int xd = -r; xd <= r; xd++) {
                    if (world.getBlock(wx + xd, y + yd, wz + zd) != Block.AIR.id) {
                        return false;
                    }
                }
            }
        }

        place(type, random, wx, y + 1, wz);
        return true;
    }

    // I'm aware of the fact that we're only being slightly better than the old copypasta here but this'll do
    private static (int r, int h) getTreeAABB(TreeType type) {
        return type switch {
            TreeType.MahoganySmall => (1, 7),
            TreeType.MahoganyMedium => (1, 16),
            TreeType.MahoganyHuge => (1, 36),
            _ => (2, 8)
        };
    }

    private void place(TreeType type, XRandom random, int x, int y, int z) {
        switch (type) {
            case TreeType.Oak: TreeGenerator.placeOakTree(world, random, x, y, z); break;
            case TreeType.Fancy: TreeGenerator.placeFancyTree(world, random, x, y, z); break;
            case TreeType.Maple: TreeGenerator.placeMapleTree(world, random, x, y, z); break;
            case TreeType.Pine: TreeGenerator.placePineTree(world, random, x, y, z); break;
            case TreeType.Candy: TreeGenerator.placeCandyTree(world, random, x, y, z); break;
            case TreeType.Palm: TreeGenerator.placePalmTree(world, random, x, y, z); break;
            case TreeType.MahoganySmall: TreeGenerator.placeSmallMahogany(world, random, x, y, z); break;
            case TreeType.MahoganyMedium: TreeGenerator.placeMediumMahogany(world, random, x, y, z); break;
            case TreeType.MahoganyHuge: TreeGenerator.placeHugeMahogany(world, random, x, y, z); break;
        }
    }

    private void placeGroundCover(Chunk chunk, XRandom random, ChunkCoord coord) {
        var density = float.Abs(WorldgenUtil.getNoise2D(foliagen, coord.x * FREQFOLIAGE, coord.z * FREQFOLIAGE,
            2, 1.5f));

        // old: (density * 128)^2 rolls at 1/127 each
        var grass = (int)(density * density * 129f);
        for (int i = 0; i < grass; i++) {
            var x = random.Next(0, Chunk.CHUNKSIZE);
            var z = random.Next(0, Chunk.CHUNKSIZE);
            var y = height[(z << 4) + x];

            if (chunk.getBlock(x, y, z) == Block.GRASS.id && y < World.WORLDHEIGHT - 1 &&
                chunk.getBlock(x, y + 1, z) == Block.AIR.id) {
                chunk.setBlockDumb(x, y + 1, z,
                    random.NextSingle() > 0.7f ? Block.TALL_GRASS.id : Block.SHORT_GRASS.id);
            }
        }

        var patches = random.Next(0, 3);
        for (int p = 0; p < patches; p++) {
            var cx = random.Next(0, Chunk.CHUNKSIZE);
            var cz = random.Next(0, Chunk.CHUNKSIZE);
            var biome = biomes[(cz << 4) + cx];

            if (biome is not (BiomeType.Plains or BiomeType.Forest or BiomeType.Taiga)) {
                continue;
            }

            var size = random.Next(4, 9);
            for (int i = 0; i < size; i++) {
                var x = cx + random.Next(-4, 5);
                var z = cz + random.Next(-4, 5);
                if (x is < 0 or >= Chunk.CHUNKSIZE || z is < 0 or >= Chunk.CHUNKSIZE) {
                    continue;
                }

                var y = height[(z << 4) + x];
                if (y >= World.WORLDHEIGHT - 1) {
                    continue;
                }

                var bl = chunk.getBlock(x, y, z);
                var above = chunk.getBlock(x, y + 1, z);

                if (biome == BiomeType.Taiga) {
                    if (bl == Block.SNOW_GRASS.id && (above == Block.AIR.id || above == Block.SNOW.id)) {
                        chunk.setBlockDumb(x, y + 1, z, Block.MUSHROOM_BROWN.id);
                    }
                }
                else if (bl == Block.GRASS.id && above == Block.AIR.id) {
                    chunk.setBlockDumb(x, y + 1, z, random.Next(4) switch {
                        0 => Block.YELLOW_FLOWER.id,
                        1 => Block.MARIGOLD.id,
                        2 => Block.BLUE_TULIP.id,
                        _ => Block.THISTLE.id
                    });
                }
            }
        }

        var cacti = random.Next(0, 4);
        for (int i = 0; i < cacti; i++) {
            var x = random.Next(0, Chunk.CHUNKSIZE);
            var z = random.Next(0, Chunk.CHUNKSIZE);
            var idx = (z << 4) + x;
            if (biomes[idx] != BiomeType.Desert) {
                continue;
            }

            var y = height[idx];
            if (y >= World.WORLDHEIGHT - 1 || chunk.getBlock(x, y, z) != Block.SAND.id ||
                chunk.getBlock(x, y + 1, z) != Block.AIR.id) {
                continue;
            }

            var cactus = (Cactus)Block.CACTUS;
            if (!cactus.canSurvive(world, x + chunk.worldX, y + 1, z + chunk.worldZ)) {
                continue;
            }

            var h = random.Next(2, 4);
            for (int yy = 0; yy < h && y + 1 + yy < World.WORLDHEIGHT; yy++) {
                chunk.setBlockDumb(x, y + 1 + yy, z, Block.CACTUS.id);
            }
        }
    }

    private void placeUndergrowth(Chunk chunk, XRandom random) {
        var count = random.Next(9, 16);
        for (int i = 0; i < count; i++) {
            var x = random.Next(0, Chunk.CHUNKSIZE);
            var z = random.Next(0, Chunk.CHUNKSIZE);
            var idx = (z << 4) + x;

            if (biomes[idx] != BiomeType.Jungle) {
                continue;
            }

            var y = height[idx];
            if (y >= World.WORLDHEIGHT - 6 || chunk.getBlock(x, y, z) != Block.GRASS.id ||
                Block.log[chunk.getBlock(x, y + 1, z)]) {
                continue;
            }

            var wx = x + chunk.worldX;
            var wz = z + chunk.worldZ;

            var roll = random.NextSingle();
            if (roll < 0.25f) {
                TreeGenerator.placeSmallFern(world, random, wx, y + 1, wz);
            }
            else if (roll < 0.5f) {
                TreeGenerator.placeDenseBush(world, random, wx, y + 1, wz);
            }
            else {
                world.setBlockSilent(wx, y + 1, wz, roll < 0.75f ? Block.FERN_RED.id : Block.FERN_GREEN.id);
            }
        }
    }
}