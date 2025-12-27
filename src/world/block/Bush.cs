using BlockGame.main;
using BlockGame.util;
using BlockGame.world.entity;
using BlockGame.world.item;

namespace BlockGame.world.block;

#pragma warning disable CS8618
public class Bush : Block {
    public Item? fruit;
    public Item? seed;

    public Bush(string name, Item? fruit = null, Item? seed = null) : base(name) {
        this.fruit = fruit;
        this.seed = seed;
    }

    protected override void onRegister(int id) {
        transparency();
        tick();
        material(Material.ORGANIC);
        setHardness(0.25);
        optionalTool[id] = true;
        // only broken by a scythe!
        tool[id] = ToolType.SCYTHE;
        tier[id] = MaterialTier.WOOD;
    }

    public override bool canPlace(World world, int x, int y, int z, Placement info) {
        // bushes can be placed on grass, dirt, or snow grass
        if (y <= 0 || !world.inWorld(x, y - 1, z)) {
            return false;
        }

        var below = world.getBlock(x, y - 1, z);
        return below == GRASS.id || below == DIRT.id || below == SNOW_GRASS.id;
    }

    public override void update(World world, int x, int y, int z) {
        if (world.inWorld(x, y - 1, z) && world.getBlock(x, y - 1, z) == 0) {
            world.setBlock(x, y, z, AIR.id);
        }
    }

    public override void getDrop(List<ItemStack> drops, World world, int x, int y, int z, byte metadata, bool canBreak) {
        if (canBreak && seed != null) {
            // broken with scythe: drop seeds
            drops.Add(new ItemStack(seed, 1, 0));
        }
        // broken without scythe: nothing
    }

    public override void scheduledUpdate(World world, int x, int y, int z) {
        // bush regrowth: reset harvested flag after cooldown
        if (fruit != null) {
            var metadata = world.getBlockMetadata(x, y, z);
            world.setMetadata(x, y, z, (byte)(metadata & ~1));
        }
    }

    public override bool onUse(World world, int x, int y, int z, Player player) {
        // only fruit-bearing bushes can be harvested
        if (fruit == null) {
            return false;
        }

        // check if player is holding a scythe
        var held = player.inventory.getSelected();
        if (held == ItemStack.EMPTY || held.getItem() is not Tool tool || tool.type != ToolType.SCYTHE) {
            return false;
        }

        // check if already harvested (metadata bit 0)
        var metadata = world.getBlockMetadata(x, y, z);
        if ((metadata & 1) != 0) {
            // already harvested, cooldown active
            return true;
        }

        // harvest: give fruit, mark as harvested, schedule regrowth
        int fruitCount = Game.random.Next(1, 3);
        player.inventory.addItem(new ItemStack(fruit, fruitCount, 0));

        // set metadata bit to mark as harvested
        world.setMetadata(x, y, z, (byte)(metadata | 1));

        // schedule regrowth after full day-night cycle
        world.scheduleBlockUpdate(new Molten.Vector3I(x, y, z), World.TICKS_PER_DAY);

        // damage scythe
        held.damageItem(player, 1);
        player.inventory.setStack(player.inventory.selected, held);

        return true;
    }
}