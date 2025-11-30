using BlockGame.util;
using BlockGame.world.item;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

public class Zombie : Hostile {
    protected override double detectRadius => 16.0;

    public override bool burnInSunlight => true;

    public Zombie(World world) : base(world, "zombie") {
        tex = "textures/entity/zombie.png";
        hp = 30;
    }

    public override AABB calcAABB(Vector3D pos) {
        return new AABB(
            pos.X - 0.3, pos.Y, pos.Z - 0.3,
            pos.X + 0.3, pos.Y + 1.8, pos.Z + 0.3
        );
    }

    public override void getDrop(List<ItemStack> drops) {
        drops.Add(new ItemStack(Item.FLINT, 1, 0));
    }
}
