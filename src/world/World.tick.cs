using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.chunk;

namespace BlockGame.world;

public partial class World {

    /**
     * Called for random block ticks
     */
    public void tick(World world, ChunkCoord coord, Chunk chunk, XRandom random, int x, int y, int z) {
        var blockID = chunk.getBlock(x, y, z);

        // skip air and non-ticking blocks
        if (blockID == Block.AIR.id || !Block.randomTick[blockID]) return;

        var worldPos = toWorldPos(coord.x, 0, coord.z, x, y, z);
        var block = Block.get(blockID);

        // call the block's random update
        block?.randomUpdate(world, worldPos.X, worldPos.Y, worldPos.Z);
    }
}