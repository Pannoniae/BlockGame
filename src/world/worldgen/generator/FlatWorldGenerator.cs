using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.chunk;

namespace BlockGame.world.worldgen.generator;

public class FlatWorldGenerator : WorldGenerator {
    public World world;

    public FlatWorldGenerator(World world) {
        this.world = world;
    }


    public void setup(XRandom random, int seed) {
        // none!
    }

    public void generate(ChunkCoord coord) {
        // fill up to 5 blocks

        var chunk = world.getChunk(coord);

        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                chunk.setBlockDumb(x, 0, z,  Block.HELLROCK.id);
                for (int i = 1; i < 5; i++) {
                    chunk.setBlockDumb(x, i, z,  Block.DIRT.id);
                }
                chunk.setBlockDumb(x, 5, z,  Block.GRASS.id);
            }
        }

        chunk.status = ChunkStatus.GENERATED;

    }

    public void surface(ChunkCoord coord) {
        // none!
        var chunk = world.getChunk(coord);
        chunk.status = ChunkStatus.POPULATED;
    }
}