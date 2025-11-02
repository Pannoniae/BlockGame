using System.Numerics;
using BlockGame.GL;
using BlockGame.util;
using BlockGame.util.xNBT;
using BlockGame.world.block;
using BlockGame.world.chunk;
using BlockGame.world.item;
using JetBrains.Annotations;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world;

public partial class Entity(World world, string type) : Persistent {
    public const int MAX_SWING_TICKS = 20;
    public const int AIR_HIT_CD = 20;

    // physics constants
    public const double GRAVITY = 30;
    public const double MAX_ACCEL = 50;
    public const double JUMP_SPEED = 10;
    public const double LIQUID_SWIM_UP_SPEED = 0.45;
    public const double LIQUID_SURFACE_BOOST = 0.2;
    public const double MAX_VSPEED = 200;
    public const double FRICTION = 0.80;
    public const double AIR_FRICTION = 0.80;
    public const double FLY_FRICTION = 0.85;
    public const double VERTICAL_FRICTION = 0.99;
    public const double LIQUID_FRICTION = 0.92;
    public const double EPSILON_GROUND_CHECK = 0.01;
    public const double GROUND_MOVE_SPEED = 0.75;
    public const double AIR_MOVE_SPEED = 0.5;
    public const double AIR_FLY_SPEED = 1.75;
    public const double LIQUID_MOVE_SPEED = 0.2;
    public const double SNEAK_FACTOR = 0.28;
    public const double STEP_HEIGHT = 0.51;

    public string type = type;
    public int id = World.ec++;

    public World world = world;

    /** does this entity block block placement? */
    public virtual bool blocksPlacement => true;

    /** is entity deleted?
     * Status update: we shouldn't use this!! it's stupid and HAS TO BE CHECKED EVERYWHERE
     */
    public bool active = true;

    /** is player walking on (colling with) ground */
    public bool onGround;

    /** is the player in the process of jumping */
    public bool jumping;

    public bool sneaking;

    /** entity positions are at feet */
    public Vector3D prevPosition;

    public Vector3D position;
    public Vector3D prevVelocity;
    public Vector3D velocity;
    public Vector3D accel;

    /** X Y Z */
    public Vector3 rotation;

    public Vector3 prevRotation;

    public Vector3 bodyRotation;
    public Vector3 prevBodyRotation;

    // slightly above so it doesn't think it's under the player
    public Vector3D feetPosition;


    /// <summary>
    /// Which direction the entity faces (horizontally only)
    /// </summary>
    public virtual Vector3D hfacing {
        get {
            var cameraDirection = Vector3.Zero;
            cameraDirection.X = MathF.Sin(Meth.deg2rad(rotation.Y));
            cameraDirection.Y = 0;
            cameraDirection.Z = MathF.Cos(Meth.deg2rad(rotation.Y));
            var v = Vector3.Normalize(cameraDirection);
            return new Vector3D(v.X, v.Y, v.Z);
        }
    }

    public AABB aabb;

    public ushort blockAtFeet;
    public bool inLiquid;
    public bool wasInLiquid;
    public bool onLadder;

    // TODO implement some MovementState system so movement constants don't have to be duplicated...
    // it would store a set of values for acceleration, drag, friction, maxspeed, etc...

    public bool collx;
    public bool collz;

    /// <summary>
    /// This number is lying to you.
    /// </summary>
    public double totalTraveled;

    public double prevTotalTraveled;

    public float hp = 100;
    public bool dead = false;

    public bool flyMode;
    public bool noClip;

    protected List<AABB> collisions = [];

    public int airHitCD;

    public int swingTicks;
    public bool swinging;

    /// 0 to 1
    public double prevSwingProgress;

    public double swingProgress;

    // animation state
    public float apos;
    public float papos;

    public float aspeed;
    public float paspeed;

    private readonly List<Vector3I> neighbours = new(26);

    /** Is it in a valid chunk?
     * STATUS UPDATE: We have a genius idea, we just store it in the bottommost/topmost chunk if out of bounds
     * This way the entity will almost never be out of world except if the chunk is unloaded or teleported far away or something
     */
    public bool inWorld;

    /** We kept losing track of which chunk the entity was in, so fuck it let's just store it */
    public SubChunkCoord subChunkCoord;

    protected static readonly List<AABB> AABBList = [];

    // riding system
    public Entity? mount; // what this entity is riding
    public Entity? rider; // who is riding this entity
    public BTexture2D tex;

    // capability flags - override in subclasses
    protected virtual bool needsPrevVars => true;
    protected virtual bool needsPhysics => true;
    protected virtual bool needsGravity => true;
    protected virtual bool needsCollision => true;
    protected virtual bool needsFriction => true;
    protected virtual bool needsBlockInteraction => true;
    protected virtual bool needsEntityCollision => false; // TODO
    protected virtual bool needsBodyRotation => false;
    protected virtual bool needsFootsteps => false;
    protected virtual bool needsFallDamage => false;
    protected virtual bool needsAnimation => false;

