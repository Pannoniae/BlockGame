using BlockGame.logic;
using BlockGame.main;
using BlockGame.net.packet;
using BlockGame.net.srv;
using BlockGame.render;
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
            case RenderDistancePacket p:
                handleRenderDistance(p);
                break;
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
            case LightUpdatePacket p:
                handleLightUpdate(p);
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
            case EntityPositionRotationPacket p:
                handleEntityPositionRotation(p);
                break;
            case EntityImpulsePacket p:
                handleEntityImpulse(p);
                break;
            case ServerStatsPacket p:
                handleServerStats(p);
                break;
            case EntityStatePacket p:
                handleEntityState(p);
                break;
            case EntityActionPacket p:
                handleEntityAction(p);
                break;
            case EntityDamagePacket p:
                handleEntityDamage(p);
                break;
            case PlayerHealthPacket p:
                handlePlayerHealth(p);
                break;
            case AddEffectPacket p:
                handleAddEffect(p);
                break;
            case RemoveEffectPacket p:
                handleRemoveEffect(p);
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
            case InventoryResyncTermPacket p:
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

    private void handleRenderDistance(RenderDistancePacket p) {
        ClientConnection.instance!.serverRenderDistance = p.dist;
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
        var world = new World(Side.CLIENT, "__multiplayer", 0, "Multiplayer World", "flat");

        // set world time
        world.worldTick = p.worldTick;

        // create player entity
        var pos = new Vector3I((int)p.spawnPos.X, (int)p.spawnPos.Y, (int)p.spawnPos.Z);
        Game.player = new ClientPlayer(world, pos.X, pos.Y, pos.Z) {
            id = p.entityID, // set entity ID from server
            position = p.spawnPos,
            rotation = p.rotation,
            gameMode = p.creative ? GameMode.creative : GameMode.survival,
            name = Settings.instance.playerName,
            hp = p.initialHP // set initial health from server
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

        ClientConnection.sendRenderDistance();

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
                Log.info("Minimum chunks loaded");
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

    public void handleLightUpdate(LightUpdatePacket p) {
        if (Game.world == null) {
            return;
        }

        if (!Game.world.getChunkMaybe(p.coord, out var chunk) || chunk == null || chunk.destroyed) {
            return;
        }

        chunk.blocks[p.y].setLightPlanes(p.skyPlane, p.blockPlane, p.uniformSky, p.uniformBlock);

        for (int dy = -1; dy <= 1; dy++) {
            var sy = p.y + dy;
            if (sy is < 0 or >= Chunk.CHUNKHEIGHT) {
                continue;
            }

            for (int dx = -1; dx <= 1; dx++) {
                for (int dz = -1; dz <= 1; dz++) {
                    Game.world.dirtyChunk(new SubChunkCoord(p.coord.x + dx, sy, p.coord.z + dz));
                }
            }
        }
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
        if (Game.world == null) {
            return;
        }

        // update other player's break progress
        if (p.progress <= 0) {
            // cancelled or finished, remove from tracking
            conn.breaks.Remove(p.position);
        }
        else {
            // update or add break progress
            conn.breaks.Set(p.position, new OtherPlayerBreak {
                entityID = p.playerEntityID,
                progress = p.progress,
                lastUpdateTick = Game.world.worldTick
            });
        }
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
        entity.resetSnapshots(p.position, p.rotation);

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

        var entity = Game.world.getEntity(p.entityID);

        if (entity != null) {
            if (entity is Player pl) {
                pl.skinTex?.Dispose();
                pl.skinTex = null;
            }

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

        var existing = Game.world.getEntity(p.entityID);
        if (existing != null) {
            Log.warn($"[Client] Player '{p.username}' (entityID={p.entityID}) already exists! Skipping duplicate spawn.");
            return;
        }

        // a remote player is just a Player that doesn't move itself
        var player = new Player(Game.world, (int)p.position.X, (int)p.position.Y, (int)p.position.Z) {
            id = p.entityID,
            name = p.username,
            sneaking = p.sneaking,
            flyMode = p.flying,
            gameMode = GameMode.creative // TODO assume creative for now...
        };

        player.resetSnapshots(p.position, p.rotation);
        Game.world.addEntity(player);

        if (ClientConnection.instance?.playerList.GetValueOrDefault(p.entityID)?.skin is { } skin) {
            applySkin(player, skin);
        }
        Log.info($"[Client] Spawned player '{p.username}' (entityID={p.entityID})");
    }

    public void handleEntityPositionRotation(EntityPositionRotationPacket p) {
        if (Game.world == null) {
            return;
        }

        NetClock.onRecv(p.tick);

        var e = Game.world.getEntity(p.entityID);
        if (e != null && !e.hasmovement) {
            e.addSnapshot(p.tick, p.position, p.rotation);
        }
    }

    public static ServerStatsPacket? serverStats;

    public const int SERVER_MSPT_HISTORY = 400;

    public static readonly XRingBuffer<float> serverMspt = history();
    public static int serverMsptIdx;

    private static XRingBuffer<float> history() {
        var b = new XRingBuffer<float>(SERVER_MSPT_HISTORY);
        for (var i = 0; i < SERVER_MSPT_HISTORY; i++) {
            b.PushBack(0f);
        }
        return b;
    }

    public static void clearServerStats() {
        serverStats = null;
        serverMsptIdx = 0;
        for (var i = 0; i < SERVER_MSPT_HISTORY; i++) {
            serverMspt[i] = 0f;
        }
    }

    public void handleServerStats(ServerStatsPacket p) {
        serverStats = p;

        foreach (var s in p.samples) {
            serverMspt[serverMsptIdx] = s;
            serverMsptIdx = (serverMsptIdx + 1) % SERVER_MSPT_HISTORY;
        }
    }

    public void handleEntityImpulse(EntityImpulsePacket p) {
        if (Game.player != null && p.entityID == Game.player.id) {
            Entity.applyImpulse(ref Game.player.velocity, p.impulse);
        }
    }

    public void handleEntityState(EntityStatePacket p) {
        if (Game.world == null) {
            return;
        }

        var entity = Game.world.getEntity(p.entityID);

        if (entity != null) {
            entity.state.deserialize(p.data);
            entity.applyState();
        }
    }

    public void handleEntityAction(EntityActionPacket p) {
        if (Game.world == null) {
            return;
        }

        var entity = Game.world.getEntity(p.entityID);
        if (entity != null) {
            switch (p.action) {
                case EntityActionPacket.Action.SWING:
                    if (entity is Player player) {
                        player.setSwinging(true);
                    }

                    break;
                // todo other actions (EAT, CRITICAL_HIT)
            }
        }
    }

    public void handleEntityDamage(EntityDamagePacket p) {
        var world = Game.world;
        if (world == null) {
            return;
        }

        var entity = world.getEntity(p.entityID);
        if (entity == null) {
            return;
        }

        if (entity == Game.player) {
            entity.dmgTime = 30;

            if (p.attackerID != -1 || world.worldTick - Game.player.lastDMGTick > 20) {
                Game.camera.applyImpact((float)p.damage * 4);
            }

            return;
        }

        if (p.attackerID == Game.player?.id && entity.consumePredictedHurt()) {
            return;
        }

        entity.showHurt(p.damage);
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

    public void handleAddEffect(AddEffectPacket p) {
        var world = Game.world;
        if (world == null) {
            return;
        }

        var entity = world.getEntity(p.entityID);
        if (entity == null) {
            return;
        }

        // create effect based on ID
        Effect? effect = null;
        if (p.effectID == EffectRegistry.REGEN) {
            effect = new RegenEffect(p.duration, p.value, p.amplifier);
        }

        if (effect != null) {
            // remove old effect of same type (override behavior)
            entity.removeEffect(p.effectID);
            entity.addEffect(effect);
        }
    }

    public void handleRemoveEffect(RemoveEffectPacket p) {
        var world = Game.world;
        if (world == null) {
            return;
        }

        world.getEntity(p.entityID)?.removeEffect(p.effectID);
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
        var list = ClientConnection.instance.playerList;
        list[p.entityID] = new PlayerListEntry(p.entityID, p.username, p.ping) {
            skin = list.GetValueOrDefault(p.entityID)?.skin
        };
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
        var conn = ClientConnection.instance;
        if (conn == null) {
            return;
        }

        // empty skin data = use default
        byte[]? skin = null;
        if (p.skinData.Length > 0) {
            if (!validSkin(p)) {
                return;
            }

            skin = p.skinData;
        }

        if (conn.playerList.TryGetValue(p.entityID, out var entry)) {
            entry.skin = skin;
        }

        if (Game.world?.getEntity(p.entityID) is Player player) {
            applySkin(player, skin);
        }
    }

    private static bool validSkin(PlayerSkinPacket p) {
        if (p.skinData.Length > 65536) {
            Log.warn($"Rejected oversized skin for player {p.entityID}: {p.skinData.Length} bytes");
            return false;
        }

        try {
            using var ms = new MemoryStream(p.skinData);
            var img = Image.Load<Rgba32>(ms);

            if (!GL.BTexture2D.validateTransparency(img)) {
                Log.warn($"Rejected transparent skin for player {p.entityID}");
                return false;
            }

            Log.info($"Got skin for player {p.entityID} ({p.skinData.Length} bytes, {img.Width}x{img.Height})");
            return true;
        } catch (Exception e) {
            Log.warn($"Failed to load skin for player {p.entityID}: {e.Message}");
            return false;
        }
    }

    private static void applySkin(Player player, byte[]? skin) {
        player.skinTex?.Dispose();
        player.skinTex = null;

        if (skin == null) {
            return;
        }

        try {
            player.skinTex = new GL.BTexture2D("");
            player.skinTex.loadFromBytes(skin);
        } catch (Exception e) {
            player.skinTex?.Dispose();
            player.skinTex = null;
            Log.warn($"Failed to upload skin for player {player.id}: {e.Message}");
        }
    }

    public void handleTimeUpdate(TimeUpdatePacket p) {
        // sync world time from server
        if (Game.world != null) {
            Game.world.worldTick = p.worldTick;

            if (p.snap) {
                // manual /time set - snap sky colours immediately to prevent lerp lag
                Game.renderer.currentSkyColour = Game.world.getSkyColour(p.worldTick);
                Game.renderer.targetSkyColour = Game.renderer.currentSkyColour;
                Game.renderer.currentHorizonColour = Game.graphics.getHorizonColour(Game.world, p.worldTick);
                Game.renderer.targetHorizonColour = Game.renderer.currentHorizonColour;

                Game.player.clearBreakProgressDecay();
            }
            else {
                Game.renderer.targetSkyColour = Game.world.getSkyColour(p.worldTick);
                Game.renderer.targetHorizonColour = Game.graphics.getHorizonColour(Game.world, p.worldTick);
            }
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
            if (p.slotIndex < PlayerInventory.TOTAL_SIZE) {
                Game.player.inventory.setStack(p.slotIndex, p.stack);
            }
            else if (Game.player.inventoryCtx is SurvivalInventoryContext surv) {
                surv.getCraftingGrid().setStack(p.slotIndex - PlayerInventory.TOTAL_SIZE, p.stack);
            }
        } else {
            // window packets are in context order
            if (Game.player.currentInventoryID == p.invID) {
                var slots = Game.player.currentCtx.getSlots();
                if (p.slotIndex < slots.Count) {
                    var slot = slots[p.slotIndex];
                    if (slot.inventory != null && slot.index >= 0) {
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

        if (p.invID == Constants.INV_ID_PLAYER) {
            var inv = Game.player.inventory;
            for (int i = 0; i < PlayerInventory.TOTAL_SIZE; i++) {
                inv.setStack(i, i < p.items.Length ? p.items[i] : ItemStack.EMPTY);
            }

            if (Game.player.inventoryCtx is SurvivalInventoryContext surv) {
                var grid = surv.getCraftingGrid();
                for (int i = 0; i < grid.grid.Length; i++) {
                    var idx = PlayerInventory.TOTAL_SIZE + i;
                    grid.grid[i] = idx < p.items.Length ? p.items[idx] : ItemStack.EMPTY;
                }

                grid.updateResult();
            }
        }
        else if (p.invID == Game.player.currentInventoryID) {
            // window packets are in context order, and the client built the same context from the open packet
            var slots = Game.player.currentCtx.getSlots();
            for (int i = 0; i < p.items.Length && i < slots.Count; i++) {
                var slot = slots[i];
                if (slot.inventory != null && slot.index >= 0) {
                    slot.inventory.setStack(slot.index, p.items[i]);
                }
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

    public void handleResyncComplete(InventoryResyncTermPacket p) {
        // resync complete - send acknowledgment and resume sending clicks
        ClientConnection.instance.send(new InventoryResyncAckPacket {
            actionID = p.actionID
        }, DeliveryMethod.ReliableOrdered);

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
        Game.player.inventory.cursor = ItemStack.EMPTY;

        // update local player's game mode
        var id = p.gamemode;
        Game.player.gameMode = GameMode.fromID(id);

        // survival means no flying...
        if (Game.player.gameMode.gameplay) {
            Game.player.flyMode = false;
        }

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

        // remote players only - our own inventory is the server's business through the inventory packets
        if (Game.world.getEntity(p.entityID) is Player { hasmovement: false } other) {
            other.inventory.selected = p.slotIndex;
            other.inventory.setStack(p.slotIndex, p.heldItem);
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
                if (chestBE == null) {
                    return;
                }

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
                if (furnaceBE == null) {
                    return;
                }

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
        if (Game.player == null || p.invID != Game.player.currentInventoryID) {
            return;
        }

        Game.player.inventory.cursor = ItemStack.EMPTY;
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
            furnace.smeltProgress = p.smeltProgress;
            furnace.smeltTime = p.smeltTime;       // direct from server - makes UI resilient to packet ordering
            furnace.fuelRemaining = p.fuelRemaining;
            furnace.fuelMax = p.fuelMax;

            // still update currentRecipe for other uses, but UI no longer depends on it
            // this can be temporarily wrong due to packet ordering, but it FINALLY won't break rendering
            if (p.smeltProgress > 0 && furnace.slots[0] != ItemStack.EMPTY) {
                furnace.currentRecipe = SmeltingRecipe.findRecipe(furnace.slots[0].getItem());
            } else if (p.smeltProgress == 0) {
                furnace.currentRecipe = null;
            }
            // else: keep existing recipe, slot data might be stale
        }
    }

    public void handleRespawn(RespawnPacket p) {
        if (Game.player == null) {
            return;
        }

        Log.info($"Respawned at {p.spawnPosition}");

        Game.player.closeInventory();
        Game.player.inventory.cursor = ItemStack.EMPTY;

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