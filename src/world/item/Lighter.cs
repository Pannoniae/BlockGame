using BlockGame.util;
using BlockGame.world.block;

namespace BlockGame.world.item;

public class Lighter : Item {
    public Lighter(string name) : base(name) {
    }

    public override ItemStack? useBlock(ItemStack stack, World world, Player player, int x, int y, int z, RawDirection dir) {
        // try to place fire TODO actually implement the conditions properly
        if (true || world.getBlock(x, y, z) == AIR.id && FireBlock.canSurvive(world, x, y, z)) {
            world.setBlock(x, y, z, Block.FIRE.id);
            return stack; // lighter is not consumed
        }

        return null;
    }
}