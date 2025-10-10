using System.Diagnostics.Contracts;
using System.Numerics;
using BlockGame.main;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.util.xNBT;
using BlockGame.world.block;
using BlockGame.world.chunk;
using BlockGame.world.entity;
using BlockGame.world.item;
using BlockGame.world.item.inventory;
using Molten;
using Molten.DoublePrecision;
using Silk.NET.Input;

namespace BlockGame.world;

public class Player : Soul {
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

    // fall damage tracking
    private bool wasInAir = false;
    private const double SAFE_FALL_SPEED = 13.0; // velocity threshold for damage
    private const double FALL_DAMAGE_MULTIPLIER = 2.0; // damage per unit velocity over threshold

    public PlayerInventory inventory;

    /**
     * The player's inventory (survival or creative)
     */
    public InventoryContext inventoryCtx;
    /**
     * Whatever's currently open (could be chest, workbench, etc.), or just the player's inventory if none
     */
    public InventoryContext currentCtx;

    public Vector3D strafeVector = new(0, 0, 0);

    // body rotation constants
    private const double IDLE_VELOCITY_THRESHOLD = 0.05;
    private const float BODY_ROTATION_SNAP = 45f; // degrees
    private const float ROTATION_SPEED = 1.8f; // deg/s when moving

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
        inventory = new PlayerInventory();
        handRenderer = new PlayerHandRenderer(this);
        modelRenderer = new PlayerRenderer();

        inventoryCtx = new CreativeInventoryContext(40);

        this.world = world;
        rotation = new Vector3();
        bodyRotation = rotation;  // initialise body rotation to match head
        prevBodyRotation = rotation;
        calcAABB(ref aabb, position);

