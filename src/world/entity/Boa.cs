using BlockGame.main;
using BlockGame.util;
using BlockGame.world.item;
using Molten.DoublePrecision;
namespace BlockGame.world.entity;

public class Boa : Hostile {
    protected override double detectRadius => 16.0;
    protected override float attackDamage => 4.0f;
    protected override int attackCooldown => 40;
    protected override bool usePathfinding => true;
    protected override bool needsFallDamage => false;
    public override double speedMul => 0.1;
    public override bool burnInSunlight => false;

    public Boa(World world) : base(world, "boa") {
        tex = "textures/entity/boa.png";
        hp = 50;
    }

    public override AABB calcAABB(Vector3D pos) {
        return new AABB(
            pos.X - 0.5, pos.Y, pos.Z - 0.5,
            pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5
        );
    }
    protected override bool shouldContinueUpdate(double dt) {
        // instant despawn on death, no animation
        if (hp <= 0 && !dead) {
            die();
            active = false;
            return false;
        }
        if (dead) {
            active = false;
            return false;
        }
        return base.shouldContinueUpdate(dt);
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
