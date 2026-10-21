using System.Numerics;
using BlockGame.logic;
using BlockGame.net.packet;
using BlockGame.util;
using BlockGame.util.cmd;
using BlockGame.util.log;
using BlockGame.util.xNBT;
using BlockGame.world.block;
using BlockGame.world.entity;
using BlockGame.world.item;
using BlockGame.world.item.inventory;
using LiteNetLib;
using Molten;
using Molten.DoublePrecision;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BlockGame.net.srv;

/** handles incoming packets on server */
public class ServerPacketHandler : PacketHandler {
    private readonly ServerConnection conn;

    public ServerPacketHandler(ServerConnection conn) {
        this.conn = conn;
    }

    public void handle(Packet packet) {
        switch (packet) {
            case HugPacket p:
                handleHug(p);
                break;
            case AuthPacket p:
                handleAuth(p);
                break;
            case PlayerPositionRotationPacket p:
                handlePlayerPositionRotation(p);
                break;
            case StartBlockBreakPacket p:
                handleStartBlockBreak(p);
                break;
            case CancelBlockBreakPacket p:
                handleCancelBlockBreak(p);
                break;
            case UpdateBlockBreakProgressPacket p:
                handleUpdateBlockBreakProgress(p);
                break;
            case FinishBlockBreakPacket p:
                handleFinishBlockBreak(p);
                break;
            case PlaceBlockPacket p:
                handlePlaceBlock(p);
                break;
            case UseItemPacket p:
                handleUseItem(p);
                break;
            case ChatMessagePacket p:
                handleChatMessage(p);
                break;
            case CommandPacket p:
                handleCommand(p);
                break;
            case RenderDistancePacket p:
                handleRenderDistance(p);
                break;
            case EntityStatePacket p:
                handleEntityState(p);
                break;
            case EntityActionPacket p:
                handleEntityAction(p);
                break;
            case AttackEntityPacket p:
                handleAttackEntity(p);
                break;
            case UpdateBlockEntityPacket p:
                handleUpdateBlockEntity(p);
                break;
            case InventorySlotClickPacket p:
                handleInventorySlotClick(p);
                break;
            case InventoryResyncAckPacket p:
                handleResyncAck(p);
                break;
            case InteractEntityPacket p:
                handleInteractEntity(p);
                break;
            case PlayerHeldItemChangePacket p:
                handlePlayerHeldItemChange(p);
                break;
            case InventoryClosePacket p:
                handleInventoryClose(p);
                break;
            case DropItemPacket p:
                handleDropItem(p);
                break;
            case RespawnRequestPacket p:
                handleRespawnRequest(p);
                break;
            case PlayerSkinPacket p:
                handlePlayerSkin(p);
                break;
            default:
                Log.warn($"Unhandled packet type: {packet.GetType().Name}");
                break;
        }
    }

    private void handleHug(HugPacket p) {
        Log.info($"Hug from {p.username} (protocol={p.netVersion}, version={p.version} contentHash={p.contentHash:X8})");

        // check protocol version
        if (p.netVersion != Constants.netVersion) {
            conn.send(new LoginFailedPacket {
                reason = $"Version mismatch (server: {Constants.netVersion}, client: {p.netVersion})"
            }, DeliveryMethod.ReliableOrdered);
            Log.info($"{p.username} tried to join with version {p.netVersion}, we are on {Constants.netVersion}");
            // let client disconnect gracefully after receiving LoginFailedPacket
            return;
        }

        // check content hash (blocks/items must match)
        if (p.contentHash != Constants.contentHash) {
            conn.send(new LoginFailedPacket {
                reason = $"Content mismatch - server and client have different blocks/items (server: {Constants.contentHash:X8}, client: {p.contentHash:X8})"
            }, DeliveryMethod.ReliableOrdered);
            Log.warn($"{p.username} tried to join with mismatched content (client: {p.contentHash:X8}, server: {Constants.contentHash:X8})");
            // let client disconnect gracefully after receiving LoginFailedPacket
            return;
        }

        // check if server full
        if (GameServer.instance.connections.Count >= GameServer.instance.maxPlayers) {
            conn.send(new LoginFailedPacket {
                reason = "Server full"
            }, DeliveryMethod.ReliableOrdered);
            Log.info($"Rejected {p.username} - server full");
            // let client disconnect gracefully after receiving LoginFailedPacket
            return;
        }

        // check if already connected with this username
        foreach (var existingConn in GameServer.instance.connections.Values) {
            if (existingConn.username == p.username) {
                conn.send(new LoginFailedPacket {
                    reason = "User already connected!"
                }, DeliveryMethod.ReliableOrdered);
                Log.info($"Rejected {p.username} - already connected");
                // let client disconnect gracefully after receiving LoginFailedPacket
                return;
            }
        }

        conn.username = p.username;

        if (conn.isHost) {
            Log.info($"User '{p.username}' logging in (integrated server)");
            finishLogin();
            return;
        }

        bool needsRegister = !GameServer.instance.userPasswords.ContainsKey(p.username);

        Log.info($"User '{p.username}' {(needsRegister ? "needs to register" : "logging in")}");

        conn.send(new AuthRequiredPacket {
            needsRegister = needsRegister
        }, DeliveryMethod.ReliableOrdered);
    }

