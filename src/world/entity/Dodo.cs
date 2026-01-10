using BlockGame.main;
using BlockGame.util;
using BlockGame.util.xNBT;
using BlockGame.world.item;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

public class Dodo : Mob {
    private int lastEggLaid = -World.TICKS_PER_DAY; // lay egg immediately when spawned

    public Dodo(World world) : base(world, "dodo") {
        tex = "textures/entity/dodo.png";
        hp = 10;
    }

    public override AABB calcAABB(Vector3D pos) {
        return new AABB(
            pos.X - 1.3f, pos.Y, pos.Z - 0.7f,
            pos.X + 1.3f, pos.Y + 2.2f, pos.Z + 0.7f
        );
    }

    public override void getDrop(List<ItemStack> drops) {
        drops.Add(new ItemStack(Item.FEATHER, 64,0));
        drops.Add(new ItemStack(Item.EGG, 1, 0));
    }

    public override void AI(double dt) {
        base.AI(dt);

        // lay egg every full day-night cycle
        if (world.worldTick - lastEggLaid >= World.TICKS_PER_DAY) {
            lastEggLaid = world.worldTick;

            var egg = new ItemEntity(world);
            egg.stack = new ItemStack(Item.EGG, 1, 0);
            egg.position = new Vector3D(position.X, position.Y + 0.3, position.Z);
            egg.velocity = new Vector3D(
                (Game.random.NextSingle() - 0.5) * 0.2,
                0.15,
                (Game.random.NextSingle() - 0.5) * 0.2
            );
            world.addEntity(egg);
        }
    }

    protected override void readx(NBTCompound data) {
        base.readx(data);
        lastEggLaid = data.getInt("lastEggLaid", -World.TICKS_PER_DAY);
    }

    public override void writex(NBTCompound data) {
        base.writex(data);
        data.addInt("lastEggLaid", lastEggLaid);
    }
}


