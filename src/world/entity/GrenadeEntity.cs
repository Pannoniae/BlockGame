using System.Numerics;
using BlockGame.main;
using BlockGame.render;
using BlockGame.util;
using BlockGame.util.xNBT;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

public class GrenadeEntity : ProjectileEntity {
    protected override double gravityMultiplier => 1.0;
    protected override double airFriction => 0.98;
    protected override double damage => 0.0; // grenades don't use contact damage
    protected override bool canCollideWithEntities => false; // no entity collision
    protected override bool needsFriction => true; // grenades use ground friction

    public int fuseTime = FUSE_LENGTH;
    public const int FUSE_LENGTH = 60; // 3 seconds
    public const double EXPLOSION_RADIUS = 5.0;
    public const double EXPLOSION_DAMAGE = 15.0;

    public GrenadeEntity(World world) : base(world, "grenade") {
    }

    public override AABB calcAABB(Vector3D pos) {
        return AABB.fromSize(
            new Vector3D(pos.X - 0.15, pos.Y - 0.15, pos.Z - 0.15),
            new Vector3D(0.3, 0.3, 0.3)
        );
    }

    protected override bool shouldContinueUpdateSubclass(double dt) {
        fuseTime--;

        if (fuseTime <= 0) {
            explode();
            active = false;
            return false;
        }

        return true;
    }

    protected override void onBlockHit() {
        // bounce instead of despawn: reverse velocity and dampen
        velocity *= -0.6;
        velocity *= 0.98; // extra friction on bounce
    }

    private void explode() {
        var explosionBox = new AABB(
            position - new Vector3D(EXPLOSION_RADIUS),
            position + new Vector3D(EXPLOSION_RADIUS)
        );

        var entities = new List<Entity>();
        world.getEntitiesInBox(entities, explosionBox);

        // apply damage with linear falloff
        foreach (var entity in entities) {
            if (entity == this) continue;

            var dist = (entity.position - position).Length();

            if (dist <= EXPLOSION_RADIUS) {
                var falloff = 1.0 - (dist / EXPLOSION_RADIUS);
                var actualDmg = EXPLOSION_DAMAGE * falloff;

                if (actualDmg > 0.5) {
                    entity.dmg(actualDmg, position);
                }
            }
        }

        // spawn explosion particles
        if (!Net.mode.isDed()) {
            for (int i = 0; i < 24; i++) {
                // random direction on sphere
                var theta = Game.clientRandom.NextDouble() * Math.PI * 2;
                var phi = Math.Acos(2 * Game.clientRandom.NextDouble() - 1);

                var dir = new Vector3D(
                    Math.Sin(phi) * Math.Cos(theta),
                    Math.Sin(phi) * Math.Sin(theta),
                    Math.Cos(phi)
                );

                var particle = new ExplosionParticle(world, position, dir);
                world.particles.add(particle);
            }
        }
    }

    // NBT serialization - extend base
    protected override void readx(NBTCompound data) {
        base.readx(data);
        if (data.has("fuseTime")) {
            fuseTime = data.getInt("fuseTime");
        }
    }

    public override void writex(NBTCompound data) {
        base.writex(data);
        data.addInt("fuseTime", fuseTime);
    }
}
