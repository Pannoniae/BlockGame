using BlockGame.logic;
using BlockGame.main;
using BlockGame.net.packet;
using BlockGame.net.srv;
using BlockGame.ui;
using BlockGame.ui.menu;
using BlockGame.ui.screen;
using BlockGame.util;
using BlockGame.util.log;
using BlockGame.util.xNBT;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.block.entity;
using BlockGame.world.chunk;
using BlockGame.world.entity;
using BlockGame.world.item;
using BlockGame.world.item.inventory;
using LiteNetLib;
using Molten;
using Molten.DoublePrecision;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame.net;

/** handles incoming packets on client */
public class ClientPacketHandler : PacketHandler {
    private readonly ClientConnection conn;

    public ClientPacketHandler(ClientConnection conn) {
        this.conn = conn;
    }

    public void handle(Packet packet) {

        //Log.info($"[Client] Received packet of type {packet.GetType().Name}");

        switch (packet) {
            case AuthRequiredPacket p:
                handleAuthRequired(p);
                break;
            case LoginSuccessPacket p:
                handleLoginSuccess(p);
                break;
            case LoginFailedPacket p:
                handleLoginFailed(p);
                break;
            case DisconnectPacket p:
                handleDisconnect(p);
                break;
            case TeleportPacket p:
                handleTeleport(p);
                break;
            case ChunkDataPacket p:
                handleChunkData(p);
                break;
            case UnloadChunkPacket p:
                handleUnloadChunk(p);
                break;
            case BlockChangePacket p:
                handleBlockChange(p);
                break;
            case MultiBlockChangePacket p:
                handleMultiBlockChange(p);
                break;
            case BlockBreakProgressPacket p:
                handleBlockBreakProgress(p);
                break;
            case SpawnEntityPacket p:
                handleSpawnEntity(p);
                break;
            case DespawnEntityPacket p:
                handleDespawnEntity(p);
                break;
            case SpawnPlayerPacket p:
                handleSpawnPlayer(p);
                break;
            case EntityPositionPacket p:
                handleEntityPosition(p);
                break;
            case EntityRotationPacket p:
                handleEntityRotation(p);
                break;
            case EntityPositionRotationPacket p:
                handleEntityPositionRotation(p);
                break;
            case EntityPositionDeltaPacket p:
                handleEntityPositionDelta(p);
                break;
            case EntityVelocityPacket p:
                handleEntityVelocity(p);
                break;
            case EntityStatePacket p:
                handleEntityState(p);
                break;
            case EntityActionPacket p:
                handleEntityAction(p);
                break;
            case PlayerHealthPacket p:
                handlePlayerHealth(p);
                break;
            case ChatMessagePacket p:
                handleChatMessage(p);
                break;
            case PlayerListAddPacket p:
                handlePlayerListAdd(p);
                break;
            case PlayerListRemovePacket p:
                handlePlayerListRemove(p);
                break;
            case PlayerListUpdatePingPacket p:
                handlePlayerListUpdatePing(p);
                break;
            case PlayerSkinPacket p:
                handlePlayerSkin(p);
                break;
            case TimeUpdatePacket p:
                handleTimeUpdate(p);
                break;
            case UpdateBlockEntityPacket p:
                handleUpdateBlockEntity(p);
                break;
            case SetSlotPacket p:
                handleSetSlot(p);
                break;
            case InventorySyncPacket p:
                handleInventorySync(p);
                break;
            case InventoryAckPacket p:
                handleInventoryAck(p);
                break;
            case ResyncCompletePacket p:
                handleResyncComplete(p);
                break;
            case HeldItemChangePacket p:
                handleHeldItemChange(p);
                break;
            case InventoryOpenPacket p:
                handleInventoryOpen(p);
                break;
            case InventoryClosePacket p:
                handleInventoryClose(p);
                break;
            case FurnaceSyncPacket p:
                handleFurnaceSync(p);
                break;
            case GamemodePacket p:
                handleGamemode(p);
                break;
            case RespawnPacket p:
                handleRespawn(p);
                break;
            default:
                Log.warn($"Unhandled packet type: {packet.GetType().Name}");
                break;
        }
    }

    public void handleAuthRequired(AuthRequiredPacket p) {
        Log.info($"Auth required, needsRegister={p.needsRegister}");

        // route to appropriate auth menu
        Game.instance.executeOnMainThread(() => {
            if (p.needsRegister) {
                Menu.REGISTER_MENU.username = ClientConnection.instance?.username ?? "Player";
                Game.instance.switchTo(Menu.REGISTER_MENU);
            } else {
                Menu.LOGIN_MENU.username = ClientConnection.instance?.username ?? "Player";
                Game.instance.switchTo(Menu.LOGIN_MENU);
            }
        });
    }

