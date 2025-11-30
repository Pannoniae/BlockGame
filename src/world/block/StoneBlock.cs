using BlockGame.util;

namespace BlockGame.world.block;

#pragma warning disable CS8618
public class StoneBlock(string name) : Block(name) {
    public override void getDrop(List<ItemStack> drops, World world, int y, int z, int i, byte metadata, bool canBreak) {
        // stone drops cobblestone
        if (canBreak) {
            drops.Add(new ItemStack(COBBLESTONE.getItem(), 1, 0));
        }
        else {
            // if can't break, drop nothing
        }
    }
}