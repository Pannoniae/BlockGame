using BlockGame.main;
using BlockGame.util;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

public class SnowballEntity : ProjectileEntity {
    protected override double gravityMultiplier => 0.7;
    protected override double airFriction => 0.98;
    protected override double damage => 1.5;
    protected override bool canCollideWithEntities => true;

    public double knockbackStrength = 5.0;

    public SnowballEntity(World world) : base(world, "snowball") {
    }

    public override AABB calcAABB(Vector3D pos) {
        return AABB.fromSize(
            new Vector3D(pos.X - 0.125, pos.Y - 0.125, pos.Z - 0.125),
            new Vector3D(0.25, 0.25, 0.25)
        );
    }

    protected override void spawnTrailParticles() {
        // spawn particle trail every 3 ticks after initial grace period
        if (!Net.mode.isDed() && age % 3 == 0 && age > 5) {
            // TODO: create SnowParticle? or this is fine?
            // var particle = new SnowParticle(world, position);
            // world.particles.add(particle);
        }
    }

    // inherits default behavior:
    // - despawns on block collision
    // - despawns on entity hit after dealing damage
    // - no rotation
}
