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
    public double knockbackStrength = 5.0;

    public SnowballEntity(World world) : base(world, "snowball") {
    }

    protected override bool needsPrevVars => true;
    protected override bool needsPhysics => true;
    protected override bool needsGravity => true;
    protected override bool needsCollision => true;
    protected override bool needsFriction => false;
    protected override bool needsBlockInteraction => false;
    protected override bool needsEntityCollision => false;

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
        // spawn particle trail
        if (!Net.mode.isDed() && age % 3 == 0 && age > 5) {
            // TODO: create SnowParticle? or this is fine?
            // var particle = new SnowParticle(world, position);
            // world.particles.add(particle);
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
            active = false;
            return;
        }

        position = nextPos;

        // air friction
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

    private void checkEntityCollision() {
        var entities = new List<Entity>();
        world.getEntitiesInBox(entities, aabb);

        foreach (var entity in entities) {
            // skip owner for first 8 ticks
            if (entity == owner && age < 8) {
                continue;
            }

            // skip self and other snowballs
            if (entity == this || entity is SnowballEntity) {
                continue;
            }

            // check collision
            if (AABB.isCollision(aabb, entity.aabb)) {
                entity.dmg(damage, this);
                active = false; // despawn immediately on hit
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