    public void handleAuth(AuthPacket p) {
        if (string.IsNullOrEmpty(conn.username)) {
            conn.disconnect("No hug!");
            return;
        }

        if (GameServer.instance.userPasswords.TryGetValue(conn.username, out var hash)) {
            // login - verify password
            if (ServerAuth.verifyPassword(p.password, hash)) {
                finishLogin();
            }
            else {
                conn.send(new LoginFailedPacket {
                    reason = "Wrong password"
                }, DeliveryMethod.ReliableOrdered);
                // let client disconnect gracefully after receiving LoginFailedPacket
            }
        }
        else {
            // register - create new account
            GameServer.instance.userPasswords[conn.username] = ServerAuth.hashPassword(p.password);
            GameServer.instance.saveUsers();
            Log.info($"Registered new user: {conn.username}");
            finishLogin();
        }
    }

    private void finishLogin() {
        conn.authenticated = true;

        // spawn player entity
        conn.entityID = GameServer.getNewID();
        var world = GameServer.instance.world;

        // try to load existing player data
        var player = GameServer.instance.loadPlayerData(conn.username);

        if (player == null && conn.isHost && world.legacyPlayerNBT != null) {
            player = new ServerPlayer(world, 0, 0, 0);
            player.read(world.legacyPlayerNBT);
            world.legacyPlayerNBT = null;
            Log.info($"Migrated test player data for '{conn.username}' at {player.position}");
        }

        if (player == null) {
            // new player - create with defaults at world spawn
            var spawnPos = world.spawn;
            player = new ServerPlayer(world, (int)spawnPos.X, (int)spawnPos.Y, (int)spawnPos.Z);
            player.position = spawnPos;
            player.rotation = new Vector3(0, 0, 0);
            // set gamemode from server properties
            var gmStr = GameServer.instance.properties.getString("gamemode", "survival");
            player.gameMode = gmStr.ToLower() switch {
                "creative" or "c" or "1" => GameMode.creative,
                _ => GameMode.survival
            };

            // set correct inventory context to match gamemode
            if (player.gameMode == GameMode.creative) {
                player.inventoryCtx = new CreativeInventoryContext(player.inventory, 40);
            } else {
                player.inventoryCtx = new SurvivalInventoryContext(player.inventory);
            }
            player.currentCtx = player.inventoryCtx;

            Log.info($"Created new player data for '{conn.username}' at spawn {spawnPos}");
        }
        else {
            Log.info($"Loaded existing player data for '{conn.username}' at {player.position}");

            // fix up players with no game mode set
            if (player.gameMode == null!) {
                player.gameMode = GameMode.survival;
            }

            // ensure inventoryCtx matches gamemode (fix for old saves or corrupted data HOPEFULLY)
            bool needsCtxFix = (player.gameMode == GameMode.creative && player.inventoryCtx is not CreativeInventoryContext)
                            || (player.gameMode == GameMode.survival && player.inventoryCtx is not SurvivalInventoryContext);

            if (needsCtxFix) {
                if (player.gameMode == GameMode.creative) {
                    player.inventoryCtx = new CreativeInventoryContext(player.inventory, 40);
                } else {
                    player.inventoryCtx = new SurvivalInventoryContext(player.inventory);
                }
                player.currentCtx = player.inventoryCtx;
            }
        }

        player.id = conn.entityID;
        player.name = conn.username;
        player.world = world;
        player.conn = conn;

        conn.player = player;

        world.addEntity(player);

        // add player as viewer to their own inventory context for auto-sync
        player.inventoryCtx.addViewer(conn, Constants.INV_ID_PLAYER);

        // send login success
        conn.send(new LoginSuccessPacket {
            entityID = conn.entityID,
            spawnPos = player.position,
            rotation = player.rotation,
            worldTick = world.worldTick,
            creative = player.gameMode == GameMode.creative,
            initialHP = player.hp
        }, DeliveryMethod.ReliableOrdered);

        // hand back whatever the last save had on the cursor / in the 2x2 grid
        player.pickupLooseItems();
        player.syncInventory();

        // send initial health
        conn.send(new PlayerHealthPacket {
            health = player.hp,
            damageTime = 0
        }, DeliveryMethod.ReliableOrdered);

        GameServer.instance.connections[conn.entityID] = conn;

        Log.info($"Player '{conn.username}' logged in (entityID={conn.entityID})");

        // send all existing players to the new client (both player list and spawn packets)
        foreach (var existingConn in GameServer.instance.connections.Values) {
            if (existingConn.entityID == conn.entityID) {
                continue; // skip self
            }

            if (existingConn.player == null) {
                continue;
            }

            // add to player list
            conn.send(new PlayerListAddPacket {
                entityID = existingConn.entityID,
                username = existingConn.username,
                ping = existingConn.metrics.ping
            }, DeliveryMethod.ReliableOrdered);

            conn.send(new SpawnPlayerPacket {
                entityID = existingConn.entityID,
                username = existingConn.username,
                position = existingConn.player.position,
                rotation = existingConn.player.rotation,
                sneaking = existingConn.player.sneaking,
                flying = existingConn.player.flyMode
            }, DeliveryMethod.ReliableOrdered);

            // send initial entity state
            existingConn.player.syncState();
            existingConn.player.state.markAllDirty();
            conn.send(new EntityStatePacket {
                entityID = existingConn.entityID,
                data = existingConn.player.state.serializeAll()
            }, DeliveryMethod.ReliableOrdered);

            // send their held item
            conn.send(new HeldItemChangePacket {
                entityID = existingConn.entityID,
                slotIndex = (byte)existingConn.player.inventory.selected,
                heldItem = existingConn.player.inventory.getSelected()
            }, DeliveryMethod.ReliableOrdered);

            // send their skin
            conn.send(new PlayerSkinPacket {
                entityID = existingConn.entityID,
                skinData = existingConn.skinData
            }, DeliveryMethod.ReliableOrdered);
        }

        // broadcast the new player to all existing clients
        // first add to player list
        GameServer.instance.send(
            new PlayerListAddPacket {
                entityID = conn.entityID,
                username = conn.username,
                ping = conn.metrics.ping
            },
            DeliveryMethod.ReliableOrdered,
            exclude: conn
        );

        GameServer.instance.send(
            new SpawnPlayerPacket {
                entityID = conn.entityID,
                username = conn.username,
                position = player.position,
                rotation = player.rotation,
                sneaking = player.sneaking,
                flying = player.flyMode
            },
            DeliveryMethod.ReliableOrdered,
            exclude: conn
        );

        // broadcast initial entity state to all existing clients
        player.state.markAllDirty();
        GameServer.instance.send(
            new EntityStatePacket {
                entityID = conn.entityID,
                data = player.state.serializeAll()
            },
            DeliveryMethod.ReliableOrdered,
            exclude: conn
        );

        // send their held item
        GameServer.instance.send(new HeldItemChangePacket {
            entityID = conn.entityID,
            slotIndex = (byte)player.inventory.selected,
            heldItem = player.inventory.getSelected()
        }, DeliveryMethod.ReliableOrdered,
            exclude: conn
        );

        // broadcast their skin to all existing clients
        GameServer.instance.send(new PlayerSkinPacket {
            entityID = conn.entityID,
            skinData = conn.skinData
        }, DeliveryMethod.ReliableOrdered,
            exclude: conn
        );

        // add self to own player list
        conn.send(new PlayerListAddPacket {
            entityID = conn.entityID,
            username = conn.username,
            ping = conn.metrics.ping
        }, DeliveryMethod.ReliableOrdered);

        // send initial chunks around player
        conn.updateLoadedChunks();

        // broadcast join message to all players
        if (GameServer.instance.isDedicated || GameServer.instance.connections.Count > 1) {
            GameServer.instance.send(
                new ChatMessagePacket { message = $"&e{conn.username} &ajoined the game" },
                DeliveryMethod.ReliableOrdered
            );
        }

        // send message to Discord bridge if up
        GameServer.instance.discord?.sendMessage($"**{conn.username}** joined the game");
        GameServer.instance.discord?.updatePlayerCountStatus();
    }

