using System.Numerics;
using BlockGame.main;
using BlockGame.util;
using BlockGame.util.path;
using BlockGame.world.block;
using Molten;
using Molten.DoublePrecision;
using Path = BlockGame.util.path.Path;

namespace BlockGame.world.entity;

public class Mob(World world, string type) : Entity(world, type) {
    private const double DESPAWN_DISTANCE = 128.0;
    private const double LOOK_AT_PLAYER_DISTANCE = 12.0;
    private const double TARGET_REACHED_DISTANCE = 1.5;
    private const double WANDER_MIN_DISTANCE = 12.0;
    private const double WANDER_MAX_DISTANCE = 16.0;
    private const float JUMP_CHANCE = 0.015f; // per tick
    private const double SAFE_FALL_SPEED = 13.0; // velocity threshold for damage
    private const double FALL_DAMAGE_MULTIPLIER = 2.0; // damage per unit velocity over threshold
    private const double FOOTSTEP_DISTANCE = 3.0; // distance between footstep sounds


    private const double IDLE_VELOCITY_THRESHOLD = 0.05;
    private const float BODY_ROTATION_SNAP = 45f; // degrees
    private const float ROTATION_SPEED = 1.8f; // deg/s when moving
    private const float HEAD_TURN_SPEED = 180f; // max deg/s for head rotation

    // AI
    private const float CHANCE_START_WANDERING = 0.008f; // when idle
    private const float CHANCE_STOP_WANDERING = 0.005f; // when moving

    // mp interpolation
    public Vector3D targetPos;
    public Vector3 targetRot;
    public int interpolationTicks;

    /*
     * The path that this mob is currently following (can be null)
     */
    public Path? path;

    /*
     * The entity that this mob is targeting (can be null)
     */
    public Entity? target;
    public int dieTime;


    private Vector3D? wanderTarget;
    private double lastFootstepDistance = 0;
    private bool wasInAir = false;
    private bool wantsToWander = false;
    protected int spawnTicks = 0; // ticks since spawn, used to delay burn check
    private int fireDamageTicks = 0; // counter for fire damage timing

    public virtual bool canSpawn => true;
    public virtual bool canDespawn => true;

    // enable mob-specific systems
    protected override bool needsEntityCollision => true;

    protected override bool needsBodyRotation => true;
    protected override bool needsFootsteps => true;
    protected override bool needsFallDamage => true;
    protected override bool needsAnimation => true;
    protected override bool needsDamageNumbers => true;

    public virtual bool hostile => false;
    public virtual bool burnInSunlight => false;
    public const int sunlightThreshold = 13;
    public virtual double eyeHeight => 1.6;

    public virtual double reach => 2;
    public virtual double speedMul => 1.0;

    /**
     * Find nearest player (excluding creative mode players) within radius
     * Returns squared distance for performance
     */
    protected Player? findNearestPlayer(double radius, out double nearestDistSq) {
        Player? nearest = null;
        nearestDistSq = radius * radius;

        foreach (var entity in world.players) {
            if (entity.gameMode == null) {
                // todo this is a horrible hack, find out why it's not initialised fully yet
                continue;
            }


            if (entity.gameMode.gameplay) {
                var distSq = Vector3D.DistanceSquared(position, entity.position);
                if (distSq < nearestDistSq) {
                    nearest = entity;
                    nearestDistSq = distSq;
                }
            }
        }

        return nearest;
    }

    /**
     * Check if there's line of sight between this mob and target entity
     * Returns false if there are solid blocks in the way
     */
    protected bool hasLineOfSight(Entity target) {
        var start = position + new Vector3D(0, eyeHeight, 0);
        var end = target.position + new Vector3D(0, target is Mob m ? m.eyeHeight : 1.6, 0);
        var dir = Vector3D.Normalize(end - start);
        var dist = Vector3D.Distance(start, end);

        // step along the ray and check for solid blocks
        const double step = 0.25;
        var steps = (int)(dist / step);

        for (int i = 1; i < steps; i++) {
            var checkPos = start + dir * (i * step);
            var bx = (int)double.Floor(checkPos.X);
            var by = (int)double.Floor(checkPos.Y);
            var bz = (int)double.Floor(checkPos.Z);

            if (world.inWorld(bx, by, bz)) {
                var block = world.getBlock(bx, by, bz);
                if (Block.collision[block]) {
                    return false;
                }
            }
        }

        return true;
    }