    public void handleLoginSuccess(LoginSuccessPacket p) {
        Log.info($"Login success! EntityID={p.entityID}, spawn={p.spawnPos}");

        if (ClientConnection.instance != null) {
            ClientConnection.instance.entityID = p.entityID;
            ClientConnection.instance.authenticated = true;
            ClientConnection.instance.initialChunksLoaded = false;
        }

        // create multiplayer world
        // create empty world for multiplayer (no worldgen, server sends chunks)
        var world = new World("__multiplayer", 0, "Multiplayer World", "flat");

        // set world time
        world.worldTick = p.worldTick;

        // create player entity
        var pos = new Vector3I((int)p.spawnPos.X, (int)p.spawnPos.Y, (int)p.spawnPos.Z);
        Game.player = new ClientPlayer(world, pos.X, pos.Y, pos.Z) {
            id = p.entityID, // set entity ID from server
            position = p.spawnPos,
            rotation = p.rotation,
            gameMode = p.creative ? GameMode.creative : GameMode.survival,
            name = Settings.instance.playerName
        };

        // set correct inventory context based on gamemode (Player constructor defaults to creative :()
        if (Game.player.gameMode == GameMode.survival) {
            Game.player.inventoryCtx = new SurvivalInventoryContext(Game.player.inventory);
            Game.player.currentCtx = Game.player.inventoryCtx;
        }

        world.spawn = p.spawnPos;
        world.addEntity(Game.player);

        // set the world (this handles renderer setup, etc)
        Game.setWorld(world);

        // setup camera for multiplayer
        Game.camera.setPlayer(Game.player);

        // mark world as initialised so meshing/updates work
        world.inited = true;

        // send your local skin to server
        sendLocalSkin();

        Log.info("Multiplayer world initialized, waiting for chunks...");
    }

    /** send local character.png skin to server */
    private static void sendLocalSkin() {
        if (ClientConnection.instance == null) {
            return;
        }

        var skinPath = Settings.instance.skinPath;
        byte[] skinData = [];

        // try to load local skin file
        if (File.Exists(skinPath)) {
            try {
                skinData = File.ReadAllBytes(skinPath);

                // validate size
                if (skinData.Length > 65536) {
                    Log.warn($"Local skin too large ({skinData.Length} bytes), using default");
                    skinData = [];
                } else {
                    // validate it's a valid PNG with transparency
                    using var ms = new MemoryStream(skinData);
                    var img = Image.Load<Rgba32>(ms);

                    if (!GL.BTexture2D.validateTransparency(img)) {
                        Log.warn("Local skin is too transparent, using default");
                        skinData = [];
                    } else {
                        Log.info($"Sending local skin to server ({skinData.Length} bytes, {img.Width}x{img.Height})");
                    }
                }
            } catch (Exception e) {
                Log.warn("Failed to load local skin, using default");
                Log.warn(e);
                skinData = [];
            }
        }

        // send skin packet to server (empty = default)
        ClientConnection.instance.send(new PlayerSkinPacket {
            entityID = ClientConnection.instance.entityID,
            skinData = skinData
        }, DeliveryMethod.ReliableOrdered);
    }

    public void handleLoginFailed(LoginFailedPacket p) {
        Log.error($"Login failed: {p.reason}");

        // if we're on an auth menu, show inline error instead of just dcing immediately
        Game.instance.executeOnMainThread(() => {
            var currentMenu = Game.instance.currentScreen.currentMenu;
            if (currentMenu == Menu.LOGIN_MENU) {
                Menu.LOGIN_MENU.showError(p.reason);
            } else if (currentMenu == Menu.REGISTER_MENU) {
                Menu.REGISTER_MENU.showError(p.reason);
            } else {
                // not on auth menu? disconnect and show error
                ClientConnection.instance?.disconnect();
                Game.disconnectAndReturnToMenu();
                Menu.DISCONNECTED_MENU.show(p.reason, kicked: false);
                Game.instance.switchTo(Menu.DISCONNECTED_MENU);
            }
        });
    }

    public void handleDisconnect(DisconnectPacket p) {
        Log.info($"Disconnected by server: {p.reason}");

        // flag already set in OnNetworkReceive (network thread) to prevent race condition

        // disconnect and show kicked menu
        ClientConnection.instance?.disconnect();
        Game.instance.executeOnMainThread(() => {
            Game.disconnectAndReturnToMenu();
            Menu.DISCONNECTED_MENU.show(p.reason, kicked: true);
            Game.instance.switchTo(Menu.DISCONNECTED_MENU);
        });
    }

