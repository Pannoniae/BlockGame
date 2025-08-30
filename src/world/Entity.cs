using BlockGame.util;
using JetBrains.Annotations;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame;

public class Entity(World world) {
    public const int MAX_SWING_TICKS = 16;
    public const int AIR_HIT_CD = 20;

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

    public World world = world;

    public bool flyMode;
    public bool noClip;

    protected List<AABB> collisionTargets = [];

    public int airHitCD;

    public int swingTicks;
    public bool swinging;

    /// 0 to 1
    public double prevSwingProgress;

    public double swingProgress;
    private readonly List<Vector3I> neighbours = new(26);

    /** Is it in a valid chunk? */
    public bool inWorld;

    protected static readonly List<AABB> AABBList = [];

    public ChunkCoord getChunk(Vector3D pos) {
        var blockPos = pos.toBlockPos();
        //world.actionQueue
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

    public virtual void teleport(Vector3D pos) {
        position = pos;
        prevPosition = pos;
        velocity = Vector3D.Zero;
    }

    public virtual void update(double dt) {
        // after movement applied, check the chunk the player is in
        var prevChunk = getChunk(prevPosition);
        var thisChunk = getChunk(position);
        if (prevChunk != thisChunk) {
            onChunkChanged();
        }
    }

    /**
     * Handle interactions with the block the entity is standing in.
     */
    public virtual void interactBlock() {
        // get blocks in aabb
        var min = aabb.min.toBlockPos();
        var max = aabb.max.toBlockPos();
        World.getBlocksInBox(neighbours, min, max);
        
        // check if any of them are liquid
        inLiquid = false;
        foreach (var pos in neighbours) {
            var block = world.getBlock(pos);
            Block.blocks[block]!.interact(world, pos.X, pos.Y, pos.Z, this);
        }
    }

    public virtual void onChunkChanged() {
        // remove from old chunk, add to new chunk
        if (inWorld) {
            var pp = World.getChunkPos(prevPosition.toBlockPos().X , prevPosition.toBlockPos().Z);
            world.getChunkMaybe(pp, out var oldChunk);
            oldChunk?.removeEntity(this);
            
            var cp = World.getChunkPos(position.toBlockPos().X , position.toBlockPos().Z);
            world.getChunkMaybe(cp, out var newChunk);
            if (newChunk != null && position.Y is >= 0 and < World.WORLDHEIGHT) {
                newChunk.addEntity(this);
                inWorld = true;
            }
            else {
                inWorld = false;
            }
        }
        else {
            var cp = World.getChunkPos(position.toBlockPos().X , position.toBlockPos().Z);
            world.getChunkMaybe(cp, out var newChunk);
            if (newChunk != null && position.Y is >= 0 and < World.WORLDHEIGHT) {
                newChunk.addEntity(this);
                inWorld = true;
            }
            else {
                inWorld = false;
            }
        }
    }

    /**
     * This one is a real mess!
     */
    protected void collisionAndSneaking(double dt) {
        var oldPos = position;
        var blockPos = position.toBlockPos();
        // collect potential collision targets
        collisionTargets.Clear();

        // if we aren't noclipping
        if (!noClip) {
            ReadOnlySpan<Vector3I> targets = [
                blockPos, new Vector3I(blockPos.X, blockPos.Y + 1, blockPos.Z)
            ];
            // for each block we might collide with
            foreach (Vector3I target in targets) {
                // first, collide with the block the player is in
                var blockPos2 = feetPosition.toBlockPos();
                world.getAABBsCollision(AABBList, blockPos2.X, blockPos2.Y, blockPos2.Z);

                // for each AABB of the block the player is in
                foreach (AABB aabb in AABBList) {
                    collisionTargets.Add(aabb);
                }

                // gather neighbouring blocks
                World.getBlocksInBox(neighbours, target + new Vector3I(-1, -1, -1),
                    target + new Vector3I(1, 1, 1));
                // for each neighbour block
                foreach (var neighbour in neighbours) {
                    var block = world.getBlock(neighbour);
                    world.getAABBsCollision(AABBList, neighbour.X, neighbour.Y, neighbour.Z);
                    foreach (AABB aabb in AABBList) {
                        collisionTargets.Add(aabb);
                    }
                }
            }
        }

        // Y axis resolution
        position.Y += velocity.Y * dt;
        foreach (var blockAABB in collisionTargets) {
            var aabbY = calcAABB(new Vector3D(position.X, position.Y, position.Z));
            if (AABB.isCollision(aabbY, blockAABB)) {
                // left side
                if (velocity.Y > 0 && aabbY.y1 >= blockAABB.y0) {
                    var diff = blockAABB.y0 - aabbY.y1;
                    //if (diff < velocity.Y) {
                    position.Y += diff;
                    velocity.Y = 0;
                    //}
                }

                else if (velocity.Y < 0 && aabbY.y0 <= blockAABB.y1) {
                    var diff = blockAABB.y1 - aabbY.y0;
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
                
                // try stepping up before blocking movement
                bool canStepUp = false;
                if (onGround && velocity.X != 0) {
                    // check if we can step up by trying positions up to stepHeight
                    // todo this is the worst piece of shit code ever, there must SURELY be a better mathsy way of not just having a for loop here lol
                    for (double stepY = Constants.epsilon; stepY <= Constants.stepHeight; stepY += 0.1) {
                        var stepAABB = calcAABB(new Vector3D(position.X, position.Y + stepY, position.Z));
                        bool hasCollisionAtStep = false;
                        
                        // check if this step position has any collisions
                        foreach (var testAABB in collisionTargets) {
                            if (AABB.isCollision(stepAABB, testAABB)) {
                                hasCollisionAtStep = true;
                                break;
                            }
                        }
                        
                        if (!hasCollisionAtStep) {
                            // found a valid step position
                            position.Y += stepY;
                            canStepUp = true;
                            break;
                        }
                    }
                }
                
                if (!canStepUp) {
                    // normal collision resolution
                    if (velocity.X > 0 && aabbX.x1 >= blockAABB.x0) {
                        var diff = blockAABB.x0 - aabbX.x1;
                        position.X += diff;
                    }
                    else if (velocity.X < 0 && aabbX.x0 <= blockAABB.x1) {
                        var diff = blockAABB.x1 - aabbX.x0;
                        position.X += diff;
                    }
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
                
                // try stepping up before blocking movement
                bool canStepUp = false;
                if (onGround && velocity.Z != 0) {
                    // check if we can step up by trying positions up to stepHeight
                    for (double stepY = Constants.epsilon; stepY <= Constants.stepHeight; stepY += 0.1) {
                        var stepAABB = calcAABB(new Vector3D(position.X, position.Y + stepY, position.Z));
                        bool hasCollisionAtStep = false;
                        
                        // check if this step position has any collisions
                        foreach (var testAABB in collisionTargets) {
                            if (AABB.isCollision(stepAABB, testAABB)) {
                                hasCollisionAtStep = true;
                                break;
                            }
                        }
                        
                        if (!hasCollisionAtStep) {
                            // found a valid step position
                            position.Y += stepY;
                            canStepUp = true;
                            break;
                        }
                    }
                }
                
                if (!canStepUp) {
                    // normal collision resolution
                    if (velocity.Z > 0 && aabbZ.z1 >= blockAABB.z0) {
                        var diff = blockAABB.z0 - aabbZ.z1;
                        position.Z += diff;
                    }
                    else if (velocity.Z < 0 && aabbZ.z0 <= blockAABB.z1) {
                        var diff = blockAABB.z1 - aabbZ.z0;
                        position.Z += diff;
                    }
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
        if (hit) {
            swinging = true;
            swingTicks = 0;
        }
        else {
            if (airHitCD == 0) {
                swinging = true;
                swingTicks = 0;
                airHitCD = AIR_HIT_CD;
            }
        }
    }
}