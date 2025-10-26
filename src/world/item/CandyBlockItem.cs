using BlockGame.util;
using BlockGame.world.block;

namespace BlockGame.world.item;

public class CandyBlockItem : BlockItem {
    public CandyBlockItem(Block block) : base(block) {
    }

    public override string getName(ItemStack stack) {
        return ((CandyBlock)block).getName((byte)stack.metadata);
    }
}