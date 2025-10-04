using System.Numerics;
using BlockGame.util;
using BlockGame.util.xNBT;
using BlockGame.world.block;
using BlockGame.world.chunk;
using JetBrains.Annotations;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world;

public class Entity(World world, int type) : Persistent {
    public const int MAX_SWING_TICKS = 20;
    public const int AIR_HIT_CD = 20;

    public int type = type;
    public int id = World.ec++;

    public World world = world;


    /** is entity deleted?
     * Status update: we shouldn't use this!! it's stupid and HAS TO BE CHECKED EVERYWHERE
     */
    public bool active = true;

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

    public Vector3 bodyRotation;
    public Vector3 prevBodyRotation;

    // slightly above so it doesn't think it's under the player
    public Vector3D feetPosition;


    /// <summary>
    /// Which direction the entity faces (horizontally only)
    /// </summary>
    public virtual Vector3D hfacing {
        get {
            var cameraDirection = Vector3.Zero;
            cameraDirection.X = MathF.Sin(Meth.deg2rad(rotation.Y));
            cameraDirection.Y = 0;
            cameraDirection.Z = MathF.Cos(Meth.deg2rad(rotation.Y));
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

    public bool collx;
    public bool collz;

    /// <summary>
    /// This number is lying to you.
    /// </summary>
    public double totalTraveled;

    public double prevTotalTraveled;

    public bool flyMode;
    public bool noClip;

    protected List<AABB> collisions = [];

    public int airHitCD;

    public int swingTicks;
    public bool swinging;

    /// 0 to 1
    public double prevSwingProgress;

    public double swingProgress;

    // animation state
    public float apos;
    public float papos;

    public float aspeed;
    public float paspeed;

    private readonly List<Vector3I> neighbours = new(26);

    /** Is it in a valid chunk?
     * STATUS UPDATE: We have a genius idea, we just store it in the bottommost/topmost chunk if out of bounds
     * This way the entity will almost never be out of world except if the chunk is unloaded or teleported far away or something
     */
    public bool inWorld;

    /** We kept losing track of which chunk the entity was in, so fuck it let's just store it */
    public SubChunkCoord subChunkCoord;

    protected static readonly List<AABB> AABBList = [];

    public ChunkCoord getChunk(Vector3D pos) {
        var blockPos = pos.toBlockPos();
        return World.getChunkPos(new Vector2I(blockPos.X, blockPos.Z));
    }

    public ChunkCoord getChunk() {
        var blockPos = position.toBlockPos();
        return World.getChunkPos(new Vector2I(blockPos.X, blockPos.Z));
    }

    [Pure]
    public virtual AABB calcAABB(Vector3D pos) {
        return new AABB(
            pos.X - 0.3, pos.Y, pos.Z - 0.3,
            pos.X + 0.3, pos.Y + 1.8, pos.Z + 0.3
        );
    }

    public virtual void teleport(Vector3D pos) {
        position = pos;
        prevPosition = pos;
        velocity = Vector3D.Zero;
    }

    // todo unfinished shit below
    public void read(NBTCompound data) {
        id = data.getInt("id");
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
        data.addInt("id", id);
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
    }

    /**
     * Swept collision - adjust velocity per axis, apply movement immediately.
     * Reduces velocity to exact contact point instead of preventing escape.
     */
    protected void collide(double dt) {
        var oldPos = position;
        var blockPos = position.toBlockPos();

        // collect potential collision targets
        collisions.Clear();

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
                    collisions.Add(aa);
                }

                // gather neighbouring blocks
                World.getBlocksInBox(neighbours, target + new Vector3I(-1, -1, -1),
                    target + new Vector3I(1, 1, 1));
                // for each neighbour block
                foreach (var neighbour in neighbours) {
                    var block = world.getBlock(neighbour);
                    world.getAABBsCollision(AABBList, neighbour.X, neighbour.Y, neighbour.Z);
                    foreach (AABB aa in AABBList) {
                        collisions.Add(aa);
                    }
                }
            }
        }

        // tl;dr: the point is that we *prevent* collisions instead of resolving them after the fact
        // and if there's already a collision, we don't do shit. We just prevent new ones.
        // This prevents the "ejecting out" behaviour and it also prevents stuff glitching where it REALLY shouldn't.

        // hehe
        double d = 0.0;

        // Y axis
        var p = calcAABB(position);

