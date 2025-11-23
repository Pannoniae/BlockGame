using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.entity;

namespace BlockGame.world.item;

public class SeedItem : Item {

    public Block crop;
    public Block farmland;

    public SeedItem(string name, Block crop, Block farmland) : base(name) {
        this.crop = crop;
        this.farmland = farmland;
    }

    public override ItemStack? useBlock(ItemStack stack, World world, Player player, int x, int y, int z, Placement info) {
        var targetBlock = world.getBlock(x, y, z);
        if (targetBlock == farmland.id) {
            // plant the crop
            world.setBlock(x, y + 1, z, crop.id);
            // consume one seed
            return stack.consume(player, 1);
        }
        return stack;
    }
}