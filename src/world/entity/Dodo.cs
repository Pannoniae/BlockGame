using BlockGame.util;
using BlockGame.world.item;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

public class Dodo : Mob {
    public Dodo(World world) : base(world, "dodo") {
        tex = "textures/entity/dodo.png";
        hp = 10;
    }

    public override AABB calcAABB(Vector3D pos) {
        return new AABB(
            pos.X-0.3, pos.Y, pos.Z - 0.3,
            pos.X + 0.3, pos.Y + 2, pos.Z + 0.3
        );
    }

    public override void getDrop(List<ItemStack> drops) {
        drops.Add(new ItemStack(Item.FEATHER, 100, 0));
    }

}