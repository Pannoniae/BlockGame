using BlockGame.main;
using BlockGame.util;
using BlockGame.world.item;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

public class Cow : Mob {
    public Cow(World world) : base(world, "cow") {
        tex = Game.textures.cow;
    }

    public override AABB calcAABB(Vector3D pos) {
        return new AABB(
            pos.X - 0.7, pos.Y, pos.Z - 0.7,
            pos.X + 0.7, pos.Y + 1, pos.Z + 0.7
        );
    }

    public override (Item item, byte metadata, int count) getDrop() {
        return (Item.STEAK_RAW, 0, 1);
    }
    

    // todo milk?
}