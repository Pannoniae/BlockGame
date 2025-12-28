using BlockGame.util;
using BlockGame.util.xNBT;
using BlockGame.world.item;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

public class Cow : Mob {
    private int lastMilked = -World.TICKS_PER_DAY; // allow milking immediately when spawned

    public Cow(World world) : base(world, "cow") {
        tex = "textures/entity/cow.png";
        hp = 20;
    }

    public override AABB calcAABB(Vector3D pos) {
        return new AABB(
            pos.X - 0.7, pos.Y, pos.Z - 0.7,
            pos.X + 0.7, pos.Y + 1.4f, pos.Z + 0.7
        );
    }

    public override void getDrop(List<ItemStack> drops) {
        drops.Add(new ItemStack(Item.RAW_BEEF, 1, 0));
    }

    public override bool interact(Player player, ItemStack stack) {
        // milk cow with bottle
        if (stack.id == Item.BOTTLE.id) {
            // check if enough time has passed since last milking (1 full day)
            if (world.worldTick - lastMilked < World.TICKS_PER_DAY) {
                return false; // cow already milked today
            }

            // remove 1 bottle from held stack
            player.inventory.removeStack(player.inventory.selected, 1);
            // add 1 milk to inventory
            player.inventory.addItem(new ItemStack(Item.BOTTLE_MILK, 1, 0));

            // update last milked time
            lastMilked = world.worldTick;
            return true;
        }
        return false;
    }

    protected override void readx(NBTCompound data) {
        base.readx(data);
        lastMilked = data.getInt("lastMilked", -World.TICKS_PER_DAY);
    }

    public override void writex(NBTCompound data) {
        base.writex(data);
        data.addInt("lastMilked", lastMilked);
    }

}