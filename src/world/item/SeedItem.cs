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
        // DEBUG
        Console.WriteLine($"SeedItem: targetBlock={targetBlock}, farmland.id={farmland.id}, crop.id={crop.id}");
        Console.WriteLine($"Position: x={x}, y={y}, z={z}");

        // for blocks like farmland, check the clicked block
        if (targetBlock == farmland.id) {
            // plant the crop
            world.setBlock(x, y + 1, z, crop.id);
            // consume one seed
            return stack.consume(player, 1);
        }

        // for blocks like grass, check the block below (since x,y,z is the air above)
        var blockBelow = world.getBlock(x, y - 1, z);
        if (blockBelow == farmland.id) {
            // plant the crop at current position
            world.setBlock(x, y, z, crop.id);
            // consume one seed
            return stack.consume(player, 1);
        }
        return stack;
    }
}