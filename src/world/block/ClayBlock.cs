using BlockGame.util;
using BlockGame.world.item;

namespace BlockGame.world.block;

public class ClayBlock : Block {
    public ClayBlock(string name) : base(name) {
    }

    public override void getDrop(List<ItemStack> drops, World world, int x, int y, int z, byte metadata, bool canBreak) {
        var q = world.random.Next(3) + 2;
        drops.Add(new ItemStack(Item.CLAY, q, 0));
    }
}