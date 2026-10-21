using System.Numerics;
using BlockGame.main;
using BlockGame.util;
using BlockGame.util.xNBT;
using BlockGame.world.block;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

public class SnowballEntity : Entity {
    public Entity? owner; // who threw the snowball
    public int age;
    public const int MAX_AGE = 1200;
    public double damage = 1.5;

    public SnowballEntity(World world) : base(world, "snowball") {
        flags &= ~(EFlag.FRICTION | EFlag.BLOCK_INTERACTION);
    }


    public override AABB calcAABB(Vector3D pos) {
        return AABB.fromSize(
            new Vector3D(pos.X - 0.125, pos.Y - 0.125, pos.Z - 0.125),
            new Vector3D(0.25, 0.25, 0.25)
        );
    }

    protected override void updateGravity(double dt) {
        velocity.Y -= GRAVITY * 0.7 * dt;
    }

    protected override bool shouldContinueUpdate(double dt) {
        age++;

        if (age > MAX_AGE) {
            active = false;
            return false;
        }

        return true;
    }

    protected override void updatePhysics(double dt) {
        updateGravity(dt);

        velocity += accel * dt;
        clamp(dt);

        var from = position;
        var to = position + velocity * dt;
        var tBlock = ProjectileEntity.collide(world, from, to);

        var hit = ProjectileEntity.collideEntities(this, owner, age, from, to, tBlock, out _);
        if (hit != null) {
            hit.dmg(damage, this);
            active = false;
            return;
        }

        if (tBlock < 1) {
            active = false;
            return;
        }

        position = to;
        velocity *= 0.98;
        aabb = calcAABB(position);
    }

    protected override void readx(NBTCompound data) {
        if (data.has("age")) {
            age = data.getInt("age");
        }
    }

    public override void writex(NBTCompound data) {
        data.addInt("age", age);
    }
}
