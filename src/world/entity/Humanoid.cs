using System.Numerics;
using BlockGame.util;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

/**
 * It's like a Player, but not really.
 * Used for other clients in multiplayer.
 *
 * Uses buffered interpolation: render 4 ticks behind, interpolate between buffered snapshots.
 * Smooth as fuck even with variable packet timing IHope
 *
 * ALSO TODO fucking merge this system with the Mob interpolation jfc, it's duplicated right now because I made this first lol
 *
 * Also arms glitch out when sneaking (animation too fast, looks like it's normal animation speed but with smaller maxpos? Something is fucked, investigate)
 */
public class Humanoid : Player {
    // buffered interp
    private struct PositionSnapshot {
        public int tick;
        public Vector3D position;
        public Vector3 rotation;
    }

    private readonly Queue<PositionSnapshot> positionBuffer = new();
    private int currentTick = 0;
    private const int RENDER_DELAY = 4; // render 4 ticks behind for hopefully less jitter

    public Humanoid(World world, int x, int y, int z) : base(world, x, y, z) {
        targetPos = position;
        targetRot = rotation;
    }

    public override void update(double dt) {
        savePrevVars();
        updateTimers(dt);

        currentTick++;

        // wait until buffer has enough data
        int renderTick = positionBuffer.Count >= RENDER_DELAY
            ? currentTick - RENDER_DELAY
            : currentTick;

        // clean old snapshots (keep last 20 ticks worth, at least 2)
        while (positionBuffer.Count > 2 && positionBuffer.Peek().tick < renderTick - 20) {
            positionBuffer.Dequeue();
        }

        // find two snapshots to interpolate between
        PositionSnapshot? before = null;
        PositionSnapshot? after = null;

        foreach (var snapshot in positionBuffer) {
            if (snapshot.tick <= renderTick) {
                before = snapshot;
            } else {
                after = snapshot;
                break;
            }
        }

        // interpolate
        if (before.HasValue && after.HasValue && before.Value.tick != after.Value.tick) {
            // interpolate between two snapshots
            double t = (renderTick - before.Value.tick) / (double)(after.Value.tick - before.Value.tick);
            var newPos = Vector3D.Lerp(before.Value.position, after.Value.position, t);
            var newRot = new Vector3(
                Meth.lerpAngle(before.Value.rotation.X, after.Value.rotation.X, (float)t),
                Meth.lerpAngle(before.Value.rotation.Y, after.Value.rotation.Y, (float)t),
                Meth.lerpAngle(before.Value.rotation.Z, after.Value.rotation.Z, (float)t)
            );

            // vel updates come separately
            //velocity = (newPos - position) / dt;
            position = newPos;
            rotation = newRot;
        } else if (before.HasValue) {
            // only have past data, use latest (freeze)
            velocity = Vector3D.Zero;
            position = before.Value.position;
            rotation = before.Value.rotation;
        } else if (after.HasValue) {
            // ahead of buffer, snap to earliest
            velocity = Vector3D.Zero;
            position = after.Value.position;
            rotation = after.Value.rotation;
        }
        // if no snapshots at all, keep current position/rotation (shouldn't happen)

        updateBodyRotation(dt);
        updateAnimation(dt);
        aabb = calcAABB(position);
    }

    protected override void updateBodyRotation(double dt) {
        // use velocity for body rotation (like Mob does), not strafeVector
        var vel = velocity.withoutY();
        var velLength = vel.Length();

        const double IDLE_VELOCITY_THRESHOLD = 0.05;
        const float BODY_ROTATION_SNAP = 45f;
        const float ROTATION_SPEED = 1.8f;

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
            angleDiff = float.CopySign(70, angleDiff);
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

    public override void mpInterpolate(Vector3D pos, Vector3 rot) {
        targetPos = pos;
        targetRot = rot;

        // add to buffer
        positionBuffer.Enqueue(new PositionSnapshot {
            tick = currentTick,
            position = targetPos,
            rotation = targetRot
        });
    }

    public void mpInterpolateVelocity(Vector3D vel) {
        velocity = vel;
    }

    /** when receiving an item equip packet, equip the item in the correct slot */
    public void equipItem(ushort slot, ItemStack stack) {
        if (slot < inventory.slots.Length) {
            inventory.slots[slot] = stack;
        }
    }
}
