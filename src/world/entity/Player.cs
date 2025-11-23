using System.Diagnostics.Contracts;
using System.Numerics;
using BlockGame.GL;
using BlockGame.logic;
using BlockGame.main;
using BlockGame.net;
using BlockGame.net.packet;
using BlockGame.render.model;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.util.cmd;
using BlockGame.util.xNBT;
using BlockGame.world.block;
using BlockGame.world.chunk;
using BlockGame.world.item;
using BlockGame.world.item.inventory;
using LiteNetLib;
using Molten;
using Molten.DoublePrecision;
using Silk.NET.Input;

namespace BlockGame.world.entity;

public class Player : Mob, CommandSource {
    public const double height = 1.75;
    public const double width = 0.625;
    public const double eyeHeight = 1.6;
    public const double sneakingEyeHeight = 1.45;
    public const double feetCheckHeight = 0.05;

    public static List<ItemStack> drops = [];

    public Vector3D inputVector;

    public PlayerHandRenderer handRenderer;
    public PlayerRenderer modelRenderer;

    public GameMode gameMode;

    /** custom skin texture for this player (null = use default skin) */
    public BTexture2D? skinTex;

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

    /**
     * Current inventory ID for packet validation.
     * Incremented each time a new inventory is opened.
     * Window ID -1 = player inventory (always open)
     */
    public int currentInventoryID = Constants.INV_ID_PLAYER;

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
    public Player(World world, int x, int y, int z) : base(world, "player") {
        position = new Vector3D(x, y, z);
        prevPosition = position;
        inventory = new PlayerInventory();


        if (!Net.mode.isDed()) {
            handRenderer = new PlayerHandRenderer(this);
            // get from registry
            modelRenderer = (PlayerRenderer)EntityRenderers.get(Entities.getID("player"));
        }

        inventoryCtx = new CreativeInventoryContext(inventory, 40);
        currentCtx = inventoryCtx;

        this.world = world;
        rotation = new Vector3();
        bodyRotation = rotation; // initialise body rotation to match head
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

        // gamemode
        var gamemodeID = data.getInt("gamemode", 1);
        gameMode = GameMode.fromID((GameModeID)gamemodeID);

        // health
        hp = data.getDouble("hp", 100);
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

        // gamemode
        data.addInt("gamemode", (int)gameMode.id);
        // health
        data.addDouble("hp", hp);
    }

    public void render(double dt, double interp) {
        Game.camera.updateFOV(isUnderWater(), dt);

        if (Game.camera.mode == CameraMode.FirstPerson) {
            handRenderer.render(interp);
        }
    }


    // ============ LIFECYCLE HOOKS ============die

    protected override bool shouldContinueUpdate(double dt) {
        if (hp <= 0 && !dead) {
            die();
        }

        if (dead) {
            // save prev vars for smooth interpolation
            prevBodyRotation = bodyRotation;

            dieTime++;

            // animate death fall (fall to side over a sec)
            const int deathAnimDuration = 60;
            float t = Math.Min(dieTime / (float)deathAnimDuration, 1f);
            // ease out for smoother animation
            t = 1f - (1f - t) * (1f - t);
            bodyRotation.Z = -90f * t;

            // prevent movement when dead
            velocity = Vector3D.Zero;
            inputVector = Vector3D.Zero;
            strafeVector = Vector3D.Zero;

            if (!Net.mode.isDed()) {
                Game.camera.updatePosition(dt); // update death tilt
            }

            return false;
        }

        dieTime = 0;
        return true;
    }

    protected override void savePrevVars() {
        base.savePrevVars();

        // player-specific prev vars

        if (!Net.mode.isDed()) {
            Game.camera.prevBob = Game.camera.bob;
            Game.camera.prevAirBob = Game.camera.airBob;
        }

        prevBreakProgress = breakProgress;
        if (!Net.mode.isDed()) {
            handRenderer.prevLower = handRenderer.lower;
        }
    }

    public override void dmg(float damage) {
        // creative mode players don't take damage
        if (!gameMode.gameplay) {
            return;
        }

        base.dmg(damage);
    }

    public override void dmg(double damage, Vector3D source) {
        // creative mode players don't take damage
        if (!gameMode.gameplay) {
            return;
        }

        base.dmg(damage, source);
    }

