namespace BlockGame.world.block;

public abstract class EntityBlock : Block {
    public EntityBlock(ushort id, string name) : base(name) {
    }

    public override void onPlace(World world, int x, int y, int z, byte metadata) {
        world.setBlockEntity(x, y, z, get());
    }

    public override void onBreak(World world, int x, int y, int z, byte metadata) {
        world.removeBlockEntity(x, y, z);
    }

    /*
     * Get the type of BlockEntity associated with this block.
     */
    public abstract BlockEntity get();
}