    public void handleTeleport(TeleportPacket p) {
        if (Game.player == null) {
            return;
        }

        // snap player to new position (no interp)
        Game.player.teleport(p.position);
        Game.player.rotation = p.rotation;

        Log.info($"Teleported to {p.position}");
    }

    public void handleChunkData(ChunkDataPacket p) {
        //Console.Out.WriteLine($"CLIENT: Received ChunkDataPacket for {p.coord}");

        // ensure world is loaded
        if (Game.world == null) {
            Log.warn("Received ChunkDataPacket but world is not loaded?");
            return;
        }

        // get or create chunk
        // we bypass chunk loading entirely!
        var succ = Game.world.getChunkMaybe(p.coord, out var chunk);
        var existing = false;
        if (!succ) {
            //Console.Out.WriteLine($"CLIENT: Creating new chunk at {p.coord}");
            chunk = new Chunk(Game.world, p.coord.x, p.coord.z);
        } else {
            //Console.Out.WriteLine($"CLIENT: Updating existing chunk at {p.coord}");
            existing = true;
        }

        // deserialize subchunks
        foreach (var subData in p.subChunks) {
            chunk.blocks[subData.y].read(subData);
        }

        // mark chunk as ready (skip worldgen, already lighted by server)
        // always set to LIGHTED when receiving chunk data from server (even for updates)
        // this ensures neighbor checks pass during meshing
        if (!existing || chunk.status < ChunkStatus.LIGHTED) {
            chunk.status = ChunkStatus.LIGHTED;
        }

        if (!succ) {
            Game.world.addChunk(p.coord, chunk);
        }

        // queue this chunk for meshing
        for (int y = 0; y < Chunk.CHUNKHEIGHT; y++) {
            Game.world.dirtyChunk(new SubChunkCoord(chunk.coord.x, y, chunk.coord.z));
        }

        // trigger remeshing of neighbours that were waiting for this chunk
        // (same logic as in World.loadChunk for LIGHTED status)
        Span<ChunkCoord> neighbours = [
            new(p.coord.x - 1, p.coord.z),
            new(p.coord.x + 1, p.coord.z),
            new(p.coord.x, p.coord.z - 1),
            new(p.coord.x, p.coord.z + 1),
            new(p.coord.x - 1, p.coord.z - 1),
            new(p.coord.x - 1, p.coord.z + 1),
            new(p.coord.x + 1, p.coord.z - 1),
            new(p.coord.x + 1, p.coord.z + 1)
        ];

        foreach (var neighbourCoord in neighbours) {
            if (Game.world.getChunkMaybe(neighbourCoord, out var neighbourChunk) &&
                neighbourChunk.status >= ChunkStatus.LIGHTED) {
                // neighbour is loaded and lighted, dirty it to trigger remesh attempt
                for (int y = 0; y < Chunk.CHUNKHEIGHT; y++) {
                    Game.world.dirtyChunk(new SubChunkCoord(neighbourCoord.x, y, neighbourCoord.z));
                }
            }
        }

        // check if we now have minimum chunks to load in
        if (ClientConnection.instance != null && !ClientConnection.instance.initialChunksLoaded) {
            if (ClientConnection.instance.hasMinimumChunks()) {
                ClientConnection.instance.initialChunksLoaded = true;
                Log.info("Minimum chunks loaded, re-meshing...");

                // re-dirty all loaded chunks to ensure clean meshing
                foreach (var loadedChunk in Game.world.chunkList) {
                    for (int y = 0; y < Chunk.CHUNKHEIGHT; y++) {
                        Game.world.dirtyChunk(new SubChunkCoord(loadedChunk.coord.x, y, loadedChunk.coord.z));
                    }
                }
            }
            else {
                // debug: log why we're stuck
                if (Game.world.chunkList.Count % 10 == 0) {
                    Log.info($"Waiting for chunks: {Game.world.chunkList.Count} chunks loaded so far");
                }
            }
        }

        //Log.info($"Received chunk {p.coord.x},{p.coord.z} with {p.subChunks.Length} subchunks");
    }

    public void handleUnloadChunk(UnloadChunkPacket p) {
        //Console.Out.WriteLine($"CLIENT: Received UnloadChunkPacket for {p.coord}");

        if (Game.world == null) {
            Log.warn("Received UnloadChunkPacket but world is not loaded?");
            return;
        }

        // unload chunk from world
        var succ = Game.world.getChunkMaybe(p.coord, out var chunk);
        if (succ && chunk != null) {
            //Console.Out.WriteLine($"CLIENT: Unloading chunk {p.coord}, total chunks: {Game.world.chunks.Count}");
            Game.world.unloadChunk(p.coord);
        } else {
            //Console.Out.WriteLine($"CLIENT: Tried to unload {p.coord} but it doesn't exist!");
        }
    }

