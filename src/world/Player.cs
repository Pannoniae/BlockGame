using System.Diagnostics.Contracts;
using System.Numerics;
using BlockGame.main;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.chunk;
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

    public PlayerRenderer renderer;

    // sound stuff
    private double lastFootstepDistance = 0;
    private const double FOOTSTEP_DISTANCE = 3.0; // Distance between footstep sounds

    public SurvivalInventory survivalInventory;

    public Vector3D strafeVector = new(0, 0, 0);
    public bool pressedMovementKey;

    public double lastPlace;
    public double lastBreak;

    /// <summary>
    /// Used for flymode
    /// </summary>
    public long spacePress;

    private bool fastMode = false;

    // positions are feet positions
    public Player(World world, int x, int y, int z) : base(world) {
        position = new Vector3D(x, y, z);
        prevPosition = position;
        survivalInventory = new SurvivalInventory();
        renderer = new PlayerRenderer(this);

        this.world = world;
        rotation.Y = Game.camera.yaw;
        calcAABB(ref aabb, position);

        swingProgress = 0;
        prevSwingProgress = 0;
    }

    public void render(double dt, double interp) {
        Game.camera.updateFOV(isUnderWater(), dt);
        renderer.render(dt, interp);
    }


    public override void update(double dt) {
        collisionXThisFrame = false;
        collisionZThisFrame = false;

        // after everything is done
        // calculate total traveled
        setPrevVars();
        renderer.update(dt);
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

        // after movement applied, check the chunk the player is in
        var prevChunk = getChunk(prevPosition);
        var thisChunk = getChunk(position);
        if (prevChunk != thisChunk) {
            onChunkChanged();
        }


        // don't increment if flying
        totalTraveled += onGround ? (position.withoutY() - prevPosition.withoutY()).Length() * 1.5f : 0;

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
        
        // Sync rotation with camera
        rotation.Y = Game.camera.yaw;
        rotation.X = Game.camera.pitch;
        calcAABB(ref aabb, position);
    }

    public void setPrevVars() {
        prevPosition = position;
        Game.camera.prevBob = Game.camera.bob;
        Game.camera.prevpForward = Game.camera.pForward;
        prevTotalTraveled = totalTraveled;
        wasInLiquid = inLiquid;
        prevSwingProgress = swingProgress;
        renderer.prevLower = renderer.lower;
    }

    // before pausing, all vars need to be updated SO THERE IS NO FUCKING JITTER ON THE PAUSE MENU
    public void catchUpOnPrevVars() {
        setPrevVars();
        Game.camera.prevPosition = Game.camera.position;
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
        onChunkChanged();
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
            if (inLiquid && (collisionXThisFrame || collisionZThisFrame)) {
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

                if (fastMode) {
                    strafeVector *= 5;
                }

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
            survivalInventory.selected = (ushort)(key - Key.Number0 - 1);
        }
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

        fastMode = Game.inputs.ctrl.down();

        // TODO this horribly breaks when you speed time up, you become a terminator and be able to break/place blocks instantly
        // oh no it's a debug feature anyway but yk
        // repeated action while held (with delay to prevent spam)
        if (Game.inputs.left.pressed()) {
            breakBlock();
        }
        else {
            if (Game.inputs.left.down() && world.worldTick - lastBreak > Constants.breakDelay) {
                breakBlock();
            }
        }


        if (Game.inputs.right.pressed()) {
            placeBlock();
        }
        else {
            if (Game.inputs.right.down() && world.worldTick - lastPlace > Constants.placeDelay) {
                placeBlock();
            }
        }


        if (Game.inputs.middle.pressed()) {
            pickBlock();
        }
    }

    public void placeBlock() {
        lastPlace = world.worldTick;
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
        if (Game.instance.targetedPos.HasValue) {
            var pos = Game.instance.targetedPos.Value;
            var bl = Block.get(world.getBlock(pos));
            bl.crack(world, pos.X, pos.Y, pos.Z);
            world.setBlock(pos.X, pos.Y, pos.Z, 0);
            // we don't set it to anything, we just propagate from neighbours
            world.blockUpdateNeighbours(pos.X, pos.Y, pos.Z);
            // place water if adjacent
            lastBreak = world.worldTick;

            Game.snd.playBlockHit();
            setSwinging(true);
        }
        else {
            setSwinging(false);
        }
    }

    public RawDirection getFacing() {
        // Get the forward vector from the camera
        Vector3 forward = Game.camera.CalculateForwardVector();

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
}