    public bool isRiding() => mount != null;
    public bool hasRider() => rider != null;

    public ChunkCoord getChunk(Vector3D pos) {
        var blockPos = pos.toBlockPos();
        return World.getChunkPos(new Vector2I(blockPos.X, blockPos.Z));
    }

    public ChunkCoord getChunk() {
        var blockPos = position.toBlockPos();
        return World.getChunkPos(new Vector2I(blockPos.X, blockPos.Z));
    }

    [Pure]
    public virtual AABB calcAABB(Vector3D pos) {
        return new AABB(
            pos.X - 0.3, pos.Y, pos.Z - 0.3,
            pos.X + 0.3, pos.Y + 1.8, pos.Z + 0.3
        );
    }

    public virtual void teleport(Vector3D pos) {
        position = pos;
        prevPosition = pos;
        velocity = Vector3D.Zero;
        prevVelocity = Vector3D.Zero;
    }

    // ============ LIFECYCLE:tm: ============

    /**
     * Main update loop - orchestrates all entity behaviour.
     * Subclasses should override the hook methods, not this!!!
     */
    public virtual void update(double dt) {
        // 1. early exit checks (death animation, despawn, etc)
        if (!shouldContinueUpdate(dt)) return;

        // 2. store prev state for interpolation
        if (needsPrevVars) {
            savePrevVars();
        }

        // 3. decrement cooldowns and timers
        updateTimers(dt);

        // 4. AI/input sets velocities/forces
        prePhysics(dt);

        // 5. physics pipeline (unless riding)
        if (needsPhysics && !isRiding()) {
            updatePhysics(dt);
        } else if (isRiding()) {
            syncToMount();
        }

        // 6. post-physics updates (body rotation, animation, etc)
        postPhysics(dt);
    }

    /**
     * Full physics pipeline
     */
    protected virtual void updatePhysics(double dt) {
        collx = false;
        collz = false;

        // entity collision
        if (needsEntityCollision) {
            handleEntityPushing(dt);
        }

        // block interactions (liquid push, etc)
        if (needsBlockInteraction) {
            interactBlock(dt);
        }

        // gravity
        if (needsGravity) {
            updateGravity(dt);
        }

        // apply acceleration
        velocity += accel * dt;
        clamp(dt);

        // update block at feet
        blockAtFeet = world.getBlock(feetPosition.toBlockPos());

        // collision + movement
        if (needsCollision) {
            collide(dt);
        } else {
            // no collision, just move
            position += velocity * dt;
        }

        // fall damage check
        if (needsFallDamage) {
            checkFallDamage(dt);
        }

        // friction
        if (needsFriction) {
            applyFriction();
            clamp(dt);
        }
    }

    // ============ LIFECYCLE HOOKS ============

    /**
     * Early exit checks. Return false to skip rest of update.
     * Override to add death animations, despawn logic, etc.
     */
    protected virtual bool shouldContinueUpdate(double dt) {
        return true;
    }

    /**
     * Store previous state for interpolation.
     * Override to add more vars (camera bob, swing progress, etc).
     */
    protected virtual void savePrevVars() {
        prevPosition = position;
        prevVelocity = velocity;
        prevRotation = rotation;
        prevBodyRotation = bodyRotation;
        prevTotalTraveled = totalTraveled;
        wasInLiquid = inLiquid;
        prevSwingProgress = swingProgress;
        papos = apos;
        paspeed = aspeed;
    }

    /**
     * Decrement cooldowns and timers.
     * Override to add more timers (iframes, dmgTime, etc).
     */
    protected virtual void updateTimers(double dt) {
        updateSwing();
    }

    /**
     * AI/input sets velocities and forces before physics.
     * Override for mob AI or player input.
     */
    protected virtual void prePhysics(double dt) {
    }

    /**
     * Post-physics updates (animation, body rotation, effects).
     * Override for entity-specific post-physics behaviour.
     */
    protected virtual void postPhysics(double dt) {
        // update feet position
        feetPosition = new Vector3D(position.X, position.Y + 0.05, position.Z);

        // update AABB
        aabb = calcAABB(position);

        // body rotation
        if (needsBodyRotation) {
            updateBodyRotation(dt);
        }

        // animation
        if (needsAnimation) {
            updateAnimation(dt);
        }

        // footsteps
        if (needsFootsteps) {
            updateFootsteps(dt);
        }
    }

    // ============ PHYSICS SYSTEMS ============

