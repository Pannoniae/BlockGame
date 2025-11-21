using System.Numerics;
using BlockGame.GL;
using BlockGame.main;
using BlockGame.net;
using BlockGame.net.packet;
using BlockGame.net.srv;
using BlockGame.render;
using BlockGame.util;
using BlockGame.util.xNBT;
using BlockGame.world.block;
using BlockGame.world.chunk;
using BlockGame.world.item;
using JetBrains.Annotations;
using LiteNetLib;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

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

    public string name;

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

    public double hp = 100;
    public bool dead = false;
    public int dmgTime = 0; // ticks remaining on damage tint

    public int fireTicks = 0; // ticks remaining on fire

    public int iframes;
    public int invulnerability = 30; // the base
    public double lastDmg;

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
    public string tex;

    // network state sync
    public readonly EntityState state = new();

    // capability flags - override in subclasses
    protected virtual bool needsPrevVars => true;
    protected virtual bool needsPhysics => true;
    protected virtual bool needsGravity => true;
    protected virtual bool needsCollision => true;
    protected virtual bool needsFriction => true;
    protected virtual bool needsBlockInteraction => true;
    protected virtual bool needsEntityCollision => false;
    protected virtual bool needsBodyRotation => false;
    protected virtual bool needsFootsteps => false;
    protected virtual bool needsFallDamage => false;
    protected virtual bool needsAnimation => false;
    protected virtual bool needsDamageNumbers => false;

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
        if (!shouldContinueUpdate(dt)) {
            return;
        }

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
        }
        else if (isRiding()) {
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
        }
        else {
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
        updateFire(dt);

        if (iframes > 0) {
            iframes--;
        }

        if (dmgTime > 0) {
            dmgTime--;
        }

        // void
        if (position.Y < -64) {
            dmg(20);
            iframes = 60;
        }

        if (position.Y < -128) {
            die();
        }
    }

    /**
     * Update fire status and apply fire damage
     */
    protected virtual void updateFire(double dt) {
        if (fireTicks > 0) {
            fireTicks--;

            // apply fire damage once per second (60 ticks)
            if (fireTicks % 60 == 0) {
                dmg(1);
                if (hp <= 0) {
                    die();
                }
            }
        }
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
        bool inWater = false;
        bool inFire = false;
        bool inLava = false;

        foreach (var pos in neighbours) {
            var block = world.getBlock(pos);
            var blockInstance = Block.blocks[block]!;

            // handle regular interactions (non-push effects)
            blockInstance.interact(world, pos.X, pos.Y, pos.Z, this);

            // check for fire/water/lava
            if (block == Block.FIRE.id) inFire = true;
            if (block == Block.WATER.id) inWater = true;
            if (block == Block.LAVA.id) inLava = true;

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

        if (inLava) {
            fireTicks = Math.Max(fireTicks, 300);
        }

        if (inFire) {
            fireTicks = Math.Max(fireTicks, 160);
        }

        // guess what, standing in the dick forest makes you choke on it!
        if (inFire || inLava) {
            dmg(3);
        }

        if (inWater && fireTicks > 0) {
            fireTicks = 0;
        }

        if (liquid > 0 && push != Vector3D.Zero) {
            // limit maximum push strength to prevent entity getting stuck
            const double maxPushStrength = 5.6;
            push = Vector3D.Normalize(push) * maxPushStrength;


            velocity += push * dt;
        }
    }

    /** damage without knockback (fall damage, fire, etc) */
    public virtual void dmg(float damage) {
        double actualDmg;

        if (iframes > 0) {
            // if new damage is greater, apply difference & reset iframes
            if (damage > lastDmg) {
                actualDmg = damage - lastDmg;
                hp -= actualDmg;
                lastDmg = damage;
                iframes = invulnerability;
                dmgTime = 30;
            }
            else {
                // ignore weaker hits during iframes
                return;
            }
        }
        else {
            // no iframes, apply full dmg
            actualDmg = damage;
            hp -= damage;
            lastDmg = damage;
            iframes = invulnerability;
            dmgTime = 30;

            if (this is Player && !Net.mode.isDed()) {
                Game.camera.applyImpact(damage * 4);
            }
        }

        // spawn damage number at top of entity with random offset
        if (needsDamageNumbers) {
            var rng = Game.clientRandom;

            // random pos, above the entity
            var h = aabb.y1 - position.Y;
            var a = new Vector3D(rng.NextSingle() * 0.14f,
                h + rng.NextSingle() * 0.17f,
                rng.NextSingle() * 0.14f);

            var np = position + a;
            world.particles.add(new DamageNumber(world, np, actualDmg));
        }

        // check if dead
        if (hp <= 0) {
            die();
        }
    }

    /** damage with knockback from src */
    public virtual void dmg(double damage, Vector3D source) {
        double actualDmg;
        bool kb = false;

        if (iframes > 0) {
            if (damage > lastDmg) {
                actualDmg = damage - lastDmg;
                hp -= actualDmg;
                lastDmg = damage;
                iframes = invulnerability;
                kb = true;
                dmgTime = 30;
            }
            else {
                return;
            }
        }
        else {
            actualDmg = damage;
            hp -= damage;
            lastDmg = damage;
            iframes = invulnerability;
            kb = true;
            dmgTime = 30;

            if (this is Player && !Net.mode.isDed()) {
                Game.camera.applyImpact((float)damage * 4);
            }
        }

        // apply knockback
        if (kb) {
            var dir = Vector3D.Normalize(position - source);
            var kbStrength = 9 + double.Sqrt(damage * 2);

            // don't yeet into the air *too* much
            // only apply upward force if on ground
            const int up = 9;

            if (velocity.Y > 6) {
                velocity.Y = 6;
            }

            var force = new Vector3D(dir.X * kbStrength, up, dir.Z * kbStrength);
            knockback(force);

            if (velocity.Y > 6) {
                velocity.Y = 6;
            }
        }

        // spawn damage number
        if (needsDamageNumbers) {
            var rng = Game.clientRandom;

            // random pos, above the entity
            var h = aabb.y1 - position.Y;
            var a = new Vector3D(rng.NextSingle() * 0.14f,
                h + rng.NextSingle() * 0.17f,
                rng.NextSingle() * 0.14f);
            var np = position + a;
            world.particles.add(new DamageNumber(world, np, actualDmg));
        }

        // check if dead
        if (hp <= 0) {
            die();
        }
    }

    /** heal entity */
    public virtual void heal(double amount) {
        hp += amount;
        if (hp > 100) {
            hp = 100;
        }
    }

    public virtual void sethp(double amount) {
        hp = amount;
        if (hp > 100) {
            hp = 100;
        }
    }

    public virtual void die() {
        dead = true;
        active = false;
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

    public virtual void setSwinging(bool hit) {
        if (hit) {
            swinging = true;
            swingTicks = 0;

            // send swing action to server when starting swing (only for local player)
            if (Net.mode.isMPC() && this == Game.player) {
                if (ClientConnection.instance != null && ClientConnection.instance.connected) {
                    ClientConnection.instance.send(
                        new EntityActionPacket {
                            entityID = id,
                            action = EntityActionPacket.Action.SWING
                        },
                        DeliveryMethod.ReliableOrdered
                    );
                }
            }
        }
        else {
            if (airHitCD == 0) {
                swinging = true;
                swingTicks = 0;
                airHitCD = AIR_HIT_CD;
                if (Net.mode.isMPC() && this == Game.player) {
                    // send swing action to server when starting swing (only for local player)
                    if (ClientConnection.instance != null && ClientConnection.instance.connected) {
                        ClientConnection.instance.send(
                            new EntityActionPacket {
                                entityID = id,
                                action = EntityActionPacket.Action.SWING
                            },
                            DeliveryMethod.ReliableOrdered
                        );
                    }
                }
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
     * Entity pushing - entities push each other apart when overlapping
     */
    protected virtual void handleEntityPushing(double dt) {
        // expand AABB slightly to detect nearby entities
        var expandedAABB = new AABB(
            aabb.x0 - 0.2, aabb.y0 - 0.1, aabb.z0 - 0.2,
            aabb.x1 + 0.2, aabb.y1 + 0.1, aabb.z1 + 0.2
        );

        List<Entity> nearby = [];
        world.getEntitiesInBox(nearby, expandedAABB);

        foreach (var other in nearby) {
            // skip self
            if (other.id == this.id) continue;

            // skip if either is riding/mounted
            if (isRiding() || hasRider() || other.isRiding() || other.hasRider()) {
                continue;
            }

            // check if AABBs actually overlap
            if (!AABB.isCollision(aabb, other.aabb)) {
                continue;
            }

            // calculate push direction (away from each other)
            var dx = position.X - other.position.X;
            var dz = position.Z - other.position.Z;
            var dist = double.Sqrt(dx * dx + dz * dz);

            // avoid divide by zero if entities at exact same pos
            if (dist < 0.01) {
                dx = (Game.clientRandom.NextSingle() - 0.5) * 0.1;
                dz = (Game.clientRandom.NextSingle() - 0.5) * 0.1;
                dist = double.Sqrt(dx * dx + dz * dz);
            }

            // normalise and apply push
            const double pushStrength = 0.15;
            var pushX = (dx / dist) * pushStrength;
            var pushZ = (dz / dist) * pushStrength;

            // push both entities apart


            velocity.X += pushX;
            velocity.Z += pushZ;

            // don't push item entities
            if (other is not ItemEntity) {
                other.velocity.X -= pushX;
                other.velocity.Z -= pushZ;
            }
        }
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
        aspeed = (float)vel.Length() * 0.6f;
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

        // broadcast velocity to all clients when entity gets knocked back
        if (Net.mode.isDed()) {
            GameServer.instance.send(
                position,
                128.0,
                new EntityVelocityPacket {
                    entityID = id,
                    velocity = velocity
                },
                // todo is this right? maybe ReliableUnordered instead?
                DeliveryMethod.ReliableOrdered
            );
        }
    }

    // ============ STATE SYNC ============

    /** sync entity fields to state buffer (server-side, before sending) */
    public virtual void syncState() {
        state.setBool(EntityState.ON_FIRE, fireTicks > 0);
        state.setBool(EntityState.SNEAKING, sneaking);
        state.setInt(EntityState.RIDING, mount?.id ?? -1);
    }

    /** apply state buffer to entity fields (client-side, after receiving) */
    public virtual void applyState() {
        sneaking = state.getBool(EntityState.SNEAKING);
        fireTicks = state.getBool(EntityState.ON_FIRE) ? 300 : 0;

        // riding sync
        int mountID = state.getInt(EntityState.RIDING, -1);
        if (mountID >= 0) {
            // todo mount handling later
            // find mount entity and set entity.mount
            var mount = world.entities.FirstOrDefault(e => e.id == mountID);
            if (mount != null) {
                this.mount = mount;
                mount.rider = this;
            }
        }
        else if (mountID == -1 && this.mount != null) {
            // dismount
            this.mount.rider = null;
            this.mount = null;
        }
    }
}