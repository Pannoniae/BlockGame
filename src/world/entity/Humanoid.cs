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


    private readonly Queue<(Vector3D pos, Vector3 rot)> snapshotQueue = new();

    private const int CONSUME_INTERVAL = 3;
    private int ticksUntilConsume = CONSUME_INTERVAL;

    private Vector3D fromPos, toPos;
    private Vector3 fromRot, toRot;
    private int interpTick = 0;

    public Humanoid(World world, int x, int y, int z) : base(world, x, y, z) {
        targetPos = position;
        targetRot = rotation;
    }

    public override void update(double dt) {
        savePrevVars();
        updateTimers(dt);

        ticksUntilConsume--;

        if (ticksUntilConsume <= 0) {
            ticksUntilConsume = CONSUME_INTERVAL;

            if (snapshotQueue.Count > 0) {
                // shift: current target becomes new start
                fromPos = toPos;
                fromRot = toRot;

                var next = snapshotQueue.Dequeue();
                toPos = next.pos;
                toRot = next.rot;
                interpTick = 0;

                // buffer overflow protection: if queue grows too large, catch up
                while (snapshotQueue.Count > 6) {
                    var skip = snapshotQueue.Dequeue();
                    toPos = skip.pos;
                    toRot = skip.rot;
                }
            }
            // else: queue starved, hold position (toPos unchanged)
        }

        // time to interp
        interpTick++;
        float t = Math.Clamp(interpTick / (float)CONSUME_INTERVAL, 0f, 1f);

        position = Vector3D.Lerp(fromPos, toPos, t);
        rotation = new Vector3(
            Meth.lerpAngle(fromRot.X, toRot.X, t),
            Meth.lerpAngle(fromRot.Y, toRot.Y, t),
            Meth.lerpAngle(fromRot.Z, toRot.Z, t)
        );

        velocity = (position - prevPosition) / dt;

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

        snapshotQueue.Enqueue((pos, rot));
    }

    public void mpInterpolateVelocity(Vector3D vel) {
        //velocity = vel;
    }

    /** when receiving an item equip packet, equip the item in the correct slot */
    public void equipItem(ushort slot, ItemStack stack) {
        if (slot < inventory.slots.Length) {
            inventory.slots[slot] = stack;
        }
    }
}
