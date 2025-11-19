using System.Numerics;
using BlockGame.util;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

/**
 * It's like a Player, but not really.
 * Used for other clients in multiplayer.
 *
 * TODO can we do better than the fixed 4-tick interpolation?
 * RN it works but it's kinda janky, has that fucking MC-like lag.
 * We could try client-side prediction or something, not sure. Or adjust based on ping?
 * And idk what happens at high ping, does it spaz out?
 *
 * ALSO TODO fucking merge this system with the Mob interpolation jfc, it's duplicated right now because I made this first lol
 *
 * Also arms glitch out when sneaking (animation too fast, looks like it's normal animation speed but with smaller maxpos? Something is fucked, investigate)
 */
public class Humanoid : Player {
    // interpolation for smooth movement
    public Vector3D targetPos;
    public Vector3 targetRot;
    public Vector3 targetBodyRot;
    public int interpolationTicks;

    public Humanoid(World world, int x, int y, int z) : base(world, x, y, z) {
        targetPos = position;
        targetRot = rotation;
        targetBodyRot = bodyRotation;
    }

    public override void update(double dt) {
        // set prev
        savePrevVars();

        // interpolate towards target position/rotation evenly
        if (interpolationTicks > 0) {
            var t = 1.0 / interpolationTicks;
            position = Vector3D.Lerp(position, targetPos, t);
            rotation = Vector3.Lerp(rotation, targetRot, (float)t);
            interpolationTicks--;
        }

        // derive velocity from actual movement for animation
        // (velocity can also be set directly by EntityVelocityPacket for knockback)
        if (dt > 0) {
            velocity = (position - prevPosition) / dt;
        }

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
        targetPos = pos;
        targetRot = rot;
        interpolationTicks = 4; // interpolate over 3 ticks (~50ms)
    }

    /** when receiving an item equip packet, equip the item in the correct slot */
    public void equipItem(ushort slot, ItemStack stack) {
        if (slot < inventory.slots.Length) {
            inventory.slots[slot] = stack;
        }
    }

    // disable player-specific behaviour that doesn't apply to remote players
    public override void updateInput(double dt) { }
    public override void blockHandling(double dt) { }
    public override void placeBlock() { }
    public override void breakBlock() { }
}