    /**
     * Check if mob should burn in sunlight and apply fire damage
     */
    protected void updateSunlightBurn() {
        // skip burn check for first 10 ticks after spawn to avoid false positives
        if (spawnTicks < 10) {
            return;
        }

        // check at head level - for ground mobs, use feet+1 for consistency
        // for flying mobs with eyeHeight=0, use their actual position
        var checkY = eyeHeight > 0
            ? (int)position.Y + 1 // ground-based mobs: check 1 block above feet
            : (int)position.Y; // flying mobs: check at their actual position
        var skylight = world.getSkyLight((int)position.X, checkY, (int)position.Z);

        // only burn during daytime (sun above horizon) with high skylight
        bool isDaytime = world.getSunElevation(world.worldTick) > 0;

        if (isDaytime && skylight >= sunlightThreshold && !inLiquid) {
            fireTicks = Math.Max(fireTicks, 160);
        }
        else {
            // not in sunlight - clear fire from sunlight
            fireTicks = 0;
        }
    }

    /**
     * AI behaviour for the mob. Called every tick before physics update.
     * Override to implement custom AI.
     */
    public virtual void AI(double dt) {
        spawnTicks++;

        if (burnInSunlight) {
            updateSunlightBurn();
        }

        // randomly jump on ground, continuously jump in water to stay afloat
        if (onGround && Game.random.NextSingle() < JUMP_CHANCE) {
            jumping = true;
        }

        // keep jumping in water to stay on surface
        if (inLiquid) {
            jumping = true;
        }

        // if target died, clear target
        if (target != null && (!target.active || target.dead)) {
            if (path != null) {
                Pathfinding.ret(path);
            }

            target = null;
            path = null;
        }

        // random small chance: change target anyway
        if (Game.random.NextDouble() < 0.04 / Game.tps) {
            if (path != null) {
                Pathfinding.ret(path);
            }

            target = null;
            path = null;
            wanderTarget = null;
        }

        bool isMoving = false;

        // if has target, go towards target
        if (target != null) {
            var distToTarget = Vector3D.Distance(position, target.position);

            // reached target?
            if (distToTarget < TARGET_REACHED_DISTANCE) {
                if (path != null) {
                    Pathfinding.ret(path);
                }

                target = null;
                path = null;
                return;
            }

            // compute path immediately if none, recompute ~1s after finishing
            if (path == null) {
                path = Pathfinding.find(this, target);
            }
            else if (path.isFinished() && Game.random.NextDouble() < 1.0 / Game.tps) {
                Pathfinding.ret(path);
                path = Pathfinding.find(this, target);
            }

            followPath(dt);
            isMoving = true;
        }
        // random wandering
        else {
            // random chance to decide to wander
            if (!wantsToWander && Game.random.NextSingle() < CHANCE_START_WANDERING) {
                wantsToWander = true;
                wanderTarget = null;
            }

            // random chance to stop wandering
            if (wantsToWander && Game.random.NextSingle() < CHANCE_STOP_WANDERING) {
                if (path != null) {
                    Pathfinding.ret(path);
                }

                wantsToWander = false;
                wanderTarget = null;
                path = null;
            }

            // generate new wander target
            if (wantsToWander && (wanderTarget == null || (path != null && path.isFinished()))) {
                if (path != null) {
                    Pathfinding.ret(path);
                }

                if (path != null && path.isFinished()) {
                    // stop wandering if reached target
                    wantsToWander = false;
                    wanderTarget = null;
                    path = null;
                    return;
                }

                var angle = Game.random.NextSingle(0, MathF.PI * 2);
                var dist = Game.random.NextSingle((float)WANDER_MIN_DISTANCE, (float)WANDER_MAX_DISTANCE);
                var tx = (int)(position.X + MathF.Cos(angle) * dist);
                var tz = (int)(position.Z + MathF.Sin(angle) * dist);
                var ty = (int)position.Y;

                wanderTarget = new Vector3D(tx, ty, tz);
                path = Pathfinding.find(this, tx, ty, tz, avoidWater: hostile);
            }

            // follow a path
            if (wantsToWander && path != null && !path.isFinished()) {
                followPath(dt);
                isMoving = true;
            }
        }

        // disable temporarily, makes player's head bugged
        // todo this was laggy as fuck in multiplayer because it iterates over all entities. optimise??
        if (false && !isMoving) {
            var player = findNearestPlayer(LOOK_AT_PLAYER_DISTANCE, out var distToPlayerSq);
            if (distToPlayerSq < LOOK_AT_PLAYER_DISTANCE * LOOK_AT_PLAYER_DISTANCE) {
                lookAt(player.position, dt);
            }
        }
    }

