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
    private readonly OreFeature clayOre = new(Block.CLAY_BLOCK.id, 24, stoneMode:false);
    private readonly OreFeature dirtOre = new(Block.DIRT.id, 32, stoneMode:false);
    private readonly OreFeature gravelOre = new(Block.GRAVEL.id, 32, stoneMode:false);
    private readonly OreFeature sandOre = new(Block.SAND.id, 32, stoneMode:false);

    public NewSurfaceGenerator(WorldGenerator worldgen, World world, int version) {
        this.worldgen = worldgen;
        this.world = world;
    }

    public void setup(XRandom random, int seed) {
        foliagen = new SimplexNoise(random.Next(seed));
    }

    /** place small fern - 1-2 blocks tall, sparse leaves */
    private void placeSmallFern(World world, XRandom random, int x, int y, int z) {

        // fern trunk
        world.setBlockSilent(x, y, z, Block.FERN_LOG.id);

        // small irregular leaf cluster
        int topY = y + 1;
        int radius = random.Next(2, 4);

        for (int xo = -radius; xo <= radius; xo++) {
            for (int zo = -radius; zo <= radius; zo++) {
                if (xo == 0 && zo == 0) {
                    continue;
                }

                // rough distance check
                if (xo * xo + zo * zo > radius * radius) {
                    continue;
                }

                if (Block.log[world.getBlock(x + xo, topY, z + zo)]) {
                    continue;
                }
                world.setBlockSilent(x + xo, y, z + zo, Block.MAHOGANY_LEAVES.id);
                world.setBlockSilent(x + xo, topY, z + zo, Block.MAHOGANY_LEAVES.id);
            }
        }

        world.setBlockSilent(x, topY, z, Block.MAHOGANY_LEAVES.id);
    }

    /** place dense bush - short but very leafy */
    private void placeDenseBush(World world, XRandom random, int x, int y, int z) {

        world.setBlockSilent(x, y, z, Block.MAHOGANY_LOG.id);

        // very dense compact leaves
        for (int h = 0; h <= 2; h++) {
            int layerY = y + h;
            int radius = random.Next(1, 3);

            for (int xo = -radius; xo <= radius; xo++) {
                for (int zo = -radius; zo <= radius; zo++) {
                    int distSq = xo * xo + zo * zo;
                    if (distSq > radius * radius) {
                        continue;
                    }

                    if (Block.log[world.getBlock(x + xo, layerY, z + zo)]) {
                        continue;
                    }
                    world.setBlockSilent(x + xo, layerY, z + zo, Block.MAHOGANY_LEAVES.id);
                }
            }
        }
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
        //Console.WriteLine($"Chunk {coord}: temp={temp:F3}, hum={hum:F3}, biome={Biomes.getType(temp, hum, centerHeight)}");

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
        // update: just generate a random-ish number lol, fuck the foliage noise
        float treeCount = random.Next(6, 8);

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
                if (hum > 0.2f) {
                    WorldgenUtil.placeRainforestTree(world, random, coord);
                }

                for (int i = 0; i < finalTreeCount; i++) {
                    WorldgenUtil.placeTree(world, random, coord);
                }

                break;

            case BiomeType.Plains:
                // place candy tree randomly in sparse areas
                if (random.NextSingle() < 0.05f) {
                    WorldgenUtil.placeCandyTree(world, random, coord);
                }

                break;
        }


        // place undergrowth in jungles - much denser and more varied
        if (cb == BiomeType.Jungle && finalTreeCount > 1) {
            //Console.WriteLine($"UNDERGROWTH: chunk {coord}, cb={cb}, finalTreeCount={finalTreeCount}");
            var undergrowthCount = random.Next(9, 16);
            for (int i = 0; i < undergrowthCount; i++) {
                var x = random.Next(0, Chunk.CHUNKSIZE);
                var z = random.Next(0, Chunk.CHUNKSIZE);
                var y = chunk.heightMap.get(x, z);

                // place on grass
                if (chunk.getBlock(x, y, z) == Block.GRASS.id && y < World.WORLDHEIGHT - 6) {
                    if (!Block.log[chunk.getBlock(x, y + 1, z)]) {
                        var wx = x + chunk.worldX;
                        var wz = z + chunk.worldZ;

                        // 50/50 ferns and bushes
                        if (random.NextSingle() < 0.5f) {
                            placeSmallFern(world, random, wx, y + 1, wz);
                        }
                        else {
                            placeDenseBush(world, random, wx, y + 1, wz);
                        }
                    }
                }
            }
        }
    }
}