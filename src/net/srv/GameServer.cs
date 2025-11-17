using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using BlockGame.main;
using BlockGame.net.packet;
using BlockGame.util;
using BlockGame.util.cmd;
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

namespace BlockGame.net.srv;

/**
 * TODO we should really vendor in LiteNetLib because it's being weird af. Like how you can't even get the fucking IP address you bound to??
 * also start using separate channels? right now everything is on channel 0
 */
public class GameServer : INetEventListener {
    public const double TPS = 60.0;
    public const double mspt = 1000.0 / TPS;
    public static GameServer instance;

    public int maxPlayers;

    public readonly bool devMode;
    public bool running;
    public NetManager netManager;

    public readonly Stopwatch sw = Stopwatch.StartNew();

    // todo migrate these to OUR collections
    public readonly Dictionary<int, ServerConnection> connections = new();
    public readonly Dictionary<NetPeer, ServerConnection> peerToConnection = new();
    public readonly Dictionary<string, string> userPasswords = new(); // username -> hashed password
    public readonly HashSet<string> ops = new(); // op usernames

    public EntityTracker entityTracker;

    /** chunk trackers for batching block updates */
    public readonly XLongMap<ChunkTracker> chunkTrackers = new();

    public readonly ServerProperties properties;
    public readonly ConcurrentQueue<(Packet, ServerConnection)> incomingPackets = new();
    public readonly ServerConsole console = null!;
    public readonly string ip;
    public readonly string ip6;
    public readonly int port;

    // game state
    public World world;
    public long lastProgress;
    public int tickCtr = 0;
    private int pingUpdateCtr = 0;
    private int timeUpdateCtr = 0;
    private int chunkSyncCtr = 0;
    private int furnaceSyncCtr = 0;
    private long lastAutoSave = 0;
    private long lastNetStats = 0;

    public GameServer(bool devMode) {
        instance = this;
        this.devMode = devMode;

        Log.info("Server initialized");
        //Log.info($"Dev mode: {devMode}");

        // set network mode
        Net.mode = NetMode.DED;

        // initialize entity tracker
        entityTracker = new EntityTracker(this);

        // load properties
        properties = new ServerProperties();
        properties.load();

        maxPlayers = properties.getInt("maxPlayers", 20);
        ip = properties.getString("ip", "0.0.0.0");
        ip6 = properties.getString("ip6", "::");
        port = properties.getInt("port", 31337);

        try {
            netManager = new NetManager(this);
            netManager.ChannelsCount = 4;  // 0-3 for different packet priorities
            netManager.Start(ip, ip6, port);
        }
        catch (Exception e) {
            Log.error("FATAL: Failed to start network!");
            Log.error(e);
            throw; // crash the server, don't run in a broken state
        }

        Log.info($"Server listening on port {port}");

        // start console
        console = new ServerConsole(this);

        running = true;

        loop();
    }

    public void start() {
        loadUsers();
        loadOps();

        Block.preLoad();
        Item.preLoad();
        Entities.preLoad();
        BlockEntity.preLoad();
        Recipe.preLoad();
        SmeltingRecipe.preLoad();

        Block.postLoad();

        Command.register();

        loadWorld();
    }

    public void loop() {
        Log.info($"Starting game server on {ip}:{port}...");
        console.start();
        Log.info("Type 'help' for available commands");

        // TODO: load world, initialize systems
        start();

        // 60 TPS game loop
        sw.Restart();
        long lastUpdate = sw.ElapsedMilliseconds;

        while (running) {
            long now = sw.ElapsedMilliseconds;

            // todo if took too long, warn
            //  if *really* long, skip updates too

            if (now - lastUpdate >= mspt) {
                update();
                lastUpdate += (long)mspt;
            }
            else {
                // sleep for remaining time to avoid busy-waiting
                int sleepms = (int)(mspt - (now - lastUpdate));
                if (sleepms > 0) {
                    Thread.Sleep(sleepms);
                }
            }
        }

        stop();
    }