    private void followPath(double dt) {
        if (path == null || path.isFinished()) return;

        var currentTarget = path.getCurrentTarget();
        if (currentTarget == null) return;

        var target = currentTarget.Value;
        var dist = Vector3D.Distance(position.withoutY(), target.withoutY());

        // reached current waypoint?
        if (dist < 0.5) {
            path.advance();
            return;
        }

        var dir = target - position;
        dir.Y = 0;

        if (dir.Length() > 0.01) {
            dir = Vector3D.Normalize(dir);

            var moveSpeed = (inLiquid ? LIQUID_MOVE_SPEED * 0.6 : GROUND_MOVE_SPEED * 0.6) * speedMul;
            velocity.X += dir.X * moveSpeed;
            velocity.Z += dir.Z * moveSpeed;

            // rotate head towards movement direction
            var targetYaw = Meth.rad2deg((float)Math.Atan2(dir.X, dir.Z));
            smoothRotateTowards(ref rotation.Y, targetYaw, dt);
            smoothRotateTowards(ref rotation.X, 0, dt);
        }
    }

    private void lookAt(Vector3D pos, double dt) {
        var dir = pos - position;
        if (dir.Length() < 0.01) return;

        var yaw = Meth.rad2deg((float)Math.Atan2(dir.X, dir.Z));
        var pitch = Meth.rad2deg((float)Math.Atan2(-dir.Y, Math.Sqrt(dir.X * dir.X + dir.Z * dir.Z)));

        smoothRotateTowards(ref rotation.Y, yaw, dt);
        smoothRotateTowards(ref rotation.X, pitch, dt);
    }

    private static void smoothRotateTowards(ref float current, float target, double dt) {
        var diff = Meth.angleDiff(current, target);
        var maxTurn = HEAD_TURN_SPEED * (float)dt;

        if (Math.Abs(diff) <= maxTurn) {
            current = target;
        }
        else {
            current += float.CopySign(maxTurn, diff);
        }

        current = Meth.clampAngle(current);
    }

    // ============ LIFECYCLE HOOKS ============

    protected override bool shouldContinueUpdate(double dt) {
        // death check + animation
        if (hp <= 0 && !dead) {
            die();
        }

        if (dead) {
            prevBodyRotation = bodyRotation;

            dieTime++;

            // animate death fall (fall to side over a sec)
            const int deathAnimDuration = 60;
            float t = Math.Min(dieTime / (float)deathAnimDuration, 1f);
            // ease out for smoother animation
            t = 1f - (1f - t) * (1f - t);
            bodyRotation.Z = -90f * t;

            // after 100 ticks of death, despawn
            if (dieTime > 100) {
                active = false;
            }

            return false; // don't update anything else when dead
        }

        // despawn check
        if (canDespawn) {

            // get closest player
            double distSq;
            var player = findNearestPlayer(DESPAWN_DISTANCE, out distSq);

            // instant despawn beyond 128 blocks
            if (distSq > DESPAWN_DISTANCE * DESPAWN_DISTANCE) {
                active = false;
                return false;
            }

            // random despawn between 32-128 blocks (prevents never despawning at low render dist lol)
            const double RANDOM_DESPAWN_MIN = 32.0 * 32.0;
            if (distSq > RANDOM_DESPAWN_MIN) {
                if (Game.random.Next(8000) == 0) {
                    active = false;
                    return false;
                }
            }
        }

        return true;
    }

    protected override void updateTimers(double dt) {
        base.updateTimers(dt);
    }

    public override void update(double dt) {
        // multiplayer client: skip AI and physics for NON-PLAYER mobs, just interpolate
        // NOTE: Player extends Mob, so we need to exclude Player subclasses!
        if (Net.mode.isMPC() && this is not Player) {
            // set prev
            savePrevVars();

            // interpolate towards target position/rotation
            if (interpolationTicks > 0) {
                var t = 1.0 / interpolationTicks;
                position = Vector3D.Lerp(position, targetPos, t);
                rotation = Vector3.Lerp(rotation, targetRot, (float)t);
                interpolationTicks--;
            }

            // derive velocity from movement for animation
            if (dt > 0) {
                velocity = (position - prevPosition) / dt;
            }

            // update body rotation, animation, timers
            updateBodyRotation(dt);
            updateAnimation(dt);
            updateTimers(dt);

            // update AABB
            aabb = calcAABB(position);
            return;
        }

        // server/singleplayer OR player: run normal update with AI and physics
        base.update(dt);
    }

    protected override void prePhysics(double dt) {
        AI(dt);
    }

