using System.Numerics;
using Molten.DoublePrecision;

namespace BlockGame.net.packet;

/**
 * S→C: 0x2B - delta entity position+rotation update for small movements
 */
public struct EntityPositionDeltaPacket : Packet {
    public int entityID;
    public short deltaX; // fixed-point: 8192 units per block
    public short deltaY;
    public short deltaZ;
    public sbyte deltaYaw;   // fixed-point: 256 units per 2π rad (1.4° precision)
    public sbyte deltaPitch;
    public sbyte deltaRoll;

    public byte channel => 1;

    // position: 4 blocks at 60 TPS (sprinting ~0.09 blocks/tick)
    private const float POS_SCALE = 8192.0f; // 2^13
    public const double MAX_POS_DELTA = 32767.0 / POS_SCALE; // 4.0 blocks

    // rotation: ±178° range, 1.4° precision (256 units per 360°)
    private const float ROT_SCALE = 256.0f / (float.Pi * 2.0f);
    public const float MAX_ROT_DELTA = 127.0f / ROT_SCALE;

    public void write(PacketBuffer buf) {
        buf.writeInt(entityID);
        buf.writeShort(deltaX);
        buf.writeShort(deltaY);
        buf.writeShort(deltaZ);
        buf.writeSByte(deltaYaw);
        buf.writeSByte(deltaPitch);
        buf.writeSByte(deltaRoll);
    }

    public void read(PacketBuffer buf) {
        entityID = buf.readInt();
        deltaX = buf.readShort();
        deltaY = buf.readShort();
        deltaZ = buf.readShort();
        deltaYaw = buf.readSByte();
        deltaPitch = buf.readSByte();
        deltaRoll = buf.readSByte();
    }

    /** encode delta from last position+rotation (returns false if delta too large) */
    public static bool tryCreate(int entityID, Vector3D currentPos, Vector3D lastPos, Vector3 currentRot, Vector3 lastRot, out EntityPositionDeltaPacket packet) {
        var posDelta = currentPos - lastPos;
        var rotDelta = currentRot - lastRot;

        // normalise rotation delta to [-pi, pi] (wraparound)
        rotDelta.X = normalizeAngle(rotDelta.X);
        rotDelta.Y = normalizeAngle(rotDelta.Y);
        rotDelta.Z = normalizeAngle(rotDelta.Z);

        // check if deltas fit in range
        if (Math.Abs(posDelta.X) > MAX_POS_DELTA || Math.Abs(posDelta.Y) > MAX_POS_DELTA || Math.Abs(posDelta.Z) > MAX_POS_DELTA) {
            packet = default;
            return false;
        }

        if (Math.Abs(rotDelta.X) > MAX_ROT_DELTA || Math.Abs(rotDelta.Y) > MAX_ROT_DELTA || Math.Abs(rotDelta.Z) > MAX_ROT_DELTA) {
            packet = default;
            return false;
        }

        packet = new EntityPositionDeltaPacket {
            entityID = entityID,
            deltaX = (short)(posDelta.X * POS_SCALE),
            deltaY = (short)(posDelta.Y * POS_SCALE),
            deltaZ = (short)(posDelta.Z * POS_SCALE),
            deltaYaw = (sbyte)(rotDelta.X * ROT_SCALE),
            deltaPitch = (sbyte)(rotDelta.Y * ROT_SCALE),
            deltaRoll = (sbyte)(rotDelta.Z * ROT_SCALE)
        };
        return true;
    }

    /** normalise angle to [-pi, pi] for shortest rotation path */
    private static float normalizeAngle(float angle) {
        const float PI = MathF.PI;
        const float TWO_PI = PI * 2.0f;

        // wrap to [0, 2pi]
        angle %= TWO_PI;
        if (angle < 0) {
            angle += TWO_PI;
        }

        // convert to [-pi, pi] (shortest path)
        if (angle > PI) {
            angle -= TWO_PI;
        }

        return angle;
    }

    /** decode and apply deltas to last position+rotation */
    public void applyDelta(Vector3D lastPos, Vector3 lastRot, out Vector3D newPos, out Vector3 newRot) {
        newPos = new Vector3D(
            lastPos.X + deltaX / POS_SCALE,
            lastPos.Y + deltaY / POS_SCALE,
            lastPos.Z + deltaZ / POS_SCALE
        );
        
        newRot = new Vector3(
            normalizeAngle(lastRot.X + deltaYaw / ROT_SCALE),
            normalizeAngle(lastRot.Y + deltaPitch / ROT_SCALE),
            normalizeAngle(lastRot.Z + deltaRoll / ROT_SCALE)
        );
    }
}