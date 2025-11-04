using BlockGame.main;
using BlockGame.util;
using BlockGame.world.block;
using Core.util;
using Molten;
using Molten.DoublePrecision;
using Path = Core.util.Path;

namespace BlockGame.world;

public class Mob(World world, string type) : Entity(world, type) {

    private const double DESPAWN_DISTANCE = 128.0;
    private const double LOOK_AT_PLAYER_DISTANCE = 12.0;
    private const double TARGET_REACHED_DISTANCE = 1.5;
    private const double WANDER_MIN_DISTANCE = 8.0;
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

    /*
     * The path that this mob is currently following (can be null)
     */
    public Path? path;
    /*
     * The entity that this mob is targeting (can be null)
     */
    public Entity? target;

    public int dmgTime;
    public int dieTime;
    public int iframes;

    private Vector3D? wanderTarget;
    private double lastFootstepDistance = 0;
    private bool wasInAir = false;
    private bool wantsToWander = false;

    public virtual bool canSpawn => true;
    public virtual bool canDespawn => true;

    // enable mob-specific systems
    protected override bool needsEntityCollision => true;

    protected override bool needsBodyRotation => true;
    protected override bool needsFootsteps => true;
    protected override bool needsFallDamage => true;
    protected override bool needsAnimation => true;

    /**
     * AI behaviour for the mob. Called every tick before physics update.
     * Override to implement custom AI.
     */
    public virtual void AI(double dt) {
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
            target = null;
            path = null;
        }

        // random small chance: change target anyway
        if (Game.random.NextDouble() < 0.04 / Game.tps) {
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
                target = null;
                path = null;
                velocity = Vector3D.Zero;
                return;
            }

            // recompute path periodically or if no path
            if (path == null || path.isFinished()) {
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
                wantsToWander = false;
                wanderTarget = null;
                path = null;
            }

            // generate new wander target
            if (wantsToWander && (wanderTarget == null || (path != null && path.isFinished()))) {
                var angle = Game.random.NextSingle(0, MathF.PI * 2);
                var dist = Game.random.NextSingle((float)WANDER_MIN_DISTANCE, (float)WANDER_MAX_DISTANCE);
                var tx = (int)(position.X + MathF.Cos(angle) * dist);
                var tz = (int)(position.Z + MathF.Sin(angle) * dist);
                var ty = (int)position.Y;

                wanderTarget = new Vector3D(tx, ty, tz);
                path = Pathfinding.find(this, tx, ty, tz);
            }

            // follow a path
            if (wantsToWander && path != null && !path.isFinished()) {
                followPath(dt);
                isMoving = true;
            }
            else {
                // idle: zero out horizontal velocity
                velocity.X = 0;
                velocity.Z = 0;
            }
        }

        // disable temporarily, makes player's head bugged
        if (!isMoving&&false) {
            var distToPlayer = Vector3D.Distance(position, world.player.position);
            if (distToPlayer < LOOK_AT_PLAYER_DISTANCE) {
                lookAt(world.player.position, dt);
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

            var moveSpeed = inLiquid ? LIQUID_MOVE_SPEED * 0.6 : GROUND_MOVE_SPEED * 0.6;
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
        } else {
            current += float.CopySign(maxTurn, diff);
        }

        current = Meth.clampAngle(current);
    }

    protected virtual void onDeath() {
        dead = true;
        // rotate entity 90 degrees to side
        // todo animate this in the model instead!
        rotation.Z = -90f;
        prevRotation.Z = -90f;
    }

    // ============ LIFECYCLE HOOKS ============

    protected override bool shouldContinueUpdate(double dt) {
        // death check + animation
        if (hp <= 0 && !dead) {
            onDeath();
        }

        if (dead) {
            dieTime++;
            return false; // don't update anything else when dead
        }

        // despawn check
        if (canDespawn) {
            var distSq = Vector3D.DistanceSquared(position, world.player.position);

            // instant despawn beyond 128 blocks
            if (distSq > DESPAWN_DISTANCE * DESPAWN_DISTANCE) {
                active = false;
                return false;
            }

            // random despawn between 32-128 blocks (prevents never despawning at low render dist lol)
            const double RANDOM_DESPAWN_MIN = 32.0 * 32.0;
            if (distSq > RANDOM_DESPAWN_MIN) {
                if (Game.random.Next(20000) == 0) {
                    active = false;
                    return false;
                }
            }
        }

        return true;
    }

    protected override void updateTimers(double dt) {
        base.updateTimers(dt);

        if (iframes > 0) {
            iframes--;
        }

        if (dmgTime > 0) {
            dmgTime--;
        }
    }

    protected override void prePhysics(double dt) {
        AI(dt);
    }

    protected override void checkFallDamage(double dt) {
        if (onGround && wasInAir && !flyMode && !inLiquid) {
            var fallSpeed = -prevVelocity.Y;
            if (fallSpeed > SAFE_FALL_SPEED) {
                var dmg = (float)((fallSpeed - SAFE_FALL_SPEED) * FALL_DAMAGE_MULTIPLIER);
                takeDamage(dmg);
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
                if (blockBelow?.mat != null) {
                    Game.snd.playFootstep(blockBelow.mat.smat);
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
        } else {
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

    public void takeDamage(float damage) {
        if (iframes > 0) return;

        hp -= damage;
        dmgTime = 30;
        iframes = 10;
    }
}