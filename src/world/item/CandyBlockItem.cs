using BlockGame.util;

namespace BlockGame.item;

public class CandyBlockItem : BlockItem {
    public CandyBlockItem(int id, string name) : base(id, name) {
    }

    public override string getName(ItemStack stack) {
        return ((CandyBlock)Block.blocks[-id]).getName((byte)stack.metadata);
    }
}