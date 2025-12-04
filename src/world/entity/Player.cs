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
using BlockGame.util.stuff;
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
    public const double decayRate = 1.5;

    public double lastMouseAction;
    public double lastAirHit;

    /// <summary>
    /// Used for flymode
    /// </summary>
    public long spacePress;

    protected bool fastMode = false;


    // bow charging state
    public bool isChargingBow;
    public int bowChargeTime;
    public double prevBowCharge;
    public double bowCharge; // 0-1 charge progress

    // auto-use for hold-to-fire weapons
    public int autoUseTimer; // cooldown for auto-use

    // recoil state for weapons
    public int recoilTime;
    public double recoilStrength;

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

    public override void dmg(double damage, Entity source) {
        // creative mode players don't take damage
        if (!gameMode.gameplay) {
            return;
        }

        base.dmg(damage, source);
    }

    /**
     * Get total pending healing from regeneration effects.
     * Used for UI to show potential healing.
     */
    public double getPotentialHealing() {
        double total = 0;
        foreach (var effect in effects) {
            if (effect is RegenEffect regen) {
                total += regen.value;
            }
        }
        return total;
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

        // decrement recoil timer
        if (recoilTime > 0) {
            recoilTime--;
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

    public void getMiningSpeed(Block block) {
    }

    //public override void interactBlock(double dt) {
    //    base.interactBlock(dt);
    //}

    public virtual void attackEntity() {
        var now = Game.permanentStopwatch.ElapsedMilliseconds;
        var entity = Game.raycast.entity;

        if (entity == null || now - lastMouseAction <= Constants.breakDelayMs) {
            return;
        }

        var heldStack = inventory.getStack(inventory.selected);
        var damage = heldStack.getItem().getDamage(heldStack);

        entity.dmg(damage, this);
        setSwinging(true);
        lastMouseAction = now;

        if (heldStack != ItemStack.EMPTY && Item.durability[heldStack.id] > 0) {
            var newStack = heldStack.damageItem(this, 1);
            inventory.setStack(inventory.selected, newStack);
        }
    }

    public void startBowCharge() {
        isChargingBow = true;
        bowChargeTime = 0;
        bowCharge = 0;
        prevBowCharge = 0;
    }

    public void stopBowCharge() {
        isChargingBow = false;
        bowChargeTime = 0;
        bowCharge = 0;
        prevBowCharge = 0;

        // reset FOV modifier
        if (!Net.mode.isDed()) {
            Game.camera.fovModifier = 0f;
        }
    }

    public void fireBow() {
        if (!isChargingBow) return;

        // calculate charge ratio
        var chargeRatio = Math.Min((double)bowChargeTime / BowItem.MAX_CHARGE_TIME, 1.0);

        // only fire if bow is charged enough (at least 10% charged)
        if (chargeRatio < 0.1) {
            stopBowCharge();
            return;
        }

        // stop charging before firing (so BowItem.use() knows we're firing)
        isChargingBow = false;
        if (!Net.mode.isDed()) {
            Game.camera.fovModifier = 0f;
        }

        // in multiplayer client, send packet to server
        if (Net.mode.isMPC()) {
            ClientConnection.instance.send(
                new UseItemPacket {
                    chargeRatio = (float)chargeRatio
                },
                DeliveryMethod.ReliableOrdered
            );
        }
        else {
            // single-player or server: fire directly via item.use()
            var stack = inventory.getSelected();
            bowCharge = chargeRatio; // store for BowItem.use() to read
            var result = stack.getItem().use(stack, world, this);
            if (result != null!) {
                inventory.setStack(inventory.selected, result);
            }
        }

        // play sound and animation
        setSwinging(true);

        // reset charge state
        bowChargeTime = 0;
        bowCharge = 0;
        prevBowCharge = 0;
    }

    /**
     * apply recoil kick from weapons
     */
    public void applyRecoil(double strength) {
        recoilTime = 12;
        recoilStrength = strength;

        if (!Net.mode.isDed()) {
            Game.camera.applyRecoil((float)strength);
        }
    }

    /**
     * generic item use method for auto-use and regular items
     */
    public void useItem() {
        var stack = inventory.getSelected();
        if (stack == ItemStack.EMPTY) return;

        var item = stack.getItem();

        // don't swing for auto-use items (they have custom animations)
        if (!Registry.ITEMS.autoUse[item.id]) {
            setSwinging(true);
        }

        // multiplayer: send packet to server
        if (Net.mode.isMPC()) {
            ClientConnection.instance.send(
                new UseItemPacket { chargeRatio = 0 },
                DeliveryMethod.ReliableOrdered
            );
        }
        else {
            // singleplayer or server: execute directly
            var result = item.use(stack, world, this);
            if (result != null!) {
                inventory.setStack(inventory.selected, result);
            }
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

        // clear all effects on death
        effects.Clear();

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

        // clear effects on respawn
        effects.Clear();

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