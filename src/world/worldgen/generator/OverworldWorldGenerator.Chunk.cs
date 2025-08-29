using BlockGame.util;

namespace BlockGame;

public partial class OverworldWorldGenerator {

    public const int WATER_LEVEL = 64;

    public void generate(ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        
        var densityMap = new float[Chunk.CHUNKSIZE, Chunk.CHUNKSIZE];
        
        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                var worldPos = World.toWorldPos(chunk.coord.x, chunk.coord.z, x, 0, z);

                var aux = getNoise(auxNoise, worldPos.X, worldPos.Z, 1, 0.5f);
                var mountainness = MathF.Pow((getNoise(terrainNoise2, worldPos.X, worldPos.Z, 1, 0.5f) + 1) / 2f, 2);
                var flatNoise = getNoise(terrainNoise, worldPos.X / 3f + aux * 5 + 1, worldPos.Z / 3f + aux * 5 + 1, 5, 0.5f);

                // this rescales the noise so there's more above than below
                flatNoise = MathF.Sin(flatNoise) * 0.8f + MathF.Sign(flatNoise) * flatNoise * 0.3f;
                flatNoise *= mountainness * 64;
                flatNoise += 64;
                
                densityMap[x, z] = flatNoise;
            }
        }
        
        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                var flatNoise = densityMap[x, z];

                chunk.setBlockFast(x, 0, z, Blocks.HELLSTONE);
                // hack until we can propagate them properly AND cheaply
                chunk.setBlockLight(x, 0, z, Block.lightLevel[Blocks.HELLSTONE]);

                for (int y = 1; y < World.WORLDHEIGHT; y++) {
                    if (y < flatNoise) {
                        chunk.setBlockFast(x, y, z, Blocks.STONE);
                        // set heightmap
                        chunk.addToHeightMap(x, y, z);
                    }
                    else {
                        break; // bail once we're above ground
                    }
                }
            }
        }

        // pass 3: decorate - dirt, water, grass
        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                var worldPos = World.toWorldPos(chunk.coord.x, chunk.coord.z, x, 0, z);
                int height = chunk.heightMap.get(x, z);

                // replace top layers with dirt
                var amt = getNoise(auxNoise2, worldPos.X, worldPos.Z, 1, 0.5f) + 2.5;
                for (int yy = height - 1; yy > height - 1 - amt; yy--) {
                    chunk.setBlockFast(x, yy, z, Blocks.DIRT);
                }

                // water if low
                if (height < WATER_LEVEL - 1) {
                    for (int y2 = height; y2 < WATER_LEVEL; y2++) {
                        chunk.setBlockFast(x, y2, z, Blocks.WATER);
                    }
                    // put sand on the lake floors
                    chunk.setBlockFast(x, height, z, getNoise2(worldPos.X, worldPos.Z) > 0 ? Blocks.SAND : Blocks.DIRT);
                }
                else {
                    chunk.setBlockFast(x, height, z, Blocks.GRASS);
                }
            }
        }

        foreach (var blocks in chunk.blocks) {
            if (blocks.inited) {
                blocks.refreshCounts();
            }
        }
        chunk.status = ChunkStatus.GENERATED;
    }

    public void populate(ChunkCoord coord) {
        var random = getRandom(coord);
        var chunk = world.getChunk(coord);

        // TREES
        var treeCount = Math.Pow(treenoise.GetNoise(chunk.worldX / 16f, chunk.worldZ / 16f), 3) * 4;
        for (int i = 0; i < treeCount; i++) {
            var randomPos = random.Next(16 * 16);
            var x = randomPos >> 4;
            var z = randomPos & 0xF;
            var height = chunk.heightMap.get(x, z);
            var worldPos = World.toWorldPos(chunk.coord.x, chunk.coord.z, x, (int)(height + 1), z);
            if ((height < 64 && random.NextSingle() < 0.25) || !(height < 64)) {
                placeOakTree(random, worldPos.X, worldPos.Y, worldPos.Z);
            }
        }
        chunk.status = ChunkStatus.POPULATED;
    }

    public XRandom getRandom(ChunkCoord coord) {
        return new XRandom(coord.GetHashCode());
    }

    public void placeOakTree(XRandom random, int x, int y, int z) {
        int randomNumber = random.Next(5, 8);
        for (int i = 0; i < randomNumber; i++) {
            world.setBlockDumb(x, y + i, z, Blocks.MAPLE_LOG);
            // leaves, thick
            for (int x1 = -2; x1 <= 2; x1++) {
                for (int z1 = -2; z1 <= 2; z1++) {
                    // don't overwrite the trunk
                    if (x1 == 0 && z1 == 0) {
                        continue;
                    }
                    for (int y1 = randomNumber - 2; y1 <= randomNumber - 1; y1++) {
                        world.setBlockDumb(x + x1, y + y1, z + z1, Blocks.MAPLE_LEAVES);
                    }
                }
            }
            // leaves, thin on top
            for (int x1 = -1; x1 <= 1; x1++) {
                for (int z1 = -1; z1 <= 1; z1++) {
                    for (int y1 = randomNumber; y1 <= randomNumber+1; y1++) {
                        world.setBlockDumb(x + x1, y + y1, z + z1, Blocks.MAPLE_LEAVES);
                    }
                }
            }
        }
    }
    public void placeMapleTree(int x, int y, int z) {
        for (int i = 0; i < 7; i++) {
            world.setBlockDumb(x, y + i, z, Blocks.MAPLE_LOG);
        }
        // leaves, thin on bottom
        for (int x2 = -1; x2 <= 1; x2++) {
            // don't overwrite the trunk
            if (x2 == 0) {
                continue;
            }
            world.setBlockDumb(x + x2, y + 4, z, Blocks.MAPLE_LEAVES);
        }
        for (int z2 = -1; z2 <= 1; z2++) {
            //don't overwrite the trunk
            if (z2 == 0) {
                continue;
            }
            world.setBlockDumb(x, y + 4, z + z2, Blocks.MAPLE_LEAVES);
        }
        // leaves, thick
        for (int x2 = -1; x2 <= 1; x2++) {
            for (int z2 = -1; z2 <= 1; z2++) {
                // don't overwrite the trunk
                if (x2 == 0 && z2 == 0) {
                    continue;
                }
                for (int y2 = 5; y2 <= 6; y2++) {
                    world.setBlockDumb(x + x2, y + y2, z + z2, Blocks.MAPLE_LEAVES);
                }
            }
        }
        // leaves, thin on top
        for (int x2 = -1; x2 <= 1; x2++) {
            world.setBlockDumb(x + x2, y + 7, z, Blocks.MAPLE_LEAVES);
        }
        for (int z2 = -1; z2 <= 1; z2++) {
            world.setBlockDumb(x, y + 7, z + z2, Blocks.MAPLE_LEAVES);
            world.setBlockDumb(x, y + 8, z, Blocks.MAPLE_LEAVES);
        }
    }

    public void placeJungleTree(int x, int y, int z) {
        for (int i = 0; i <= 6; i++) {
            if (i == 0) {
                for (int x3 = -1; x3 <= 1; x3++) {
                    world.setBlockDumb(x + x3, y, z, Blocks.MAPLE_LOG);
                }
                for (int z3 = -1; z3 <= 1; z3++) {
                    world.setBlockDumb(x, y, z + z3, Blocks.MAPLE_LOG);
                }
            }
            world.setBlockDumb(x, y + i, z, Blocks.MAPLE_LOG);
        }

        world.setBlockDumb(x + 1, y + 4, z, Blocks.MAPLE_LOG);
        world.setBlockDumb(x - 1, y + 6, z, Blocks.MAPLE_LOG);
        world.setBlockDumb(x - 1, y + 7, z, Blocks.MAPLE_LOG);
        world.setBlockDumb(x - 1, y + 8, z, Blocks.MAPLE_LOG);

        for (int x3 = 1; x3 <= 2; x3++) {
            for (int z3 = -1; z3 <= 1; z3++) {
                world.setBlockDumb(x + x3, y + 5, z + z3, Blocks.MAPLE_LEAVES);
            }
        }

        world.setBlockDumb(x + 3, y + 5, z, Blocks.MAPLE_LEAVES);

        for (int x3 = 1; x3 <= 2; x3++) {
            world.setBlockDumb(x + x3, y + 6, z, Blocks.MAPLE_LEAVES);
        }

        for (int x3 = -3; x3 <= 2; x3++) {
            for (int z3 = -1; z3 <= 1; z3++) {
                // don't overwrite the trunk
                if (x3 == -1 && z3 == 0) {
                    continue;
                }
                world.setBlockDumb(x + x3, y + 8, z + z3, Blocks.MAPLE_LEAVES);
            }
        }

        for (int x3 = -2; x3 <= 1; x3++) {
            world.setBlockDumb(x + x3, y + 9, z, Blocks.MAPLE_LEAVES);
        }
        world.setBlockDumb(x - 1, y + 5, z, Blocks.MAPLE_LEAVES);
    }
}