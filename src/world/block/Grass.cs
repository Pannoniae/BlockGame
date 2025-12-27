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
        // 12.5% total seeds drop chance: wheat 40%, carrot 40%, strawberry 20%
        if (canBreak && world.random.NextDouble() < 0.125) {
            var rng = world.random.NextDouble();
            if (rng < 0.4) {
                drops.Add(new ItemStack(Item.WHEAT_SEEDS, 1, 0));
            } else if (rng < 0.8) {
                drops.Add(new ItemStack(Item.CARROT_SEEDS, 1, 0));
            } else {
                drops.Add(new ItemStack(Item.STRAWBERRY_SEEDS, 1, 0));
            }
        }
    }
}