    private void loadWorld() {
        Log.info("Loading world...");

        var levelName = properties.getString("levelName", "mplevel");
        var levelPath = $"{levelName}/level.xnbt";

        try {
            bool loading;
            if (File.Exists(levelPath)) {
                // load existing world
                world = WorldIO.load(levelName);
                Log.info($"Loaded existing world: {world.displayName}");
                loading = true;
            } else {
                // create new world
                var seed = properties.getInt("seed", new Random().Next());
                var generator = properties.getString("generator", "v3");

                Log.error(generator);
                world = new World(levelName, seed, levelName, generator);
                Log.info($"Created new world with seed {seed}");
                loading = false;
            }

            // actually load
            var co = loadWorldCoroutine(isLoading: loading);
            while (co.MoveNext()) {
                // just complete
            }

            Log.info($"World '{world.displayName}' loaded! (seed: {world.seed})");
            
            world.listen(new ServerWorldListener(this));
        }
        catch (Exception e) {
            Log.error("FATAL: Failed to load world!");
            Log.error(e);
            throw; // crash the server, don't run in a broken state
        }
    }


    private int updateCounter = 0;
    private long lastLogTime = 0;

    public void update() {
        // 60 TPS game loop
        updateCounter++;
        if (sw.ElapsedMilliseconds - lastLogTime >= 1000) {
            Log.info($"TPS: {updateCounter} (expected 60)");
            updateCounter = 0;
            lastLogTime = sw.ElapsedMilliseconds;
        }

        netManager.PollEvents();

        // process all incoming packets on game thread
        while (incomingPackets.TryDequeue(out var item)) {
            var (packet, conn) = item;

            try {
                conn.handler.handle(packet);
            }
            catch (Exception e) {
                Log.error($"Error handling packet {packet.GetType().Name}:");
                Log.error(e);
            }
        }

        // queue chunks for loading around all players
        foreach (var conn in connections.Values) {
            if (conn.player == null) {
                Log.info("skipping?");
                continue;
            }

            var playerChunk = new ChunkCoord(
                (int)conn.player.position.X >> 4,
                (int)conn.player.position.Z >> 4
            );

            // queue chunks around player for loading
            int queueBefore = world.chunkLoadQueue.Count;
            world.loadChunksAroundChunk(playerChunk, conn.renderDistance + 1, ChunkStatus.LIGHTED);
            int queueAfter = world.chunkLoadQueue.Count;
            if (queueAfter - queueBefore > 0) {
                Log.info($"Queued {queueAfter - queueBefore} chunks for {conn.username} at {playerChunk}, queue now: {queueAfter}");
            }
        }

        // process chunk loading queue
        int loadedChunks = 0;
        int queueSize = world.chunkLoadQueue.Count;
        world.updateChunkloading(sw.ElapsedMilliseconds, loading: false, ref loadedChunks);
        if (loadedChunks > 0 || world.chunkLoadQueue.Count > 0) {
            Log.info($"Loaded {loadedChunks} chunks, queue: {queueSize} -> {world.chunkLoadQueue.Count}");
        }

        // update world, entities, lighting, etc
        world.update(mspt / 1000.0);

        // update entity tracker (position/state broadcasting)
        entityTracker.update();

        // sync entity state changes to clients
        syncEntityStates();

        // update chunk tracking for all connected players
        foreach (var conn in connections.Values) {
            conn.updateLoadedChunks();
        }

        // unload chunks not needed players every 5 seconds
        tickCtr++;
        if (tickCtr >= 300) {
            unloadUnusedChunks();
            tickCtr = 0;
        }

        // update player list pings every 3 seconds
        pingUpdateCtr++;
        if (pingUpdateCtr >= 180) {
            updatePlayerListPings();
            pingUpdateCtr = 0;
        }

        // sync world time every 3 seconds
        timeUpdateCtr++;
        if (timeUpdateCtr >= 180) {
            sendTimeUpdate();
            timeUpdateCtr = 0;
        }

        // sync furnace state to viewers every 2 ticks
        furnaceSyncCtr++;
        if (furnaceSyncCtr >= 2) {
            syncOpenFurnaces();
            furnaceSyncCtr = 0;
        }

        // periodic chunk sync check every 15 seconds
        chunkSyncCtr++;
        if (chunkSyncCtr >= 900) {
            syncLoadedChunks();
            chunkSyncCtr = 0;
        }

        // reset netstats every second
        if (sw.ElapsedMilliseconds - lastNetStats >= 1000) {
            foreach (var conn in connections.Values) {
                conn.metrics.clearNet();
            }
            lastNetStats = sw.ElapsedMilliseconds;
        }

        /*foreach (var conn in connections.Values) {
            var stats = conn.peer.Statistics;
            Console.WriteLine($"[{conn.username}] Sent: {stats.PacketsSent}, Loss: {stats.PacketLoss} Queue: {netManager.Statistics.BytesSent}");
        }*/

        // autosave every 10 seconds
        if (sw.ElapsedMilliseconds - lastAutoSave >= 10000) {
            autoSave();
            lastAutoSave = sw.ElapsedMilliseconds;
        }

        // flush all chunk trackers (send batched block changes)
        flushChunkTrackers();
    }