    protected override void updateTimers(double dt) {
        base.updateTimers(dt);

        if (!Net.mode.isDed()) {
            handRenderer.update(dt);
        }

        // decay shiny values
        for (int i = 0; i < inventory.shiny.Length; i++) {
            if (inventory.shiny[i] > 0) {
                inventory.shiny[i] -= (float)(dt * 4.0);
                if (inventory.shiny[i] < 0) {
                    inventory.shiny[i] = 0;
                }
            }
        }
    }

    protected override void prePhysics(double dt) {
        updateInputVelocity(dt);
        applyInputMovement(dt);
    }

    protected override void checkFallDamage(double dt) {
        if (gameMode.gameplay && onGround && wasInAir && !flyMode && !inLiquid) {
            var fallSpeed = -prevVelocity.Y;
            if (fallSpeed > SAFE_FALL_SPEED) {
                var dmg = (float)((fallSpeed - SAFE_FALL_SPEED) * FALL_DAMAGE_MULTIPLIER);
                base.dmg(dmg);
                if (!Net.mode.isDed()) {
                    Game.camera.applyImpact(dmg);
                }
            }
        }

        wasInAir = !onGround && !flyMode;
    }

    protected override void updateFootsteps(double dt) {
        if (onGround && Math.Abs(velocity.withoutY().Length()) > 0.05 && !inLiquid) {
            if (totalTraveled - lastFootstepDistance > FOOTSTEP_DISTANCE) {
                // get block below player
                var pos = position.toBlockPos() + new Vector3I(0, -1, 0);
                var blockBelow = Block.get(world.getBlock(pos));
                if (!Net.mode.isDed() && blockBelow?.mat != null) {
                    Game.snd.playFootstep(blockBelow.mat.smat, position);
                }

                lastFootstepDistance = totalTraveled;
            }
        }
    }

    protected override void postPhysics(double dt) {
        base.postPhysics(dt);

        // totalTraveled already updated in Mob.postPhysics()

        // update camera
        if (!Net.mode.isDed()) {
            Game.camera.updatePosition(dt);
        }

        // item pickup
        pickup();
    }

