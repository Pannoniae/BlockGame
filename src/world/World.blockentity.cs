using BlockGame.world.block;

namespace BlockGame.world;

public partial class World {

    public readonly List<BlockEntity> blockEntities = [];

    public void setBlockEntity(int x, int y, int z, BlockEntity be) {
        if (!inWorld(x, y, z)) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var chunk = getChunk(x, z);

        if (chunk.getBlockEntity(blockPos.X, blockPos.Y, blockPos.Z) != null) {
            removeBlockEntity(x, y, z);
        }

        be.pos = new Molten.Vector3I(x, y, z);
        blockEntities.Add(be);
        chunk.setBlockEntity(blockPos.X, blockPos.Y, blockPos.Z, be);
    }

    public BlockEntity? getBlockEntity(int x, int y, int z) {
        if (!inWorld(x, y, z)) {
            return null;
        }

        var blockPos = getPosInChunk(x, y, z);
        var chunk = getChunk(x, z);

        return chunk.getBlockEntity(blockPos.X, blockPos.Y, blockPos.Z);
    }

    public void removeBlockEntity(int x, int y, int z) {
        if (!inWorld(x, y, z)) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var chunk = getChunk(x, z);

        blockEntities.Remove(chunk.getBlockEntity(blockPos.X, blockPos.Y, blockPos.Z)!);
        chunk.removeBlockEntity(blockPos.X, blockPos.Y, blockPos.Z);
    }

    public void updateBlockEntities() {
        foreach (var be in blockEntities) {
            be.update(this, be.pos.X, be.pos.Y, be.pos.Z);
        }
    }
}