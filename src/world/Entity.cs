using System.Numerics;
using BlockGame.util;
using BlockGame.util.xNBT;
using BlockGame.world.block;
using BlockGame.world.chunk;
using JetBrains.Annotations;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world;

public class Entity(World world) : Persistent {
    public const int MAX_SWING_TICKS = 16;
    public const int AIR_HIT_CD = 20;

    /** is player walking on (colling with) ground */
    public bool onGround;

    /** is the player in the process of jumping */
    public bool jumping;

    public bool sneaking;

    /** entity positions are at feet */
    public Vector3D prevPosition;

    public Vector3D position;
    public Vector3D velocity;
    public Vector3D accel;

    /** X Y Z */
    public Vector3 rotation;

    public Vector3 prevRotation;

    // slightly above so it doesn't think it's under the player
    public Vector3D feetPosition;


    /// <summary>
    /// Which direction the entity faces (horizontally)
    /// TODO also store pitch/yaw for head without camera
    /// </summary>
    public virtual Vector3D forward {
        get {
            var cameraDirection = Vector3.Zero;
            cameraDirection.X = MathF.Cos(Meth.deg2rad(rotation.Y));
            cameraDirection.Y = 0;
            cameraDirection.Z = MathF.Sin(Meth.deg2rad(rotation.Y));
            var v = Vector3.Normalize(cameraDirection);
            return new Vector3D(v.X, v.Y, v.Z);
        }
    }

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

    // todo unfinished shit below
    public void read(NBTCompound data) {
        position = new Vector3D(
            data.getDouble("posX"),
            data.getDouble("posY"),
            data.getDouble("posZ")
        );
        prevPosition = position;
        rotation = new Vector3(
            data.getFloat("rotX"),
            data.getFloat("rotY"),
            data.getFloat("rotZ")
        );
        prevRotation = rotation;
        velocity = new Vector3D(
            data.getDouble("velX"),
            data.getDouble("velY"),
            data.getDouble("velZ")
        );
        accel = Vector3D.Zero;
    }