    // before pausing, all vars need to be updated SO THERE IS NO FUCKING JITTER ON THE PAUSE MENU
    public void catchUpOnPrevVars() {
        savePrevVars();
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

        if (!Net.mode.isDed() && !Net.mode.isMPC()) {
            loadChunksAroundThePlayer(Settings.instance.renderDistance);
        }
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
                var moveSpeed = onGround ? GROUND_MOVE_SPEED : AIR_MOVE_SPEED;

                // on non-natural blocks, go 15% faster
                var blockBelowPos = position.toBlockPos() + new Vector3I(0, -1, 0);
                var blockBelow = Block.get(world.getBlock(blockBelowPos));
                if (blockBelow != null && !Block.natural[blockBelow.id]) {
                    moveSpeed *= 1.15;
                }

                // debuff movement speed on ice so we don't go superfast note this should be refactored into something more general / less of a clusterfuck later
                if (blockBelow != null && blockBelow.id == Block.ICE.id) {
                    moveSpeed *= 1 / 6f;
                }

                if (inLiquid) {
                    moveSpeed = LIQUID_MOVE_SPEED;
                }

                if (sneaking) {
                    moveSpeed *= SNEAK_FACTOR;
                }

                // first, normalise (v / v.length) then multiply with movespeed
                strafeVector = Vector3D.Normalize(strafeVector) * moveSpeed;

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
                var moveSpeed = AIR_FLY_SPEED;

                // first, normalise (v / v.length) then multiply with movespeed
                strafeVector = Vector3D.Normalize(strafeVector) * moveSpeed;

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

            // notify server of held item change in multiplayer
            if (Net.mode.isMPC()) {
                ClientConnection.instance.send(new PlayerHeldItemChangePacket {
                    slot = (byte)inventory.selected
                }, DeliveryMethod.ReliableOrdered);
            }
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

    public virtual void updateInput(double dt) {
        if (world.inMenu) {
            return;
        }

        // reset strafe vector at start of each frame
        strafeVector = Vector3D.Zero;

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

        fastMode = Game.inputs.ctrl.down() && Game.devMode;

        var now = Game.permanentStopwatch.ElapsedMilliseconds;

        // repeated action while held (with delay to prevent spam)
        if (Game.inputs.left.pressed()) {
            if (now - lastAirHit > Constants.airHitDelayMs) {
                // check what we're hitting
                if (Game.raycast.hit && Game.raycast.type == Result.ENTITY) {
                    attackEntity();
                }
                else {
                    breakBlock();
                }

                //lastMouseAction = now;
                if (!Game.instance.targetedPos.HasValue) {
                    lastAirHit = now;
                }
            }
        }
        else {
            if (Game.inputs.left.down() &&
                now - lastAirHit > Constants.airHitDelayMs) {
                // check what we're hitting
                if (Game.raycast.hit && Game.raycast.type == Result.ENTITY) {
                    attackEntity();
                }
                else {
                    breakBlock();
                }

                //lastMouseAction = now;
                if (!Game.instance.targetedPos.HasValue) {
                    lastAirHit = now;
                }
            }
        }

        if (Game.inputs.right.pressed()) {
            if (now - lastMouseAction > Constants.breakMissDelayMs && now - lastAirHit > Constants.airHitDelayMs) {
                // todo entity interact here?
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
                // todo entity interact here?
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

    public virtual void blockHandling(double dt) {
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

            // unbreakable blocks
            if (hardness < 0) {
                isBreaking = false;
                breakProgress = 0;
                breakTime = 0;
                return;
            }

            var heldItem = inventory.getSelected().getItem();
            var toolBreakSpeed = heldItem.getBreakSpeed(inventory.getSelected(), block);
            var breakSpeed = toolBreakSpeed / hardness * 0.5;
            var canBreak = heldItem.canBreak(inventory.getSelected(), block);

            // jumping penalty (4x slower while airborne)
            if (!onGround) {
                breakSpeed *= 0.25;
            }

            if (inLiquid) {
                breakSpeed *= 0.25;
            }

            if (!canBreak) {
                breakSpeed *= 0.25;
            }


            prevBreakProgress = breakProgress;
            breakProgress += breakSpeed * dt;

            // spawn mining particles every 4 ticks
            if (breakTime % 4 == 0) {
                block.shatter(world, pos.X, pos.Y, pos.Z, Game.raycast.face, Game.raycast.hitAABB);
            }

            if (breakTime % 12 == 0) {
                Game.snd.playBlockKnock(block.mat.smat);
            }

            breakTime++;

            if (breakProgress >= 1.0) {
                // block is fully broken
                block.shatter(world, pos.X, pos.Y, pos.Z);

                // get block drop and spawn item entity in survival mode
                var metadata = val.getMetadata();

                // only spawn drops if using correct tool
                drops.Clear();
                block.getDrop(drops, world, pos.X, pos.Y, pos.Z, metadata, canBreak);

                //if (dropItem != null && dropCount > 0 && gameMode.gameplay) {
                if (gameMode.gameplay) {
                    foreach (var drop in drops) {
                        var dropItem = drop.getItem();
                        var dropCount = drop.quantity;
                        var meta = (byte)drop.metadata;
                        world.spawnBlockDrop(pos.X, pos.Y, pos.Z, dropItem, dropCount, meta);
                    }
                }

                world.setBlock(pos.X, pos.Y, pos.Z, 0);
                world.blockUpdateNeighbours(pos.X, pos.Y, pos.Z);
                if (block.mat != null) {
                    Game.snd.playBlockBreak(block.mat.smat);
                }

                // dec durability
                var stack = inventory.getSelected().damageItem(this, 1);
                inventory.setStack(inventory.selected, stack);

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

    public virtual void placeBlock() {
        var facing = getFacing();
        var hfacing = getHFacing();

        // construct placement info
        var info = new Placement {
            face = Game.raycast.face,
            facing = facing,
            hfacing = hfacing,
            hitPoint = Game.raycast.point
        };

        // first check if player is clicking on an existing block with onUse behaviour
        if (Game.raycast.hit && Game.raycast.type == Result.BLOCK) {
            var targetPos = Game.raycast.block;
            var blockId = world.getBlock(targetPos.X, targetPos.Y, targetPos.Z);
            var block = Block.get(blockId);
            if (block != null && block != Block.AIR) {
                if (block.onUse(world, targetPos.X, targetPos.Y, targetPos.Z, this)) {
                    // send packet
                    if (Net.mode.isMPC()) {
                        ClientConnection.instance.send(
                            new PlaceBlockPacket {
                                position = targetPos,
                                info = info
                            },
                            DeliveryMethod.ReliableOrdered);
                    }

                    // if block has custom behaviour, stop here (don't place)
                    return;
                }
            }
        }

        if (Game.raycast.hit && Game.raycast.type == Result.BLOCK) {
            var pos = Game.raycast.previous;
            var stack = inventory.getSelected();

            var metadata = (byte)stack.metadata;

            // if item, fire the hook and handle replacement
            var replacement = stack.getItem().useBlock(stack, world, this, pos.X, pos.Y, pos.Z, info);
            if (replacement != null) {
                inventory.setStack(inventory.selected, replacement);
                setSwinging(true);


                if (Net.mode.isMPC()) {
                    ClientConnection.instance.send(
                        new PlaceBlockPacket {
                            position = pos,
                            info = info
                        },
                        DeliveryMethod.ReliableOrdered);
                }

                return;
            }

            // if block, place it
            if (stack.getItem().isBlock()) {
                var block = Block.get(stack.getItem().getBlockID());

                // check block-specific placement rules first
                if (!block.canPlace(world, pos.X, pos.Y, pos.Z, info)) {
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
                            if (entity.blocksPlacement && AABB.isCollision(aabb, entity.aabb)) {
                                hasCollisions = true;
                                break;
                            }
                        }

                        if (hasCollisions) break;
                    }
                }

                if (!hasCollisions) {
                    // in survival mode, check if player has blocks to place
                    if (gameMode.gameplay) {
                        if (stack == ItemStack.EMPTY || stack.quantity <= 0) {
                            setSwinging(false);
                            return;
                        }
                    }

                    block.place(world, pos.X, pos.Y, pos.Z, metadata, info);

                    // consume block from inventory in survival mode
                    if (gameMode.gameplay) {
                        inventory.removeStack(inventory.selected, 1);
                    }

                    // play sound
                    if (block.mat != null) {
                        Game.snd.playBlockBreak(block.mat.smat);
                    }

                    setSwinging(true);

                    // send packet
                    if (Net.mode.isMPC()) {
                        ClientConnection.instance.send(
                            new PlaceBlockPacket {
                                position = pos,
                                info = info
                            },
                            DeliveryMethod.ReliableOrdered);
                    }
                }
                else {
                    setSwinging(false);
                }
            }
        }
        else {
            var stack = inventory.getSelected();
            var result = stack.getItem().use(stack, world, this);
            if (result != null!) {
                inventory.setStack(inventory.selected, result);
                setSwinging(true);
                return;
            }

            setSwinging(false);
        }
    }

    public virtual void attackEntity() {
        var now = Game.permanentStopwatch.ElapsedMilliseconds;
        var entity = Game.raycast.entity;

        if (entity == null || now - lastMouseAction <= Constants.breakDelayMs) {
            return;
        }

        var heldStack = inventory.getStack(inventory.selected);
        var damage = heldStack.getItem().getDamage(heldStack);

        entity.dmg(damage, position);
        setSwinging(true);
        lastMouseAction = now;

        if (heldStack != ItemStack.EMPTY && Item.durability[heldStack.id] > 0) {
            var newStack = heldStack.damageItem(this, 1);
            inventory.setStack(inventory.selected, newStack);
        }
    }

    public virtual void breakBlock() {
        var now = Game.permanentStopwatch.ElapsedMilliseconds;

        if (Game.raycast.hit && Game.raycast.type == Result.BLOCK) {
            var pos = Game.raycast.block;


            var prev = Game.raycast.previous;
            // special fire handling lol
            if (world.getBlock(prev.X, prev.Y, prev.Z) == Block.FIRE.id) {
                world.setBlock(prev.X, prev.Y, prev.Z, 0);
                Game.snd.playBlockBreak(Block.FIRE.mat.smat);
                setSwinging(true);
                lastMouseAction = now;
                return;
            }

            // instabreak
            // delay because we'll end up breaking blocks too fast otherwise
            if (!gameMode.gameplay && now - lastMouseAction > Constants.breakDelayMs) {
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

            if (gameMode.gameplay) {
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

    public RawDirectionH getHFacing() {
        // Get the forward vector from the camera
        Vector3 forward = facing();

        // Determine the horizontal facing direction based on the forward vector
        if (Math.Abs(forward.X) > Math.Abs(forward.Z)) {
            return forward.X > 0 ? RawDirectionH.EAST : RawDirectionH.WEST;
        }
        else {
            return forward.Z > 0 ? RawDirectionH.NORTH : RawDirectionH.SOUTH;
        }
    }

    public bool isUnderWater() {
        // If not in liquid at all, definitely not underwater
        if (!inLiquid) {
            //return false;
        }

        double currentEyeHeight = sneaking ? sneakingEyeHeight : eyeHeight;
        Vector3D eyePosition = new Vector3D(position.X, position.Y + currentEyeHeight, position.Z);

        Vector3I eyeBlockPos = eyePosition.toBlockPos();
        ushort blockAtEyes = world.getBlock(eyeBlockPos);

        return Block.liquid[blockAtEyes];
    }

    public Block? getBlockAtEyes() {
        double currentEyeHeight = sneaking ? sneakingEyeHeight : eyeHeight;
        Vector3D eyePosition = new Vector3D(position.X, position.Y + currentEyeHeight, position.Z);

        Vector3I eyeBlockPos = eyePosition.toBlockPos();
        ushort blockAtEyes = world.getBlock(eyeBlockPos);

        return Block.get(blockAtEyes);
    }

    public void pickBlock() {
        var raycast = Game.raycast;
        if (raycast.hit && raycast.type == Result.BLOCK) {
            var pos = raycast.block;
            var raw = world.getBlockRaw(pos);
            var bl = Block.get(raw.getID());
            if (bl != null) {
                var stack = bl.getActualItem(raw.getMetadata());

                // let's be a bit smarter.
                // first check if player already has the block in their hotbar
                // can be an item too, who knows!
                for (ushort i = 0; i < 10; i++) {
                    var s = inventory.slots[i];
                    if (s != ItemStack.EMPTY) {
                        if (bl.same(s, stack)) {
                            // found it!
                            inventory.selected = i;

                            // sync with server

                            if (Net.mode.isMPC()) {
                                ClientConnection.instance.send(new PlayerHeldItemChangePacket {
                                    slot = (byte)inventory.selected
                                }, DeliveryMethod.ReliableOrdered);
                            }

                            return;
                        }
                    }
                }

                if (!gameMode.gameplay) {
                    // second, if not found, put it in the selected slot
                    if (stack != null!) {

                        // we have to zero the metadata so we don't get fucked over by things like stairs
                        var normalisedItem = bl.getCanonical(raw.getMetadata());
                        inventory.setStack(inventory.selected, normalisedItem);

                        // sync with server, since we are in creative, we can bullshit and server will accept it
                        if (Net.mode.isMPC()) {
                            // creative inventory hotbar is slots 40-49 (map from player inventory slots 0-9)
                            ClientConnection.instance.send(new InventorySlotClickPacket {
                                invID = Constants.INV_ID_CREATIVE,
                                idx = (ushort)(inventory.selected + 40),
                                button = 2,
                                actionID = ClientConnection.instance.nextActionID++,
                                mode = 0, // normal click
                                expectedSlot = inventory.getStack(inventory.selected)
                            }, DeliveryMethod.ReliableOrdered);

                            ClientConnection.instance.send(new PlayerHeldItemChangePacket {
                                slot = (byte)inventory.selected
                            }, DeliveryMethod.ReliableOrdered);
                        }
                    }
                }
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
     * Player uses strafeVector input, mob uses velocity
     */
    protected override void updateBodyRotation(double dt) {
        var input = strafeVector.withoutY();
        var inputLength = input.Length();

        bool moving = inputLength > IDLE_VELOCITY_THRESHOLD;
        bool swinging = swingProgress > 0;

        float targetYaw;
        float rotSpeed;

        if (swinging) {
            targetYaw = rotation.Y;
            rotSpeed = 9.6f;
        }
        else if (moving) {
            // Body faces movement direction relative to head
            float inputAngle = Meth.rad2deg((float)Math.Atan2(-input.X, input.Z));
            targetYaw = rotation.Y + inputAngle;
            rotSpeed = ROTATION_SPEED * 2;
        }
        else {
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

        // in multiplayer client, send packet to server
        if (Net.mode.isMPC()) {
            Game.client.send(new DropItemPacket {
                slotIndex = (byte)inventory.selected,
                quantity = 1
            }, DeliveryMethod.ReliableOrdered);
            return;
        }

        // singleplayer - drop directly
        var droppedStack = inventory.removeStack(inventory.selected, 1);
        dropItemStack(droppedStack, withVelocity: true);
    }

    /** Drop an item stack at player position. withVelocity = throw in facing direction */
    public void dropItemStack(ItemStack stack, bool withVelocity = false) {
        if (Net.mode.isMPC()) {
            return;
        }

        if (stack == ItemStack.EMPTY || stack.quantity <= 0) {
            return;
        }

        var itemEntity = new ItemEntity(world);
        itemEntity.stack = stack;
        itemEntity.plotArmour = 144;

        if (withVelocity) {
            // throw in horizontal facing direction
            var eyePos = new Vector3D(position.X, position.Y + eyeHeight, position.Z);
            var forward = facing(); // horizontal facing only, ignore pitch
            itemEntity.position = eyePos + forward.toVec3D() * 0.5;
            itemEntity.velocity = forward.toVec3D() * 3.5 + new Vector3D(0, 1.5, 0);
        }
        else {
            // drop at feet, radomly offset
            var r = Game.random;
            itemEntity.position = new Vector3D(
                position.X + r.NextDouble() * 0.5 - 0.25,
                position.Y + 0.5,
                position.Z + r.NextDouble() * 0.5 - 0.25
            );
        }

        world.addEntity(itemEntity);
    }

    public override void die() {
        dead = true;

        // drop inventory items on death (survival only, blocks only)
        if (gameMode.gameplay) {
            dropInventoryOnDeath();
        }

        if (!Net.mode.isDed()) {
            // switch to death screen
            Game.instance.executeOnMainThread(() => {
                Game.instance.currentScreen.switchToMenu(Screen.GAME_SCREEN.DEATH_MENU);
                Game.instance.unlockMouse();
            });
        }
    }

    private void dropInventoryOnDeath() {
        // drop all materials (blocks used for building, not tools)
        for (int i = 0; i < inventory.slots.Length; i++) {
            var stack = inventory.slots[i];
            if (stack != ItemStack.EMPTY && stack.quantity > 0) {
                var item = stack.getItem();
                // only drop materials (blocks), not tools
                if (item.isBlock() && Item.material[item.id]) {
                    dropItemStack(stack);
                    inventory.slots[i] = ItemStack.EMPTY;
                }
            }
        }
    }

    public void respawn() {
        // in multiplayer client, send respawn request to server instead of respawning locally
        if (Net.mode.isMPC() && ClientConnection.instance != null) {
            ClientConnection.instance.send(new RespawnRequestPacket(), DeliveryMethod.ReliableOrdered);
            return;
        }

        // singleplayer or server - respawn locally
        dead = false;
        hp = 100;
        bodyRotation.Z = 0f;
        prevBodyRotation.Z = 0f;
        dieTime = 0;

        fireTicks = 0;

        // teleport to spawn point
        teleport(world.spawn);

        // reset velocity
        velocity = Vector3D.Zero;
        prevVelocity = Vector3D.Zero;

        // back to game
        Game.instance.executeOnMainThread(() => { Screen.GAME_SCREEN.backToGame(); });
    }

    public virtual void sendMessage(string msg) {
        Screen.GAME_SCREEN.CHAT.addMessage(msg);
    }

    public World getWorld() {
        return world;
    }

    public override void syncState() {
        base.syncState();
        state.setBool(EntityState.FLYING, flyMode);
    }

    public override void applyState() {
        base.applyState();
        flyMode = state.getBool(EntityState.FLYING);
    }

    public void openInventory(int invID, InventoryContext ctx) {
        currentInventoryID = invID;
        currentCtx = ctx;
    }

    public void closeInventory() {
        currentCtx.close();
        currentInventoryID = -1;
        currentCtx = inventoryCtx;
    }
}