    /** the EntityTracker relays it to everyone else, like any other entity */
    private void handlePlayerPositionRotation(PlayerPositionRotationPacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        conn.player.applyMove(p.position, normrot(p.rotation), p.onGround);
    }

    private void handleStartBlockBreak(StartBlockBreakPacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        // validate reach distance (max 5 blocks)
        var dist = Vector3D.Distance(conn.player.position, new Vector3D(p.position.X, p.position.Y, p.position.Z));
        if (dist > 7.5) {
            return; // too far
        }

        // track breaking state
        conn.breakingBlock = p.position;
        conn.breakProgress = 0;
        conn.lastProgressBroadcastTick = GameServer.instance.world.worldTick;

        // broadcast start to nearby players
        GameServer.instance.send(
            new Vector3D(p.position.X, p.position.Y, p.position.Z),
            128.0,
            new BlockBreakProgressPacket {
                playerEntityID = conn.entityID,
                position = p.position,
                progress = 0
            },
            DeliveryMethod.ReliableOrdered,
            conn // exclude sender
        );
    }

    private void handleCancelBlockBreak(CancelBlockBreakPacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        // broadcast cancel to nearby players if we were breaking something
        if (conn.breakingBlock.HasValue) {
            var pos = conn.breakingBlock.Value;
            GameServer.instance.send(
                new Vector3D(pos.X, pos.Y, pos.Z),
                128.0,
                new BlockBreakProgressPacket {
                    playerEntityID = conn.entityID,
                    position = pos,
                    progress = 0 // 0 or negative = cancelled
                },
                DeliveryMethod.Unreliable,
                conn // exclude sender
            );
        }

        // clear breaking state
        conn.breakingBlock = null;
        conn.breakProgress = 0;
    }