    /**
     * Handle interactions with the block the entity is standing in.
     */
    public virtual void interactBlock(double dt) {
        // get blocks in aabb
        var min = aabb.min.toBlockPos();
        var max = aabb.max.toBlockPos();
        World.getBlocksInBox(neighbours, min, max);
        
        onLadder = false;

        // check if any of them are liquid and accumulate push forces
        inLiquid = false;
        Vector3D push = Vector3D.Zero;
        int liquid = 0;

        foreach (var pos in neighbours) {
            var block = world.getBlock(pos);
            var blockInstance = Block.blocks[block]!;

            // handle regular interactions (non-push effects)
            blockInstance.interact(world, pos.X, pos.Y, pos.Z, this);

            // accumulate push forces for liquids
            if (Block.liquid[block]) {
                inLiquid = true;
                var pushForce = blockInstance.push(world, pos.X, pos.Y, pos.Z, this);
                if (pushForce != Vector3D.Zero) {
                    push += pushForce;
                    liquid++;
                }
            }
        }

        if (liquid > 0 && push != Vector3D.Zero) {
            // limit maximum push strength to prevent entity getting stuck
            const double maxPushStrength = 5.6;
            push = Vector3D.Normalize(push) * maxPushStrength;


            velocity += push * dt;
        }
    }

    public virtual void onChunkChanged() {
    }

    public virtual (Item item, byte metadata, int count) getDrop() {
        throw new NotImplementedException();
    }

    public double getSwingProgress(double dt) {
        var value = double.Lerp(prevSwingProgress, swingProgress, dt);
        // if it just finished swinging, lerp to 1
        if (prevSwingProgress != 0 && swingProgress == 0) {
            value = double.Lerp(prevSwingProgress, 1, dt);
        }

        return value;
    }

    public void updateSwing() {
        swingProgress = (double)swingTicks / MAX_SWING_TICKS;
        if (swinging) {
            swingTicks++;
            if (swingTicks >= MAX_SWING_TICKS) {
                swinging = false;
                swingTicks = 0;
            }
        }
        else {
            swingTicks = 0;
        }

        if (airHitCD > 0) {
            airHitCD--;
        }
    }

    public void setSwinging(bool hit) {
        if (hit) {
            swinging = true;
            swingTicks = 0;
        }
        else {
            if (airHitCD == 0) {
                swinging = true;
                swingTicks = 0;
                airHitCD = AIR_HIT_CD;
            }
        }
    }

    public Vector3 facing() {
        var cameraDirection = Vector3.Zero;
        cameraDirection.X = MathF.Sin(Meth.deg2rad(rotation.Y));
        cameraDirection.Y = 0;
        cameraDirection.Z = MathF.Cos(Meth.deg2rad(rotation.Y));

        return Vector3.Normalize(cameraDirection);
    }

    public Vector3 camFacing() {
        var cameraDirection = Vector3.Zero;
        cameraDirection.X = MathF.Cos(Meth.deg2rad(rotation.X)) * MathF.Sin(Meth.deg2rad(rotation.Y));
        cameraDirection.Y = MathF.Sin(Meth.deg2rad(rotation.X));
        cameraDirection.Z = MathF.Cos(Meth.deg2rad(rotation.X)) * MathF.Cos(Meth.deg2rad(rotation.Y));

        return Vector3.Normalize(cameraDirection);
    }

    // ============ STUBS ============

    /**
     * Entity pushing
     */
    protected virtual void handleEntityPushing(double dt) {
        // TODO implement entity-entity collision
    }

    /**
     * Sync position to mount when riding
     */
    protected virtual void syncToMount() {
        if (mount != null) {
            position = mount.position;
            velocity = mount.velocity;
        }
    }

    /**
     * Check for fall damage based on fall velocity
     */
    protected virtual void checkFallDamage(double dt) {
        // no-op by default, mobs/player can override this?
    }

    /**
     * Update body rotation to smoothly follow head/movement
     * Override to customize body rotation behavior
     */
    protected virtual void updateBodyRotation(double dt) {
        // default: body follows head exactly
        bodyRotation = rotation;
    }

    /**
     * Update animation state (apos, aspeed) based on movement
     * Override to customize animation
     */
    protected virtual void updateAnimation(double dt) {
        var vel = velocity.withoutY();
        aspeed = (float)vel.Length() * 0.3f;
        aspeed = Meth.clamp(aspeed, 0f, 1f);

        if (aspeed > 0f) {
            apos += aspeed * (float)dt;
        }
    }

    /**
     * Play footstep sounds when moving
     * Override to customise
     */
    protected virtual void updateFootsteps(double dt) {

    }

    /**
     * Apply knockback to the entity
     */
    public virtual void knockback(Vector3D force) {
        velocity += force;
    }
}