using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.chunk;

namespace BlockGame.world.worldgen.generator;

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
                    chunk.setBlockDumb(x, y, z, Blocks.DIRT);
                }

                // water if low
                if (height < 64) {
                    chunk.setBlockDumb(x, (int)height, z, Blocks.DIRT);
                    for (int y2 = (int)Math.Round(height); y2 <= 64; y2++) {
                        chunk.setBlockDumb(x, y2, z, Blocks.WATER);
                    }
                    // put sand on the lake floors
                    if (getNoise2(x, z) > 0) {
                        chunk.setBlockDumb(x, (int)Math.Round(height) - 1, z, Blocks.SAND);
                    }
                }
                else {
                    chunk.setBlockDumb(x, (int)height, z, Blocks.GRASS);
                }
            }
        }
        chunk.status = ChunkStatus.GENERATED;
    }

    public void surface(ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        var random = getRandom(coord);
        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                var worldPos = World.toWorldPos(chunk.coord.x, chunk.coord.z, x, 0, z);
                var height = getNoise(worldPos.X, worldPos.Z) * 25 + 80;
                // TREES
                if (MathF.Abs(treenoise.GetNoise(worldPos.X, worldPos.Z) - 1) < 0.01f) {
                    worldPos = World.toWorldPos(chunk.coord.x, chunk.coord.z, x, (int)(height + 1), z);
                    // 1/15 chance for fancy tree
                    if (random.Next(15) == 0) {
                        TreeGenerator.placeFancyTree(world, random, worldPos.X, worldPos.Y, worldPos.Z);
                    }
                    else {
                        TreeGenerator.placeOakTree(world, random, worldPos.X, worldPos.Y, worldPos.Z);
                    }
                }
            }
        }
        chunk.status = ChunkStatus.POPULATED;
    }

    public XRandom getRandom(ChunkCoord coord) {
        return new XRandom(coord.GetHashCode());
    }
}