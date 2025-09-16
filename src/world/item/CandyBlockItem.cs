using BlockGame.util;
using BlockGame.world.block;

namespace BlockGame.world.item;

public class CandyBlockItem : BlockItem {
    public CandyBlockItem(int id, string name) : base(id, name) {
    }

    public override string getName(ItemStack stack) {
        return ((CandyBlock)Block.blocks[-id]).getName((byte)stack.metadata);
    }
}