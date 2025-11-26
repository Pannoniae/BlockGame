using System.Numerics;
using BlockGame.main;
using BlockGame.render;
using BlockGame.util;
using BlockGame.util.xNBT;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

/**
 * Arrow projectile entity.
 */
public class ArrowEntity : ProjectileEntity {
    protected override double gravityMultiplier => 0.5;
    protected override double airFriction => 0.99;
    protected override double damage => 5.0;
    protected override bool canCollideWithEntities => true;

    public int dieTimer = -1; // ticks until despawn after hitting something

    public ArrowEntity(World world) : base(world, "arrow") {
    }

    public override AABB calcAABB(Vector3D pos) {
        return AABB.fromSize(
            new Vector3D(pos.X - 0.25, pos.Y - 0.25, pos.Z - 0.25),
            new Vector3D(0.5, 0.5, 0.5)
        );
    }

    protected override bool shouldContinueUpdateSubclass(double dt) {
        if (dieTimer >= 0) {
            dieTimer--;
            if (dieTimer <= 0) {
                active = false;
                return false;
            }
        }
        return true;
    }

    protected override void updatePhysics(double dt) {
        // if arrow is stuck in ground, do not update physics
        if (dieTimer >= 0) {
            return;
        }

        base.updatePhysics(dt);
    }

    protected override void spawnTrailParticles() {
        // spawn particles every 2 ticks after initial grace period
        if (!Net.mode.isDed() && age % 2 == 0 && age > 8) {
            var particle = new ArrowParticle(world, position);
            world.particles.add(particle);
        }
    }

    protected override void onBlockHit() {
        // arrow sticks in block instead of despawning
        dieTimer = 60;
    }

    protected override void onEntityHit(Entity entity) {
        entity.dmg(damage, position);
        dieTimer = 60; // stick after hitting entity
    }

    protected override void updateRotation() {
        // align rotation with velocity
        if (velocity.LengthSquared() > 0.01) {
            var vel = velocity.toVec3();
            var norm = Vector3.Normalize(vel);

            // yaw from X/Z
            rotation.Y = float.Atan2(norm.X, norm.Z) * 180f / float.Pi;

            // pitch from Y
            var horizontalDist = float.Sqrt(norm.X * norm.X + norm.Z * norm.Z);
            rotation.X = -float.Atan2(norm.Y, horizontalDist) * 180f / float.Pi;
        }
    }

    // NBT serialization - extend base
    protected override void readx(NBTCompound data) {
        base.readx(data);
        if (data.has("dieTimer")) {
            dieTimer = data.getInt("dieTimer");
        }
    }

    public override void writex(NBTCompound data) {
        base.writex(data);
        data.addInt("dieTimer", dieTimer);
    }
}
