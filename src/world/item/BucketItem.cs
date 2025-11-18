using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.entity;
using Molten;

namespace BlockGame.world.item;

public class BucketItem : Item {
    private readonly Block? liquidBlock;

    /** empty bucket */
    public BucketItem(string name) : base(name) {
        this.liquidBlock = null;
    }

    /** filled bucket */
    public BucketItem(string name, Block liquidBlock) : base(name) {
        this.liquidBlock = liquidBlock;
    }

    public override ItemStack? useBlock(ItemStack stack, World world, Player player, int x, int y, int z, Placement info) {
        var cb = world.getBlock(x, y, z);
        var metadata = world.getBlockMetadata(x, y, z);

        // empty bucket: try to pick up liquid
        if (liquidBlock == null) {
            // check for water source
            if (Liquid.getWaterLevel(metadata) == 0 && !Liquid.isFalling(metadata)) {
                world.setBlock(x, y, z, Block.AIR.id);
                if (cb == Block.WATER.id) {
                    return new ItemStack(WATER_BUCKET, 1);
                }
                if (cb == Block.LAVA.id) {
                    return new ItemStack(LAVA_BUCKET, 1);
                }
            }
        }
        // filled bucket: try to place liquid
        else {
            if (cb == 0 || !Block.fullBlock[cb]) {
                // place liquid source with dynamic flag
                world.setBlockMetadata(x, y, z, ((uint)liquidBlock.id).setMetadata(Liquid.setDynamic(0, true)));
                // manually schedule update if replacing same liquid (onPlace won't trigger :()
                if (cb == liquidBlock.id) {
                    world.scheduleBlockUpdate(new Vector3I(x, y, z));
                }
                return new ItemStack(BUCKET, 1);
            }
        }

        return null;
    }

    public override int getMaxStackSize() => 1;
}