    private void handleUpdateBlockBreakProgress(UpdateBlockBreakProgressPacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        // validate player is actually breaking this block
        if (!conn.breakingBlock.HasValue || conn.breakingBlock.Value != p.position) {
            return; // ignore spurious progress updates
        }

        // update tracked progress
        conn.breakProgress = p.progress;
    }

    private void handleFinishBlockBreak(FinishBlockBreakPacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        var world = GameServer.instance.world;

        // validate reach distance
        var dist = Vector3D.Distance(conn.player.position, new Vector3D(p.position.X, p.position.Y, p.position.Z));
        if (dist > 7.5) {
            // too far - send revert (restore block to client)
            var currentBlock = world.getBlock(p.position.X, p.position.Y, p.position.Z);
            var currentMeta = world.getBlockRaw(p.position.X, p.position.Y, p.position.Z).getMetadata();
            conn.send(new BlockChangePacket {
                position = p.position,
                blockID = currentBlock,
                metadata = currentMeta
            }, DeliveryMethod.ReliableOrdered);
            return;
        }

        // validate block hasn't changed
        var blockID = world.getBlock(p.position.X, p.position.Y, p.position.Z);
        if (blockID == 0) {
            // already broken - send revert (confirm air)
            conn.send(new BlockChangePacket {
                position = p.position,
                blockID = 0,
                metadata = 0
            }, DeliveryMethod.ReliableOrdered);
            return;
        }

        // actually break the block
        var block = Block.get(blockID);
        if (block == null) {
            return;
        }

        // spawn drops (in survival mode)
        if (conn.player.gameMode.gameplay) {
            var val = world.getBlockRaw(p.position.X, p.position.Y, p.position.Z);
            var metadata = val.getMetadata();

            // check if player has correct tool
            var heldStack = conn.player.inventory.getSelected();
            var heldItem = heldStack.getItem();

            var canBreak = heldItem.canBreak(heldStack, block);
            Player.drops.Clear();
            block.getDrop(Player.drops, world, p.position.X, p.position.Y, p.position.Z, metadata, canBreak);
            foreach (var drop in Player.drops) {
                world.spawnBlockDrop(p.position.X, p.position.Y, p.position.Z, drop.getItem(), drop.quantity, drop.metadata);
            }

            // damage tool
            if (heldStack != ItemStack.EMPTY) {
                var newStack = heldStack.damageItem(conn.player, 1);
                conn.player.inventory.setStack(conn.player.inventory.selected, newStack);
                conn.player.syncSelected();
            }
        }

        // remove block
        world.setBlock(p.position.X, p.position.Y, p.position.Z, 0);
        world.blockUpdateNeighbours(p.position.X, p.position.Y, p.position.Z);

        // broadcast to all nearby clients
        GameServer.instance.send(
            new Vector3D(p.position.X, p.position.Y, p.position.Z),
            128.0,
            new BlockChangePacket {
                position = p.position,
                blockID = 0,
                metadata = 0
            },
            DeliveryMethod.ReliableOrdered
        );

        // clear breaking state
        conn.breakingBlock = null;
        conn.breakProgress = 0;
    }

    /** mirrors ClientPlayer.placeBlock() logic */
    private void handlePlaceBlock(PlaceBlockPacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        // validate reach - early check before processing
        if (!validateReach(p.position)) {
            revertBlockAndInventory(p.position);
            return;
        }

        // chain of responsibility - matches client precedence
        if (tryInteractWithBlock(p)) {
            return;
        }

        if (tryUseItemOnBlock(p)) {
            return;
        }

        tryPlaceBlockItem(p);
    }

    /** block.onUse() - chests, furnaces, etc */
    private bool tryInteractWithBlock(PlaceBlockPacket p) {
        var world = GameServer.instance.world;
        var block = Block.get(world.getBlock(p.position));

        if (block == null || block == Block.AIR) {
            return false;
        }

        // execute onUse (opens inventory, etc)
        if (!block.onUse(world, p.position.X, p.position.Y, p.position.Z, conn.player)) {
            return false;
        }

        conn.player.syncInventory();
        return true;
    }

