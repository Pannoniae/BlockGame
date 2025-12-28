using System.Numerics;
using BlockGame.main;
using BlockGame.net;
using BlockGame.net.packet;
using BlockGame.util;
using BlockGame.util.log;
using BlockGame.util.stuff;
using BlockGame.world.block;
using BlockGame.world.item;
using LiteNetLib;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world.entity;

public struct BreakState {
    public double progress;
    public double prevProgress;
    public int time;
    public int lastUpdate;
}

public class ClientPlayer : Player {
    private Vector3D lastSentPos;
    private Vector3 lastSentRot;
    private int ticksSinceUpdate;

    // block breaking tracking for multiplayer
    private Vector3I? lastBreaking;
    private bool sentStartBreak;
    private int lastProgressSend;

    // state tracking for multiplayer
    private bool lastSentSneaking;
    private bool lastSentFlying;


    public readonly XMap<Vector3I, BreakState> breakStates = [];

    public ClientPlayer(World world, int x, int y, int z) : base(world, x, y, z) {
    }

    public override void update(double dt) {
        base.update(dt);

        // send position updates to server
        if (ClientConnection.instance != null && ClientConnection.instance.connected) {
            // send every tick... it was quite buggy otherwise
            ClientConnection.instance.send(
                new PlayerPositionRotationPacket {
                    position = position,
                    rotation = rotation,
                },
                DeliveryMethod.Unreliable
            );

            // velocity is only needed for knockback/special effects, send less frequently
            ticksSinceUpdate++;
            ClientConnection.instance.send(
                new PlayerVelocityPacket {
                    velocity = velocity
                },
                DeliveryMethod.Unreliable
            );
            ticksSinceUpdate = 0;

            // send state changes (sneaking, flying)
            // todo we need this for now because players don't use syncState yet. (we skip them in the EntityTracker....)
            //  we should probably migrate to that system later.
            if (sneaking != lastSentSneaking) {
                state.setBool(EntityState.SNEAKING, sneaking);
                ClientConnection.instance.send(
                    new EntityStatePacket {
                        entityID = id,
                        data = state.serialize()
                    },
                    DeliveryMethod.ReliableOrdered
                );
                lastSentSneaking = sneaking;
            }

            if (flyMode != lastSentFlying) {
                state.setBool(EntityState.FLYING, flyMode);
                ClientConnection.instance.send(
                    new EntityStatePacket {
                        entityID = id,
                        data = state.serialize()
                    },
                    DeliveryMethod.ReliableOrdered
                );
                lastSentFlying = flyMode;
            }
        }
    }

    public void breakBlock() {
        var now = Game.permanentStopwatch.ElapsedMilliseconds;
        bool connected = ClientConnection.instance != null && ClientConnection.instance.connected;

        if (Game.raycast.hit && Game.raycast.type == Result.BLOCK) {
            var pos = Game.raycast.block;
            var prev = Game.raycast.previous;

            // handle fire breaking (instant break)
            if (world.getBlock(prev.X, prev.Y, prev.Z) == Block.FIRE.id) {
                world.setBlock(prev.X, prev.Y, prev.Z, 0);
                Game.snd.playBlockBreak(Block.FIRE.mat.smat);
                if (connected) {
                    ClientConnection.instance.send(
                        new FinishBlockBreakPacket { position = prev },
                        DeliveryMethod.ReliableOrdered
                    );
                }
                setSwinging(true);
                lastMouseAction = now;
                return;
            }

            // creative instant-break (respect delay)
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
                if (connected) {
                    ClientConnection.instance.send(
                        new FinishBlockBreakPacket { position = pos },
                        DeliveryMethod.ReliableOrdered
                    );
                }
                setSwinging(true);
                lastMouseAction = now;
                return;
            }
        }

