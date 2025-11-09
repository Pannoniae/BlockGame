using System.Diagnostics.Contracts;
using System.Numerics;
using BlockGame.main;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.chunk;
using BlockGame.world.entity;
using BlockGame.world.item;
using Molten;
using Molten.DoublePrecision;
using Silk.NET.Input;

namespace BlockGame.world;

public class Player : Entity {
    public const double height = 1.75;
    public const double width = 0.625;
    public const double eyeHeight = 1.6;
    public const double sneakingEyeHeight = 1.45;
    public const double feetCheckHeight = 0.05;

    public Vector3D inputVector;

    public PlayerHandRenderer handRenderer;
    public PlayerRenderer modelRenderer;

    // sound stuff
    private double lastFootstepDistance = 0;
    private const double FOOTSTEP_DISTANCE = 3.0; // Distance between footstep sounds

    public SurvivalInventory survivalInventory;

    public Vector3D strafeVector = new(0, 0, 0);
    public bool pressedMovementKey;

    public double lastMouseAction;
    public double lastAirHit;

    /// <summary>
    /// Used for flymode
    /// </summary>
    public long spacePress;

    private bool fastMode = false;

    public bool isBreaking;

    public Vector3I breaking;

    public double breakProgress;
    public double prevBreakProgress;

    public int breakTime;

    // positions are feet positions
    public Player(World world, int x, int y, int z) : base(world, Entities.PLAYER) {
        position = new Vector3D(x, y, z);
        prevPosition = position;
        survivalInventory = new SurvivalInventory();
        handRenderer = new PlayerHandRenderer(this);
        modelRenderer = new PlayerRenderer();

        this.world = world;
        rotation = new Vector3();
        calcAABB(ref aabb, position);

        swingProgress = 0;
        prevSwingProgress = 0;
    }

    public void render(double dt, double interp) {
        Game.camera.updateFOV(isUnderWater(), dt);

        if (Game.camera.mode == CameraMode.FirstPerson) {
            handRenderer.render(interp);
        }
    }


    public override void update(double dt) {
        collx = false;
        collz = false;

        // after everything is done
        // calculate total traveled
        setPrevVars();
        handRenderer.update(dt);
        updateSwing();

        interactBlock(dt);

        updateInputVelocity(dt);
        applyInputMovement(dt);

        //Console.Out.WriteLine(inLiquid);

        updateGravity(dt);

        velocity += accel * dt;
        //position += velocity * dt;
        clamp(dt);

        blockAtFeet = world.getBlock(feetPosition.toBlockPos());
        //inLiquid = Block.liquid[blockAtFeet];

        collide(dt);

        applyFriction();
        clamp(dt);


        // don't increment if flying
        totalTraveled += onGround ? (position.withoutY() - prevPosition.withoutY()).Length() * 2f : 0;

        // Play footstep sounds when moving on ground
        if (onGround && Math.Abs(velocity.withoutY().Length()) > 0.05 && !inLiquid) {
            if (totalTraveled - lastFootstepDistance > FOOTSTEP_DISTANCE) {
                Game.snd.playFootstep();
                lastFootstepDistance = totalTraveled;
            }
        }

        feetPosition = new Vector3D(position.X, position.Y + feetCheckHeight, position.Z);

        // Update the camera system
        Game.camera.updatePosition(dt);
        calcAABB(ref aabb, position);

        // update animation state
        var vel = velocity.withoutY();


        aspeed = (float)vel.Length() * 0.3f;
        // cap aspeed!
        aspeed = Meth.clamp(aspeed, 0f, 1f);

        bool isMoving = aspeed > 0f;
        if (isMoving) {
            apos += aspeed * (float)dt;
        }

        // item pickup logic

        // print number of entities
        /*Console.Out.WriteLine("w: " + world.entities.Count);
        var tc = world.getSubChunk(position.toBlockPos().X, position.toBlockPos().Y, position.toBlockPos().Z);
        Console.Out.WriteLine("c: " + tc.chunk.entities[tc.coord.y].Count);
        foreach (var e in tc.chunk.entities[tc.coord.y]) {
            Console.Out.WriteLine(" - " + e.id + " at " + e.position + "type: " + e.GetType());
        }*/

        pickup();
    }

    public void setPrevVars() {
        prevPosition = position;
        prevRotation = rotation;
        prevBodyRotation = bodyRotation;
        Game.camera.prevBob = Game.camera.bob;
        prevTotalTraveled = totalTraveled;
        wasInLiquid = inLiquid;
        prevSwingProgress = swingProgress;
        prevBreakProgress = breakProgress;
        handRenderer.prevLower = handRenderer.lower;
        papos = apos;
        paspeed = aspeed;
    }