    /** item.useBlock() - doors, buckets, etc */
    private bool tryUseItemOnBlock(PlaceBlockPacket p) {
        var stack = conn.player.inventory.getSelected();
        if (stack == ItemStack.EMPTY) {
            Log.warn($"Player '{conn.username}' tried to use item on block but has no item");
            revertBlockAndInventory(p.position);
            return true; // handled (as error)
        }

        // try item's useBlock hook
        var result = stack.getItem().useBlock(stack, GameServer.instance.world, conn.player, p.position.X, p.position.Y, p.position.Z, p.info);
        if (result == null) {
            return false;
        }

        // item handled it - update inventory. full sync for the same reason as onUse (harvesting adds items)
        conn.player.inventory.setStack(conn.player.inventory.selected, result);
        conn.player.syncInventory();

        return true;
    }

    /** place block from item */
    private void tryPlaceBlockItem(PlaceBlockPacket p) {
        var stack = conn.player.inventory.getSelected();
        if (stack == ItemStack.EMPTY || !stack.getItem().isBlock()) {
            return;
        }

        var block = Block.get(stack.getItem().getBlockID());
        if (block == null || block.id == 0) {
            return;
        }

        var metadata = (byte)stack.metadata;

        // validate placement
        if (!block.canPlace(GameServer.instance.world, p.position.X, p.position.Y, p.position.Z, p.info)) {
            revertBlockAndInventory(p.position);
            return;
        }

        // validate collisions (matches client checks)
        if (wouldCollideWithPlayer(block, p.position, metadata) || wouldCollideWithEntities(block, p.position, metadata)) {
            revertBlockAndInventory(p.position);
            return;
        }

        // validate stack quantity in survival
        if (conn.player.gameMode.gameplay && stack.quantity <= 0) {
            revertBlockAndInventory(p.position);
            return;
        }

        // place block
        block.place(GameServer.instance.world, p.position.X, p.position.Y, p.position.Z, metadata, p.info);

        // read back actual metadata (stairs, etc compute their own)
        metadata = GameServer.instance.world.getBlockMetadata(p.position);

        // consume in survival
        if (conn.player.gameMode.gameplay) {
            conn.player.inventory.removeStack(conn.player.inventory.selected, 1);
            conn.player.syncSelected();
        }

        // broadcast to nearby clients
        GameServer.instance.send(
            new Vector3D(p.position.X, p.position.Y, p.position.Z),
            128.0,
            new BlockChangePacket {
                position = p.position,
                blockID = block.id,
                metadata = metadata
            },
            DeliveryMethod.ReliableOrdered
        );
    }

    private bool wouldCollideWithPlayer(Block block, Vector3I pos, byte metadata) {
        var world = GameServer.instance.world;
        var aabbList = new List<AABB>();

        world.getAABBsCollision(aabbList, pos.X, pos.Y, pos.Z);
        foreach (var aabb in aabbList) {
            if (AABB.isCollision(aabb, conn.player.aabb)) {
                return true;
            }
        }

        return false;
    }

    private bool wouldCollideWithEntities(Block block, Vector3I pos, byte metadata) {
        if (!Block.collision[block.id]) {
            return false;
        }

        var world = GameServer.instance.world;
        var aabbList = new List<AABB>();
        block.getAABBs(world, pos.X, pos.Y, pos.Z, metadata, aabbList);

        var entities = new List<Entity>();
        foreach (var aabb in aabbList) {
            entities.Clear();
            world.getEntitiesInBox(entities, aabb.min.toBlockPos(), aabb.max.toBlockPos() + 1);
            foreach (var entity in entities) {
                if (entity.blocksPlacement && AABB.isCollision(aabb, entity.aabb)) {
                    return true;
                }
            }
        }

        return false;
    }

    private bool validateReach(Vector3I pos) {
        var dist = Vector3D.Distance(conn.player.position, new Vector3D(pos.X, pos.Y, pos.Z));
        return dist <= 7.5;
    }

    private void revertBlockAndInventory(Vector3I pos) {
        var world = GameServer.instance.world;

        // revert block state
        var raw = world.getBlockRaw(pos);
        conn.send(new BlockChangePacket {
            position = pos,
            blockID = raw.getID(),
            metadata = raw.getMetadata()
        }, DeliveryMethod.ReliableOrdered);

        conn.player.syncInventory();
    }

    private void handleUseItem(UseItemPacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        var stack = conn.player.inventory.getSelected();
        if (stack == ItemStack.EMPTY) {
            Log.warn($"Player '{conn.username}' tried to use empty item (selected slot: {conn.player.inventory.selected})");
            return;
        }

        Log.info($"Player '{conn.username}' using item: {stack.getItem().name} from slot {conn.player.inventory.selected}");

        // call item's use hook (eat food, throw, shoot bow, etc)
        var result = stack.getItem().use(stack, GameServer.instance.world, conn.player);
        if (result != null!) {
            conn.player.inventory.setStack(conn.player.inventory.selected, result);
            conn.player.syncSelected();
        }
    }

