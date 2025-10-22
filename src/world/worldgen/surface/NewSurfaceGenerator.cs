using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.chunk;
using BlockGame.world.worldgen.feature;
using BlockGame.world.worldgen.generator;

namespace BlockGame.world.worldgen.surface;

public class NewSurfaceGenerator : SurfaceGenerator {

    public WorldGenerator worldgen;
    public World world;

    public SimplexNoise foliagen;

    public const float FREQFOLIAGE = 1 / 169f;

    private readonly Cave caves = new();
    private readonly Ravine ravines = new();
    private readonly OreFeature ironOre = new(Blocks.IRON_ORE, 6, 12);
    private readonly OreFeature coalOre = new(Blocks.COAL_ORE, 8, 16);
    private readonly OreFeature goldOre = new(Blocks.GOLD_ORE, 6, 8);
    private readonly OreFeature diamondOre = new(Blocks.DIAMOND_ORE, 4, 6);
    private readonly OreFeature cinnabarOre = new(Blocks.CINNABAR, 4, 6);

    public NewSurfaceGenerator(WorldGenerator worldgen, World world) {
        this.worldgen = worldgen;
        this.world = world;
    }

    public void setup(XRandom random, int seed) {
        foliagen = new SimplexNoise(random.Next(seed));
    }

    public void surface(XRandom random, ChunkCoord coord) {

        var chunk = world.getChunk(coord);

        var xWorld = coord.x * Chunk.CHUNKSIZE;
        var zWorld = coord.z * Chunk.CHUNKSIZE;

        var xChunk = coord.x;
        var zChunk = coord.z;

        // Do ore
        for (int i = 0; i < 16; i++) {
            var x = xWorld + random.Next(0, Chunk.CHUNKSIZE);
            var z = zWorld + random.Next(0, Chunk.CHUNKSIZE);
            var y = random.Next(0, World.WORLDHEIGHT);
            coalOre.place(world, random, x, y, z);
        }

        for (int i = 0; i < 16; i++) {
            var x = xWorld + random.Next(0, Chunk.CHUNKSIZE);
            var z = zWorld + random.Next(0, Chunk.CHUNKSIZE);
            var y = random.Next(0, World.WORLDHEIGHT / 2);
            ironOre.place(world, random, x, y, z);
        }

        for (int i = 0; i < 6; i++) {
            var x = xWorld + random.Next(0, Chunk.CHUNKSIZE);
            var z = zWorld + random.Next(0, Chunk.CHUNKSIZE);
            var y = random.Next(0, World.WORLDHEIGHT / 3);
            goldOre.place(world, random, x, y, z);
        }

        for (int i = 0; i < 4; i++) {
            var x = xWorld + random.Next(0, Chunk.CHUNKSIZE);
            var z = zWorld + random.Next(0, Chunk.CHUNKSIZE);
            var y = random.Next(0, World.WORLDHEIGHT / 4);
            diamondOre.place(world, random, x, y, z);
        }

        for (int i = 0; i < 4; i++) {
            var x = xWorld + random.Next(0, Chunk.CHUNKSIZE);
            var z = zWorld + random.Next(0, Chunk.CHUNKSIZE);
            var y = random.Next(0, World.WORLDHEIGHT / 4);
            cinnabarOre.place(world, random, x, y, z);
        }

        var foliage = WorldgenUtil.getNoise2D(foliagen, xChunk * FREQFOLIAGE, zChunk * FREQFOLIAGE, 4, 2);
        var treeCount = foliage * 3f;

        // todo this will be replaced with biomes later!!
        // right now we just don't want trees in plains stuff for obvious reasons
        if (foliage < 0.25f) {
            treeCount = 0;
        }
        else {
            treeCount += 4;
        }

        // 4..7
        treeCount *= treeCount;
        // 16..49

        for (int i = 0; i < treeCount; i++) {
            WorldgenUtil.placeTree(world, random, coord);
        }

        // get e
        //var e = getNoise2D(en, 1 / 342f, 1 / 342f, 8, 2f);

        //e = float.Clamp(e, 0, 1);

        // Do caves
        //caves.freq = e;
        caves.place(world, coord);
        // Do ravines
        //ravines.freq = e;
        ravines.place(world, coord);

        // place grass
        var grassDensity = float.Abs(WorldgenUtil.getNoise2D(foliagen, xChunk * FREQFOLIAGE, zChunk * FREQFOLIAGE, 2, 1.5f));
        var grassCount = grassDensity * World.WORLDHEIGHT;

        if (grassDensity < 0) {
            grassCount = 0;
        }

        grassCount *= grassCount;

        for (int i = 0; i < grassCount; i++) {
            var x = random.Next(0, Chunk.CHUNKSIZE);
            var z = random.Next(0, Chunk.CHUNKSIZE);
            // var y = chunk.heightMap.get(x, z);
            // the problem with the heightmap approach is that you get chunks FULL of grass vs. literally nothing elsewhere
            // STOCHASTIC RANDOMISATION FOR THE LULZ
            var y = random.Next(0, World.WORLDHEIGHT - 1);

            if (chunk.getBlock(x, y, z) == Blocks.GRASS && y < World.WORLDHEIGHT - 1) {
                if (chunk.getBlock(x, y + 1, z) == Blocks.AIR) {
                    var grassType = random.NextSingle() > 0.7f ? Blocks.TALL_GRASS : Blocks.SHORT_GRASS;
                    chunk.setBlockFast(x, y + 1, z, grassType);
                }
            }
        }

        // place flower patches
        var flowerPatchCount = random.Next(0, 3);
        for (int p = 0; p < flowerPatchCount; p++) {
            // pick flower type for this patch
            var r = random.NextSingle();
            ushort flowerType;
            if (r < 0.25f) {
                flowerType = Blocks.YELLOW_FLOWER;
            }
            else if (r < 0.5f) {
                flowerType = Blocks.MARIGOLD;
            }
            else if (r < 0.75f) {
                flowerType = Blocks.BLUE_TULIP;
            }
            else {
                flowerType = Blocks.THISTLE;
            }

            // patch centre
            var cx = random.Next(0, Chunk.CHUNKSIZE);
            var cz = random.Next(0, Chunk.CHUNKSIZE);

            // place 4-8 flowers in patch
            // NVM FUCK THIS IT WONT SPAWN
            var patchSize = random.Next(32, 96);
            for (int i = 0; i < patchSize; i++) {
                var x = cx + random.Next(-3, 4);
                var z = cz + random.Next(-3, 4);

                /*if (x < 0 || x >= Chunk.CHUNKSIZE || z < 0 || z >= Chunk.CHUNKSIZE) {
                    continue;
                }*/

                var y = random.Next(0, World.WORLDHEIGHT - 1);

                x += chunk.worldX;
                z += chunk.worldZ;

                if (world.getBlock(x, y, z) == Blocks.GRASS && y < World.WORLDHEIGHT - 1) {
                    if (world.getBlock(x, y + 1, z) == Blocks.AIR) {
                        world.setBlock(x, y + 1, z, flowerType);
                    }
                }
            }
        }
    }
}