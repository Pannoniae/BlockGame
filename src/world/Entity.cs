using BlockGame.util;
using JetBrains.Annotations;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame;

public class Entity {
    public const int MAX_SWING_TICKS = 8;
    public const int AIR_HIT_CD = 10;

    // is player walking on (colling with) ground
    public bool onGround;

    // is the player in the process of jumping
    public bool jumping;

    public bool sneaking;

    // entity positions are at feet
    public Vector3D prevPosition;
    public Vector3D position;
    public Vector3D velocity;
    public Vector3D accel;

    // slightly above so it doesn't think it's under the player
    public Vector3D feetPosition;


    /// <summary>
    /// Which direction the entity faces (horizontally)
    /// TODO also store pitch/yaw for head without camera
    /// </summary>
    public Vector3D forward;

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

    public World world;

    public bool flyMode;
    protected List<AABB> collisionTargets = [];

    public int airHitCD;

    public int swingTicks;
    public bool swinging;

    /// 0 to 1
    public double prevSwingProgress;
    public double swingProgress;

    public Entity(World world) {
        this.world = world;
    }

    public ChunkCoord getChunk(Vector3D pos) {
        var blockPos = pos.toBlockPos();
        return World.getChunkPos(new Vector2I(blockPos.X, blockPos.Z));
    }

    public ChunkCoord getChunk() {
        var blockPos = position.toBlockPos();
        return World.getChunkPos(new Vector2I(blockPos.X, blockPos.Z));
    }

    [Pure]
    protected virtual AABB calcAABB(Vector3D pos) {
        return AABB.empty;
    }

    public void teleport(Vector3D pos) {
        position = pos;
        prevPosition = pos;
        velocity = Vector3D.Zero;
    }

    protected void collisionAndSneaking(double dt) {
        var oldPos = position;
        var blockPos = position.toBlockPos();
        // collect potential collision targets
        collisionTargets.Clear();
        ReadOnlySpan<Vector3I> targets = [
            blockPos, new Vector3I(blockPos.X, blockPos.Y + 1, blockPos.Z)
        ];
        foreach (Vector3I target in targets) {
            // first, collide with the block the player is in
            var blockPos2 = feetPosition.toBlockPos();
            var currentAABB = world.getAABB(blockPos2.X, blockPos2.Y, blockPos2.Z, world.getBlock(feetPosition.toBlockPos()));
            if (currentAABB != null) {
                collisionTargets.Add(currentAABB.Value);
            }
            foreach (var neighbour in world.getBlocksInBox(target + new Vector3I(-1, -1, -1), target + new Vector3I(1, 1, 1))) {
                var block = world.getBlock(neighbour);
                var blockAABB = world.getAABB(neighbour.X, neighbour.Y, neighbour.Z, block);
                if (blockAABB == null) {
                    continue;
                }

                collisionTargets.Add(blockAABB.Value);
            }
        }

        // Y axis resolution
        position.Y += velocity.Y * dt;
        foreach (var blockAABB in collisionTargets) {
            var aabbY = calcAABB(new Vector3D(position.X, position.Y, position.Z));
            if (AABB.isCollision(aabbY, blockAABB)) {
                // left side
                if (velocity.Y > 0 && aabbY.maxY >= blockAABB.minY) {
                    var diff = blockAABB.minY - aabbY.maxY;
                    //if (diff < velocity.Y) {
                    position.Y += diff;
                    velocity.Y = 0;
                    //}
                }

                else if (velocity.Y < 0 && aabbY.minY <= blockAABB.maxY) {
                    var diff = blockAABB.maxY - aabbY.minY;
                    //if (diff > velocity.Y) {
                    position.Y += diff;
                    velocity.Y = 0;
                    //}
                }
            }
        }


        // X axis resolution
        position.X += velocity.X * dt;
        var hasAtLeastOneCollision = false;
        foreach (var blockAABB in collisionTargets) {
            var aabbX = calcAABB(new Vector3D(position.X, position.Y, position.Z));
            var sneakaabbX = calcAABB(new Vector3D(position.X, position.Y - 0.1, position.Z));
            if (AABB.isCollision(aabbX, blockAABB)) {
                collisionXThisFrame = true;
                // left side
                if (velocity.X > 0 && aabbX.maxX >= blockAABB.minX) {
                    var diff = blockAABB.minX - aabbX.maxX;
                    //if (diff < velocity.X) {
                    position.X += diff;
                    //}
                }

                else if (velocity.X < 0 && aabbX.minX <= blockAABB.maxX) {
                    var diff = blockAABB.maxX - aabbX.minX;
                    //if (diff > velocity.X) {
                    position.X += diff;
                    //}
                }
            }
            if (sneaking && AABB.isCollision(sneakaabbX, blockAABB)) {
                hasAtLeastOneCollision = true;
            }
        }
        // don't fall off while sneaking
        if (sneaking && onGround && !hasAtLeastOneCollision) {
            // revert movement
            position.X = oldPos.X;
        }

        position.Z += velocity.Z * dt;
        hasAtLeastOneCollision = false;
        foreach (var blockAABB in collisionTargets) {
            var aabbZ = calcAABB(new Vector3D(position.X, position.Y, position.Z));
            var sneakaabbZ = calcAABB(new Vector3D(position.X, position.Y - 0.1, position.Z));
            if (AABB.isCollision(aabbZ, blockAABB)) {
                collisionZThisFrame = true;
                if (velocity.Z > 0 && aabbZ.maxZ >= blockAABB.minZ) {
                    var diff = blockAABB.minZ - aabbZ.maxZ;
                    //if (diff < velocity.Z) {
                    position.Z += diff;
                    //}
                }

                else if (velocity.Z < 0 && aabbZ.minZ <= blockAABB.maxZ) {
                    var diff = blockAABB.maxZ - aabbZ.minZ;
                    //if (diff > velocity.Z) {
                    position.Z += diff;
                    //}
                }
            }
            if (sneaking && AABB.isCollision(sneakaabbZ, blockAABB)) {
                hasAtLeastOneCollision = true;
            }
        }
        // don't fall off while sneaking
        if (sneaking && onGround && !hasAtLeastOneCollision) {
            // revert movement
            position.Z = oldPos.Z;
        }

        // is player on ground? check slightly below
        var groundCheck = calcAABB(new Vector3D(position.X, position.Y - Constants.epsilonGroundCheck, position.Z));
        onGround = false;
        foreach (var blockAABB in collisionTargets) {
            if (AABB.isCollision(blockAABB, groundCheck)) {
                onGround = true;
                flyMode = false;
            }
        }
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