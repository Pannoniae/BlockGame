using BlockGame.id;
using BlockGame.util;

namespace BlockGame;

public partial class SimpleWorldGenerator {
    
    public void generate(ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                var worldPos = World.toWorldPos(chunk.coord.x, chunk.coord.z, x, 0, z);
                // -1 to 1
                // transform to the range -25 to 25, add 80 for 50 - 105
                var height = getNoise(worldPos.X, worldPos.Z) * 25 + 80;
                for (int y = 0; y < height - 1; y++) {
                    chunk.setBlock(x, y, z, Blocks.DIRT);
                }

                // water if low
                if (height < 64) {
                    chunk.setBlock(x, (int)height, z, Blocks.DIRT);
                    for (int y2 = (int)Math.Round(height); y2 <= 64; y2++) {
                        chunk.setBlock(x, y2, z, Blocks.WATER);
                    }
                    // put sand on the lake floors
                    if (getNoise2(x, z) > 0) {
                        chunk.setBlock(x, (int)Math.Round(height) - 1, z, Blocks.SAND);
                    }
                }
                else {
                    chunk.setBlock(x, (int)height, z, Blocks.GRASS);
                }
            }
        }
        chunk.status = ChunkStatus.GENERATED;
    }

    public void populate(ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                var worldPos = World.toWorldPos(chunk.coord.x, chunk.coord.z, x, 0, z);
                var height = getNoise(worldPos.X, worldPos.Z) * 25 + 80;
                // TREES
                if (MathF.Abs(treenoise.GetNoise(worldPos.X, worldPos.Z) - 1) < 0.01f) {
                    worldPos = World.toWorldPos(chunk.coord.x, chunk.coord.z, x, (int)(height + 1), z);
                    //Console.Out.WriteLine($"{worldPos} {chunk.coord.x} {chunk.coord.z} {x} {z} {chunk.GetHashCode()}");
                    placeTree(worldPos.X, worldPos.Y, worldPos.Z);
                }
            }
        }
        chunk.status = ChunkStatus.POPULATED;
    }

    public XRandom getRandom(ChunkCoord coord) {
        return new XRandom(coord.GetHashCode());
    }

    // Can place in neighbouring chunks, so they must be loaded first

    // todo the trees are cut off when they are placed in a neighbouring chunk... but only when the coords are more?
    // 63 to 64 is fine but 32 to 31 is not, it's cut off
    // probably something to do with the chunk position calculations?
    private void placeTree(int x, int y, int z) {
        // tree
        for (int i = 0; i < 7; i++) {
            world.setBlock(x, y + i, z, Blocks.LOG);
        }
        // leaves, thick
        for (int x1 = -2; x1 <= 2; x1++) {
            for (int z1 = -2; z1 <= 2; z1++) {
                // don't overwrite the trunk
                if (x1 == 0 && z1 == 0) {
                    continue;
                }
                for (int y1 = 4; y1 < 6; y1++) {
                    world.setBlock(x + x1, y + y1, z + z1, Blocks.LEAVES);
                }
            }
        }
        // leaves, thin on top
        for (int x2 = -1; x2 <= 1; x2++) {
            for (int z2 = -1; z2 <= 1; z2++) {
                for (int y2 = 6; y2 <= 7; y2++) {
                    // don't overwrite the trunk
                    if (x2 == 0 && z2 == 0 && y2 == 6) {
                        continue;
                    }
                    world.setBlock(x + x2, y + y2, z + z2, Blocks.LEAVES);
                }
            }
        }
    }
}