    private IEnumerator loadWorldCoroutine(bool isLoading) {

        Log.info("Setting up world...");
        progress(0.05f); // Setup complete: 5%
        Log.info("Loading spawn chunks...");
        world.preInit(isLoading);
        progress(0.10f); // Init complete: 10%
        world.init(isLoading);

        Log.info("Generating initial terrain");
        // load spawn chunks on dedicated server
        world.loadChunksAroundChunk(new ChunkCoord(0, 0), 8, ChunkStatus.LIGHTED);
        progress(0.15f);

        // Initial chunk loading phase
        int total = world.chunkLoadQueue.Count;
        int c = 0;
        Log.info("Loading chunks...");

        while (world.chunkLoadQueue.Count > 0) {
            world.updateChunkloading(Game.permanentStopwatch.Elapsed.TotalMilliseconds, loading: true,
                ref c);

            int currentChunks = world.chunkLoadQueue.Count;
            c = total - currentChunks;
            float chunkProgress = total > 0 ? (float)c / total : 1f;
            // Initial chunk loading: 15%-45%
            progress(mapProgress(chunkProgress, 0.15f, 0.45f));

            Log.info($"Loading chunks ({c}/{total})...");
        }

        // process all lighting updates
        Log.info("Lighting up...");
        var totalSkyLight = world.skyLightQueue.Count;
        c = 0;

        while (world.skyLightQueue.Count > 0) {
            int before = world.skyLightQueue.Count;
            world.processSkyLightQueueLoading(1000);
            int after = world.skyLightQueue.Count;
            // it hits hard
            c += (before - after);

            if (c % 1000 == 0 || world.skyLightQueue.Count == 0) {
                float skyLightProgress = totalSkyLight > 0 ? (float)c / totalSkyLight : 1f;
                progress(mapProgress(skyLightProgress, 0.45f, 0.65f));
                Log.info($"Processing skylight ({c}/{totalSkyLight})...");
            }
        }

        // skip meshing on dedicated server (client-only)
        progress(0.95f);

        world.postInit(isLoading);

        Log.info("Ready!");
        progress(1.0f); // Complete: 100%

        yield break;
    }

    public void progress(float progress) {
        // only log every second
        if (sw.ElapsedMilliseconds > lastProgress + 1000) {
            lastProgress = sw.ElapsedMilliseconds;
            Log.info($"Loading progress: {progress * 100f:0.00}%");
        }
    }

    private static float mapProgress(float progress, float startPercent, float endPercent) {
        return startPercent + (progress * (endPercent - startPercent));
    }

    public void send<T>(T packet, DeliveryMethod method, ServerConnection? exclude = null) where T : Packet {
        // broadcast to all connected clients
        foreach (var conn in connections.Values) {
            if (conn == exclude) continue;
            conn.send(packet, method);
        }
    }

    public void send<T>(Vector3D pos, double radius, T packet, DeliveryMethod method, ServerConnection? exclude = null) where T : Packet {
        // broadcast to clients near position
        double radiusSq = radius * radius;
        foreach (var conn in connections.Values) {
            if (conn == exclude) continue;
            if (conn.player == null) continue;

            double distSq = Vector3D.DistanceSquared(conn.player.position, pos);
            if (distSq <= radiusSq) {
                conn.send(packet, method);
            }
        }
    }