    private void handleChatMessage(ChatMessagePacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        // validate message length (max 256 chars)
        if (string.IsNullOrEmpty(p.message) || p.message.Length > 256) {
            return;
        }

        // format with username
        var formattedMsg = $"<{conn.username}> {p.message}";

        // log to server console
        Log.info(formattedMsg);

        // broadcast to all clients
        GameServer.instance.send(
            new ChatMessagePacket { message = formattedMsg },
            DeliveryMethod.ReliableOrdered
        );

        // strip the colour from the message
        var discordMsg = TextColours.strip(formattedMsg);

        GameServer.instance.discord?.sendMessage(discordMsg);
    }

    private void handleRenderDistance(RenderDistancePacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        // only cap on actual servers, it's fine on integrated
        var max = conn.isHost ? 96 : GameServer.instance.properties.getInt("renderDistance", 8);
        var dist = Math.Clamp((int)p.dist, 2, max);

        if (dist == conn.renderDistance) {
            return;
        }

        conn.renderDistance = dist;
        Log.info($"{conn.username} render distance -> {dist}");

        conn.send(new RenderDistancePacket { dist = (byte)dist }, DeliveryMethod.ReliableOrdered);

        conn.invalidateChunks();
        GameServer.instance.unsortChunks();
        conn.updateLoadedChunks();
    }

    private void handleCommand(CommandPacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        // validate command length (max 256 chars)
        if (string.IsNullOrEmpty(p.command) || p.command.Length > 256) {
            return;
        }

        // log to server console
        Log.info($"[{conn.username}] /{p.command}");

        // execute command with player as source
        var args = p.command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Command.execute(conn.player, args);

        // responses are sent via player.sendMessage() which will send ChatMessagePacket back to client
    }

    private void handleEntityState(EntityStatePacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        var s = new EntityState();
        s.deserialize(p.data);
        conn.player.sneaking = s.getBool(EntityState.SNEAKING, conn.player.sneaking);
        conn.player.flyMode = s.getBool(EntityState.FLYING, conn.player.flyMode);
    }

    private void handleEntityAction(EntityActionPacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        // apply action locally (e.g., trigger swing animation)
        switch (p.action) {
            case EntityActionPacket.Action.SWING:
                conn.player.setSwinging(true);
                break;
            // todo other actions handled later (TAKE_DAMAGE, DEATH, EAT, CRITICAL_HIT)
        }

        // broadcast to nearby players
        GameServer.instance.send(
            conn.player.position,
            128.0,
            p,
            DeliveryMethod.ReliableOrdered,
            exclude: conn
        );
    }

    private void handleInteractEntity(InteractEntityPacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        var target = GameServer.instance.world.getEntity(p.entityID);
        if (target == null || target.dead || Vector3D.Distance(conn.player.position, target.position) > 7.5) {
            conn.player.syncInventory();
            return;
        }

        target.interact(conn.player, conn.player.inventory.getSelected());

        conn.player.syncInventory();
    }

    private void handleAttackEntity(AttackEntityPacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        var world = GameServer.instance.world;

        var target = world.getEntity(p.targetEntityID);
        if (target == null || target.dead) {
            return;
        }

        // validate reach distance (max 5 blocks)
        var dist = Vector3D.Distance(conn.player.position, target.position);
        if (dist > 7.5) {
            return; // too far
        }

        // calculate damage from held item
        var heldStack = conn.player.inventory.getSelected();
        var damage = heldStack.getItem().getDamage(heldStack);

        var before = target.pendingHurt;
        target.dmg(damage, conn.player);

        if (target.pendingHurt > before && heldStack != ItemStack.EMPTY && Item.durability[heldStack.id] > 0) {
            conn.player.inventory.setStack(conn.player.inventory.selected, heldStack.damageItem(conn.player, 1));
            conn.player.syncSelected();
        }
    }

    private void handleUpdateBlockEntity(UpdateBlockEntityPacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        var world = GameServer.instance.world;

        // validate reach distance
        var dist = Vector3D.Distance(conn.player.position, new Vector3D(p.position.X, p.position.Y, p.position.Z));
        if (dist > 7.5) {
            return; // too far
        }

        // get block entity
        var blockEntity = world.getBlockEntity(p.position.X, p.position.Y, p.position.Z);
        if (blockEntity == null) {
            return; // no block entity at position
        }

        // update block entity data
        var nbt = (NBTCompound)NBT.read(p.nbt);
        blockEntity.read(nbt);

        // broadcast to all nearby clients (include sender for confirmation)
        GameServer.instance.send(
            new Vector3D(p.position.X, p.position.Y, p.position.Z),
            128.0,
            p,
            DeliveryMethod.ReliableOrdered
        );
    }

