using BlockGame.main;
using BlockGame.render;
using BlockGame.util;
using BlockGame.util.xNBT;
using BlockGame.world.block;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

public class GrenadeEntity : Entity {
    public Entity? owner;
    public int age;
    public int fuseTime = 60; // 3 sec
    public const int FUSE_LENGTH = 60;
    public const double EXPLOSION_RADIUS = 5.0;
    public const double EXPLOSION_DAMAGE = 15.0;

    public GrenadeEntity(World world) : base(world, "grenade") {
    }

    protected override bool needsPrevVars => true;
    protected override bool needsPhysics => true;
    protected override bool needsGravity => true;
    protected override bool needsCollision => true;
    protected override bool needsFriction => true;
    protected override bool needsBlockInteraction => false;
    protected override bool needsEntityCollision => false;

    public override AABB calcAABB(Vector3D pos) {
        return AABB.fromSize(
            new Vector3D(pos.X - 0.15, pos.Y - 0.15, pos.Z - 0.15),
            new Vector3D(0.3, 0.3, 0.3)
        );
    }

    protected override void updateGravity(double dt) {
        velocity.Y -= GRAVITY * dt;
    }

    protected override bool shouldContinueUpdate(double dt) {
        age++;
        fuseTime--;

        if (fuseTime <= 0) {
            explode();
            active = false;
            return false;
        }

        return true;
    }

    protected override void updatePhysics(double dt) {
        // apply gravity
        updateGravity(dt);

        // apply velocity
        velocity += accel * dt;
        clamp(dt);

        // check block collision and bounce
        var nextPos = position + velocity * dt;
        if (checkBlockCollision(nextPos)) {
            // bounce: reverse velocity and dampen
            velocity *= -0.6;
            velocity *= 0.98; // extra friction on bounce
        }
        else {
            position = nextPos;
        }

        // apply friction when on ground or in air
        velocity *= 0.98;

        // update AABB
        aabb = calcAABB(position);
    }

    private bool checkBlockCollision(Vector3D pos) {
        var blockPos = pos.toBlockPos();
        if (!world.inWorld(blockPos.X, blockPos.Y, blockPos.Z)) {
            return false;
        }

        var block = world.getBlock(blockPos);

        // check if block is solid
        if (Block.collision[block]) {
            return true;
        }

        return false;
    }

    private void explode() {
        var explosionBox = new AABB(
            position - new Vector3D(EXPLOSION_RADIUS),
            position + new Vector3D(EXPLOSION_RADIUS)
        );

        var entities = new List<Entity>();
        world.getEntitiesInBox(entities, explosionBox);

        // apply damage with falloff
        foreach (var entity in entities) {
            if (entity == this) continue;

            var dist = (entity.position - position).Length();

            if (dist <= EXPLOSION_RADIUS) {
                // linear
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

    protected override void readx(NBTCompound data) {
        if (data.has("age")) {
            age = data.getInt("age");
        }
        if (data.has("fuseTime")) {
            fuseTime = data.getInt("fuseTime");
        }
    }

    public override void writex(NBTCompound data) {
        data.addInt("age", age);
        data.addInt("fuseTime", fuseTime);
    }
}