    /**
     * helper to open an inventory for a player (server-side)
     * eliminates boilerplate from block entity onUse handlers
     * TODO I switched everything to int IDs to not have to worry about overflows and stuff ever, but this should be cleaned up later
     */
    public static bool openInventory(
        ServerPlayer player,
        InventoryContext ctx,
        byte invType,
        string title,
        Vector3I position,
        ItemStack[] slots,
        Action<ServerConnection>? additionalPackets = null
    ) {
        if (player == null) {
            return false;
        }

        var conn = player.conn;
        if (conn == null) {
            return false;
        }

        // increment window ID
        player.currentInventoryID++;

        // set server-side context for validation
        player.currentCtx = ctx;

        // send open packet
        conn.send(new InventoryOpenPacket {
            invID = player.currentInventoryID,
            invType = invType,
            title = title,
            slotCount = (byte)slots.Length,
            position = position
        }, DeliveryMethod.ReliableOrdered);

        // send inventory sync
        conn.send(new InventorySyncPacket {
            invID = player.currentInventoryID,
            items = slots
        }, DeliveryMethod.ReliableOrdered);

        // send cursor state (the client needs to know what's in cursor when switching inventories, desync alert!!)
        conn.send(new SetSlotPacket {
            invID = Constants.INV_ID_CURSOR,
            slotIndex = 0,
            stack = player.inventory.cursor
        }, DeliveryMethod.ReliableOrdered);

        // send any additional packets (e.g., FurnaceSyncPacket)
        additionalPackets?.Invoke(conn);

        // add player as viewer for broadcasting changes
        // IMPORTANT: we pass currentInventoryID, not invType!
        // invType is 0/1/2 (chest/crafting/furnace), but invID is the actual window ID and we keep fucking it up
        ctx.addViewer(conn, player.currentInventoryID);

        return true;
    }

    // authentication
    public void loadUsers() {
        const string path = "users.snbt";
        if (!File.Exists(path)) {
            Log.info("No users.snbt found, starting with empty user db");
            return;
        }

        try {
            var nbt = (NBTCompound)SNBT.readFromFile(path);
            userPasswords.Clear();

            foreach (var key in nbt.dict.Keys) {
                userPasswords[key] = nbt.getString(key);
            }

            Log.info($"Loaded {userPasswords.Count} users from {path}");
        }
        catch (Exception e) {
            Log.error($"Error loading users from {path}:");
            Log.error(e);
            Log.warn("Continuing with empty user database");
            userPasswords.Clear();
        }
    }

    public void saveUsers() {
        const string path = "users.snbt";
        try {
            var nbt = new NBTCompound();
            foreach (var (username, hash) in userPasswords) {
                nbt.addString(username, hash);
            }

            SNBT.writeToFile(nbt, path, prettyPrint: true);
            Log.info($"Saved {userPasswords.Count} users to {path}");
        }
        catch (Exception e) {
            Log.error($"Error saving users to {path}:");
            Log.error(e);
        }
    }

