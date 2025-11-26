using System.Numerics;
using BlockGame.main;
using BlockGame.render;
using BlockGame.util;
using BlockGame.util.xNBT;
using BlockGame.world.block;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

/**
 * Arrow projectile entity.
 */
public class ArrowEntity : Entity {
    public Entity? owner; // who shot the arrow
    public int age;
    public const int MAX_AGE = 1200; // 20 seconds
    public  double damage = 5.0;

    public int dieTimer = -1; // ticks until despawn after hitting something

    public ArrowEntity(World world) : base(world, "arrow") {
    }

    protected override bool needsPrevVars => true;
    protected override bool needsPhysics => true;
    protected override bool needsGravity => true;
    protected override bool needsCollision => true;
    protected override bool needsFriction => false; // arrows don't slow down much in air
    protected override bool needsBlockInteraction => false; // custom block interaction
    protected override bool needsEntityCollision => false; // custom entity collision

    public override AABB calcAABB(Vector3D pos) {
        return AABB.fromSize(
            new Vector3D(pos.X - 0.25, pos.Y - 0.25, pos.Z - 0.25),
            new Vector3D(0.5, 0.5, 0.5)
        );
    }

    protected override void updateGravity(double dt) {
        // lighter gravity than default
        velocity.Y -= GRAVITY * 0.5 * dt;
    }

    protected override bool shouldContinueUpdate(double dt) {
        age++;

        if (age > MAX_AGE) {
            active = false;
            return false;
        }

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

        // spawn particles every tick
        if (!Net.mode.isDed() && age % 2 == 0 && age > 8) {
            var particle = new ArrowParticle(world, position);
            world.particles.add(particle);
        }

        // check entity collision before moving
        checkEntityCollision();

        // apply gravity
        updateGravity(dt);

        // apply velocity
        velocity += accel * dt;
        clamp(dt);

        // check block collision
        var nextPos = position + velocity * dt;
        if (checkBlockCollision(nextPos)) {
            // hit block, despawn
            dieTimer = 60;
            return;
        }

        position = nextPos;

        velocity *= 0.99;

        // update AABB
        aabb = calcAABB(position);

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

    private bool checkBlockCollision(Vector3D pos) {
        var blockPos = pos.toBlockPos();
        var block = world.getBlock(blockPos);

        // check if block is solid
        if (Block.collision[block]) {
            return true;
        }

        return false;
    }

    private void checkEntityCollision() {
        var entities = new List<Entity>();
        world.getEntitiesInBox(entities, aabb);

        foreach (var entity in entities) {
            // skip owner for first 8 ticks
            if (entity == owner && age < 8) {
                continue;
            }

            // skip self and other arrows
            if (entity == this || entity is ArrowEntity) {
                continue;
            }

            // check collision
            if (AABB.isCollision(aabb, entity.aabb)) {
                entity.dmg(damage, position);
                dieTimer = 60;
                return;
            }
        }
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
