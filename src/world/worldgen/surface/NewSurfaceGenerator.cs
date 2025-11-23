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
    private readonly OreFeature copperOre = new(Block.COPPER_ORE.id, 12);
    private readonly OreFeature tinOre = new(Block.TIN_ORE.id, 8);
    private readonly OreFeature ironOre = new(Block.IRON_ORE.id, 8);
    private readonly OreFeature coalOre = new(Block.COAL_ORE.id, 16);
    private readonly OreFeature goldOre = new(Block.GOLD_ORE.id, 8);
    private readonly OreFeature diamondOre = new(Block.DIAMOND_ORE.id, 8);
    private readonly OreFeature cinnabarOre = new(Block.CINNABAR_ORE.id, 6);
    private readonly OreFeature clayOre = new(Block.CLAY_BLOCK.id, 24, stoneMode: false);
    private readonly OreFeature dirtOre = new(Block.DIRT.id, 32, stoneMode: false);
    private readonly OreFeature gravelOre = new(Block.GRAVEL.id, 32, stoneMode: false);
    private readonly OreFeature sandOre = new(Block.SAND.id, 32, stoneMode: false);

    public NewSurfaceGenerator(WorldGenerator worldgen, World world, int version) {
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

        // same frequency but distributed on less surface area = denser veins
        for (int i = 0; i < 16; i++) {
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
            tinOre.place(world, random, x, y, z);
        }

        // iron ore spawns smaller but still frequent..
        for (int i = 0; i < 16; i++) {
            var x = xWorld + random.Next(0, Chunk.CHUNKSIZE);
            var z = zWorld + random.Next(0, Chunk.CHUNKSIZE);
            var y = random.Next(0, World.WORLDHEIGHT / 2);
            ironOre.place(world, random, x, y, z);
        }


        for (int i = 0; i < 4; i++) {
            var x = xWorld + random.Next(0, Chunk.CHUNKSIZE);
            var z = zWorld + random.Next(0, Chunk.CHUNKSIZE);
            var y = random.Next(0, World.WORLDHEIGHT / 3);
            goldOre.place(world, random, x, y, z);
        }


        // cosmetic shit!
        for (int i = 0; i < 2; i++) {
            var x = xWorld + random.Next(0, Chunk.CHUNKSIZE);
            var z = zWorld + random.Next(0, Chunk.CHUNKSIZE);
            var y = random.Next(0, World.WORLDHEIGHT / 4);
            diamondOre.place(world, random, x, y, z);
        }

        for (int i = 0; i < 2; i++) {
            var x = xWorld + random.Next(0, Chunk.CHUNKSIZE);
            var z = zWorld + random.Next(0, Chunk.CHUNKSIZE);
            var y = random.Next(0, World.WORLDHEIGHT / 4);
            cinnabarOre.place(world, random, x, y, z);
        }


        // place clay in hills
        for (int i = 0; i < 16; i++) {
            var x = xWorld + random.Next(0, Chunk.CHUNKSIZE);
            var z = zWorld + random.Next(0, Chunk.CHUNKSIZE);
            var y = random.Next(72, World.WORLDHEIGHT);
            clayOre.place(world, random, x, y, z);
        }

        // place dirt pockets underground
        for (int i = 0; i < 16; i++) {
            var x = xWorld + random.Next(0, Chunk.CHUNKSIZE);
            var z = zWorld + random.Next(0, Chunk.CHUNKSIZE);
            var y = random.Next(16, World.WORLDHEIGHT - 16);
            dirtOre.place(world, random, x, y, z);
        }

        // place gravel pockets underground
        for (int i = 0; i < 8; i++) {
            var x = xWorld + random.Next(0, Chunk.CHUNKSIZE);
            var z = zWorld + random.Next(0, Chunk.CHUNKSIZE);
            var y = random.Next(16, World.WORLDHEIGHT - 16);
            gravelOre.place(world, random, x, y, z);
        }

        // place sand pockets underground (le funny)
        for (int i = 0; i < 6; i++) {
            var x = xWorld + random.Next(0, Chunk.CHUNKSIZE);
            var z = zWorld + random.Next(0, Chunk.CHUNKSIZE);
            var y = random.Next(16, World.WORLDHEIGHT - 16);
            sandOre.place(world, random, x, y, z);
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

        // check biome at chunk centre
        var centerHeight = chunk.heightMap.get(8, 8);
        var temp = chunk.biomeData.getTemp(8, centerHeight, 8);
        var hum = chunk.biomeData.getHum(8, centerHeight, 8);
        var cb = Biomes.getType(temp, hum, centerHeight);

        // place cactus in deserts
        if (Biomes.canPlaceCactus(cb)) {
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

        // tree placement based on foliage noise and biome
        var foliage = WorldgenUtil.getNoise2D(foliagen, xChunk * FREQFOLIAGE, zChunk * FREQFOLIAGE, 4, 2);
        var treeCount = foliage * 2f;

        if (foliage < 0.25f) {
            // sparse edge
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

        treeCount *= treeCount; // 16..49

        // apply biome density multiplier
        var biomeDensity = Biomes.getTreeDensity(cb);
        treeCount *= biomeDensity;

        // place trees based on biome type
        var finalTreeCount = (int)treeCount;
        switch (cb) {
            case BiomeType.Taiga:
                for (int i = 0; i < finalTreeCount; i++) {
                    WorldgenUtil.placePineTree(world, random, coord);
                }

                break;

            case BiomeType.Jungle:
                for (int i = 0; i < finalTreeCount; i++) {
                    WorldgenUtil.placeRainforestTree(world, random, coord);
                }

                break;

            case BiomeType.Forest:
                // dense forest - add occasional large tree
                if (foliage > 0.3f) {
                    WorldgenUtil.placeRainforestTree(world, random, coord);
                }

                for (int i = 0; i < finalTreeCount; i++) {
                    WorldgenUtil.placeTree(world, random, coord);
                }

                break;
        }

        // place candy tree randomly in sparse areas
        if (foliage < -0.2f && random.NextSingle() < 0.05f) {
            WorldgenUtil.placeCandyTree(world, random, coord);
        }

        // place undergrowth in jungles
        if (cb == BiomeType.Jungle && finalTreeCount > 1) {
            var undergrowthCount = random.Next(96, 160);
            for (int i = 0; i < undergrowthCount; i++) {
                var x = random.Next(0, Chunk.CHUNKSIZE);
                var z = random.Next(0, Chunk.CHUNKSIZE);
                var y = random.Next(64, World.WORLDHEIGHT - 1);

                // place on grass
                if (chunk.getBlock(x, y, z) == Block.GRASS.id && y < World.WORLDHEIGHT - 2) {
                    if (chunk.getBlock(x, y + 1, z) == Block.AIR.id) {
                        var wx = x + chunk.worldX;
                        var wz = z + chunk.worldZ;

                        // centre log
                        world.setBlockSilent(wx, y + 1, wz, Block.OAK_LOG.id);

                        // leaves in 4-block radius (9x9 area)
                        for (int xo = -4; xo <= 4; xo++) {
                            for (int zo = -4; zo <= 4; zo++) {
                                // circular-ish shape
                                if (xo * xo + zo * zo > 18 + random.Next(0, 4)) {
                                    continue;
                                }

                                // todo implement proper tags and not this hackjob!!
                                if (world.getBlock(wx + xo, y + 1, wz + zo) == Block.AIR.id ||
                                    world.getBlock(wx + xo, y + 1, wz + zo) == Block.LEAVES.id ||
                                    world.getBlock(wx + xo, y + 1, wz + zo) == Block.SHORT_GRASS.id ||
                                    world.getBlock(wx + xo, y + 1, wz + zo) == Block.TALL_GRASS.id) {
                                    world.setBlockSilent(wx + xo, y + 1, wz + zo, Block.MAHOGANY_LEAVES.id);
                                }
                            }
                        }

                        // leaves on top of log
                        if (world.getBlock(wx, y + 2, wz) == Block.AIR.id) {
                            world.setBlockSilent(wx, y + 2, wz, Block.MAHOGANY_LEAVES.id);
                        }
                    }
                }
            }
        }
    }
}