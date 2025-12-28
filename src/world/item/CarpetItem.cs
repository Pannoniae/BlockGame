using BlockGame.util;
using BlockGame.world.block;

namespace BlockGame.world.item;

public class CarpetItem : BlockItem {
    public CarpetItem(Block block) : base(block) {
    }

    public override string getName(ItemStack stack) {
        return ((Carpet)block).getName((byte)stack.metadata);
    }
}
