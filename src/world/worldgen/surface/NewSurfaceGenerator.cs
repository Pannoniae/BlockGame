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

    public const float FREQFOLIAGE = 1 / 19f;

    private readonly Cave caves = new();
    private readonly Ravine ravines = new();
    private readonly OreFeature copperOre = new(Block.COPPER_ORE.id, 6, 16);
    private readonly OreFeature ironOre = new(Block.IRON_ORE.id, 6, 12);
    private readonly OreFeature coalOre = new(Block.COAL_ORE.id, 8, 16);
    private readonly OreFeature goldOre = new(Block.GOLD_ORE.id, 6, 8);
    private readonly OreFeature diamondOre = new(Block.DIAMOND_ORE.id, 4, 6);
    private readonly OreFeature cinnabarOre = new(Block.CINNABAR_ORE.id, 4, 6);
    private readonly OreFeature clayOre = new(Block.CLAY_BLOCK.id, 24, 36);

    public NewSurfaceGenerator(WorldGenerator worldgen, World world, int version) {
        this.worldgen = worldgen;
        this.world = world;

        clayOre.stoneMode = false;
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

        for (int i = 0; i < 20; i++) {
            var x = xWorld + random.Next(0, Chunk.CHUNKSIZE);
            var z = zWorld + random.Next(0, Chunk.CHUNKSIZE);
            // copper spawns more on the surface!
            var y = random.Next(World.WORLDHEIGHT / 4, (int)(World.WORLDHEIGHT * (3 / 4f)));
            copperOre.place(world, random, x, y, z);
        }

        for (int i = 0; i < 12; i++) {
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


        // place clay in hills
        for (int i = 0; i < 24; i++) {
            var x = xWorld + random.Next(0, Chunk.CHUNKSIZE);
            var z = zWorld + random.Next(0, Chunk.CHUNKSIZE);
            var y = random.Next(72, World.WORLDHEIGHT);
            clayOre.place(world, random, x, y, z);
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

            if (chunk.getBlock(x, y, z) == Block.GRASS.id && y < World.WORLDHEIGHT - 1) {
                if (chunk.getBlock(x, y + 1, z) == Block.AIR.id) {
                    var grassType = random.NextSingle() > 0.7f ? Block.TALL_GRASS.id : Block.SHORT_GRASS.id;
                    chunk.setBlockFast(x, y + 1, z, grassType);
                }
            }
        }

        // place flower patches
        var flowerPatchCount = random.Next(0, 3);
        for (int p = 0; p < flowerPatchCount; p++) {
            // pick flower type for this patch
            var r = random.NextSingle();
            ushort flowerType = r switch {
                < 0.25f => Block.YELLOW_FLOWER.id,
                < 0.5f => Block.MARIGOLD.id,
                < 0.75f => Block.BLUE_TULIP.id,
                _ => Block.THISTLE.id
            };

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

                if (world.getBlock(x, y, z) == Block.GRASS.id && y < World.WORLDHEIGHT - 1) {
                    if (world.getBlock(x, y + 1, z) == Block.AIR.id) {
                        world.setBlock(x, y + 1, z, flowerType);
                    }
                }
            }
        }

        // place cactus in deserts - check biome at chunk centre
        var centerHeight = chunk.heightMap.get(8, 8);
        var temp = chunk.biomeData.getTemp(8, centerHeight, 8);
        var hum = chunk.biomeData.getHum(8, centerHeight, 8);

        // hot + dry = desert
        if (temp > 0.3f && hum < 0.3f) {
            var cactusCount = random.Next(0, 96);
            for (int i = 0; i < cactusCount; i++) {
                var x = random.Next(0, Chunk.CHUNKSIZE);
                var z = random.Next(0, Chunk.CHUNKSIZE);
                var y = random.Next(0, World.WORLDHEIGHT - 1);

                // place on sand
                if (chunk.getBlock(x, y, z) == Block.SAND.id && y < World.WORLDHEIGHT - 1) {
                    if (chunk.getBlock(x, y + 1, z) == Block.AIR.id) {

                        // check if root can survive
                        var cactus = (Cactus)Block.CACTUS;
                        if (!cactus.canSurvive(world, x + chunk.worldX, y + 1, z + chunk.worldZ)) {
                            continue;
                        }

                        var h = random.Next(2, 4);
                        for (int yy = 0; yy < h; yy++) {
                            if (y + 1 + yy >= World.WORLDHEIGHT) {
                                break;
                            }

                            chunk.setBlockFast(x, y + 1 + yy, z, Block.CACTUS.id);
                        }
                    }
                }
            }
        }

        var foliage = WorldgenUtil.getNoise2D(foliagen, xChunk * FREQFOLIAGE, zChunk * FREQFOLIAGE, 4, 2);
        var treeCount = foliage * 2f;

        // todo this will be replaced with biomes later!!
        // right now we just don't want trees in plains stuff for obvious reasons
        if (foliage < 0.25f) {
            // edge
            if (foliage > 0.1f) {
                treeCount += ((foliage - 0.1f) * 4);
            }
            else {
                treeCount = 0;
            }
        }
        else {
            treeCount += 4;
        }

        // 4..7
        treeCount *= treeCount;
        // 16..49

        var taiga = false;

        // place pine trees in snowy biomes
        if (temp < -0.25f) {
            taiga = true;
            if (hum > 0.05f) {
                var pineCount = (int)(treeCount * 0.8f);
                for (int i = 0; i < pineCount; i++) {
                    WorldgenUtil.placePineTree(world, random, coord);
                }
            }
        }

        var jungle = false;

        // place mahogany trees in jungles
        if (temp > 0.25f && hum > 0.3f) {
            jungle = true;
            var mahoganyCount = (int)(treeCount * 0.6f);
            for (int i = 0; i < mahoganyCount; i++) {
                WorldgenUtil.placeRainforestTree(world, random, coord);
            }
        }

        // if dense forest, place a rainforest tree
        if (!taiga && !jungle) {
            if (foliage > 0.3f) {
                WorldgenUtil.placeRainforestTree(world, random, coord);
            }

            for (int i = 0; i < treeCount; i++) {
                WorldgenUtil.placeTree(world, random, coord);
            }
        }

        // place candy tree randomly (not near the other ones lol)
        if (foliage < -0.2f && random.NextSingle() < 0.05f) {
            WorldgenUtil.placeCandyTree(world, random, coord);
        }
    }
}