    // before pausing, all vars need to be updated SO THERE IS NO FUCKING JITTER ON THE PAUSE MENU
    public void catchUpOnPrevVars() {
        setPrevVars();
        // camera position is now computed from player state, no need to sync
    }

    public void loadChunksAroundThePlayer(int renderDistance) {
        var blockPos = position.toBlockPos();
        var chunk = World.getChunkPos(new Vector2I(blockPos.X, blockPos.Z));
        world.loadChunksAroundChunk(chunk, renderDistance);
        world.sortChunks();
    }

    public void loadChunksAroundThePlayer(int renderDistance, ChunkStatus status) {
        var blockPos = position.toBlockPos();
        var chunk = World.getChunkPos(new Vector2I(blockPos.X, blockPos.Z));
        world.loadChunksAroundChunk(chunk, renderDistance, status);
        world.sortChunks();
    }

    public override void teleport(Vector3D pos) {
        base.teleport(pos);
        //onChunkChanged();
    }

    public override void onChunkChanged() {
        base.onChunkChanged();
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

    private void updateGravity(double dt) {
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

                Vector3D moveVector = strafeVector.Z * hfacing +
                                      strafeVector.X *
                                      Vector3D.Normalize(Vector3D.Cross(Vector3D.UnitY, hfacing));


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

                if (fastMode) {
                    strafeVector *= 5;
                }

                Vector3D moveVector = strafeVector.Z * hfacing +
                                      strafeVector.X *
                                      Vector3D.Normalize(Vector3D.Cross(Vector3D.UnitY, hfacing)) +
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
    public override AABB calcAABB(Vector3D pos) {
        var sizehalf = width / 2;
        return AABB.fromSize(new Vector3D(pos.X - sizehalf, pos.Y, pos.Z - sizehalf),
            new Vector3D(width, height, width));
    }

    public void updatePickBlock(IKeyboard keyboard, Key key, int scancode) {
        if (key is < Key.Number0 or > Key.Number9)
            return;

        // 0 should map to the 10th slot since it comes after 9 on a keyboard
        if (key == Key.Number0) {
            survivalInventory.selected = 9;
            return;
        }

        // [1-9]
        survivalInventory.selected = (ushort)Meth.mod(key - Key.Number1, SurvivalInventory.HOTBAR_SIZE);
    }

    public void dropSelectedItem(bool dropEntireStack) {
        var selectedStack = survivalInventory.getSelected();
        if (dropEntireStack)
            selectedStack.dropAll(world, position);
        else
            selectedStack.drop(world, position);
    }

    public void handleMouseInput(float xOffset, float yOffset) {
        // why did the sign get inverted? I DUNNO TBH
        rotation.Y += xOffset; // yaw
        rotation.X -= yOffset * Settings.instance.mouseInv; // pitch

        // clamp pitch to prevent looking behind by going over head or under feet
        rotation.X = Math.Clamp(rotation.X, -Constants.maxPitch, Constants.maxPitch);

        // update body rotation - follows yaw but not pitch (stays level and doesn't bend)
        bodyRotation.Y = rotation.Y;
        bodyRotation.Z = rotation.Z;
        // bodyRotation.X stays 0! (no pitch)
    }

    public void updateInput(double dt) {
        if (world.inMenu) {
            return;
        }

        pressedMovementKey = false;

        if (Game.inputs.shift.down()) {
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

        if (Game.inputs.w.down()) {
            // Move forwards
            strafeVector.Z += 1;
            pressedMovementKey = true;
        }

        if (Game.inputs.s.down()) {
            //Move backwards
            strafeVector.Z -= 1;
            pressedMovementKey = true;
        }

        if (Game.inputs.a.down()) {
            //Move left
            strafeVector.X -= 1;
            pressedMovementKey = true;
        }

        if (Game.inputs.d.down()) {
            //Move right
            strafeVector.X += 1;
            pressedMovementKey = true;
        }

        if (Game.inputs.space.down()) {
            if ((onGround || inLiquid) && !flyMode) {
                jumping = true;
                pressedMovementKey = true;
            }

            if (flyMode) {
                strafeVector.Y += 0.8;
            }
        }

        // if mouse up, stop breaking
        if (!Game.inputs.left.down() && isBreaking) {
            isBreaking = false;
            breakProgress = 0;
            prevBreakProgress = 0;
        }

        fastMode = Game.inputs.ctrl.down();

        var now = Game.permanentStopwatch.ElapsedMilliseconds;

        // repeated action while held (with delay to prevent spam)
        if (Game.inputs.left.pressed()) {
            if (now - lastAirHit > Constants.airHitDelayMs) {
                breakBlock();
                //lastMouseAction = now;
                if (!Game.instance.targetedPos.HasValue) {
                    lastAirHit = now;
                }
            }
        }
        else {
            if (Game.inputs.left.down() &&
                now - lastAirHit > Constants.airHitDelayMs) {
                breakBlock();
                //lastMouseAction = now;
                if (!Game.instance.targetedPos.HasValue) {
                    lastAirHit = now;
                }
            }
        }

        if (Game.inputs.right.pressed()) {
            if (now - lastMouseAction > Constants.breakMissDelayMs && now - lastAirHit > Constants.airHitDelayMs) {
                placeBlock();
                lastMouseAction = now;
                if (!Game.instance.previousPos.HasValue) {
                    lastAirHit = now;
                }
            }
        }
        else {
            if (Game.inputs.right.down() && now - lastMouseAction > Constants.placeDelayMs &&
                now - lastAirHit > Constants.airHitDelayMs) {
                placeBlock();
                lastMouseAction = now;
                if (!Game.instance.previousPos.HasValue) {
                    lastAirHit = now;
                }
            }
        }

        if (Game.inputs.middle.pressed()) {
            if (now - lastMouseAction > Constants.breakMissDelayMs && now - lastAirHit > Constants.airHitDelayMs) {
                pickBlock();
                lastMouseAction = now;
                if (!Game.instance.targetedPos.HasValue) {
                    lastAirHit = now;
                }
            }
        }
    }

    public void getMiningSpeed(Block block) {
    }

    //public override void interactBlock(double dt) {
    //    base.interactBlock(dt);
    //}

    public void blockHandling(double dt) {
        // handle block breaking progress
        if (isBreaking && Game.instance.targetedPos.HasValue) {
            var pos = Game.instance.targetedPos.Value;


            // check if we're still looking at the same block
            if (pos != breaking) {
                // switched targets, reset progress
                isBreaking = false;
                breakProgress = 0;
                return;
            }

            var block = Block.get(world.getBlock(pos));
            if (block == null || block.id == 0) {
                // block no longer exists
                isBreaking = false;
                breakProgress = 0;
                return;
            }

            // breaking logic
            var hardness = Block.hardness[block.id];
            var heldItem = survivalInventory.getSelected().getItem();
            var toolBreakSpeed = heldItem.getBreakSpeed(survivalInventory.getSelected(), block);
            var breakSpeed = toolBreakSpeed / hardness;

            prevBreakProgress = breakProgress;
            breakProgress += breakSpeed * dt;

            if (breakProgress >= 1.0) {
                // block is fully broken
                block.shatter(world, pos.X, pos.Y, pos.Z);

                // get block drop and spawn item entity in survival mode
                var (dropItem, dropCount) = block.getDrop(world, pos.X, pos.Y, pos.Z, 0);
                if (dropCount > 0) {
                    var itemEntity = ItemEntity.create(world,  new Vector3D(pos.X, pos.Y, pos.Z) + new Vector3D(0.5), dropItem, dropCount);
                    // add to world (chunk will add it to its entity list)
                    world.addEntity(itemEntity);
                }

                world.setBlock(pos.X, pos.Y, pos.Z, 0);
                world.blockUpdateNeighbours(pos.X, pos.Y, pos.Z);
                Game.snd.playBlockHit();

                isBreaking = false;
                breakProgress = 0;
                prevBreakProgress = 0;
            }
        }
        else {
            // not breaking, reset progress
            if (isBreaking) {
                isBreaking = false;
                breakProgress = 0;
                prevBreakProgress = 0;
            }
        }
    }

    public void placeBlock() {
        if (Game.instance.previousPos.HasValue) {
            var pos = Game.instance.previousPos.Value;
            var stack = survivalInventory.getSelected();

            var metadata = (byte)stack.metadata;

            // if item, fire the hook (WHICH DOESN'T EXIST YET LOL)
            stack.getItem().useBlock(stack, world, this, pos.X, pos.Y, pos.Z, getFacing());

            // if block, place it
            if (stack.getItem().isBlock()) {
                var block = Block.get(stack.getItem().getBlockID());

                RawDirection dir = getFacing();

                // check block-specific placement rules first
                if (!block.canPlace(world, pos.X, pos.Y, pos.Z, dir)) {
                    setSwinging(false);
                    return;
                }

                // check entity collisions with the proposed block placement
                bool hasCollisions = false;

                // don't intersect the player with already placed block
                world.getAABBsCollision(AABBList, pos.X, pos.Y, pos.Z);
                foreach (AABB aabb in AABBList) {
                    if (AABB.isCollision(aabb, this.aabb)) {
                        hasCollisions = true;
                        break;
                    }
                }

                // check entity collisions with the new block's bounding boxes
                if (!hasCollisions && Block.collision[block.id]) {
                    var entities = new List<Entity>();
                    block.getAABBs(world, pos.X, pos.Y, pos.Z, metadata, AABBList);

                    foreach (var aabb in AABBList) {
                        entities.Clear();
                        world.getEntitiesInBox(entities, aabb.min.toBlockPos(), aabb.max.toBlockPos() + 1);

                        foreach (var entity in entities) {
                            if (AABB.isCollision(aabb, entity.aabb)) {
                                hasCollisions = true;
                                break;
                            }
                        }

                        if (hasCollisions) break;
                    }
                }

                if (!hasCollisions) {
                    block.place(world, pos.X, pos.Y, pos.Z, metadata, dir);
                    setSwinging(true);
                }
                else {
                    setSwinging(false);
                }
            }
        }
        else {
            var stack = survivalInventory.getSelected();
            stack.getItem().use(stack, world, this);
            setSwinging(false);
        }
    }

    public void breakBlock() {

        var now = Game.permanentStopwatch.ElapsedMilliseconds;

        if (Game.instance.targetedPos.HasValue) {
            var pos = Game.instance.targetedPos.Value;

            // instabreak
            // delay because we'll end up breaking blocks too fast otherwise
            if (!Game.gamemode.gameplay && now - lastMouseAction > Constants.breakDelayMs) {
                var block = Block.get(world.getBlock(pos));
                if (block != null && block.id != 0) {
                    block.shatter(world, pos.X, pos.Y, pos.Z);
                    world.setBlock(pos.X, pos.Y, pos.Z, 0);
                    world.blockUpdateNeighbours(pos.X, pos.Y, pos.Z);
                    Game.snd.playBlockHit();
                }

                setSwinging(true);
                lastMouseAction = now;
                return;
            }

            if (Game.gamemode.gameplay) {
                // survival mode - start breaking process
                if (!isBreaking || breaking != pos) {
                    isBreaking = true;
                    breaking = pos;
                    breakProgress = 0;
                    prevBreakProgress = 0;
                }
            }

            // don't swing too fast!
            if (now - lastMouseAction > Constants.breakDelayMs) {
                setSwinging(true);
                lastMouseAction = now;
            }
        }
        else {
            setSwinging(false);
        }
    }

    public RawDirection getFacing() {
        // Get the forward vector from the camera
        Vector3 forward = facing();

        double verticalThreshold = Math.Cos(Math.PI / 4f); // 45 degrees in radians

        // Check for up/down first
        if (forward.Y > verticalThreshold) {
            return RawDirection.UP;
        }

        if (forward.Y < -verticalThreshold) {
            return RawDirection.DOWN;
        }

        // Determine the facing direction based on the forward vector
        if (Math.Abs(forward.X) > Math.Abs(forward.Z)) {
            return forward.X > 0 ? RawDirection.EAST : RawDirection.WEST;
        }
        else {
            return forward.Z > 0 ? RawDirection.NORTH : RawDirection.SOUTH;
        }
    }

    public bool isUnderWater() {
        // If not in liquid at all, definitely not underwater
        if (!inLiquid) {
            //return false;
        }

        // Calculate eye position based on sneaking state
        double currentEyeHeight = sneaking ? sneakingEyeHeight : eyeHeight;
        Vector3D eyePosition = new Vector3D(position.X, position.Y + currentEyeHeight, position.Z);

        // Check if the block at eye position is liquid
        Vector3I eyeBlockPos = eyePosition.toBlockPos();
        ushort blockAtEyes = world.getBlock(eyeBlockPos);

        // Return true if eyes are in liquid
        return Block.liquid[blockAtEyes];
    }

    public void pickBlock() {
        if (Game.instance.targetedPos.HasValue) {
            var pos = Game.instance.targetedPos.Value;
            var bl = Block.get(world.getBlock(pos));
            if (bl != null) {
                survivalInventory.slots[survivalInventory.selected] = new ItemStack(Item.blockID(bl.id), 1);
            }
        }
    }

    private void pickup() {
        // get nearby entities
        var entities = new List<Entity>();
        const int PICKUP_RADIUS = 2;
        var min = position.toBlockPos() - new Vector3I(PICKUP_RADIUS);
        var max = position.toBlockPos() + new Vector3I(PICKUP_RADIUS);
        world.getEntitiesInBox(entities, min, max);

        // try to pickup any ItemEntities
        foreach (var entity in entities) {
            if (entity is ItemEntity itemEntity && Vector3D.Distance(entity.position, position) <= PICKUP_RADIUS) {
                itemEntity.pickup(this);
            }
        }
    }
}
