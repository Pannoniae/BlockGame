using System.Numerics;
using BlockGame.util;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

/**
 * It's like a Player, but not really.
 * Used for other clients in multiplayer.
 *
 * Uses adaptive interpolation based on actual update rate to reduce lag.
 * Extrapolates with velocity when updates are delayed.
 *
 * ALSO TODO fucking merge this system with the Mob interpolation jfc, it's duplicated right now because I made this first lol
 */
public class Humanoid : Player {
    // interpolation for smooth movement
    public Vector3D targetPos;
    public Vector3 targetRot;
    public Vector3 targetBodyRot;
    public int interpolationTicks;

    // adaptive interpolation tracking
    private double lastUpdateTime = 0;
    private double updateInterval = 4.0; // measured ticks between updates
    private int ticksSinceLastUpdate = 0;

    public Humanoid(World world, int x, int y, int z) : base(world, x, y, z) {
        targetPos = position;
        targetRot = rotation;
        targetBodyRot = bodyRotation;
    }

    public override void update(double dt) {
        // set prev
        savePrevVars();

        ticksSinceLastUpdate++;

        // timeout check - if no update for 30 ticks, snap to target
        if (ticksSinceLastUpdate > 30) {
            position = targetPos;
            rotation = targetRot;
            interpolationTicks = 0;
        }

        // interpolate towards target position/rotation
        if (interpolationTicks > 0) {
            var t = 1.0 / interpolationTicks;
            position = Vector3D.Lerp(position, targetPos, t);
            rotation = Vector3.Lerp(rotation, targetRot, (float)t);
            interpolationTicks--;
        }
        else if (velocity.LengthSquared() > 0.001) {
            // ran out of interpolation ticks but still moving - extrapolate with velocity
            // (prevents stutter when waiting for next update)
            var extrapolated = position + velocity * dt;

            // don't drift too far from last known position (max 1.5 blocks)
            if (Vector3D.Distance(extrapolated, targetPos) < 1.5) {
                position = extrapolated;
            }
        }

        // velocity is now set by EntityVelocityPacket, not derived
        // (this HOPEFULLY fixes the animation jank from interpolation artifacts)

        // update body movement (uses velocity like Mob does)
        updateBodyRotation(dt);

        // update walk animation (uses velocity)
        updateAnimation(dt);

        // update swinging
        updateTimers(dt);

        // update AABB
        aabb = calcAABB(position);

        // don't bother with input/physics, server controls movement
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

    public void mpInterpolate(Vector3D pos, Vector3 rot) {
        double now = world.worldTick;
        if (lastUpdateTime > 0) {
            double measured = now - lastUpdateTime;
            updateInterval = updateInterval * 0.7 + measured * 0.3;
        }
        lastUpdateTime = now;

        targetPos = pos;
        targetRot = rot;

        interpolationTicks = (int)Math.Clamp(updateInterval, 2, 10);

        ticksSinceLastUpdate = 0; // reset timeout
    }

    /** when receiving an item equip packet, equip the item in the correct slot */
    public void equipItem(ushort slot, ItemStack stack) {
        if (slot < inventory.slots.Length) {
            inventory.slots[slot] = stack;
        }
    }
}