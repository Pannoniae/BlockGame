using BlockGame.util;
using BlockGame.util.stuff;
using BlockGame.util.xNBT;
using BlockGame.world.block.entity;
using Molten;

namespace BlockGame.world.block;

public abstract class BlockEntity : Persistent {

    public static readonly int FURNACE = register("furnace", w => new FurnaceBlockEntity(w));
    public static readonly int CHEST = register("chest", w => new ChestBlockEntity(w));

    public Vector3I pos;

    public BlockEntity(World world) {

    }

    public abstract void update(World world, int x, int y, int z);

    public void read(NBTCompound data) {
        pos = new Vector3I(
            data.getInt("x"),
            data.getInt("y"),
            data.getInt("z")
        );

        readx();
    }

    public void write(NBTCompound data) {
        data.addInt("x", pos.X);
        data.addInt("y", pos.Y);
        data.addInt("z", pos.Z);

        writex();
    }

    protected abstract void readx();

    protected abstract void writex();

    /**
     * Register a block entity type with a string ID.
     * Returns runtime int ID for fast lookups.
     */
    public static int register(string type, Func<World, BlockEntity> factory) {
        return Registry.BLOCK_ENTITIES.register(type, factory);
    }

    /**
     * Create a block entity instance by runtime int ID.
     */
    public static BlockEntity? create(World world, int type) {
        var factory = Registry.BLOCK_ENTITIES.factory(type);
        return factory != null ? factory(world) : null;
    }

    /**
     * Create a block entity instance by string ID (used for loading saves).
     */
    public static BlockEntity? create(World world, string type) {
        var factory = Registry.BLOCK_ENTITIES.factory(type);
        return factory != null ? factory(world) : null;
    }
}