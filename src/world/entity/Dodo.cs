using BlockGame.main;
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
            pos.X - 1.3f, pos.Y, pos.Z - 0.7f,
            pos.X + 1.3f, pos.Y + 2.2f, pos.Z + 0.7f
        );
    }

    public override void getDrop(List<ItemStack> drops) {
        drops.Add(new ItemStack(Item.FEATHER, 64,0));
        drops.Add(new ItemStack(Item.EGG, 1, 0));
    }
}


