using BlockGame.util;
using Silk.NET.Maths;

namespace BlockGame;

public class Entity {
    public const int MAX_SWING_TICKS = 8;
    public const int AIR_HIT_CD = 10;

    // entity positions are at feet
    public Vector3D<double> prevPosition;
    public Vector3D<double> position;
    public Vector3D<double> velocity;
    public Vector3D<double> accel;

    // slightly above so it doesn't think it's under the player
    public Vector3D<double> feetPosition;


    /// <summary>
    /// Which direction the entity faces (horizontally)
    /// TODO also store pitch/yaw for head without camera
    /// </summary>
    public Vector3D<double> forward;

    public AABB aabb;

    public ushort blockAtFeet;
    public bool inLiquid;
    public bool wasInLiquid;

    // TODO implement some MovementState system so movement constants don't have to be duplicated...
    // it would store a set of values for acceleration, drag, friction, maxspeed, etc...

    public bool collisionXThisFrame;
    public bool collisionZThisFrame;

    /// <summary>
    /// This number is lying to you.
    /// </summary>
    public double totalTraveled;
    public double prevTotalTraveled;

    public int airHitCD;

    public int swingTicks;
    public bool swinging;

    /// 0 to 1
    public double prevSwingProgress;
    public double swingProgress;

    public ChunkCoord getChunk(Vector3D<double> pos) {
        var blockPos = pos.toBlockPos();
        return World.getChunkPos(new Vector2D<int>(blockPos.X, blockPos.Z));
    }

    public ChunkCoord getChunk() {
        var blockPos = position.toBlockPos();
        return World.getChunkPos(new Vector2D<int>(blockPos.X, blockPos.Z));
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
        if (!hit) {
            if (airHitCD == 0) {
                swinging = true;
                swingTicks = 0;
                airHitCD = AIR_HIT_CD;
            }
        }
        else {
            swinging = true;
            swingTicks = 0;
        }
    }
}