namespace BlockGame.world.block;

public abstract class EntityBlock : Block {
    public EntityBlock(string name) : base(name) {
    }

    protected override void onRegister(int id) {
        isBlockEntity[id] = true;
    }

    public override void onPlace(World world, int x, int y, int z, byte metadata) {
        var be = get();
        be.pos = new Molten.Vector3I(x, y, z);
        world.setBlockEntity(x, y, z, be);
    }

    public override void onBreak(World world, int x, int y, int z, byte metadata) {
        world.removeBlockEntity(x, y, z);
    }

    /*
     * Get the type of BlockEntity associated with this block.
     */
    public abstract BlockEntity get();
}