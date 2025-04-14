using System.Diagnostics.Contracts;
using BlockGame.ui;
using BlockGame.util;
using Molten;
using Silk.NET.Input;
using Molten.DoublePrecision;

namespace BlockGame;

public class Player : Entity {
    public const double height = 1.75;
    public const double width = 0.625;
    public const double eyeHeight = 1.6;
    public const double sneakingEyeHeight = 1.45;
    public const double feetCheckHeight = 0.05;

    public PlayerCamera camera;

    public Vector3D inputVector;

    public PlayerRenderer renderer;


    public Inventory hotbar;

    public Vector3D strafeVector = new(0, 0, 0);
    public bool pressedMovementKey;

    public double lastPlace;
    public double lastBreak;

    /// <summary>
    /// Used for flymode
    /// </summary>
    public long spacePress;




    // positions are feet positions
    public Player(World world, int x, int y, int z) : base(world) {
        position = new Vector3D(x, y, z);
        prevPosition = position;
        hotbar = new Inventory();
        camera = new PlayerCamera(this, new Vector3D(x, (float)(y + eyeHeight), z), Vector3D.UnitZ * 1, Vector3D.UnitY,
            Constants.initialWidth, Constants.initialHeight);
        renderer = new PlayerRenderer(this);

        this.world = world;
        var f = camera.CalculateForwardVector();
        forward = new Vector3D(f.X, f.Y, f.Z);
        calcAABB(ref aabb, position);

        swingProgress = 0;
        prevSwingProgress = 0;
    }

    public void render(double dt, double interp) {
        renderer.render(dt, interp);
    }


    public void update(double dt) {
        collisionXThisFrame = false;
        collisionZThisFrame = false;

        // after everything is done
        // calculate total traveled
        setPrevVars();
        renderer.update(dt);
        updateSwing();

        updateInputVelocity(dt);
        velocity += accel * dt;
        //position += velocity * dt;
        clamp(dt);

        blockAtFeet = world.getBlock(feetPosition.toBlockPos());
        inLiquid = Block.liquid[blockAtFeet];


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
        totalTraveled += onGround ? (position.withoutY() - prevPosition.withoutY()).Length() * 2f : 0;

        feetPosition = new Vector3D(position.X, position.Y + feetCheckHeight, position.Z);

        var trueEyeHeight = sneaking ? sneakingEyeHeight : eyeHeight;
        camera.position = new Vector3D(position.X, (position.Y + trueEyeHeight), position.Z);
        camera.prevPosition = new Vector3D(prevPosition.X, (prevPosition.Y + trueEyeHeight),
            prevPosition.Z);
        var f = camera.CalculateForwardVector();
        forward = new Vector3D(f.X, f.Y, f.Z);
        calcAABB(ref aabb, position);
        if (Math.Abs(velocity.withoutY().Length()) > 0.0001 && onGround) {
            camera.bob = Math.Clamp((float)(velocity.Length() / 4), 0, 1);
        }
        else {
            camera.bob *= 0.9f;
        }
    }

    public void setPrevVars() {
        prevPosition = position;
        camera.prevBob = camera.bob;
        prevTotalTraveled = totalTraveled;
        wasInLiquid = inLiquid;
        prevSwingProgress = swingProgress;
        renderer.prevLower = renderer.lower;
    }
    // before pausing, all vars need to be updated SO THERE IS NO FUCKING JITTER ON THE PAUSE MENU
    public void catchUpOnPrevVars() {
        setPrevVars();
        camera.prevPosition = camera.position;
    }

    public void loadChunksAroundThePlayer(int renderDistance) {
        var blockPos = position.toBlockPos();
        var chunk = World.getChunkPos(new Vector2I(blockPos.X, blockPos.Z));
        world.loadChunksAroundChunk(chunk, renderDistance);
        world.sortChunks();
    }

    public void onChunkChanged() {
        //Console.Out.WriteLine("chunk changed");
        loadChunksAroundThePlayer(Settings.instance.renderDistance);
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
            velocity.Y *= Constants.liquidFriction + 0.05;
        }

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
        var torsoBlockPos = new Vector3D(feetPosition.X, feetPosition.Y + 1, feetPosition.Z).toBlockPos();
        var torso = world.getBlock(torsoBlockPos);

