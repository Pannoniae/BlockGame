using BlockGame.main;
using BlockGame.util;
using BlockGame.world.item;
using Molten.DoublePrecision;
namespace BlockGame.world.entity;

public class Boa : Hostile {
    protected override double detectRadius => 24.0;
    protected override float attackDamage => 4.0f;
    protected override int attackCooldown => 40;
    protected override bool usePathfinding => false; // uses pathfinding for ground movement
    // don't take fall damage
    protected override bool needsFallDamage => false;

    public override bool burnInSunlight => false;
    //public override double eyeHeight => 0;

    public Boa(World world) : base(world, "boa") {
        tex = "textures/entity/boa.png";
        hp = 10;
    }

    public override AABB calcAABB(Vector3D pos) {
        return new AABB(
            pos.X - 0.25, pos.Y, pos.Z - 0.25,
            pos.X + 0.25, pos.Y + 1, pos.Z + 0.25
        );
    }
    public override void AI(double dt) {
        base.AI(dt);
        jumping = false; // boa doesn't jump
    }

    public override void getDrop(List<ItemStack> drops) {
        //if (id == Boa.id && Game.random.Next(20) == 0) {
        //    drops.Add(new ItemStack(.item, 1, 0));
        //}
    }
}