        // survival mode handled by blockHandling()
        // set swinging for visual feedback
        if (now - lastMouseAction > Constants.breakDelayMs) {
            setSwinging(true);
            lastMouseAction = now;
        }
    }

    public virtual void blockHandling(double dt) {
        // 1. get current target before decay (so we can skip it)
        var currentTarget = Game.instance.targetedPos;
        var activelyBreaking = currentTarget.HasValue && Game.inputs.left.down();

        // 2. decay all blocks EXCEPT the one we're currently breaking
        decayAllBlocks(activelyBreaking ? currentTarget : null);

        bool connected = ClientConnection.instance != null && ClientConnection.instance.connected;

        // 3. check if actively breaking
        if (!activelyBreaking) {
            // stopped breaking - send cancel if needed
            if (sentStartBreak && connected) {
                ClientConnection.instance.send(
                    new CancelBlockBreakPacket(),
                    DeliveryMethod.ReliableOrdered
                );
                lastBreaking = null;
                sentStartBreak = false;
            }
            return;
        }

        var pos = currentTarget!.Value;

        // 4. get or create state
        if (!breakStates.TryGetValue(pos, out var state)) {
            state = new BreakState();
        }

        // 5. check block validity
        var val = world.getBlockRaw(pos);
        var block = Block.get(val.getID());
        if (block == null || block.id == 0) return;

        var hardness = Block.hardness[block.id];
        if (hardness < 0) return;

        // 6. calculate break speed
        var heldItem = inventory.getSelected().getItem();
        var toolSpeed = heldItem.getBreakSpeed(inventory.getSelected(), block);
        var breakSpeed = toolSpeed / hardness * 0.5;
        var canBreak = heldItem.canBreak(inventory.getSelected(), block);

        if (!onGround) {
            breakSpeed *= 0.25;
        }

        if (inLiquid) {
            breakSpeed *= 0.25;
        }

        if (!canBreak && !Block.optionalTool[block.id]) {
            breakSpeed *= 0.25;
        }

        // 7. update state
        state.prevProgress = state.progress;
        state.progress += breakSpeed * dt;
        state.time++;
        state.lastUpdate = world.worldTick;

        // 8. effects
        if (state.time % 4 == 0) {
            block.shatter(world, pos.X, pos.Y, pos.Z, Game.raycast.face, Game.raycast.hitAABB);
        }
        if (state.time % 12 == 0 && block.mat != null) {
            Game.snd.playBlockKnock(block.mat.smat);
        }

        // 9. check if fully broken
        if (state.progress >= 1.0) {
            block.shatter(world, pos.X, pos.Y, pos.Z);

            var metadata = val.getMetadata();
            if (gameMode.gameplay) {
                drops.Clear();
                block.getDrop(drops, world, pos.X, pos.Y, pos.Z, metadata, canBreak);
                foreach (var drop in drops) {
                    world.spawnBlockDrop(pos.X, pos.Y, pos.Z, drop.getItem(), drop.quantity, drop.metadata);
                }
            }

            world.setBlock(pos.X, pos.Y, pos.Z, 0);
            world.blockUpdateNeighbours(pos.X, pos.Y, pos.Z);
            if (block.mat != null) {
                Game.snd.playBlockBreak(block.mat.smat);
            }

            var stack = inventory.getSelected().damageItem(this, 1);
            inventory.setStack(inventory.selected, stack);

            breakStates.Remove(pos);

            // multiplayer: send finish packet
            if (connected && sentStartBreak) {
                ClientConnection.instance.send(
                    new FinishBlockBreakPacket { position = pos },
                    DeliveryMethod.ReliableOrdered
                );
                lastBreaking = null;
                sentStartBreak = false;
            }

            return;
        }

        // 10. save state
        breakStates.Set(pos, state);

        // multiplayer sync
        if (!connected) return;

        // check if we started breaking a new block
        if (!lastBreaking.HasValue || lastBreaking.Value != pos) {
            // send cancel for previous block if we were breaking one
            if (sentStartBreak) {
                ClientConnection.instance.send(
                    new CancelBlockBreakPacket(),
                    DeliveryMethod.ReliableOrdered
                );
            }

            // send start for new block
            ClientConnection.instance.send(
                new StartBlockBreakPacket { position = pos },
                DeliveryMethod.ReliableOrdered
            );

            lastBreaking = pos;
            sentStartBreak = true;
        }

        if (sentStartBreak && world.worldTick - lastProgressSend >= 5) {
            ClientConnection.instance.send(
                new UpdateBlockBreakProgressPacket {
                    position = pos,
                    progress = state.progress
                },
                DeliveryMethod.ReliableOrdered
            );
            lastProgressSend = world.worldTick;
        }
    }

    public override void attackEntity() {
        // check if connected to server
        bool connected = ClientConnection.instance != null && ClientConnection.instance.connected;

        if (connected && Game.raycast.entity != null) {
            var now = Game.permanentStopwatch.ElapsedMilliseconds;

            if (now - lastMouseAction <= Constants.breakDelayMs) {
                return;
            }

            // send attack packet to server
            ClientConnection.instance.send(
                new AttackEntityPacket {
                    targetEntityID = Game.raycast.entity.id
                },
                DeliveryMethod.ReliableOrdered
            );

            // play swing animation locally
            setSwinging(true);
            lastMouseAction = now;
        } else {
            // singleplayer - use base implementation
            base.attackEntity();
        }
    }

    public void clearBreakProgressDecay() {
        breakStates.Clear();
    }

    private void decayAllBlocks(Vector3I? skipPos) {
        int currentTick = world.worldTick;
        var toRemove = new List<Vector3I>();

        foreach (var pos in breakStates.Keys) {
            // skip the block we're currently breaking
            if (skipPos.HasValue && pos == skipPos.Value) {
                continue;
            }

            var state = breakStates[pos];
            int ticksPassed = currentTick - state.lastUpdate;
            double secondsPassed = ticksPassed / 60.0;

            state.prevProgress = state.progress;
            state.progress -= decayRate * secondsPassed;

            if (state.progress <= 0.01) {
                toRemove.Add(pos);
            } else {
                state.lastUpdate = currentTick;
                breakStates.Set(pos, state);
            }
        }

        foreach (var pos in toRemove) {
            breakStates.Remove(pos);
        }
    }

    // see base.setSwinging for impl
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

        // check for auto-use item
        var stack = inventory.getSelected();
        var item = stack != ItemStack.EMPTY ? stack.getItem() : null;

        if (Game.inputs.right.pressed()) {
            if (now - lastMouseAction > Constants.breakMissDelayMs && now - lastAirHit > Constants.airHitDelayMs) {
                // auto-use item: fire immediately and start timer
                if (item != null && Registry.ITEMS.autoUse[item.id]) {
                    useItem();
                    autoUseTimer = Registry.ITEMS.useDelay[item.id];
                    lastMouseAction = now;
                }
                else {
                    // regular item/block placement
                    placeBlock();
                    lastMouseAction = now;
                    if (!Game.instance.previousPos.HasValue) {
                        lastAirHit = now;
                    }
                }
            }
        }
        else {
            if (Game.inputs.right.down() && now - lastMouseAction > Constants.placeDelayMs &&
                now - lastAirHit > Constants.airHitDelayMs) {
                // auto-use: continue firing if timer expired
                if (item != null && Registry.ITEMS.autoUse[item.id]) {
                    if (autoUseTimer <= 0) {
                        useItem();
                        autoUseTimer = Registry.ITEMS.useDelay[item.id];
                    }
                }
                else {
                    // regular block placement
                    placeBlock();
                    lastMouseAction = now;
                    if (!Game.instance.previousPos.HasValue) {
                        lastAirHit = now;
                    }
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

        // survival: gradual breaking with progress

        if (gameMode.gameplay) {
            blockHandling(dt);
        }
    }

    public virtual void placeBlock() {
        // chain of responsibility - first match wins
        if (tryInteractWithEntity()) return;
        if (tryInteractWithBlock()) return;
        if (tryUseItemOnBlock()) return;
        if (tryPlaceBlockItem()) return;
        tryUseItemInAir();
    }

    /** entity.interact() - cows, sheep, etc */
    private bool tryInteractWithEntity() {
        if (!Game.raycast.hit || Game.raycast.type != Result.ENTITY) return false;

        var entity = Game.raycast.entity;
        if (entity == null) return false;

        var stack = inventory.getSelected();
        if (stack == ItemStack.EMPTY) return false;

        // try entity interaction
        if (!entity.interact(this, stack)) return false;

        // entity handled it - swing animation
        setSwinging(true);

        // send to server (reuse PlaceBlockPacket for now)
        if (Net.mode.isMPC()) {
            var info = getPlacementInfo();
            ClientConnection.instance.send(
                new PlaceBlockPacket { position = new Vector3I(0, 0, 0), info = info },
                DeliveryMethod.ReliableOrdered
            );
        }

        return true;
    }

    /** block.onUse() - chests, furnaces, etc */
    private bool tryInteractWithBlock() {
        if (!Game.raycast.hit || Game.raycast.type != Result.BLOCK) return false;

        var pos = Game.raycast.block;
        var block = Block.get(world.getBlock(pos));
        if (block == null || block == Block.AIR) return false;

        var info = getPlacementInfo();

        // execute locally (opens UI, etc)
        if (!block.onUse(world, pos.X, pos.Y, pos.Z, this)) return false;

        // send to server
        if (Net.mode.isMPC()) {
            ClientConnection.instance.send(
                new PlaceBlockPacket { position = pos, info = info },
                DeliveryMethod.ReliableOrdered
            );
        }

        return true;
    }

    /** item.useBlock() - doors, buckets, etc */
    private bool tryUseItemOnBlock() {
        if (!Game.raycast.hit || Game.raycast.type != Result.BLOCK) return false;

        var stack = inventory.getSelected();
        if (stack == ItemStack.EMPTY) return false;

        var pos = Game.raycast.previous;
        var info = getPlacementInfo();

        // try item's useBlock hook
        var result = stack.getItem().useBlock(stack, world, this, pos.X, pos.Y, pos.Z, info);
        if (result == null) return false;

        // item handled it - update inventory locally
        inventory.setStack(inventory.selected, result);
        setSwinging(true);

        // send to server
        if (Net.mode.isMPC()) {
            ClientConnection.instance.send(
                new PlaceBlockPacket { position = pos, info = info },
                DeliveryMethod.ReliableOrdered
            );
        }

        return true;
    }

    /** place block from item */
    private bool tryPlaceBlockItem() {
        if (!Game.raycast.hit || Game.raycast.type != Result.BLOCK) return false;

        var stack = inventory.getSelected();
        if (stack == ItemStack.EMPTY || !stack.getItem().isBlock()) return false;

        var pos = Game.raycast.previous;
        var block = Block.get(stack.getItem().getBlockID());
        var metadata = (byte)stack.metadata;
        var info = getPlacementInfo();

        // validate placement
        if (!block.canPlace(world, pos.X, pos.Y, pos.Z, info)) {
            setSwinging(false);
            return false;
        }

        // check collisions (player + entities)
        if (wouldCollideWithPlayer(block, pos, metadata) || wouldCollideWithEntities(block, pos, metadata)) {
            setSwinging(false);
            return false;
        }

        // validate stack in survival
        if (gameMode.gameplay && (stack.quantity <= 0)) {
            setSwinging(false);
            return false;
        }

        // execute locally
        block.place(world, pos.X, pos.Y, pos.Z, metadata, info);
        if (gameMode.gameplay) {
            inventory.removeStack(inventory.selected, 1);
        }
        if (block.mat != null) {
            Game.snd.playBlockBreak(block.mat.smat);
        }

        // multiplayer client: send packet, server will handle placement
        if (Net.mode.isMPC()) {
            ClientConnection.instance.send(
                new PlaceBlockPacket { position = pos, info = info },
                DeliveryMethod.ReliableOrdered
            );
        }

        setSwinging(true);
        return true;
    }

    /** use item in air - food, bow, throwables */
    private void tryUseItemInAir() {
        useItem();
    }

    private Placement getPlacementInfo() {
        return new Placement {
            face = Game.raycast.face,
            facing = getFacing(),
            hfacing = getHFacing(),
            hitPoint = Game.raycast.point
        };
    }

    private bool wouldCollideWithPlayer(Block block, Vector3I pos, byte metadata) {
        world.getAABBsCollision(AABBList, pos.X, pos.Y, pos.Z);
        foreach (var aabb in AABBList) {
            if (AABB.isCollision(aabb, this.aabb)) return true;
        }
        return false;
    }

    private bool wouldCollideWithEntities(Block block, Vector3I pos, byte metadata) {
        if (!Block.collision[block.id]) return false;

        block.getAABBs(world, pos.X, pos.Y, pos.Z, metadata, AABBList);
        var entities = new List<Entity>();

        foreach (var aabb in AABBList) {
            entities.Clear();
            world.getEntitiesInBox(entities, aabb.min.toBlockPos(), aabb.max.toBlockPos() + 1);
            foreach (var entity in entities) {
                if (entity.blocksPlacement && AABB.isCollision(aabb, entity.aabb)) return true;
            }
        }

        return false;
    }
}