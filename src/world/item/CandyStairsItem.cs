using BlockGame.util;
using BlockGame.world.block;

namespace BlockGame.world.item;

public class CandyStairsItem : BlockItem {
    public CandyStairsItem(Block block) : base(block) {
    }

    public override string getName(ItemStack stack) {
        return ((CandyStairs)block).getName((byte)stack.metadata);
    }
}
