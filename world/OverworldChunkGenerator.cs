namespace BlockGame;

public class OverworldChunkGenerator : ChunkGenerator {

    public OverworldWorldGenerator generator;

    public OverworldChunkGenerator(OverworldWorldGenerator generator) {
        this.generator = generator;
    }

    public void generate(ChunkCoord coord) {
        var world = generator.world;
        var chunk = world.getChunk(coord);
        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                var worldPos = world.toWorldPos(chunk.coord.x, chunk.coord.z, x, 0, z);
                // -1 to 1
                // transform to the range 5 - 10
                var height = generator.noise.GetNoise(worldPos.X, worldPos.Z) * 2.5 + 7.5;
                for (int y = 0; y < height - 1; y++) {
                    chunk.setBlock(x, y, z, Blocks.DIRT.id, false);
                }
                chunk.setBlock(x, (int)height, z, Blocks.GRASS.id, false);

                // water if low
                if (height < 7) {
                    for (int y2 = (int)Math.Round(height); y2 <= 7; y2++) {
                        chunk.setBlock(x, y2, z, Blocks.WATER.id, false);
                    }
                }
            }
        }
        chunk.status = ChunkStatus.GENERATED;
    }

    public void populate(ChunkCoord coord) {
        var world = generator.world;
        var chunk = world.getChunk(coord);
        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                var worldPos = world.toWorldPos(chunk.coord.x, chunk.coord.z, x, 0, z);
                var height = generator.noise.GetNoise(worldPos.X, worldPos.Z) * 2.5 + 7.5;
                // TREES
                if (MathF.Abs(generator.treenoise.GetNoise(worldPos.X, worldPos.Z) - 1) < 0.01f) {
                    worldPos = world.toWorldPos(chunk.coord.x, chunk.coord.z, x, (int)(height + 1), z);
                    placeTree(worldPos.X, worldPos.Y, worldPos.Z);
                }
            }
        }
        chunk.status = ChunkStatus.POPULATED;
    }

    // Can place in neighbouring chunks, so they must be loaded first
    private void placeTree(int x, int y, int z) {
        var world = generator.world;
        // tree
        for (int i = 0; i < 7; i++) {
            world.setBlock(x, y + i, z, Blocks.LOG.id, false);
        }
        // leaves, thick
        for (int x1 = -2; x1 <= 2; x1++) {
            for (int z1 = -2; z1 <= 2; z1++) {
                // don't overwrite the trunk
                if (x1 == 0 && z1 == 0) {
                    continue;
                }
                for (int y1 = 4; y1 < 6; y1++) {
                    world.setBlock(x + x1, y + y1, z + z1, Blocks.LEAVES.id, false);
                }
            }
        }
        // leaves, thin on top
        for (int x2 = -1; x2 <= 1; x2++) {
            for (int z2 = -1; z2 <= 1; z2++) {
                for (int y2 = 6; y2 <= 7; y2++) {
                    // don't overwrite the trunk
                    if (x2 == 0 && z2 == 0 && y == 6) {
                        continue;
                    }
                    world.setBlock(x + x2, y + y2, z + z2, Blocks.LEAVES.id, false);
                }
            }
        }
    }
}