    public void handleBlockChange(BlockChangePacket p) {
        if (Game.world == null) {
            return;
        }

        // apply block change from server
        var pos = p.position;
        var cb = 0u.setID(p.blockID).setMetadata(p.metadata);
        Game.world.setBlockMetadata(pos.X, pos.Y, pos.Z, cb);
    }

    public void handleMultiBlockChange(MultiBlockChangePacket p) {
        if (Game.world == null) {
            return;
        }

        // apply multiple block changes from server
        for (int i = 0; i < p.pos.Length; i++) {
            var position = p.pos[i];
            var cb = 0u.setID(p.blockIDs[i]).setMetadata(p.metadata[i]);
            Game.world.setBlockMetadata(position.X, position.Y, position.Z, cb);
        }
    }

    public void handleBlockBreakProgress(BlockBreakProgressPacket p) {
        // TODO: render breaking animation for other players
        // for now, just ignore (visual only)
    }

    public void handleSpawnEntity(SpawnEntityPacket p) {
        if (Game.world == null) {
            return;
        }

        // create entity by type
        var entity = Entities.create(Game.world, p.entityType);
        if (entity == null) {
            Log.error($"Failed to create entity of type {p.entityType}");
            return;
        }

        // set basic properties
        entity.id = p.entityID;
        entity.position = p.position;
        entity.rotation = p.rotation;
        entity.velocity = p.velocity;

        // initialize interpolation targets for mobs (delta packets apply to these...)
        if (entity is Mob mob) {
            mob.targetPos = p.position;
            mob.targetRot = p.rotation;
            mob.interpolationTicks = 0;
        }

        // deserialize entity-specific data
        EntityTracker.deserializeExtraData(entity, p.extraData);

        // add to world
        Game.world.addEntity(entity);
        //Log.info($"Spawned entity type={Entities.getName(p.entityType)} id={p.entityID}");
    }

    public void handleDespawnEntity(DespawnEntityPacket p) {
        if (Game.world == null) {
            return;
        }

        // find and remove entity
        var entity = Game.world.entities.FirstOrDefault(e => e.id == p.entityID);
        if (entity != null) {
            Game.world.removeEntity(entity);
            Log.info($"Despawned entity id={p.entityID}");
        }
    }

    public void handleSpawnPlayer(SpawnPlayerPacket p) {
        if (Game.world == null) {
            Log.warn("we might crash?");
        }

        // don't spawn ourselves
        if (ClientConnection.instance != null && p.entityID == ClientConnection.instance.entityID) {
            Log.info($"[Client] Ignoring spawn for own player '{p.username}' (entityID={p.entityID})");
            return;
        }

        // check if already spawned
        var existing = Game.world.entities.FirstOrDefault(e => e.id == p.entityID);
        if (existing != null) {
            Log.warn($"[Client] Player '{p.username}' (entityID={p.entityID}) already exists! Skipping duplicate spawn.");
            return;
        }

        // spawn other player entity
        var player = new Humanoid(Game.world, (int)p.position.X, (int)p.position.Y, (int)p.position.Z) {
            id = p.entityID,
            name = p.username,
            position = p.position,
            rotation = p.rotation,
            sneaking = p.sneaking,
            flyMode = p.flying,
            gameMode = GameMode.creative // TODO assume creative for now...
        };

        Game.world.addEntity(player);
        Log.info($"[Client] Spawned player '{p.username}' (entityID={p.entityID})");
    }

    public void handleEntityPosition(EntityPositionPacket p) {
        if (Game.world == null) {
            return;
        }

        // find entity and update position
        var entity = Game.world.entities.FirstOrDefault(e => e.id == p.entityID);
        if (entity != null) {
            if (entity is Humanoid humanoid) {
                // use packet's position with current target rotation
                humanoid.mpInterpolate(p.position, humanoid.targetRot);
            }
            else {
                entity.position = p.position;
            }
        }
    }

    public void handleEntityRotation(EntityRotationPacket p) {
        if (Game.world == null) {
            return;
        }

        // find entity and update rotation
        var entity = Game.world.entities.FirstOrDefault(e => e.id == p.entityID);
        if (entity != null) {
            if (entity is Humanoid humanoid) {
                // use packet's rotation with current target position
                humanoid.mpInterpolate(humanoid.targetPos, p.rotation);
            }
            else {
                entity.rotation = p.rotation;
                entity.bodyRotation = p.bodyRotation;
            }
        }
    }

