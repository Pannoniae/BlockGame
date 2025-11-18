using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.entity;

namespace BlockGame.world.item;

public class Lighter : Item {
    public Lighter(string name) : base(name) {
    }

    public override ItemStack? useBlock(ItemStack stack, World world, Player player, int x, int y, int z, Placement info) {
        // try to place fire TODO actually implement the conditions properly
        if (true || world.getBlock(x, y, z) == Block.AIR.id && FireBlock.canSurvive(world, x, y, z)) {
            world.setBlock(x, y, z, Block.FIRE.id);
            return stack; // lighter is not consumed
        }

        return null;
    }
}