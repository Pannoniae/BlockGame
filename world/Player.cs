using System.Diagnostics.Contracts;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace BlockGame;

public class Player {
    public const double playerHeight = 1.8;
    public const double eyeHeight = 1.6;
    public const double sneakingEyeHeight = 1.45;
    public const double feetCheckHeight = 0.15;

    // is player walking on (colling with) ground
    public bool onGround;

    // is the player in the process of jumping
    public bool jumping;

    // jump cooldown to prevent player jumping immediately again
    // which we don't have
    public double jumpCD;

    public bool sneaking;

    public PlayerCamera camera;

    public AABB aabb;

    // entity positions are at feet
    public Vector3D<double> prevPosition;
    public Vector3D<double> position;
    public Vector3D<double> velocity;
    public Vector3D<double> accel;

    // slightly above so it doesn't think it's under the player
    public Vector3D<double> feetPosition;

    // TODO implement some MovementState system so movement constants don't have to be duplicated...
    // it would store a set of values for acceleration, drag, friction, maxspeed, etc...

    public ushort blockAtFeet;
    public bool inLiquid;
    public bool wasInLiquid;

    /// <summary>
    /// This number is lying to you.
    /// </summary>
    public double totalTraveled;
    public double prevTotalTraveled;

    public Vector3D<double> forward;

    public Vector3D<double> inputVector;


    /// <summary>
    /// Used for transparent chunk sorting
    /// </summary>
    public Vector3D<double> lastSort = new(double.MinValue, double.MinValue, double.MinValue);


    public Inventory hotbar;
    public World world;
    public Vector2D<double> strafeVector = new(0, 0);
    public bool pressedMovementKey;

    public double lastPlace;
    public double lastBreak;


    // positions are feet positions
    public Player(World world, int x, int y, int z) {
        position = new Vector3D<double>(x, y, z);
        prevPosition = position;
        hotbar = new Inventory();
        camera = new PlayerCamera(this, new Vector3(x, (float)(y + eyeHeight), z), Vector3.UnitZ * 1, Vector3.UnitY,
            Constants.initialWidth, Constants.initialHeight);

        this.world = world;
        var f = camera.CalculateForwardVector();
        forward = new Vector3D<double>(f.X, f.Y, f.Z);
        aabb = calcAABB(position);
    }

    public void render(double dt, double interp) {

    }


    public void update(double dt) {
        updateInputVelocity(dt);
        velocity += accel * dt;
        //position += velocity * dt;
        clamp(dt);

        blockAtFeet = world.getBlock(feetPosition.As<int>());
        inLiquid = Blocks.get(blockAtFeet).liquid;


        collisionAndSneaking(dt);
        applyInputMovement(dt);
        updateGravity(dt);
        applyFriction();
        clamp(dt);

        // after movement applied, check the chunk the player is in
        var prevChunk = getChunk(prevPosition);
        var thisChunk = getChunk(position);
        if (prevChunk != thisChunk) {
            onChunkChanged();
        }

        // don't increment if flying
        totalTraveled += onGround ? (position.withoutY() - prevPosition.withoutY()).Length * 2f : 0;

        feetPosition = new Vector3D<double>(position.X, position.Y + feetCheckHeight, position.Z);

        var trueEyeHeight = sneaking ? sneakingEyeHeight : eyeHeight;
        camera.position = new Vector3((float)position.X, (float)(position.Y + trueEyeHeight), (float)position.Z);
        camera.prevPosition = new Vector3((float)prevPosition.X, (float)(prevPosition.Y + trueEyeHeight),
            (float)prevPosition.Z);
        var f = camera.CalculateForwardVector();
        forward = new Vector3D<double>(f.X, f.Y, f.Z);
        aabb = calcAABB(position);
        if (Math.Abs(velocity.withoutY().Length) > 0.0001 && onGround) {
            camera.bob = (float)(velocity.Length / Constants.maxhSpeed);
        }
        // after everything is done
        // calculate total traveled
        prevPosition = position;
        camera.prevBob = camera.bob;
        prevTotalTraveled = totalTraveled;
        wasInLiquid = inLiquid;
    }