    public void handleEntityPositionRotation(EntityPositionRotationPacket p) {
        if (Game.world == null) {
            return;
        }

        // find entity and update both position and rotation
        var entity = Game.world.entities.FirstOrDefault(e => e.id == p.entityID);
        if (entity != null) {
            if (entity is Humanoid humanoid) {
                humanoid.mpInterpolate(p.position, p.rotation);
            }
            else if (entity is Mob mob) {
                mob.mpInterpolate(p.position, p.rotation);
            }
            else {
                entity.position = p.position;
                entity.rotation = p.rotation;
            }
        }
    }

    public void handleEntityPositionDelta(EntityPositionDeltaPacket p) {
        if (Game.world == null) {
            return;
        }

        // find entity and apply delta to last received position+rotation
        var entity = Game.world.entities.FirstOrDefault(e => e.id == p.entityID);
        if (entity != null) {
            if (entity is Humanoid humanoid) {
                p.applyDelta(humanoid.targetPos, humanoid.targetRot, out var newPos, out var newRot);
                humanoid.mpInterpolate(newPos, newRot);
            }
            else if (entity is Mob mob) {
                p.applyDelta(mob.targetPos, mob.targetRot, out var newPos, out var newRot);
                mob.mpInterpolate(newPos, newRot);
            }
            else {
                p.applyDelta(entity.position, entity.rotation, out var newPos, out var newRot);
                entity.position = newPos;
                entity.rotation = newRot;
            }
        }
    }

    public void handleEntityVelocity(EntityVelocityPacket p) {
        if (Game.world == null) {
            return;
        }

        // find entity and update velocity
        var entity = Game.world.entities.FirstOrDefault(e => e.id == p.entityID);
        if (entity != null) {
            entity.prevVelocity = entity.velocity;
            entity.velocity = p.velocity;
        }
    }

    public void handleEntityState(EntityStatePacket p) {
        if (Game.world == null) {
            return;
        }

        // find entity and apply state
        var entity = Game.world.entities.FirstOrDefault(e => e.id == p.entityID);
        if (entity != null) {
            entity.state.deserialize(p.data);
            entity.applyState();
        }
    }

    public void handleEntityAction(EntityActionPacket p) {
        if (Game.world == null) {
            return;
        }

        // find entity and apply action
        var entity = Game.world.entities.FirstOrDefault(e => e.id == p.entityID);
        if (entity != null) {
            switch (p.action) {
                case EntityActionPacket.Action.SWING:
                    if (entity is Player player) {
                        player.setSwinging(true);
                    }

                    break;
                case EntityActionPacket.Action.TAKE_DAMAGE:
                    // trigger damage flash/animation on entity
                    entity.dmgTime = 30;
                    break;
                case EntityActionPacket.Action.DEATH:
                    // mark entity as dead (starts death animation)
                    if (!entity.dead) {
                        entity.dead = true;
                        if (entity is Mob mob) {
                            mob.dieTime = 0;
                        }
                    }
                    break;
                // todo other actions (EAT, CRITICAL_HIT)
            }
        }
    }

    public void handlePlayerHealth(PlayerHealthPacket p) {
        // update local player's health
        if (Game.player != null) {
            Game.player.hp = p.health;
            Game.player.dmgTime = p.damageTime;

            // check if player died (health <= 0)
            if (p.health <= 0 && !Game.player.dead) {
                Game.player.die();
            }
        }
    }

    public void handleChatMessage(ChatMessagePacket p) {
        // add to chat UI
        if (Game.instance.currentScreen is GameScreen gs) {
            gs.CHAT.addMessage(p.message);

            // log to console as well
            Log.info($"{p.message}");

            // also open chat
        }
    }

    public void handlePlayerListAdd(PlayerListAddPacket p) {
        if (ClientConnection.instance == null) {
            return;
        }

        // add or update player in list
        ClientConnection.instance.playerList[p.entityID] = new PlayerListEntry(p.entityID, p.username, p.ping);
        Log.info($"Player list: added {p.username} (ping={p.ping}ms)");
    }

    public void handlePlayerListRemove(PlayerListRemovePacket p) {
        if (ClientConnection.instance == null) {
            return;
        }

        // remove player from list
        if (ClientConnection.instance.playerList.Remove(p.entityID)) {
            Log.info($"Player list: removed entityID={p.entityID}");
        }
    }

    public void handlePlayerListUpdatePing(PlayerListUpdatePingPacket p) {
        if (ClientConnection.instance == null) {
            return;
        }

        // update ping for player
        if (ClientConnection.instance.playerList.TryGetValue(p.entityID, out var entry)) {
            entry.ping = p.ping;
        }
    }