        foreach (var b in collisions) {
            // wtf are we even doing here then?
            if (!AABB.isCollisionX(p, b) || !AABB.isCollisionZ(p, b)) {
                continue;
            }

            switch (velocity.Y) {
                case > 0: {
                    d = b.y0 - p.y1;
                    if (p.y1 <= b.y0 && d < velocity.Y * dt) {
                        velocity.Y = d / dt;
                    }

                    break;
                }
                case < 0: {
                    d = b.y1 - p.y0;
                    if (p.y0 >= b.y1 && d > velocity.Y * dt) {
                        velocity.Y = d / dt;
                    }

                    break;
                }
            }
        }

        position.Y += velocity.Y * dt;
        // recalc aabb after Y movement!
        p = calcAABB(position);

        // X axis
        foreach (var b in collisions) {
            if (!AABB.isCollisionY(p, b) || !AABB.isCollisionZ(p, b)) {
                continue;
            }

            // try stepping up if on ground
            bool canStepUp = false;
            if (onGround && velocity.X != 0) {
                for (double stepY = Constants.epsilon; stepY <= Constants.stepHeight; stepY += 0.1) {
                    var stepAABB = calcAABB(new Vector3D(position.X + velocity.X * dt, position.Y + stepY, position.Z));
                    bool cb = false;

                    foreach (var testAABB in collisions) {
                        if (AABB.isCollision(stepAABB, testAABB)) {
                            cb = true;
                            break;
                        }
                    }

                    if (!cb) {
                        position.Y += stepY;
                        canStepUp = true;
                        break;
                    }
                }
            }

            if (!canStepUp) {
                switch (velocity.X) {
                    case > 0: {
                        d = b.x0 - p.x1;
                        if (p.x1 <= b.x0 && d < velocity.X * dt) {
                            velocity.X = d / dt;
                            collx = true;
                        }
                        break;
                    }
                    case < 0: {
                        d = b.x1 - p.x0;
                        if (p.x0 >= b.x1 && d > velocity.X * dt) {
                            velocity.X = d / dt;
                            collx = true;
                        }
                        break;
                    }
                }
            }
        }

        // sneaking edge prevention for X
        if (sneaking && onGround) {
            var sneakAABB = calcAABB(new Vector3D(position.X + velocity.X * dt, position.Y - 0.1, position.Z));
            bool hasEdge = false;
            foreach (var blockAABB in collisions) {
                if (AABB.isCollision(sneakAABB, blockAABB)) {
                    hasEdge = true;
                    break;
                }
            }

            if (!hasEdge) {
                velocity.X = 0;
            }
        }

        position.X += velocity.X * dt;
        // recalc aabb after X movement!
        p = calcAABB(position);

        // Z axis
        foreach (var b in collisions) {
            if (!AABB.isCollisionX(p, b) || !AABB.isCollisionY(p, b)) {
                continue;
            }

            // try stepping up if on ground
            bool canStepUp = false;
            if (onGround && velocity.Z != 0) {
                for (double stepY = Constants.epsilon; stepY <= Constants.stepHeight; stepY += 0.1) {
                    var stepAABB = calcAABB(new Vector3D(position.X, position.Y + stepY, position.Z + velocity.Z * dt));
                    bool cb = false;

                    foreach (var testAABB in collisions) {
                        if (AABB.isCollision(stepAABB, testAABB)) {
                            cb = true;
                            break;
                        }
                    }

                    if (!cb) {
                        position.Y += stepY;
                        canStepUp = true;
                        break;
                    }
                }
            }

            if (!canStepUp) {
                switch (velocity.Z) {
                    case > 0: {
                        d = b.z0 - p.z1;
                        if (p.z1 <= b.z0 && d < velocity.Z * dt) {
                            velocity.Z = d / dt;
                            collz = true;
                        }
                        break;
                    }
                    case < 0: {
                        d = b.z1 - p.z0;
                        if (p.z0 >= b.z1 && d > velocity.Z * dt) {
                            velocity.Z = d / dt;
                            collz = true;
                        }
                        break;
                    }
                }
            }
        }

        // sneaking edge prevention for Z
        if (sneaking && onGround) {
            var sneakAABB = calcAABB(new Vector3D(position.X, position.Y - 0.1, position.Z + velocity.Z * dt));
            bool hasEdge = false;
            foreach (var blockAABB in collisions) {
                if (AABB.isCollision(sneakAABB, blockAABB)) {
                    hasEdge = true;
                    break;
                }
            }

            if (!hasEdge) {
                velocity.Z = 0;
            }
        }