        var feetLiquid = Block.liquid[feet];
        var torsoLiquid = Block.liquid[torso];

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
        if (!onGround && !flyMode) {
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

        if (!flyMode) {
            if (strafeVector.X != 0 || strafeVector.Z != 0) {
                // if air, lessen control
                Constants.moveSpeed = onGround ? Constants.groundMoveSpeed : Constants.airMoveSpeed;
                if (inLiquid) {
                    Constants.moveSpeed = Constants.liquidMoveSpeed;
                }

                if (sneaking) {
                    Constants.moveSpeed *= Constants.sneakFactor;
                }

                // first, normalise (v / v.length) then multiply with movespeed
                strafeVector = Vector3D.Normalize(strafeVector) * Constants.moveSpeed;

                Vector3D moveVector = strafeVector.Z * forward +
                                              strafeVector.X *
                                              Vector3D.Normalize(Vector3D.Cross(Vector3D.UnitY, forward));


                moveVector.Y = 0;
                inputVector = new Vector3D(moveVector.X, 0, moveVector.Z);

            }
        }
        else {
            if (strafeVector.X != 0 || strafeVector.Y != 0 || strafeVector.Z != 0) {
                // if air, lessen control
                Constants.moveSpeed = Constants.airFlySpeed;

                // first, normalise (v / v.length) then multiply with movespeed
                strafeVector = Vector3D.Normalize(strafeVector) * Constants.moveSpeed;

                Vector3D moveVector = strafeVector.Z * forward +
                                              strafeVector.X *
                                              Vector3D.Normalize(Vector3D.Cross(Vector3D.UnitY, forward)) +
                                              strafeVector.Y * Vector3D.UnitY;

                inputVector = new Vector3D(moveVector.X, moveVector.Y, moveVector.Z);

            }
        }
    }

    protected void calcAABB(ref AABB aabb, Vector3D pos) {
        const double sizehalf = width / 2;
        AABB.update(ref aabb, new Vector3D(pos.X - sizehalf, pos.Y, pos.Z - sizehalf),
            new Vector3D(width, height, width));
    }

    [Pure]
    protected override AABB calcAABB(Vector3D pos) {
        var sizehalf = width / 2;
        return AABB.fromSize(new Vector3D(pos.X - sizehalf, pos.Y, pos.Z - sizehalf),
            new Vector3D(width, height, width));
    }

    public void updatePickBlock(IKeyboard keyboard, Key key, int scancode) {
        if (key >= Key.Number0 && key <= Key.Number9) {
            hotbar.selected = (ushort)(key - Key.Number0 - 1);
            if (!Block.tryGet(hotbar.getSelected().block, out _)) {
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


        if (keyboard.IsKeyPressed(Key.ShiftLeft)) {
            if (flyMode) {
                strafeVector.Y -= 0.8;
            }
            else {
                sneaking = true;
            }
        }
        else {
            sneaking = false;
        }

        if (keyboard.IsKeyPressed(Key.W)) {
            // Move forwards
            strafeVector.Z += 1;
            pressedMovementKey = true;
        }

        if (keyboard.IsKeyPressed(Key.S)) {
            //Move backwards
            strafeVector.Z -= 1;
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

        if (keyboard.IsKeyPressed(Key.Space)) {
            if ((onGround || inLiquid) && !flyMode) {
                jumping = true;
                pressedMovementKey = true;
            }
            if (flyMode) {
                strafeVector.Y += 0.8;
            }
        }

        if (mouse.IsButtonPressed(MouseButton.Left) && world.worldTick - lastBreak > Constants.breakDelay) {
            breakBlock();
        }

        if (mouse.IsButtonPressed(MouseButton.Right) && world.worldTick - lastPlace > Constants.placeDelay) {
            placeBlock();
        }
    }

    public void placeBlock() {
        if (Game.instance.previousPos.HasValue) {
            setSwinging(true);
            var pos = Game.instance.previousPos.Value;
            var bl = hotbar.getSelected().block;
            // don't intersect the player
            var blockAABB = world.getAABB(pos.X, pos.Y, pos.Z, bl);
            if (blockAABB == null || !AABB.isCollision(aabb, blockAABB.Value)) {
                world.setBlockRemesh(pos.X, pos.Y, pos.Z, bl);
                world.blockUpdateWithNeighbours(pos);
                lastPlace = world.worldTick;
            }
        }
        else {
            setSwinging(false);
        }
    }

    public void breakBlock() {
        if (Game.instance.targetedPos.HasValue) {
            setSwinging(true);
            var pos = Game.instance.targetedPos.Value;
            var bl = Block.get(world.getBlock(pos));
            bl.crack(world, pos.X, pos.Y, pos.Z);
            world.setBlockRemesh(pos.X, pos.Y, pos.Z, 0);
            // we don't set it to anything, we just propagate from neighbours
            world.blockUpdateWithNeighbours(pos);
            // place water if adjacent
            lastBreak = world.worldTick;
        }
        else {
            setSwinging(false);
        }
    }
}