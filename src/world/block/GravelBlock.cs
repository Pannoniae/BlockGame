using BlockGame.util;
using BlockGame.world.item;

namespace BlockGame.world.block;

#pragma warning disable CS8618
public class GravelBlock(string name) : FallingBlock(name) {
    public override void getDrop(List<ItemStack> drops, World world, int x, int y, int z, byte metadata, bool canBreak) {
        drops.Add(world.random.Next(12) == 0 ? new ItemStack(Item.FLINT, 1, 0) : new ItemStack(getItem(), 1, 0));
    }
}