    public void write(NBTCompound data) {
        data.addDouble("posX", position.X);
        data.addDouble("posY", position.Y);
        data.addDouble("posZ", position.Z);
        data.addFloat("rotX", rotation.X);
        data.addFloat("rotY", rotation.Y);
        data.addFloat("rotZ", rotation.Z);
        data.addDouble("velX", velocity.X);
        data.addDouble("velY", velocity.Y);
        data.addDouble("velZ", velocity.Z);
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
    public virtual void interactBlock(double dt) {
        // get blocks in aabb
        var min = aabb.min.toBlockPos();
        var max = aabb.max.toBlockPos();
        World.getBlocksInBox(neighbours, min, max);

        // check if any of them are liquid and accumulate push forces
        inLiquid = false;
        Vector3D push = Vector3D.Zero;
        int liquid = 0;

        foreach (var pos in neighbours) {
            var block = world.getBlock(pos);
            var blockInstance = Block.blocks[block]!;

            // handle regular interactions (non-push effects)
            blockInstance.interact(world, pos.X, pos.Y, pos.Z, this);

            // accumulate push forces for liquids
            if (Block.liquid[block]) {
                inLiquid = true;
                var pushForce = blockInstance.push(world, pos.X, pos.Y, pos.Z, this);
                if (pushForce != Vector3D.Zero) {
                    push += pushForce;
                    liquid++;
                }
            }
        }

        // apply accumulated push force with smart normalization
        if (liquid > 0 && push != Vector3D.Zero) {
            // limit maximum push strength to prevent entity getting stuck
            const double maxPushStrength = 5.6;
            push = Vector3D.Normalize(push) * maxPushStrength;


            velocity += push * dt;
        }
    }

    public virtual void onChunkChanged() {
        // remove from old chunk, add to new chunk
        if (inWorld) {
            var pp = World.getChunkPos(prevPosition.toBlockPos().X, prevPosition.toBlockPos().Z);
            world.getChunkMaybe(pp, out var oldChunk);
            oldChunk?.removeEntity(this);

            var cp = World.getChunkPos(position.toBlockPos().X, position.toBlockPos().Z);
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
            var cp = World.getChunkPos(position.toBlockPos().X, position.toBlockPos().Z);
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
    protected void collide(double dt) {
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
                foreach (AABB aa in AABBList) {
                    collisionTargets.Add(aa);
                }

                // gather neighbouring blocks
                World.getBlocksInBox(neighbours, target + new Vector3I(-1, -1, -1),
                    target + new Vector3I(1, 1, 1));
                // for each neighbour block
                foreach (var neighbour in neighbours) {
                    var block = world.getBlock(neighbour);
                    world.getAABBsCollision(AABBList, neighbour.X, neighbour.Y, neighbour.Z);
                    foreach (AABB aa in AABBList) {
                        collisionTargets.Add(aa);
                    }
                }
            }
        }

        var caabb = calcAABB(position);
        AABB aabb;
        AABB sneakaabb;
        var loss = new List<AABB>();
        foreach (var blockAABB in collisionTargets) {
            if (AABB.isCollision(caabb, blockAABB)) {
                loss.Add(blockAABB);
            }
        }

        // Y axis resolution
        position.Y += velocity.Y * dt;
        foreach (var blockAABB in collisionTargets) {
            aabb = calcAABB(new Vector3D(position.X, position.Y, position.Z));
            if (AABB.isCollision(aabb, blockAABB)) {
                // If we're stuck in this block, only prevent escape
                if (loss.Contains(blockAABB)) {
                    // Check if this movement would take us OUT of the block
                    if (velocity.Y > 0 && aabb.y1 > blockAABB.y1) {
                        position.Y = oldPos.Y; // Prevent escape upward
                        velocity.Y = 0;
                    }
                    else if (velocity.Y < 0 && aabb.y0 < blockAABB.y0) {
                        position.Y = oldPos.Y; // Prevent escape downward
                        velocity.Y = 0;
                    }
                }
                else {
                    // Normal collision - we're hitting from outside
                    // left side
                    if (velocity.Y > 0 && aabb.y1 >= blockAABB.y0) {
                        var d = blockAABB.y0 - aabb.y1;
                        position.Y += d;
                        velocity.Y = 0;
                    }

                    else if (velocity.Y < 0 && aabb.y0 <= blockAABB.y1) {
                        var diff = blockAABB.y1 - aabb.y0;
                        position.Y += diff;
                        velocity.Y = 0;
                    }
                }
            }
        }


        // X axis resolution
        position.X += velocity.X * dt;
        var hasCollision = false;
        foreach (var blockAABB in collisionTargets) {
            aabb = calcAABB(new Vector3D(position.X, position.Y, position.Z));
            sneakaabb = calcAABB(new Vector3D(position.X, position.Y - 0.1, position.Z));
            if (AABB.isCollision(aabb, blockAABB)) {
                collisionXThisFrame = true;

                // If stuck in this block, only prevent escape
                if (loss.Contains(blockAABB)) {
                    if (velocity.X > 0 && aabb.x1 > blockAABB.x1) {
                        position.X = oldPos.X;
                        velocity.X = 0;
                    }
                    else if (velocity.X < 0 && aabb.x0 < blockAABB.x0) {
                        position.X = oldPos.X;
                        velocity.X = 0;
                    }
                }
                else {
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
                        if (velocity.X > 0 && aabb.x1 >= blockAABB.x0) {
                            var d = blockAABB.x0 - aabb.x1;
                            position.X += d;
                        }
                        else if (velocity.X < 0 && aabb.x0 <= blockAABB.x1) {
                            var diff = blockAABB.x1 - aabb.x0;
                            position.X += diff;
                        }
                    }
                }
            }

            if (sneaking && AABB.isCollision(sneakaabb, blockAABB)) {
                hasCollision = true;
            }
        }

        // don't fall off while sneaking
        if (sneaking && onGround && !hasCollision) {
            // revert movement
            position.X = oldPos.X;
        }

        position.Z += velocity.Z * dt;
        hasCollision = false;
        foreach (var blockAABB in collisionTargets) {
            aabb = calcAABB(new Vector3D(position.X, position.Y, position.Z));
            sneakaabb = calcAABB(new Vector3D(position.X, position.Y - 0.1, position.Z));
            if (AABB.isCollision(aabb, blockAABB)) {
                collisionZThisFrame = true;

                // If stuck in this block, only prevent escape
                if (loss.Contains(blockAABB)) {
                    if (velocity.Z > 0 && aabb.z1 > blockAABB.z1) {
                        position.Z = oldPos.Z;
                        velocity.Z = 0;
                    }
                    else if (velocity.Z < 0 && aabb.z0 < blockAABB.z0) {
                        position.Z = oldPos.Z;
                        velocity.Z = 0;
                    }
                }
                else {
                    // try stepping up before blocking movement
                    bool canStepUp = false;
                    if (onGround && velocity.Z != 0) {
                        // check if we can step up by trying positions up to stepHeight
                        for (double stepY = Constants.epsilon; stepY <= Constants.stepHeight; stepY += 0.1) {
                            var stepAABB = calcAABB(new Vector3D(position.X, position.Y + stepY, position.Z));
                            bool collideStep = false;

                            // check if this step position has any collisions
                            foreach (var testAABB in collisionTargets) {
                                if (AABB.isCollision(stepAABB, testAABB)) {
                                    collideStep = true;
                                    break;
                                }
                            }

                            if (!collideStep) {
                                // found a valid step position
                                position.Y += stepY;
                                canStepUp = true;
                                break;
                            }
                        }
                    }

                    if (!canStepUp) {
                        // normal collision resolution
                        if (velocity.Z > 0 && aabb.z1 >= blockAABB.z0) {
                            var d = blockAABB.z0 - aabb.z1;
                            position.Z += d;
                        }
                        else if (velocity.Z < 0 && aabb.z0 <= blockAABB.z1) {
                            var diff = blockAABB.z1 - aabb.z0;
                            position.Z += diff;
                        }
                    }
                }
            }

            if (sneaking && AABB.isCollision(sneakaabb, blockAABB)) {
                hasCollision = true;
            }
        }

        // don't fall off while sneaking
        if (sneaking && onGround && !hasCollision) {
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