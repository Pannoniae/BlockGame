using System.Diagnostics.Contracts;
using System.Numerics;
using BlockGame.util;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace BlockGame;

public class Player {
    public const double height = 1.75;
    public const double width = 0.625;
    public const double eyeHeight = 1.6;
    public const double sneakingEyeHeight = 1.45;
    public const double feetCheckHeight = 0.05;

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

    public int waterPushTicks = 0;

    public bool collisionXThisFrame;
    public bool collisionZThisFrame;

    /// <summary>
    /// This number is lying to you.
    /// </summary>
    public double totalTraveled;
    public double prevTotalTraveled;

    public Vector3D<double> forward;

    public Vector3D<double> inputVector;

    public PlayerRenderer renderer;


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
        renderer = new PlayerRenderer(this);

        this.world = world;
        var f = camera.CalculateForwardVector();
        forward = new Vector3D<double>(f.X, f.Y, f.Z);
        calcAABB(ref aabb, position);
    }

    public void render(double dt, double interp) {
        renderer.render(dt, interp);
    }


    public void update(double dt) {
        collisionXThisFrame = false;
        collisionZThisFrame = false;
        updateInputVelocity(dt);
        velocity += accel * dt;
        //position += velocity * dt;
        clamp(dt);

        blockAtFeet = world.getBlock(feetPosition.toBlockPos());
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
        calcAABB(ref aabb, position);
        if (Math.Abs(velocity.withoutY().Length) > 0.0001 && onGround) {
            camera.bob = Math.Clamp((float)(velocity.Length / 4), 0, 1);
        }
        // after everything is done
        // calculate total traveled
        setPrevVars();
    }

    public void setPrevVars() {
        prevPosition = position;
        camera.prevBob = camera.bob;
        prevTotalTraveled = totalTraveled;
        wasInLiquid = inLiquid;
    }
    // before pausing, all vars need to be updated SO THERE IS NO FUCKING JITTER ON THE PAUSE MENU
    public void catchUpOnPrevVars() {
        setPrevVars();
        camera.prevPosition = camera.position;
    }

    public ChunkCoord getChunk(Vector3D<double> pos) {
        var blockPos = pos.toBlockPos();
        return World.getChunkPos(new Vector2D<int>(blockPos.X, blockPos.Z));
    }

    public ChunkCoord getChunk() {
        var blockPos = position.toBlockPos();
        return World.getChunkPos(new Vector2D<int>(blockPos.X, blockPos.Z));
    }

    public void loadChunksAroundThePlayer(int renderDistance) {
        var blockPos = position.toBlockPos();
        var chunk = World.getChunkPos(new Vector2D<int>(blockPos.X, blockPos.Z));
        world.loadChunksAroundChunk(chunk, renderDistance);
        world.sortChunks();
    }

    public void loadChunksAroundThePlayerLoading(int renderDistance) {
        var blockPos = position.toBlockPos();
        var chunk = World.getChunkPos(new Vector2D<int>(blockPos.X, blockPos.Z));
        world.loadChunksAroundChunkLoading(chunk, renderDistance);
        world.sortChunks();
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
            /*
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
            */
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
        if (!inLiquid) {
            var f2 = Constants.verticalFriction;
            if (onGround) {
                //if (sneaking) {
                //    velocity = Vector3D<double>.Zero;
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
            velocity.Y *= Constants.liquidFriction + 0.05;
        }

        var level = getWaterLevel();

        if (jumping && !wasInLiquid && inLiquid) {
            velocity.Y -= 1.4;
        }
        //Console.Out.WriteLine(level);
        if (jumping && (onGround || inLiquid)) {
            velocity.Y += inLiquid ? Constants.liquidSwimUpSpeed : Constants.jumpSpeed;

            // if on the edge of water, boost
            if (inLiquid && (collisionXThisFrame || collisionZThisFrame)) {
                velocity.Y += Constants.liquidSurfaceBoost;
            }
            onGround = false;
            jumping = false;
        }
    }


    /// <summary>
    /// Get how deep the player is in water. 0 if not in water, 1 if submerged
    /// </summary>
    private double getWaterLevel() {
        var feet = world.getBlock(feetPosition.toBlockPos());
        var torsoBlockPos = new Vector3D<double>(feetPosition.X, feetPosition.Y + 1, feetPosition.Z).toBlockPos();
        var torso = world.getBlock(torsoBlockPos);

        var feetLiquid = Blocks.get(feet).liquid;
        var torsoLiquid = Blocks.get(torso).liquid;

        // if no liquid at feet, don't bother
        if (!feetLiquid) {
            return 0;
        }
        // if submerged entirely, 1
        if (feetLiquid && torsoLiquid) {
            return 1;
        }
        // if completely dry, 0
        if (!feetLiquid && !torsoLiquid) {
            return 0;
        }
        // if feet has liquid but torso does not
        // get the difference between the torso block pos and the feet position
        return torsoBlockPos.Y - feetPosition.Y;
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
        if (world.paused || world.inMenu) {
            return;
        }
        // convert strafe vector into actual movement

        if (strafeVector.X != 0 || strafeVector.Y != 0) {
            // if air, lessen control
            Constants.moveSpeed = onGround ? Constants.groundMoveSpeed : Constants.airMoveSpeed;
            if (inLiquid) {
                Constants.moveSpeed = Constants.liquidMoveSpeed;
            }

            if (sneaking) {
                Constants.moveSpeed *= Constants.sneakFactor;
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

    private static void calcAABB(ref AABB aabb, Vector3D<double> pos) {
        const double sizehalf = width / 2;
        AABB.update(ref aabb, new Vector3D<double>(pos.X - sizehalf, pos.Y, pos.Z - sizehalf),
            new Vector3D<double>(width, height, width));
    }

    [Pure]
    private AABB calcAABB(Vector3D<double> pos) {
        var sizehalf = width / 2;
        return AABB.fromSize(new Vector3D<double>(pos.X - sizehalf, pos.Y, pos.Z - sizehalf),
            new Vector3D<double>(width, height, width));
    }

    private void collisionAndSneaking(double dt) {
        var oldPos = position;
        var blockPos = position.toBlockPos();
        // collect potential collision targets
        List<AABB> collisionTargets = [];
        ReadOnlySpan<Vector3D<int>> targets = [blockPos, new Vector3D<int>(blockPos.X, blockPos.Y + 1, blockPos.Z)];
        foreach (Vector3D<int> target in targets) {
            // first, collide with the block the player is in
            var blockPos2 = feetPosition.toBlockPos();
            var currentAABB = world.getAABB(blockPos2.X, blockPos2.Y, blockPos2.Z, world.getBlock(feetPosition.toBlockPos()));
            if (currentAABB != null) {
                collisionTargets.Add(currentAABB.Value);
            }
            foreach (var neighbour in world.getBlocksInBox(target + new Vector3D<int>(-1, -1, -1), target + new Vector3D<int>(1, 1, 1))) {
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
            var aabbY = calcAABB(new Vector3D<double>(position.X, position.Y, position.Z));
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
            var aabbX = calcAABB(new Vector3D<double>(position.X, position.Y, position.Z));
            var sneakaabbX = calcAABB(new Vector3D<double>(position.X, position.Y - 0.1, position.Z));
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
            var aabbZ = calcAABB(new Vector3D<double>(position.X, position.Y, position.Z));
            var sneakaabbZ = calcAABB(new Vector3D<double>(position.X, position.Y - 0.1, position.Z));
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
            if (!Blocks.tryGet(hotbar.getSelected().block, out _)) {
                hotbar.selected = 1;
            }
        }
    }

    public void updateInput(double dt) {
        if (world.inMenu) {
            return;
        }

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
            var bl = hotbar.getSelected().block;
            // don't intersect the player
            var blockAABB = world.getAABB(pos.X, pos.Y, pos.Z, bl);
            if (blockAABB == null || !AABB.isCollision(aabb, blockAABB.Value)) {
                world.setBlockRemesh(pos.X, pos.Y, pos.Z, bl);
                world.blockUpdateWithNeighbours(pos);
                lastPlace = world.worldTime;
            }
        }
    }

    public void breakBlock() {
        if (Game.instance.targetedPos.HasValue) {
            var pos = Game.instance.targetedPos.Value;
            world.setBlockRemesh(pos.X, pos.Y, pos.Z, 0);

            // we don't set it to anything, we just propagate from neighbours
            world.blockUpdateWithNeighbours(pos);
            // place water if adjacent
            lastBreak = world.worldTime;
        }
    }
}