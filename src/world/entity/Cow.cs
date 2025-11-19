using BlockGame.main;
using BlockGame.util;
using BlockGame.world.item;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

public class Cow : Mob {
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

    //public override (Item item, byte metadata, int count) getDrop() {
    //    return (Item.RAW_BEEF, 0, 1);
    //}


    // todo milk?

    public override (Item? item, byte metadata, int count) getDrop() {
        // 1 in 5 chance to drop beef
        if (id == Entities.COW && Game.random.Next(5) == 0) {
            return (Item.RAW_BEEF, 0, 1);
        }

        // 1 in 5 chance to drop milk
        if (id == Entities.COW && Game.random.Next(5) == 0) {
            return (Item.BOTTLE_MILK, 0, 1);
        }

        return (null, 0, 0);
    }

}