        position.Z += velocity.Z * dt;
        // recalc aabb after Z movement!
        p = calcAABB(position);

        // zero out velocity on collision?
        //if (hasXCollision) velocity.X = 0;
        //if (hasZCollision) velocity.Z = 0;

        // is player on ground? check slightly below
        var groundCheck = calcAABB(new Vector3D(position.X, position.Y - Constants.epsilonGroundCheck, position.Z));
        onGround = false;
        foreach (var blockAABB in collisions) {
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

    public Vector3 facing() {
        var cameraDirection = Vector3.Zero;
        cameraDirection.X = MathF.Sin(Meth.deg2rad(rotation.Y));
        cameraDirection.Y = 0;
        cameraDirection.Z = MathF.Cos(Meth.deg2rad(rotation.Y));

        return Vector3.Normalize(cameraDirection);
    }

    public Vector3 camFacing() {
        var cameraDirection = Vector3.Zero;
        cameraDirection.X = MathF.Cos(Meth.deg2rad(rotation.X)) * MathF.Sin(Meth.deg2rad(rotation.Y));
        cameraDirection.Y = MathF.Sin(Meth.deg2rad(rotation.X));
        cameraDirection.Z = MathF.Cos(Meth.deg2rad(rotation.X)) * MathF.Cos(Meth.deg2rad(rotation.Y));

        return Vector3.Normalize(cameraDirection);
    }

    protected void clamp(double dt) {
        // clamp
        if (Math.Abs(velocity.X) < Constants.epsilon) {
            velocity.X = 0;
        }

        if (Math.Abs(velocity.Y) < Constants.epsilon) {
            velocity.Y = 0;
        }

        if (Math.Abs(velocity.Z) < Constants.epsilon) {
            velocity.Z = 0;
        }

        // clamp fallspeed
        if (Math.Abs(velocity.Y) > Constants.maxVSpeed) {
            var cappedVel = Constants.maxVSpeed;
            velocity.Y = cappedVel * Math.Sign(velocity.Y);
        }

        // clamp accel (only Y for now, other axes aren't used)
        if (Math.Abs(accel.Y) > Constants.maxAccel) {
            accel.Y = Constants.maxAccel * Math.Sign(accel.Y);
        }

        // world bounds check
        //var s = world.getWorldSize();
        //position.X = Math.Clamp(position.X, 0, s.X);
        //position.Y = Math.Clamp(position.Y, 0, s.Y);
        //position.Z = Math.Clamp(position.Z, 0, s.Z);
    }

    protected void applyFriction() {
        if (flyMode) {
            var f = Constants.flyFriction;
            velocity.X *= f;
            velocity.Z *= f;
            velocity.Y *= f;
            return;
        }

        // ground friction
        if (!inLiquid) {
            var f2 = Constants.verticalFriction;
            if (onGround) {
                //if (sneaking) {
                //    velocity = Vector3D.Zero;
                //}
                //else {
                var f = Constants.friction;
                velocity.X *= f;
                velocity.Z *= f;
                velocity.Y *= f2;
                //}
            }
            else {
                var f = Constants.airFriction;
                velocity.X *= f;
                velocity.Z *= f;
                velocity.Y *= f2;
            }
        }

        // liquid friction
        if (inLiquid) {
            velocity.X *= Constants.liquidFriction;
            velocity.Z *= Constants.liquidFriction;
            velocity.Y *= Constants.liquidFriction;
            velocity.Y -= 0.25;
        }

        if (jumping && !wasInLiquid && inLiquid) {
            velocity.Y -= 2.5;
        }

        //Console.Out.WriteLine(level);
        if (jumping && (onGround || inLiquid)) {
            velocity.Y += inLiquid ? Constants.liquidSwimUpSpeed : Constants.jumpSpeed;

            // if on the edge of water, boost
            if (inLiquid && (collx || collz)) {
                velocity.Y += Constants.liquidSurfaceBoost;
            }

            onGround = false;
            jumping = false;
        }
    }

    protected void updateGravity(double dt) {
        // if in liquid, don't apply gravity
        if (inLiquid) {
            accel.Y = 0;
            return;
        }

        if (!onGround && !flyMode) {
            accel.Y = -Constants.gravity;
        }
        else {
            accel.Y = 0;
        }
    }
}