        swingProgress = 0;
        prevSwingProgress = 0;
    }

    protected override void readx(NBTCompound data) {
        if (data.has("inv")) {
            var invData = data.getCompoundTag("inv");

            // read slots
            if (invData.has("slots")) {
                var slotsList = invData.getListTag<NBTCompound>("slots");
                for (int i = 0; i < slotsList.count() && i < inventory.slots.Length; i++) {
                    inventory.slots[i] = ItemStack.fromTag(slotsList.get(i));
                }
            }

            // read armour
            if (invData.has("armour")) {
                var armourList = invData.getListTag<NBTCompound>("armour");
                for (int i = 0; i < armourList.count() && i < inventory.armour.Length; i++) {
                    inventory.armour[i] = ItemStack.fromTag(armourList.get(i));
                }
            }

            // read accessories
            if (invData.has("accessories")) {
                var accList = invData.getListTag<NBTCompound>("accessories");
                for (int i = 0; i < accList.count() && i < inventory.accessories.Length; i++) {
                    inventory.accessories[i] = ItemStack.fromTag(accList.get(i));
                }
            }
        }
    }

    public override void writex(NBTCompound data) {
        var invData = new NBTCompound("inv");

        // write slots
        var slotsList = new NBTList<NBTCompound>(NBTType.TAG_Compound, "slots");
        foreach (var stack in inventory.slots) {
            var stackData = new NBTCompound();
            stack.write(stackData);
            slotsList.add(stackData);
        }
        invData.add(slotsList);

        // write armour
        var armourList = new NBTList<NBTCompound>(NBTType.TAG_Compound, "armour");
        foreach (var stack in inventory.armour) {
            var stackData = new NBTCompound();
            stack.write(stackData);
            armourList.add(stackData);
        }
        invData.add(armourList);

        // write accessories
        var accList = new NBTList<NBTCompound>(NBTType.TAG_Compound, "accessories");
        foreach (var stack in inventory.accessories) {
            var stackData = new NBTCompound();
            stack.write(stackData);
            accList.add(stackData);
        }
        invData.add(accList);

        data.add(invData);
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

        // fall damage check
        if (Game.gamemode.gameplay && onGround && wasInAir && !flyMode && !inLiquid) {
            var fallSpeed = -prevVelocity.Y;
            if (fallSpeed > SAFE_FALL_SPEED) {
                var dmg = (float)((fallSpeed - SAFE_FALL_SPEED) * FALL_DAMAGE_MULTIPLIER);
                hp -= dmg;
                Game.camera.applyImpact(dmg);
            }
        }
        wasInAir = !onGround && !flyMode;

        applyFriction();
        clamp(dt);

        updateBodyRotation(dt);

        // don't increment if flying
        totalTraveled += onGround ? (position.withoutY() - prevPosition.withoutY()).Length() * 2f : 0;

        // Play footstep sounds when moving on ground
        if (onGround && Math.Abs(velocity.withoutY().Length()) > 0.05 && !inLiquid) {
            if (totalTraveled - lastFootstepDistance > FOOTSTEP_DISTANCE) {
                // get block below player
                var pos = position.toBlockPos() + new Vector3I(0, -1, 0);
                var blockBelow = Block.get(world.getBlock(pos));
                if (blockBelow?.mat != null) {
                    Game.snd.playFootstep(blockBelow.mat.smat);
                }
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

        // decay shiny values
        for (int i = 0; i < inventory.shiny.Length; i++) {
            if (inventory.shiny[i] > 0) {
                inventory.shiny[i] -= (float)(dt * 4.0); // historically 6, 3, probably settle at 4?
                if (inventory.shiny[i] < 0) {
                    inventory.shiny[i] = 0;
                }
            }
        }
    }

    public void setPrevVars() {
        prevPosition = position;
        prevVelocity = velocity;
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
        if (key >= Key.Number0 && key <= Key.Number9) {
            inventory.selected = key > Key.Number0 ? (ushort)(key - Key.Number0 - 1) : 9;
        }
    }

    public void handleMouseInput(float xOffset, float yOffset) {
        // why did the sign get inverted? I DUNNO TBH
        rotation.Y += xOffset; // yaw
        rotation.X -= yOffset * Settings.instance.mouseInv; // pitch

        // clamp pitch to prevent looking behind by going over head or under feet
        rotation.X = Math.Clamp(rotation.X, -Constants.maxPitch, Constants.maxPitch);

        // body rotation is updated separately in updateBodyRotation() based on movement
    }

    public void updateInput(double dt) {
        if (world.inMenu) {
            return;
        }

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
        }

        if (Game.inputs.s.down()) {
            //Move backwards
            strafeVector.Z -= 1;
        }

        if (Game.inputs.a.down()) {
            //Move left
            strafeVector.X -= 1;
        }

        if (Game.inputs.d.down()) {
            //Move right
            strafeVector.X += 1;
        }

        if (Game.inputs.space.down()) {
            if ((onGround || inLiquid) && !flyMode) {
                jumping = true;
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
            breakTime = 0;
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

        if (Game.inputs.q.pressed()) {
            dropItem();
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
                breakTime = 0;
                return;
            }

            var val = world.getBlockRaw(pos);
            var block = Block.get(val.getID());
            if (block == null || block.id == 0) {
                // block no longer exists
                isBreaking = false;
                breakProgress = 0;
                breakTime = 0;
                return;
            }

            // breaking logic
            var hardness = Block.hardness[block.id];
            var heldItem = inventory.getSelected().getItem();
            var toolBreakSpeed = heldItem.getBreakSpeed(inventory.getSelected(), block);
            var breakSpeed = toolBreakSpeed / hardness;

            prevBreakProgress = breakProgress;
            breakProgress += breakSpeed * dt;

            // spawn mining particles every 4 ticks
            if (breakTime % 4 == 0) {
                block.shatter(world, pos.X, pos.Y, pos.Z, Game.raycast.face);
            }
            if ((breakTime / 4f) % 4 == 0) {
                Game.snd.playBlockKnock(block.mat.smat);
            }
            breakTime++;

            if (breakProgress >= 1.0) {
                // block is fully broken
                block.shatter(world, pos.X, pos.Y, pos.Z);

                // get block drop and spawn item entity in survival mode
                var metadata = val.getMetadata();
                var (dropItem, meta, dropCount) = block.getDrop(world, pos.X, pos.Y, pos.Z, metadata);
                    if (dropCount > 0) {
                    var itemEntity = new ItemEntity(world);
                    itemEntity.stack = new ItemStack(dropItem, dropCount, metadata);
                    itemEntity.position = new Vector3D(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5);

                    // add some random velocity
                    var random = Game.clientRandom;
                    itemEntity.velocity = new Vector3D(
                        (random.NextSingle() - 0.5) * 0.3,
                        random.NextSingle() * 0.3 + 0.1,
                        (random.NextSingle() - 0.5) * 0.3
                    );

                    // add to world (chunk will add it to its entity list)
                    world.addEntity(itemEntity);
                }

                world.setBlock(pos.X, pos.Y, pos.Z, 0);
                world.blockUpdateNeighbours(pos.X, pos.Y, pos.Z);
                if (block.mat != null) {
                    Game.snd.playBlockBreak(block.mat.smat);
                }

                isBreaking = false;
                breakProgress = 0;
                prevBreakProgress = 0;
                breakTime = 0;
            }
        }
        else {
            // not breaking, reset progress
            if (isBreaking) {
                isBreaking = false;
                breakProgress = 0;
                prevBreakProgress = 0;
                breakTime = 0;
            }
        }
    }

    public void placeBlock() {
        if (Game.instance.previousPos.HasValue) {
            var pos = Game.instance.previousPos.Value;
            var stack = inventory.getSelected();

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
                            if (Entities.blocksPlacement[entity.type] && AABB.isCollision(aabb, entity.aabb)) {
                                hasCollisions = true;
                                break;
                            }
                        }

                        if (hasCollisions) break;
                    }
                }

                if (!hasCollisions) {
                    // in survival mode, check if player has blocks to place
                    if (Game.gamemode.gameplay) {
                        if (stack == ItemStack.EMPTY || stack.quantity <= 0) {
                            setSwinging(false);
                            return;
                        }
                    }

                    block.place(world, pos.X, pos.Y, pos.Z, metadata, dir);

                    // consume block from inventory in survival mode
                    if (Game.gamemode.gameplay) {
                        inventory.removeStack(inventory.selected, 1);
                    }

                    setSwinging(true);
                }
                else {
                    setSwinging(false);
                }
            }
        }
        else {
            var stack = inventory.getSelected();
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
                    if (block.mat != null) {
                        Game.snd.playBlockBreak(block.mat.smat);
                    }
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
                    breakTime = 0;
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
                inventory.slots[inventory.selected] = new ItemStack(Item.blockID(bl.id), 1);
            }
        }
    }

    public Vector3D nomnomPos() {
        return new Vector3D(position.X, position.Y + eyeHeight - 0.4, position.Z);
    }

    private void pickup() {
        // get nearby entities
        var entities = new List<Entity>();
        var min = position.toBlockPos() - new Vector3I(2, 2, 2);
        var max = position.toBlockPos() + new Vector3I(2, 2, 2);
        world.getEntitiesInBox(entities, min, max);

        // try to pickup any ItemEntities
        foreach (var entity in entities) {
            if (entity is ItemEntity itemEntity) {
                itemEntity.pickup(this);
            }
        }

        // todo play popcat sound!!
    }

    /**
     * Updates body rotation to face movement direction with smooth interpolation
     * Freshly made claudeslop
     */
    private void updateBodyRotation(double dt) {
        var input = strafeVector.withoutY();
        var inputLength = input.Length();

        bool moving = inputLength > IDLE_VELOCITY_THRESHOLD;
        bool swinging = swingProgress > 0;

        float targetYaw;
        float rotSpeed;

        if (swinging) {
            targetYaw = rotation.Y;
            rotSpeed = 9.6f;
        } else if (moving) {
            // Body faces movement direction relative to head
            float inputAngle = Meth.rad2deg((float)Math.Atan2(-input.X, input.Z));
            targetYaw = rotation.Y + inputAngle;
            rotSpeed = ROTATION_SPEED * 2;
        } else {
            // Idle - face head
            targetYaw = bodyRotation.Y;
            rotSpeed = ROTATION_SPEED * 2;
        }

        // Rotate towards target yaw
        //float targetDiff = Meth.angleDiff(targetYaw, rotation.Y);
        bodyRotation.Y = Meth.lerpAngle(bodyRotation.Y, targetYaw, rotSpeed * (float)dt);

        // Rotate if outside deadzone
        float angleDiff = Meth.angleDiff(bodyRotation.Y, rotation.Y);

        // hardcap at 70 degrees
        if (angleDiff is > 70 or < -70) {
            bodyRotation.Y = rotation.Y - float.CopySign(70, angleDiff);
        }

        //Console.Out.WriteLine(angleDiff);
        var a = Math.Abs(angleDiff);
        if (a > BODY_ROTATION_SNAP) {
            // idk why the number 2 is good here but here it is!
            bodyRotation.Y = Meth.lerpAngle(bodyRotation.Y, rotation.Y, rotSpeed * 0.6f * (float)dt * (a / BODY_ROTATION_SNAP));
        }

        //Console.Out.WriteLine(bodyRotation.Y);

        bodyRotation.X = 0;
        bodyRotation.Z = 0;

        // clamp angles
        bodyRotation = Meth.clampAngle(bodyRotation);
        rotation = Meth.clampAngle(rotation);
    }

    public void dropItem() {
        var stack = inventory.getSelected();
        if (stack == ItemStack.EMPTY || stack.quantity <= 0) {
            return;
        }

        // remove 1 item from stack
        var droppedStack = inventory.removeStack(inventory.selected, 1);

        // create item entity
        var itemEntity = new ItemEntity(world);
        itemEntity.stack = droppedStack;

        // add plot armour!! otherwise we'll just pick it up immediately LOL
        itemEntity.plotArmour = 144;

        // position at eye height slightly in front of player
        var eyePos = new Vector3D(position.X, position.Y + eyeHeight, position.Z);
        var forward = camFacing();
        itemEntity.position = eyePos + forward.toVec3D() * 0.5;

        // give it velocity in the direction player is facing + a bit up
        itemEntity.velocity = forward.toVec3D() * 5 + new Vector3D(0, 2, 0);

        // add to world
        world.addEntity(itemEntity);
    }
}