    private void handleInventorySlotClick(InventorySlotClickPacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        // handle creative inventory specially (client-side UI, server just validates)
        if (p.invID == Constants.INV_ID_CREATIVE) {
            handleCreativeInventoryClick(p);
            return;
        }

        // discard clicks while out of sync (waiting for resync acknowledgment)
        if (conn.outOfSync) {
            return;
        }

        var player = conn.player;

        // client thinks a window is open that the server already closed (death, block broken, gamemode...)
        if (p.invID != player.currentInventoryID) {
            Log.warn($"Player '{conn.username}' sent invalid inventory ID {p.invID} (expected {player.currentInventoryID})");
            if (p.invID != Constants.INV_ID_PLAYER) {
                conn.send(new InventoryClosePacket { invID = p.invID }, DeliveryMethod.ReliableOrdered);
            }

            rejectClick(p);
            return;
        }

        var ctx = player.currentCtx;
        if (p.idx >= ctx.getSlots().Count) {
            Log.warn($"Player '{conn.username}' sent invalid slot index {p.idx} for inventory ID {p.invID} (type={ctx.GetType()})");
            rejectClick(p);
            return;
        }

        var slot = ctx.getSlots()[p.idx];
        var left = p.button == 0;
        var clickType = p.mode == 1 ? (left ? ClickType.SHIFT_LEFT : ClickType.SHIFT_RIGHT) : (left ? ClickType.LEFT : ClickType.RIGHT);
        ctx.updatePrevItems();
        ctx.handleSlotClick(slot, clickType, player);

        // fuck it resync the whole thing
        var after = slot.getStack();
        if (!after.same(p.expectedSlot) || after.quantity != p.expectedSlot.quantity) {
            Log.warn($"Inventory desync for player '{conn.username}' (invID={p.invID}, actionID={p.actionID}). Expected slot {p.idx} to be {p.expectedSlot}, but was {after}");
            rejectClick(p);
            return;
        }

        ctx.broadcastSlotUpdates(conn);

        conn.send(new InventoryAckPacket {
            invID = p.invID,
            actionID = p.actionID,
            acc = true
        }, DeliveryMethod.ReliableOrdered);

        player.syncCursor();
    }

    private void rejectClick(InventorySlotClickPacket p) {
        conn.outOfSync = true;

        conn.send(new InventoryAckPacket {
            invID = p.invID,
            actionID = p.actionID,
            acc = false
        }, DeliveryMethod.ReliableOrdered);

        var player = conn.player;
        player.syncInventory();
        if (player.currentCtx != player.inventoryCtx) {
            player.currentCtx.notifyAllSlotsChanged();
        }

        conn.send(new InventoryResyncTermPacket {
            actionID = p.actionID
        }, DeliveryMethod.ReliableOrdered);
    }

    /**
     * handle creative inventory slot clicks
     * creative inventory is client-side UI, server just validates and syncs
     */
    private void handleCreativeInventoryClick(InventorySlotClickPacket p) {
        // validate player is in creative mode (anti-cheat)
        if (conn.player.gameMode.gameplay) {
            Log.warn($"Player '{conn.username}' tried to use creative inventory while not in creative mode!");
            rejectClick(p);
            return;
        }

        // creative inventory layout (from CreativeInventoryContext.setupSlots):
        // - slots 0-39: creative slots (infinite items, client-only)
        // - slots 40-49: player hotbar (actual inventory slots 0-9)
        if (p.idx >= 40 && p.idx < 50) {
            var invSlot = p.idx - 40;
            conn.player.inventory.setStack(invSlot, p.expectedSlot);
        }

        // creative slots (0-39) are client-only, no server-side state
        // cursor is also client-side for creative inventory (server doesn't track it)

        // accept transaction
        conn.send(new InventoryAckPacket {
            invID = p.invID,
            actionID = p.actionID,
            acc = true
        }, DeliveryMethod.ReliableOrdered);

        // don't sync cursor - creative inventory is client-authoritative for cursor
        // (server doesn't perform click operations, so it doesn't know correct cursor state)
    }

    private void handleResyncAck(InventoryResyncAckPacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        // client confirmed resync received - clear OOS flag to resume processing clicks
        conn.outOfSync = false;
    }

    private void handlePlayerHeldItemChange(PlayerHeldItemChangePacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        // validate hotbar slot (0-9)
        if (p.slot > 9) {
            Log.warn($"Player '{conn.username}' tried to select invalid slot {p.slot}!");
            return; // invalid slot
        }

        conn.player.inventory.selected = p.slot;
        conn.player.broadcastHeld();
    }