    public void handlePlayerSkin(PlayerSkinPacket p) {
        if (Game.world == null) {
            return;
        }

        // find player entity
        var entity = Game.world.entities.FirstOrDefault(e => e.id == p.entityID);
        if (entity is not Player player) {
            return;
        }

        // empty skin data = use default
        if (p.skinData.Length == 0) {
            player.skinTex?.Dispose();
            player.skinTex = null;
            return;
        }

        // validate size (max 64KB)
        if (p.skinData.Length > 65536) {
            Log.warn($"Rejected oversized skin for player {p.entityID}: {p.skinData.Length} bytes");
            return;
        }

        try {
            // load and validate skin
            using var ms = new MemoryStream(p.skinData);
            var img = Image.Load<Rgba32>(ms);

            if (!GL.BTexture2D.validateTransparency(img)) {
                Log.warn($"Rejected transparent skin for player {p.entityID}");
                return;
            }

            // create texture and load from bytes
            player.skinTex?.Dispose();
            player.skinTex = new GL.BTexture2D("");
            player.skinTex.loadFromBytes(p.skinData);

            Log.info($"Loaded skin for player {p.entityID} ({p.skinData.Length} bytes, {img.Width}x{img.Height})");
        } catch (Exception e) {
            Log.warn($"Failed to load skin for player {p.entityID}: {e.Message}");
        }
    }

    public void handleTimeUpdate(TimeUpdatePacket p) {
        // sync world time from server
        if (Game.world != null) {
            Game.world.worldTick = p.worldTick;
        }
    }

    public void handleUpdateBlockEntity(UpdateBlockEntityPacket p) {
        if (Game.world == null) {
            return;
        }

        // get or create block entity
        var blockEntity = Game.world.getBlockEntity(p.position.X, p.position.Y, p.position.Z);
        if (blockEntity == null) {
            // block entity doesn't exist - create it if the block is an EntityBlock
            var blockID = Game.world.getBlock(p.position.X, p.position.Y, p.position.Z);
            var block = Block.get(blockID);
            if (block is EntityBlock entityBlock) {
                blockEntity = entityBlock.get();
                blockEntity.pos = p.position;
                Game.world.setBlockEntity(p.position.X, p.position.Y, p.position.Z, blockEntity);
            } else {
                Log.warn("Tried to create a block entity for a plain block?");
                return; // not an entity block, ignore
            }
        }

        // deserialize and apply block entity data
        var nbt = (NBTCompound)NBT.read(p.nbt);
        blockEntity.read(nbt);
    }

    public void handleSetSlot(SetSlotPacket p) {
        if (Game.player == null) {
            return;
        }

        // update specific inventory slot (server authoritative)
        if (p.invID == Constants.INV_ID_CURSOR) {
            // cursor update
            Game.player.inventory.cursor = p.stack;
        } else if (p.invID == Constants.INV_ID_PLAYER) {
            // player inventory
            if (p.slotIndex < Game.player.inventory.size()) {
                Game.player.inventory.setStack((int)p.slotIndex, p.stack);
            }
        } else {
            // other inventory (chest, furnace, etc)
            if (Game.player.currentInventoryID == p.invID && Game.player.currentCtx != null) {
                var slots = Game.player.currentCtx.getSlots();
                if (p.slotIndex < slots.Count) {
                    var slot = slots[p.slotIndex];
                    if (slot.inventory != null) {
                        slot.inventory.setStack(slot.index, p.stack);

                        // if furnace input slot changed, recalculate recipe
                        // todo we could make this more general but this will do for now...
                        if (Game.player.currentCtx is FurnaceMenuContext && slot.inventory is FurnaceBlockEntity furnace && slot.index == 0) {
                            furnace.currentRecipe = p.stack != ItemStack.EMPTY ? SmeltingRecipe.findRecipe(p.stack.getItem()) : null;
                        }
                    }
                }
            }
        }
    }

