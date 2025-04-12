using BlockGame.util;

namespace BlockGame;

public class TechDemoChunkGenerator : ChunkGenerator {

    public TechDemoWorldGenerator generator;

    public TechDemoChunkGenerator(TechDemoWorldGenerator generator) {
        this.generator = generator;
    }

    public void generate(ChunkCoord coord) {
        var world = generator.world;
        var chunk = world.getChunk(coord);
        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                var worldPos = World.toWorldPos(chunk.coord.x, chunk.coord.z, x, 0, z);
                // -1 to 1
                // transform to the range 10 - 30
                var height = generator.noise.GetNoise(worldPos.X, worldPos.Z) * 20 + 20;
                for (int y = 0; y < height - 1; y++) {
                    chunk.setBlock(x, y, z, Block.DIRT.id);
                }
                chunk.setBlock(x, (int)height, z, Block.GRASS.id);
            }
        }
        chunk.status = ChunkStatus.GENERATED;
    }

    public void populate(ChunkCoord coord) {
        var chunk = generator.world.getChunk(coord);
        chunk.status = ChunkStatus.POPULATED;
    }

    public Random getRandom(ChunkCoord coord) {
        return new Random(coord.GetHashCode());
    }
}