    /** called when receiving position update from server (client-side) */
    public virtual void mpInterpolate(Vector3D pos, Vector3 rot) {
        targetPos = pos;
        targetRot = rot;
        interpolationTicks = 4; // interpolate over 4 ticks (~67ms)
    }

    protected override void checkFallDamage(double dt) {
        if (onGround && wasInAir && !flyMode && !inLiquid) {
            var fallSpeed = -prevVelocity.Y;
            if (fallSpeed > SAFE_FALL_SPEED) {
                var dmg = (float)((fallSpeed - SAFE_FALL_SPEED) * FALL_DAMAGE_MULTIPLIER);
                this.dmg(dmg);
            }
        }

        wasInAir = !onGround && !flyMode;
    }

    protected override void updateFootsteps(double dt) {
        if (onGround && Math.Abs(velocity.withoutY().Length()) > 0.05 && !inLiquid) {
            if (totalTraveled - lastFootstepDistance > FOOTSTEP_DISTANCE) {
                // get block below entity
                var pos = position.toBlockPos() + new Vector3I(0, -1, 0);
                var blockBelow = Block.get(world.getBlock(pos));
                if (!Net.mode.isDed() && blockBelow?.mat != null) {
                    Game.snd.playFootstep(blockBelow.mat.smat, position);
                }

                lastFootstepDistance = totalTraveled;
            }
        }
    }

    protected override void updateBodyRotation(double dt) {
        // use velocity as movement input (mobs don't have strafeVector :()
        var vel = velocity.withoutY();
        var velLength = vel.Length();

        bool moving = velLength > IDLE_VELOCITY_THRESHOLD;

        float targetYaw;
        float rotSpeed;

        if (moving) {
            targetYaw = rotation.Y;
            rotSpeed = ROTATION_SPEED * 2;
        }
        else {
            // idle - keep current body rotation
            targetYaw = bodyRotation.Y;
            rotSpeed = ROTATION_SPEED * 2;
        }

        // rotate towards target yaw
        bodyRotation.Y = Meth.lerpAngle(bodyRotation.Y, targetYaw, rotSpeed * (float)dt);

        // rotate if outside deadzone
        float angleDiff = Meth.angleDiff(bodyRotation.Y, rotation.Y);

        // hardcap at 70 degrees
        if (angleDiff is > 70 or < -70) {
            bodyRotation.Y = rotation.Y - float.CopySign(70, angleDiff);
            angleDiff = float.CopySign(70, angleDiff); // recalculate after snap!
        }

        // pull body towards head if difference exceeds threshold
        var a = Math.Abs(angleDiff);
        if (a > BODY_ROTATION_SNAP) {
            bodyRotation.Y = Meth.lerpAngle(bodyRotation.Y, rotation.Y, rotSpeed * 0.6f * (float)dt * (a / BODY_ROTATION_SNAP));
        }

        bodyRotation.X = 0;
        bodyRotation.Z = 0;

        // clamp angles
        bodyRotation = Meth.clampAngle(bodyRotation);
        rotation = Meth.clampAngle(rotation);
    }

    protected override void postPhysics(double dt) {
        base.postPhysics(dt);

        // update totalTraveled
        totalTraveled += onGround ? (position.withoutY() - prevPosition.withoutY()).Length() * 2f : 0;
    }

    public override void dmg(float damage) {
        base.dmg(damage);
    }

    protected override void updateFire(double dt) {
        if (fireTicks > 0) {
            fireTicks--;
            fireDamageTicks++;

            // non-hostile mobs take normal fire damage
            if (fireDamageTicks >= 60) {
                dmg(4);
                fireDamageTicks = 0;
                if (hp <= 0) {
                    die();
                }
            }
        }
        else {
            fireDamageTicks = 0;
        }
    }

    public override void die() {
        base.die();

        // spawn drops
        dropList.Clear();
        getDrop(dropList);

        foreach (var drop in dropList) {
            if (drop == null || drop.quantity <= 0) {
                continue;
            }

            var itemEntity = new ItemEntity(world);
            itemEntity.stack = drop;
            itemEntity.position = new Vector3D(position.X, position.Y + 0.5, position.Z);

            // randomise pos
            itemEntity.position.X += (Game.random.NextSingle() - 0.5) * 0.5;
            itemEntity.position.Z += (Game.random.NextSingle() - 0.5) * 0.5;

            // add some random velocity
            itemEntity.velocity = new Vector3D(
                (Game.random.NextSingle() - 0.5) * 0.5,
                Game.random.NextSingle() * 0.3 + 0.2,
                (Game.random.NextSingle() - 0.5) * 0.5
            );

            world.addEntity(itemEntity);
        }
    }
}