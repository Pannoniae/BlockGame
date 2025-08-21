using BlockGame.util;

namespace BlockGame;

public partial class TechDemoWorldGenerator {
    public void generate(ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                var worldPos = World.toWorldPos(chunk.coord.x, chunk.coord.z, x, 0, z);
                // -1 to 1
                // transform to the range 10 - 30
                var height = noise.GetNoise(worldPos.X, worldPos.Z) * 20 + 20;
                for (int y = 0; y < height - 1; y++) {
                    chunk.setBlock(x, y, z, Blocks.DIRT);
                }
                chunk.setBlock(x, (int)height, z, Blocks.GRASS);
            }
        }
        chunk.status = ChunkStatus.GENERATED;
    }

    public void populate(ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        chunk.status = ChunkStatus.POPULATED;
    }
}