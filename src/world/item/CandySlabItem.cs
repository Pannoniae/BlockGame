using BlockGame.util;
using BlockGame.world.block;

namespace BlockGame.world.item;

public class CandySlabItem : BlockItem {
    public CandySlabItem(Block block) : base(block) {
    }

    public override string getName(ItemStack stack) {
        return ((CandySlab)block).getName((byte)stack.metadata);
    }
}
