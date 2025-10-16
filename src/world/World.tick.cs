using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.chunk;

namespace BlockGame.world;

public partial class World {

    /**
     * Sloppiest thing ever
     */
    public void tick(World world, ChunkCoord coord, Chunk chunk, XRandom random, int x, int y, int z) {
        var block = chunk.getBlock(x, y, z);
        var worldPos = toWorldPos(coord.x, 0, coord.z, x, y, z);

        if (block == Blocks.GRASS) {
            if (y < 127 && Block.isFullBlock(world.getBlock(worldPos.X, worldPos.Y + 1, worldPos.Z))) {
                chunk.setBlock(x, y, z, Blocks.DIRT);
                // dirty block
                world.setBlockNeighboursDirty(worldPos);
            }
            // spread grass to nearby dirt blocks
            // in a 3x3x3 area
            var r = Meth.getRandomCoord(random, 6, 6, 6);
            var wc = toWorldPos(coord.x, 0, coord.z, r.X, r.Y, r.Z);
            var x1 = wc.X;
            var y1 = wc.Y;
            var z1 = wc.Z;
            // if dirt + air above
            if (world.getBlock(x + x1 - 3, y + y1 - 3, z + z1 - 3) == Blocks.DIRT && world.getBlock(x + x1 - 3, y + y1 - 2, z + z1 - 3) == Blocks.AIR) {
                world.setBlockDumb(x + x1 - 3, y + y1 - 3, z + z1 - 3, Blocks.GRASS);
                // dirty this chunk
                world.dirtyChunk(new SubChunkCoord(coord.x, (y + y1 - 3) >> 4, coord.z));
            }
        }
    }
}