    public ChunkCoord getChunk(Vector3D<double> position) {
        return world.getChunkPos(new Vector2D<int>((int)position.X, (int)position.Z));
    }

    public ChunkCoord getChunk() {
        return world.getChunkPos(new Vector2D<int>((int)position.X, (int)position.Z));
    }

    public void loadChunksAroundThePlayer(int renderDistance) {
        var chunk = world.getChunkPos(new Vector2D<int>((int)position.X, (int)position.Z));
        world.loadChunksAroundChunk(chunk, renderDistance);
        // sort queue based on position
        // don't reorder across statuses though
        world.chunkLoadQueue.Sort((ticket1, ticket2)
            => {
            var comparison = new ChunkCoordComparer(position).Compare(ticket1.chunkCoord, ticket2.chunkCoord);
            var statusDiff = ticket1.level - ticket2.level;
            return comparison + statusDiff * 1000;
        });
    }

    public void onChunkChanged() {
        //Console.Out.WriteLine("chunk changed");
        loadChunksAroundThePlayer(World.RENDERDISTANCE);
    }

    private void applyInputMovement(double dt) {
        velocity += inputVector;
    }

    private void resetFrameVars() {
        pressedMovementKey = false;
    }

    private void clamp(double dt) {
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

        if (velocity != Vector3D<double>.Zero) {
            // clamp max speed
            // If speed velocity is 0, we are fucked so check for that
            var hVel = new Vector3D<double>(velocity.X, 0, velocity.Z);
            double maxSpeed;
            if (inLiquid) {
                maxSpeed = Constants.maxhLiquidSpeed;
                if (sneaking) {
                    maxSpeed = Constants.maxhLiquidSpeedSneak;
                }
            }
            else {
                if (onGround) {
                    maxSpeed = Constants.maxhSpeed;
                    if (sneaking) {
                        maxSpeed = Constants.maxhSpeedSneak;
                    }
                }
                else {
                    maxSpeed = Constants.maxhAirSpeed;
                    if (sneaking) {
                        maxSpeed = Constants.maxhAirSpeedSneak;
                    }
                }
            }
            if (hVel.Length > maxSpeed) {
                var cappedVel = Vector3D.Normalize(hVel) * maxSpeed;
                velocity = new Vector3D<double>(cappedVel.X, velocity.Y, cappedVel.Z);
            }
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

    private void applyFriction() {
        // ground friction
        if (!pressedMovementKey && !inLiquid) {
            if (onGround) {
                if (sneaking) {
                    velocity = Vector3D<double>.Zero;
                }
                else {
                    var f = Constants.friction;
                    velocity *= f;
                }
            }
            else {
                var f = Constants.airFriction;
                velocity *= f;
            }
        }

        // liquid friction
        if (inLiquid) {
            velocity *= Constants.liquidFriction;
        }

        if (jumping && (onGround || inLiquid)) {
            velocity.Y = inLiquid ? Constants.liquidSwimUpSpeed : Constants.jumpSpeed;
            onGround = false;
            jumping = false;
        }

        // if just exiting the water, give a slight boost
        if (!inLiquid && wasInLiquid) {
            velocity.Y += Constants.liquidSurfaceBoost;
        }
    }

    private void updateGravity(double dt) {
        if (!onGround) {
            accel.Y = -Constants.gravity;
        }
        else {
            accel.Y = 0;
        }
    }

    private void updateInputVelocity(double dt) {
        if (!Game.focused) {
            return;
        }
        // convert strafe vector into actual movement

        if (strafeVector.X != 0 || strafeVector.Y != 0) {
            // if air, lessen control
            Constants.moveSpeed = onGround ? Constants.groundMoveSpeed : Constants.airMoveSpeed;
            if (inLiquid) {
                Constants.moveSpeed = Constants.liquidMoveSpeed;
            }

            // first, normalise (v / v.length) then multiply with movespeed
            strafeVector = Vector2D.Normalize(strafeVector) * Constants.moveSpeed;

            Vector3D<double> moveVector = strafeVector.Y * forward +
                                          strafeVector.X *
                                          Vector3D.Normalize(Vector3D.Cross(Vector3D<double>.UnitY, forward));


            moveVector.Y = 0;
            inputVector = new Vector3D<double>(moveVector.X, 0, moveVector.Z);

        }
    }

    [Pure]
    private AABB calcAABB(Vector3D<double> pos) {
        var size = 0.75;
        var sizehalf = 0.75 / 2;
        var height = 1.75;
        return AABB.fromSize(new Vector3D<double>(pos.X - sizehalf, pos.Y, pos.Z - sizehalf),
            new Vector3D<double>(size, height, size));
    }

    private void collisionAndSneaking(double dt) {
        var oldPos = position;
        var blockPos = world.toBlockPos(position);
        // collect potential collision targets
        List<AABB> collisionTargets = new List<AABB>();
        foreach (Vector3D<int> target in (Vector3D<int>[]) [blockPos, new Vector3D<int>(blockPos.X, blockPos.Y + 1, blockPos.Z)]) {
            foreach (var neighbour in world.getBlocksInBox(target + new Vector3D<int>(-1, -1, -1), target + new Vector3D<int>(1, 1, 1))) {
                var block = world.getBlock(neighbour);
                var blockAABB = world.getAABB(neighbour.X, neighbour.Y, neighbour.Z, block);
                if (blockAABB == null) {
                    continue;
                }

                collisionTargets.Add(blockAABB);
            }
        }

        // Y axis resolution
        position.Y += velocity.Y * dt;
        foreach (var blockAABB in collisionTargets) {
            var aabbY = calcAABB(new Vector3D<double>(position.X, position.Y, position.Z));
            if (AABB.isCollision(aabbY, blockAABB)) {
                // left side
                if (velocity.Y > 0 && aabbY.maxY >= blockAABB.minY) {
                    var diff = blockAABB.minY - aabbY.maxY;
                    if (diff < velocity.Y) {
                        position.Y += diff;
                        velocity.Y = 0;
                    }
                }

                else if (velocity.Y < 0 && aabbY.minY <= blockAABB.maxY) {
                    var diff = blockAABB.maxY - aabbY.minY;
                    if (diff > velocity.Y) {
                        position.Y += diff;
                        velocity.Y = 0;
                    }
                }
            }
        }


        // X axis resolution
        position.X += velocity.X * dt;
        var hasAtLeastOneCollision = false;
        foreach (var blockAABB in collisionTargets) {
            var aabbX = calcAABB(new Vector3D<double>(position.X, position.Y, position.Z));
            var sneakaabbX = calcAABB(new Vector3D<double>(position.X, position.Y - 0.1, position.Z));
            if (AABB.isCollision(aabbX, blockAABB)) {
                // left side
                if (velocity.X > 0 && aabbX.maxX >= blockAABB.minX) {
                    var diff = blockAABB.minX - aabbX.maxX;
                    if (diff < velocity.X) {
                        position.X += diff;
                    }
                }

                else if (velocity.X < 0 && aabbX.minX <= blockAABB.maxX) {
                    var diff = blockAABB.maxX - aabbX.minX;
                    if (diff > velocity.X) {
                        position.X += diff;
                    }
                }
            }
            if (AABB.isCollision(sneakaabbX, blockAABB)) {
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
            var aabbZ = calcAABB(new Vector3D<double>(position.X, position.Y, position.Z));
            var sneakaabbZ = calcAABB(new Vector3D<double>(position.X, position.Y - 0.1, position.Z));
            if (AABB.isCollision(aabbZ, blockAABB)) {
                if (velocity.Z > 0 && aabbZ.maxZ >= blockAABB.minZ) {
                    var diff = blockAABB.minZ - aabbZ.maxZ;
                    if (diff < velocity.Z) {
                        position.Z += diff;
                    }
                }

                else if (velocity.Z < 0 && aabbZ.minZ <= blockAABB.maxZ) {
                    var diff = blockAABB.maxZ - aabbZ.minZ;
                    if (diff > velocity.Z) {
                        position.Z += diff;
                    }
                }
            }
            if (AABB.isCollision(sneakaabbZ, blockAABB)) {
                hasAtLeastOneCollision = true;
            }
        }
        // don't fall off while sneaking
        if (sneaking && onGround && !hasAtLeastOneCollision) {
            // revert movement
            position.Z = oldPos.Z;
        }

        // is player on ground? check slightly below
        var groundCheck = calcAABB(new Vector3D<double>(position.X, position.Y - Constants.epsilonGroundCheck, position.Z));
        onGround = false;
        foreach (var blockAABB in collisionTargets) {
            if (AABB.isCollision(blockAABB, groundCheck)) {
                onGround = true;
            }
        }
    }

    public void updatePickBlock(IKeyboard keyboard, Key key, int scancode) {
        if (key >= Key.Number0 && key <= Key.Number9) {
            hotbar.selected = (ushort)(key - Key.Number0 - 1);
            if (!Blocks.tryGet(hotbar.getSelected(), out _)) {
                hotbar.selected = 1;
            }
        }
    }

    public void updateInput(double dt) {
        pressedMovementKey = false;
        var keyboard = Game.keyboard;
        var mouse = Game.mouse;

        sneaking = keyboard.IsKeyPressed(Key.ShiftLeft);

        if (keyboard.IsKeyPressed(Key.W)) {
            // Move forwards
            strafeVector.Y += 1;
            pressedMovementKey = true;
        }

        if (keyboard.IsKeyPressed(Key.S)) {
            //Move backwards
            strafeVector.Y -= 1;
            pressedMovementKey = true;
        }

        if (keyboard.IsKeyPressed(Key.A)) {
            //Move left
            strafeVector.X -= 1;
            pressedMovementKey = true;
        }

        if (keyboard.IsKeyPressed(Key.D)) {
            //Move right
            strafeVector.X += 1;
            pressedMovementKey = true;
        }

        if (keyboard.IsKeyPressed(Key.Space) && (onGround || inLiquid)) {
            jumping = true;
            pressedMovementKey = true;
        }

        if (mouse.IsButtonPressed(MouseButton.Left) && world.worldTime - lastBreak > Constants.breakDelay) {
            breakBlock();
        }

        if (mouse.IsButtonPressed(MouseButton.Right) && world.worldTime - lastPlace > Constants.placeDelay) {
            placeBlock();
        }
    }

    public void placeBlock() {
        if (Game.instance.previousPos.HasValue) {
            var pos = Game.instance.previousPos.Value;
            // don't intersect the player
            var aabb = world.getAABB(pos.X, pos.Y, pos.Z, world.player.hotbar.getSelected());
            if (aabb == null || !AABB.isCollision(world.player.aabb, aabb)) {
                world.setBlock(pos.X, pos.Y, pos.Z, world.player.hotbar.getSelected());
                world.blockUpdate(pos);
                world.player.lastPlace = world.worldTime;
            }
        }
    }

    public void breakBlock() {
        if (Game.instance.targetedPos.HasValue) {
            var pos = Game.instance.targetedPos.Value;
            world.setBlock(pos.X, pos.Y, pos.Z, 0);
            world.blockUpdate(pos);
            // place water if adjacent
            world.player.lastBreak = world.worldTime;
        }
    }
}