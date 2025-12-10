using BlockGame.world.block;

namespace BlockGame.util.stuff;

public class BlockRegistry : Registry<Block> {

    /**
     * Stores whether the block is a full, opaque block or not.
     */
    public readonly XUList<bool> fullBlock;

    /**
     * Is this block transparent? (glass, leaves, etc.)
     */
    public readonly XUList<bool> transparent;

    public readonly XUList<bool> translucent;

    /**
     * If false, water can break this block (like tall grass, flowers, etc.)
     * If true, water cannot break this block (like stone, dirt, stairs, etc.)
     */
    public readonly XUList<bool> waterSolid;
    public readonly XUList<bool> lavaSolid;

    public readonly XUList<bool> randomTick;
    public readonly XUList<bool> renderTick;
    public readonly XUList<bool> liquid;
    public readonly XUList<bool> customCulling;
    public readonly XUList<bool> renderItemLike;

    public readonly XUList<bool> selection;
    public readonly XUList<bool> collision;
    public readonly XUList<byte> lightLevel;
    public readonly XUList<byte> lightAbsorption;
    public readonly XUList<double> hardness;
    public readonly XUList<double> flammable;

    public readonly XUList<bool> log;
    public readonly XUList<bool> leaves;

    /**
     Block update delay in ticks. 0 = normal immediate block updates
    */
    public readonly XUList<byte> updateDelay;

    public readonly XUList<AABB?> AABB;
    public readonly XUList<bool> customAABB;

    public readonly XUList<RenderType> renderType;
    public readonly XUList<ToolType> tool;
    public readonly XUList<bool> optionalTool;
    public readonly XUList<MaterialTier> tier;
    public readonly XUList<float> friction;
    public readonly XUList<bool> natural;

    /**
     * Disable creating an item for this block.
     */
    public readonly XUList<bool> noItem;
    public readonly XUList<bool> isBlockEntity;
    public readonly XUList<bool> circuit;

    public BlockRegistry() {
        fullBlock = track(true);
        transparent = track(false);
        translucent = track(false);
        waterSolid = track(true);
        lavaSolid = track(true);
        randomTick = track(false);
        renderTick = track(false);
        liquid = track(false);
        customCulling = track(false);
        renderItemLike = track(false);

        selection = track(true);
        collision = track(true);
        lightLevel = track((byte)0);
        lightAbsorption = track((byte)0);
        hardness = track(-0.1);
        flammable = track(0.0);

        log = track(false);
        leaves = track(false);
        updateDelay = track((byte)0);
        AABB = track((AABB?)Block.fullBlockAABB());
        customAABB = track(false);
        renderType = track(RenderType.CUBE);
        tool = track(ToolType.NONE);
        optionalTool = track(false);
        tier = track(MaterialTier.NONE);
        noItem = track(false);
        isBlockEntity = track(false);

        friction = track(0.8f);
        natural = track(false);
        circuit = track(false);
    }

    public override int register(string type, Block value) {
        return base.register(type, value);
    }
}