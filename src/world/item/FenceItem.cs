using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.entity;

namespace BlockGame.world.item;

public class FenceItem : Item {
    private readonly Block fenceBlock;

    public FenceItem(string name, Block block) : base(name) {
        fenceBlock = block;
    }

    public override ItemStack? useBlock(ItemStack stack, World world, Player player, int x, int y, int z, Placement info) {
        if (!fenceBlock.canPlace(world, x, y, z, info)) {
            return null;
        }

        fenceBlock.place(world, x, y, z, 0, info);
        return stack.consume(player, 1);
    }
}