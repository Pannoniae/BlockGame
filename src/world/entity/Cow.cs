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

    public override void getDrop(List<ItemStack> drops) {
        drops.Add(new ItemStack(Item.RAW_BEEF, 1, 0));
    }

}