    public void loadOps() {
        const string path = "ops.txt";
        if (!File.Exists(path)) {
            Log.info("No ops.txt found, starting with no operators");
            return;
        }

        try {
            ops.Clear();
            var lines = File.ReadAllLines(path);
            foreach (var line in lines) {
                var trimmed = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith('#')) {
                    ops.Add(trimmed);
                }
            }

            Log.info($"Loaded {ops.Count} operators from {path}");
        }
        catch (Exception e) {
            Log.error($"Error loading ops from {path}:");
            Log.error(e);
            ops.Clear();
        }
    }

    public void saveOps() {
        const string path = "ops.txt";
        try {
            var lines = new List<string> { "# Operator list - one username per line" };
            lines.AddRange(ops.OrderBy(s => s));

            File.WriteAllLines(path, lines);
            Log.info($"Saved {ops.Count} operators to {path}");
        }
        catch (Exception e) {
            Log.error($"Error saving ops to {path}:");
            Log.error(e);
        }
    }

    public bool isOp(string username) {
        return ops.Contains(username);
    }

    // player data persistence
    public string getPlayerDataPath(string username) {
        var playersDir = Path.Combine(world.name, "players");
        Directory.CreateDirectory(playersDir);
        return Path.Combine(playersDir, $"{username}.enbt");
    }

    public ServerPlayer? loadPlayerData(string username) {
        var path = getPlayerDataPath(username);
        if (!File.Exists(path)) {
            return null; // new player
        }

        try {
            var nbt = NBTScrambler.loadScrambled(path);
            var player = new ServerPlayer(world, 0, 0, 0);
            player.read(nbt);
            return player;
        }
        catch (Exception e) {
            Log.error($"Error loading player data for {username}:");
            Log.error(e);
            return null;
        }
    }

    public void savePlayerData(ServerConnection conn) {
        if (conn.player == null || string.IsNullOrEmpty(conn.username)) {
            return;
        }

        var path = getPlayerDataPath(conn.username);
        try {
            var nbt = new NBTCompound();
            conn.player.write(nbt);
            NBTScrambler.saveScrambled(path, nbt);
        }
        catch (Exception e) {
            Log.error($"Error saving player data for {conn.username}:");
            Log.error(e);
        }
    }

    public void saveAllPlayers() {
        int saved = 0;
        foreach (var conn in connections.Values) {
            if (conn.authenticated && conn.player != null) {
                savePlayerData(conn);
                saved++;
            }
        }
        if (saved > 0) {
            Log.info($"Saved {saved} player(s)");
        }
    }


    public void OnConnectionRequest(ConnectionRequest request) {
        Log.info($"Connection request from {request.RemoteEndPoint}");

        // TODO: validate connection key properly
        // For now, just accept all connections
        request.Accept();
    }


    public void OnPeerConnected(NetPeer peer) {
        Log.info($"Player connected: {peer}");
        var conn = new ServerConnection(peer);
        peerToConnection[peer] = conn;
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
        Log.info($"Player disconnected: {peer}, reason: {disconnectInfo.Reason}");
        if (peerToConnection.Remove(peer, out var conn)) {
            connections.Remove(conn.entityID);

            // broadcast leave message if player was authenticated
            if (conn.authenticated && !string.IsNullOrEmpty(conn.username)) {
                send(
                    new ChatMessagePacket { message = $"&e{conn.username} &cleft the game" },
                    DeliveryMethod.ReliableOrdered
                );

                // remove from player list
                send(
                    new PlayerListRemovePacket { entityID = conn.entityID },
                    DeliveryMethod.ReliableOrdered
                );
            }

            // close any open invs
            if (conn.player != null) {
                // remove from viewer list before context reset
                conn.player.currentCtx?.removeViewer(conn);
                conn.player.currentInventoryID = -1;
                conn.player.currentCtx = conn.player.inventoryCtx;
            }

            // save player data before removing
            if (conn.authenticated && conn.player != null) {
                savePlayerData(conn);
            }

            // remove player entity from world
            if (conn.player != null) {
                world.removeEntity(conn.player);
            }
        }
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod) {
        try {
            if (!peerToConnection.TryGetValue(peer, out var conn)) {
                return;
            }

            var bytes = reader.GetRemainingBytes();

            // track metrics
            conn.metrics.bytesReceived += bytes.Length;
            conn.metrics.packetsReceived++;

            // read packet ID
            var br = new BinaryReader(new MemoryStream(bytes));
            var buf = new PacketBuffer(br);
            int packetID = buf.readInt();

            // create packet instance
            var type = PacketRegistry.getType(packetID);
            var packet = (Packet)Activator.CreateInstance(type)!;
            packet.read(buf);

            // queue for game thread processing
            incomingPackets.Enqueue((packet, conn));
        }
        catch (Exception e) {
            Log.error($"Error processing packet:");
            Log.error(e);
        }
        finally {
            reader.Recycle();
        }
    }

    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError) {
        Log.error($"Network error from {endPoint}: {socketError}");
    }

    public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) {
        // ignore
        reader.Recycle();
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) {
        if (peerToConnection.TryGetValue(peer, out var conn)) {
            conn.ping = latency;
            conn.metrics.ping = latency;
        }
    }

    /** broadcast ping updates to all clients */
    private void updatePlayerListPings() {
        foreach (var conn in connections.Values) {
            if (!conn.authenticated) continue;

            // broadcast this player's ping to all clients
            send(
                new PlayerListUpdatePingPacket {
                    entityID = conn.entityID,
                    ping = conn.metrics.ping
                },
                DeliveryMethod.Unreliable
            );
        }
    }

    /** send time sync to all clients */
    private void sendTimeUpdate() {
        if (world == null || !world.inited) {
            return;
        }

        send(
            new TimeUpdatePacket {
                worldTick = world.worldTick
            },
            DeliveryMethod.ReliableSequenced
        );
    }

    /** autosave world data and modified chunks */
    private void autoSave() {
        if (world == null || !world.inited) {
            return;
        }

        // save all online players
        saveAllPlayers();

        // save world metadata
        world.worldIO.saveWorldData();

        // save modified chunks (async to prevent blocking)
        int saved = 0;
        foreach (var chunk in world.chunks) {
            // save chunks that have been modified and not recently saved
            if (chunk.status >= ChunkStatus.LIGHTED &&
                chunk.lastSaved + 5000 < (ulong)Game.permanentStopwatch.ElapsedMilliseconds) {
                world.worldIO.saveChunk(world, chunk);
                saved++;
            }
        }

        if (saved > 0) {
            Log.info($"Autosaved world data + {saved} chunks");
        }
    }

    /** unload chunks not needed by any player */
    private void unloadUnusedChunks() {
        var toUnload = new List<ChunkCoord>();

        // gather chunks that aren't loaded by any player
        foreach (var chunk in world.chunks) {
            var coord = chunk.coord;
            bool needed = false;

            // check if any player needs this chunk
            foreach (var conn in connections.Values) {
                if (conn.loadedChunks.Contains(coord)) {
                    needed = true;
                    break;
                }
            }

            if (!needed) {
                toUnload.Add(coord);
            }
        }

        // unload chunks
        if (toUnload.Count > 0) {
            Log.info($"Unloading {toUnload.Count} unused chunks");
            foreach (var coord in toUnload) {
                world.unloadChunk(coord);
            }
        }
    }

    /**
     * This needs to be slightly more complicated than it should be.
     * TODO fix? this is a mess
     */
    public static int getNewID() {
        World.ec++;
        return World.ec;
    }

    /** sync entity state changes (sneaking, on fire, flying, etc.) */
    private void syncEntityStates() {
        foreach (var entity in world.entities) {
            // sync state from entity properties to EntityState
            entity.syncState();

            // check if state changed (dirty)
            if (entity.state.isDirty()) {
                // serialize state
                var data = entity.state.serialize();

                // broadcast to all clients in range
                send(
                    entity.position,
                    128.0,
                    new EntityStatePacket {
                        entityID = entity.id,
                        data = data
                    },
                    DeliveryMethod.ReliableOrdered
                );
            }
        }
    }

    /** safety guard: verify loadedChunks matches reality:tm: */
    private void syncLoadedChunks() {
        foreach (var conn in connections.Values) {
            if (conn.player == null) {
                continue;
            }

            var toRemove = new List<ChunkCoord>();

            // check if chunks in loadedChunks still exist in world
            foreach (var coord in conn.loadedChunks) {
                if (!world.getChunkMaybe(coord, out _)) {
                    toRemove.Add(coord);
                }
            }

            // remove stale chunks
            foreach (var coord in toRemove) {
                conn.unloadChunk(coord);
            }

            if (toRemove.Count > 0) {
                Log.warn($"Cleaned up {toRemove.Count} stale chunks from {conn.username}'s loadedChunks, fuckup?");
            }
        }
    }

    /** get or create a chunk tracker for a given chunk coord */
    public ChunkTracker get(ChunkCoord coord) {
        long key = coord.toLong();
        ref var tracker = ref chunkTrackers.GetOrAdd(key, out var added);
        if (added) {
            tracker = new ChunkTracker(coord, this);
        }
        return tracker;
    }

    /** sync furnace state to all players viewing furnaces */
    private void syncOpenFurnaces() {
        // for each connection viewing a furnace, send furnace sync
        foreach (var conn in connections.Values) {
            if (conn.player?.currentCtx == null) continue;

            // check if viewing a furnace
            if (conn.player.currentInventoryID >= 0 && conn.player.currentCtx is FurnaceMenuContext furnaceCtx) {
                if (furnaceCtx.getFurnaceInventory() is FurnaceBlockEntity furnace) {
                    conn.send(new FurnaceSyncPacket {
                        position = furnace.pos,
                        smeltProgress = furnace.smeltProgress,
                        fuelRemaining = furnace.fuelRemaining,
                        fuelMax = furnace.fuelMax,
                        lit = furnace.isLit()
                    }, DeliveryMethod.Unreliable); // unreliable is fine, sent every 2 ticks
                }
            }
        }
    }

    /** flush all dirty chunk trackers (batched block update sending) */
    private void flushChunkTrackers() {
        foreach (var tracker in chunkTrackers) {
            tracker.flush();
        }
    }

    public void stop() {
        running = false;

        Log.info("Stopping server...");

        // save all players before shutdown
        saveAllPlayers();

        // save world before shutdown
        if (world != null && world.inited) {
            Log.info("Saving world...");
            world.worldIO.save(world, world.name, saveChunks: true);
            world.worldIO.Dispose();
            Log.info("World saved");
        }

        console.stop();
        netManager.Stop();
        saveUsers();
        Net.mode = NetMode.NONE;
        Log.info("Server stopped");
    }
}