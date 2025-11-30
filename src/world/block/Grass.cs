using BlockGame.util;
using BlockGame.world.item;

namespace BlockGame.world.block;

#pragma warning disable CS8618
public class Grass(string name) : Block(name) {
    public override void update(World world, int x, int y, int z) {
        if (world.inWorld(x, y - 1, z) && world.getBlock(x, y - 1, z) == 0) {
            world.setBlock(x, y, z, AIR.id);
        }
    }

    public override void getDrop(List<ItemStack> drops, World world, int x, int y, int z, byte metadata, bool canBreak) {
        // 12.5% total drop chance, split 50-50 between wheat and carrot seeds
        if (canBreak && world.random.NextDouble() < 0.125) {
            if (world.random.NextDouble() < 0.5) {
                drops.Add(new ItemStack(Item.WHEAT_SEEDS, 1, 0));
            } else {
                drops.Add(new ItemStack(Item.CARROT_SEEDS, 1, 0));
            }
        }
    }
}