    public void handleInventorySync(InventorySyncPacket p) {
        if (Game.player == null) {
            return;
        }

        // full inventory sync from server (on login or inventory opening)
        if (p.invID == Constants.INV_ID_PLAYER) {
            // player inventory
            // split array back into slots, armour, accessories, and crafting grid (if survival)
            int idx = 0;
            for (int i = 0; i < Game.player.inventory.slots.Length && idx < p.items.Length; i++, idx++) {
                Game.player.inventory.slots[i] = p.items[idx];
            }

            for (int i = 0; i < Game.player.inventory.armour.Length && idx < p.items.Length; i++, idx++) {
                Game.player.inventory.armour[i] = p.items[idx];
            }

            for (int i = 0; i < Game.player.inventory.accessories.Length && idx < p.items.Length; i++, idx++) {
                Game.player.inventory.accessories[i] = p.items[idx];
            }

            // if survival mode, also sync crafting grid (2x2 = 4 slots)
            if (Game.player.inventoryCtx is SurvivalInventoryContext survCtx) {
                var grid = survCtx.getCraftingGrid();
                for (int i = 0; i < grid.grid.Length && idx < p.items.Length; i++, idx++) {
                    grid.grid[i] = p.items[idx];
                }
                grid.updateResult();
            }

            Log.info("Received full inventory sync from server");
        }
        else {
            // inventory - sync to currentCtx
            if (Game.player.currentCtx != null && p.invID == Game.player.currentInventoryID) {
                // currentCtx has references to the block entity's inventory
                // the InventorySyncPacket contains just the inventory slots
                // we need to update the underlying inventory that currentCtx points to

                if (Game.player.currentCtx is ChestMenuContext chestCtx) {
                    for (int i = 0; i < p.items.Length && i < (chestCtx.chestInv as ChestBlockEntity).slots.Length; i++) {
                        (chestCtx.chestInv as ChestBlockEntity).slots[i] = p.items[i];
                    }
                }
                else if (Game.player.currentCtx is FurnaceMenuContext furnaceCtx) {
                    for (int i = 0; i < p.items.Length && i < (furnaceCtx.getFurnaceInventory() as FurnaceBlockEntity).slots.Length; i++) {
                        (furnaceCtx.getFurnaceInventory() as FurnaceBlockEntity).slots[i] = p.items[i];
                    }
                }
                else if (Game.player.currentCtx is CraftingTableContext craftingCtx) {
                    var grid = craftingCtx.getCraftingGrid();
                    for (int i = 0; i < p.items.Length && i < grid.grid.Length; i++) {
                        grid.grid[i] = p.items[i];
                    }
                    grid.updateResult();
                }

                Log.info($"Received invsync (invID={p.invID})");
            }
        }
    }

    public void handleInventoryAck(InventoryAckPacket p) {
        if (!p.acc) {
            Log.warn($"Server rejected inventory transaction (invID={p.invID}, actionID={p.actionID})");
            // set flag to stop sending clicks until resync completes
            ClientConnection.instance.waitingForResync = true;
        }
    }

    public void handleResyncComplete(ResyncCompletePacket p) {
        // resync complete - send acknowledgment and resume sending clicks
        ClientConnection.instance.send(new ResyncAckPacket {
            actionID = p.actionID
        }, LiteNetLib.DeliveryMethod.ReliableOrdered);

        ClientConnection.instance.waitingForResync = false;
    }

    public void handleGamemode(GamemodePacket p) {
        if (Game.player == null) {
            return;
        }

        // close any open menu before switching contexts
        // (prevents fuckup changing game modes with the playing somehow hacing chest/crafting table open??? you cant write in a chatbox like that but someone else might switch you like the console)
        if (Screen.GAME_SCREEN.currentMenu != null) {
            Screen.GAME_SCREEN.backToGame();
        }

        // close context - reset to player inventory
        Game.player.closeInventory();

        // update local player's game mode
        var id = p.gamemode;
        Game.player.gameMode = GameMode.fromID(id);

        // update both inventoryCtx and currentCtx (SurvivalInventoryMenu reads from inventoryCtx!)
        if (Game.player.gameMode == GameMode.creative) {
            Game.player.inventoryCtx = new CreativeInventoryContext(Game.player.inventory, 40);
            Game.player.currentCtx = Game.player.inventoryCtx;
        }
        else {
            Game.player.inventoryCtx = new SurvivalInventoryContext(Game.player.inventory);
            Game.player.currentCtx = Game.player.inventoryCtx;
        }

        Log.info($"Game mode changed to {p.gamemode}");
    }

    public void handleHeldItemChange(HeldItemChangePacket p) {
        if (Game.world == null) {
            return;
        }

        // update held item for other players
        var entity = Game.world.entities.FirstOrDefault(e => e.id == p.entityID);
        if (entity is Humanoid humanoid) {
            humanoid.inventory.selected = p.slotIndex;
            // set the held item in their inventory
            humanoid.inventory.setStack(p.slotIndex, p.heldItem);
        }
    }

