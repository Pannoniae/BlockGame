using BlockGame.util;

namespace BlockGame;

public partial class PerlinWorldGenerator {

    public const int WATER_LEVEL = 64;

    public void generate(ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        // fill up with dirt until 64
        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            for (int y = 0; y < WATER_LEVEL; y++) {
                for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                    chunk.setBlock(x, y, z, Block.DIRT.id);
                }
            }
        }
        // grass on 64
        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                chunk.setBlock(x, WATER_LEVEL, z, Block.GRASS.id);
            }
        }
        chunk.status = ChunkStatus.GENERATED;
    }

    public void populate(ChunkCoord coord) {
        var random = getRandom(coord);
        var chunk = world.getChunk(coord);
        
        chunk.status = ChunkStatus.POPULATED;
    }

    public XRandom getRandom(ChunkCoord coord) {
        return new XRandom(coord.GetHashCode());
    }
}