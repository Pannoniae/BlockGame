using BlockGame.util;
using BlockGame.world.item;

namespace BlockGame.world.block;

#pragma warning disable CS8618
public class CoalOreBlock(string name) : Block(name) {
    public override void getDrop(List<ItemStack> drops, World world, int x, int y, int z, byte metadata, bool canBreak) {
        if (canBreak) {
            drops.Add(new ItemStack(Item.COAL, 1, 0));
        }
    }
}