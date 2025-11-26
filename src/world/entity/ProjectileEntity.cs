using BlockGame.util;
using BlockGame.util.xNBT;
using BlockGame.world.block;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

/**
 * Base class for projectile entities (arrows, snowballs, grenades, etc.)
 * Consolidates common projectile behavior: physics, collision detection, aging
 */
public abstract class ProjectileEntity : Entity {
    // shared state
    public Entity? owner; // who fired/threw this projectile
    public int age;
    protected const int MAX_AGE = 1200; // 60 seconds

    // subclass configuration - override these
    protected abstract double gravityMultiplier { get; }
    protected abstract double airFriction { get; }
    protected abstract double damage { get; }
    protected abstract bool canCollideWithEntities { get; }

    protected ProjectileEntity(World world, string name) : base(world, name) {
    }

    // common capability flags
    protected override bool needsPrevVars => true;
    protected override bool needsPhysics => true;
    protected override bool needsGravity => true;
    protected override bool needsCollision => true;
    protected override bool needsFriction => false; // most projectiles don't use ground friction
    protected override bool needsBlockInteraction => false; // custom block interaction
    protected override bool needsEntityCollision => false; // custom entity collision

    protected override void updateGravity(double dt) {
        velocity.Y -= GRAVITY * gravityMultiplier * dt;
    }

    protected override bool shouldContinueUpdate(double dt) {
        age++;

        if (age > MAX_AGE) {
            active = false;
            return false;
        }

        // subclass can add more checks (fuse, stuck timer, etc.)
        return shouldContinueUpdateSubclass(dt);
    }

    // hook for subclass-specific despawn logic
    protected virtual bool shouldContinueUpdateSubclass(double dt) {
        return true;
    }

    protected override void updatePhysics(double dt) {
        // optional particle spawning
        spawnTrailParticles();

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
            onBlockHit();
            return;
        }

        // move to next position
        position = nextPos;

        // apply air friction
        velocity *= airFriction;

        // update AABB
        aabb = calcAABB(position);

        // optional: update rotation based on velocity
        updateRotation();
    }

    // unified block collision with bounds check
    protected bool checkBlockCollision(Vector3D pos) {
        var blockPos = pos.toBlockPos();
        if (!world.inWorld(blockPos.X, blockPos.Y, blockPos.Z)) {
            return false;
        }

        var block = world.getBlock(blockPos);
        return Block.collision[block];
    }

    // unified entity collision with owner grace period
    protected virtual void checkEntityCollision() {
        if (!canCollideWithEntities) return;

        var entities = new List<Entity>();
        world.getEntitiesInBox(entities, aabb);

        foreach (var entity in entities) {
            // skip owner for first 8 ticks
            if (entity == owner && age < 8) {
                continue;
            }

            // skip self and other projectiles of same type
            if (entity == this || entity.GetType() == GetType()) {
                continue;
            }

            // check collision
            if (AABB.isCollision(aabb, entity.aabb)) {
                onEntityHit(entity);
                return;
            }
        }
    }

    // hooks for subclass behavior
    protected virtual void onEntityHit(Entity entity) {
        entity.dmg(damage, position);
        active = false; // default: despawn on hit
    }

    protected virtual void onBlockHit() {
        active = false; // default: despawn on block collision
    }

    protected virtual void spawnTrailParticles() {
        // optional override for particle trails
    }

    protected virtual void updateRotation() {
        // optional override for velocity-aligned rotation (arrows)
    }

    // NBT serialization
    protected override void readx(NBTCompound data) {
        if (data.has("age")) {
            age = data.getInt("age");
        }
    }

    public override void writex(NBTCompound data) {
        data.addInt("age", age);
    }
}
