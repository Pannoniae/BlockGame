using BlockGame.render.model;
using BlockGame.util.stuff;
using BlockGame.util.xNBT;
using BlockGame.world.block.entity;
using Molten;

namespace BlockGame.world.block;

public abstract class BlockEntity : Persistent {

    public static int FURNACE;
    public static int CHEST;
    public static int SIGN;

    public Vector3I pos;

    public string type;

    public BlockEntity(string type) {
        this.type = type;
    }

    public abstract void update(World world, int x, int y, int z);

    public void read(NBTCompound data) {
        pos = new Vector3I(
            data.getInt("x"),
            data.getInt("y"),
            data.getInt("z")
        );

        readx(data);
    }

    public void write(NBTCompound data) {
        data.addInt("x", pos.X);
        data.addInt("y", pos.Y);
        data.addInt("z", pos.Z);

        writex(data);
    }

    protected abstract void readx(NBTCompound data);

    protected abstract void writex(NBTCompound data);

    public static void preLoad() {
        // force class load to register block entities
        FURNACE = register("furnace", () => new FurnaceBlockEntity());
        CHEST = register("chest", () => new ChestBlockEntity());
        SIGN = register("sign", () => new SignBlockEntity());

        // sign has a custom renderer
        Registry.BLOCK_ENTITIES.hasRenderer[SIGN] = true;

        BlockEntityRenderers.reloadAll();
    }

    /**
     * Register a block entity type with a string ID.
     * Returns runtime int ID for fast lookups.
     */
    public static int register(string type, Func<BlockEntity> factory) {
        return Registry.BLOCK_ENTITIES.register(type, factory);
    }

    /**
     * Create a block entity instance by runtime int ID.
     */
    public static BlockEntity? create(World world, int type) {
        var factory = Registry.BLOCK_ENTITIES.factory(type);
        return factory != null ? factory() : null;
    }

    /**
     * Create a block entity instance by string ID (used for loading saves).
     */
    public static BlockEntity? create(string type) {
        var factory = Registry.BLOCK_ENTITIES.factory(type);
        return factory != null ? factory() : null;
    }
}