    private void handleInventoryClose(InventoryClosePacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        // validate window ID matches open window
        if (p.invID != conn.player.currentInventoryID) {
            return; // no window open or wrong window
        }

        var player = conn.player;

        if (player.currentCtx != player.inventoryCtx) {
            player.currentCtx.removeViewer(conn);
        }

        player.dropItemStack(player.inventory.cursor, withVelocity: true);
        player.inventory.cursor = ItemStack.EMPTY;

        player.closeInventory();
        player.syncInventory();
    }

    private void handleDropItem(DropItemPacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        // validate slot index
        if (p.slotIndex >= conn.player.inventory.size()) {
            Log.warn($"Player '{conn.username}' tried to drop item from invalid slot {p.slotIndex}!");
            return; // invalid slot
        }

        // validate quantity
        if (p.quantity == 0) {
            Log.warn($"Player '{conn.username}' tried to drop 0 items from slot {p.slotIndex}!");
            return; // nothing to drop
        }

        var stack = conn.player.inventory.getStack(p.slotIndex);
        if (stack == ItemStack.EMPTY || stack.quantity == 0) {
            Log.warn($"Player '{conn.username}' tried to drop item from empty slot {p.slotIndex}!");
            // the client already removed it, naive thing
            conn.player.syncSlot(p.slotIndex);
            return;
        }

        // clamp quantity to what's available
        var dropCount = Math.Min(p.quantity, stack.quantity);

        // create dropped item stack
        var droppedStack = new ItemStack(stack.getItem(), dropCount, stack.metadata);

        // remove from inventory
        conn.player.inventory.removeStack(p.slotIndex, dropCount);

        // spawn item entity in world with velocity (thrown)
        conn.player.dropItemStack(droppedStack, true);
        conn.player.syncSlot(p.slotIndex);
    }

    private void handleRespawnRequest(RespawnRequestPacket p) {
        if (!conn.authenticated || conn.player == null) {
            return;
        }

        var player = conn.player;

        if (!player.dead) {
            Log.warn($"Player '{conn.username}' tried to respawn but is not dead!");
            return;
        }

        // respawn the player
        player.dead = false;
        player.hp = 100;
        player.bodyRotation = new Vector3(0, 0, 0);
        player.prevBodyRotation = new Vector3(0, 0, 0);
        player.rotation = new Vector3(0, 0, 0);
        player.prevRotation = new Vector3(0, 0, 0);
        player.dieTime = 0;
        player.fireTicks = 0;

        // teleport to spawn
        var spawnPos = GameServer.instance.world.spawn;
        player.position = spawnPos;
        player.prevPosition = spawnPos;
        player.velocity = Vector3D.Zero;
        player.prevVelocity = Vector3D.Zero;

        player.resetFall();

        Log.info($"Player '{conn.username}' respawned at {spawnPos}");

        // send respawn packet to client (position + rotation)
        conn.send(new RespawnPacket {
            spawnPosition = spawnPos,
            rotation = player.rotation
        }, DeliveryMethod.ReliableOrdered);
        // save player data immediately to prevent (potential...) data loss if server crashes
        GameServer.instance.savePlayerData(conn);
    }

    /** normalise rotation angles to [-180, 180] NO SPINNING */
    private static Vector3 normrot(Vector3 rot) {
        return new Vector3(
            norma(rot.X),
            norma(rot.Y),
            norma(rot.Z)
        );
    }

    private static float norma(float angle) {
        // wrap to [0, 360]
        angle %= 360f;
        if (angle < 0) {
            angle += 360f;
        }

        // convert to [-180, 180]
        if (angle > 180f) {
            angle -= 360f;
        }

        return angle;
    }

    private void handlePlayerSkin(PlayerSkinPacket p) {
        if (conn.player == null) {
            return;
        }

        // validate size (max 64KB)
        if (p.skinData.Length > 65536) {
            Log.warn($"Rejected oversized skin from {conn.username}: {p.skinData.Length} bytes");
            return;
        }

        // validate transparency if not default skin
        if (p.skinData.Length > 0) {
            try {
                using var ms = new MemoryStream(p.skinData);
                var img = Image.Load<Rgba32>(ms);

                if (!GL.BTexture2D.validateTransparency(img)) {
                    Log.warn($"Rejected transparent skin from {conn.username}");
                    return;
                }

                Log.info($"Received skin from {conn.username} ({p.skinData.Length} bytes, {img.Width}x{img.Height})");
            } catch (Exception e) {
                Log.warn($"Invalid skin data from {conn.username}:");
                Log.warn(e);
                return;
            }
        }

        conn.skinData = p.skinData;

        GameServer.instance.send(new PlayerSkinPacket {
            entityID = conn.entityID,
            skinData = p.skinData
        }, DeliveryMethod.ReliableOrdered, exclude: conn);
    }
}