    public void handleInventoryOpen(InventoryOpenPacket p) {
        if (Game.world == null || Game.player == null) {
            return;
        }

        // sync window ID
        Game.player.currentInventoryID = p.invID;

        // get block entity at position if applicable (chest, furnace need it; crafting table doesn't)
        BlockEntity? blockEntity = null;
        if (p.position.HasValue && p.invType != 1) { // invType 1 = crafting table (no block entity)
            var pos = p.position.Value;
            blockEntity = Game.world.getBlockEntity(pos.X, pos.Y, pos.Z);
            if (blockEntity == null) {
                Log.warn($"Received InventoryOpenPacket but no block entity at {p.position}?");
                return;
            }
        }

        // open UI on main thread
        // invType: 0=chest, 1=crafting table, 2=furnace
        switch (p.invType) {
            case 0: {
                // chest
                var chestBE = blockEntity as ChestBlockEntity;
                if (chestBE == null) return;

                var ctx = new ChestMenuContext(Game.player.inventory, chestBE);
                Game.player.currentCtx = ctx;

                Screen.GAME_SCREEN.switchToMenu(new ChestMenu(new Vector2I(0, 32), ctx));
                ((ChestMenu)Screen.GAME_SCREEN.currentMenu!).setup();

                Game.world.inMenu = true;
                Game.instance.unlockMouse();
                break;
            }
            case 1: {
                // crafting table (no block entity, temporary crafting grid)
                var ctx = new CraftingTableContext(Game.player);
                Game.player.currentCtx = ctx;

                Screen.GAME_SCREEN.switchToMenu(new CraftingTableMenu(new Vector2I(0, 32), ctx));
                ((CraftingTableMenu)Screen.GAME_SCREEN.currentMenu!).setup();

                Game.world.inMenu = true;
                Game.instance.unlockMouse();
                break;
            }
            case 2: {
                // furnace
                var furnaceBE = blockEntity as FurnaceBlockEntity;
                if (furnaceBE == null) return;

                var ctx = new FurnaceMenuContext(Game.player.inventory, furnaceBE);
                Game.player.currentCtx = ctx;

                Screen.GAME_SCREEN.switchToMenu(new FurnaceMenu(new Vector2I(0, 32), ctx));
                ((FurnaceMenu)Screen.GAME_SCREEN.currentMenu!).setup();

                Game.world.inMenu = true;
                Game.instance.unlockMouse();
                break;
            }
        }

        Log.info($"Opened inventory type={p.invType} title='{p.title}' at {p.position}");
    }

    public void handleInventoryClose(InventoryClosePacket p) {
        if (Game.player == null) {
            return;
        }

        // clear cursor
        Game.player.inventory.cursor = ItemStack.EMPTY;

        // close current inventory
        Game.player.closeInventory();

        // close menu on main thread if it's open
        Game.instance.executeOnMainThread(() => {
            if (Screen.GAME_SCREEN.currentMenu != null) {
                Screen.GAME_SCREEN.backToGame();
            }
        });

        Log.info("Closed inventory");
    }

    public void handleFurnaceSync(FurnaceSyncPacket p) {
        if (Game.world == null) {
            return;
        }

        // update furnace state
        var blockEntity = Game.world.getBlockEntity(p.position.X, p.position.Y, p.position.Z);
        if (blockEntity is FurnaceBlockEntity furnace) {
            furnace.fuelRemaining = p.fuelRemaining;
            furnace.fuelMax = p.fuelMax;
            furnace.smeltProgress = p.smeltProgress;

            // update currentRecipe on client (needed for getSmeltProgress() calculation)
            // server sets this during update(), but client doesn't run update() :)
            if (p.smeltProgress > 0 && furnace.slots[0] != ItemStack.EMPTY) {
                // only update recipe if we don't have one or input changed
                furnace.currentRecipe ??= SmeltingRecipe.findRecipe(furnace.slots[0].getItem());
            } else {
                // no progress or no item = no recipe
                furnace.currentRecipe = null;
            }
        }
    }

    public void handleRespawn(RespawnPacket p) {
        if (Game.player == null) {
            return;
        }

        Log.info($"Respawned at {p.spawnPosition}");

        // respawn the player locally
        Game.player.dead = false;
        Game.player.hp = 100;
        Game.player.bodyRotation.Z = 0f;
        Game.player.prevBodyRotation.Z = 0f;
        Game.player.dieTime = 0;
        Game.player.fireTicks = 0;

        // teleport to spawn position from server
        Game.player.teleport(p.spawnPosition);
        Game.player.rotation = p.rotation;

        // reset velocity
        Game.player.velocity = Vector3D.Zero;
        Game.player.prevVelocity = Vector3D.Zero;

        // close death menu and return to game
        Game.instance.executeOnMainThread(() => {
            Screen.GAME_SCREEN.backToGame();
        });
    }
}