using System.Numerics;
using System.Diagnostics;
using BlockGame.logic;
using BlockGame.main;
using BlockGame.render;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.util.log;
using BlockGame.util.xNBT;
using BlockGame.world.block;
using BlockGame.world.chunk;
using BlockGame.world.entity;
using BlockGame.world.item.inventory;
using BlockGame.world.worldgen;
using BlockGame.world.worldgen.generator;
using Molten;
using Molten.DoublePrecision;
using MonoMod.Utils;

namespace BlockGame.world;

/**
 * this will get yeeted once we stop having a split integrated server + real SP
 */
public enum Side : byte {
    CLIENT,
    SERVER,
    BOTH
}

public partial class World : IDisposable {
    public const int WORLDSIZE = 12;

    // todo optimise the chunkload radius...
    public const int POPULATE_REACH = 2;
    public const int REGIONSIZE = 16;
    public const int WORLDHEIGHT = Chunk.CHUNKHEIGHT * Chunk.CHUNKSIZE;

    // try to keep 120 FPS at least
    public const double MAX_CHUNKLOAD_FRAMETIME = 1000 / 120.0;
    //public const double MAX_MESHLOAD_FRAMETIME = 1000 / 480.0;
    public const double MAX_MESHLOAD_FRAMETIME = 1000 / 120.0;
    // since we've got the integrated server, we can afford to spend more time meshing because the server is doing the ticks;)


    // when loading the world, we can load chunks faster because fuck cares about a loading screen?
    public const double MAX_CHUNKLOAD_FRAMETIME_FAST = 1000 / 10.0;

    // headless side: most of a 60TPS tick, but leave room for entities, packets and the tracker flush
    public const double MAX_CHUNKLOAD_TICKTIME = 10.0;

    // this applies to the queues *separately* so it's lower
    public const double MAX_LIGHT_FRAMETIME = 1000 / 60.0;
    public const int SPAWNCHUNKS_SIZE = 1;
    public const int MAX_TICKING_DISTANCE = 128;

    /// <summary>
    /// Random ticks per chunk section per tick.
    /// </summary>
    public const int numTicks = 1;

    public string name;
    public string displayName;
    public string generatorName;

    public readonly XUList<WorldListener> listeners = [];

    public readonly XLongMap<Chunk> chunks;

    // used for rendering
    public readonly XUList<Chunk> chunkList;


    // Queues
    public readonly List<ChunkLoadTicket> chunkLoadQueue = [];
    private readonly HashSet<ChunkLoadTicket> chunkLoadQueueSet = [];

    public readonly List<Vector3D> playerPositions = [];

    public int playerRadius = 8;

    public readonly XUList<BlockUpdate> blockUpdateQueue = [];


    public readonly List<TickAction> actionQueue = [];

    public readonly List<Chunk> lightDirtyChunks = [];

    public readonly Queue<LightNode> skyLightQueue = [];
    public readonly Queue<LightRemovalNode> skyLightRemovalQueue = [];
    public readonly Queue<LightNode> blockLightQueue = [];
    public readonly Queue<LightRemovalNode> blockLightRemovalQueue = [];

    public readonly WorldGenerator generator;

    public bool isLoading;

    public readonly XUList<Player> players = [];

    /**
     * True if the world has actually been initialised, false if the init method hasn't been called yet.
     */
    public bool inited;

    public bool paused;
    public bool inMenu;

    private volatile bool disposing;

    public WorldIO worldIO;

    public int seed;

    public bool isMP;

    public readonly Side side;

    /** is this a client with a renderer, camera and a local player? */
    public bool isClient => side != Side.SERVER;

    /** is this a server with game logic like worldgen, world tick, saving, etc.? */
    public bool isServer => side != Side.CLIENT;

    /** if true, suppress network packet broadcasts (used during worldgen/chunk loading) */
    public bool nosend;

    public int worldTick;

    public const int TICKS_PER_DAY = 72000;

    public XRandom random;
    private TimerAction saveWorld;
    public NBTCompound toBeLoadedNBT;

    public NBTCompound? legacyPlayerNBT;

    [ThreadStatic] private static List<AABB>? _listAABB;
    private static List<AABB> listAABB => _listAABB ??= [];

    public World(Side side, string name, int seed, string? displayName = null, string? generatorName = null) {
        this.side = side;
        this.name = name;
        this.displayName = displayName ?? name;
        this.generatorName = generatorName ?? "perlin";

        inited = false;
        worldIO = new WorldIO(this);

        if (name != "__multiplayer") {
            generator = WorldGenerators.create(this, generatorName);
        }

        if (name == "__multiplayer") {
            // it's a mp world, no saving and shit!
            isMP = true;
        }

        random = new XRandom(seed);
        worldTick = 0;

        if (name != "__multiplayer") {
            generator.setup(random, seed);
        }

        this.seed = seed;

        chunks = [];
        chunkList = new XUList<Chunk>(2048);

        entities = [];
        particles = new Particles(this);
    }

    public void preInit(bool loadingSave = false) {
        // load a minimal amount of chunks so the world can get started
        if (!loadingSave) {
            loadSpawnChunks();


            // after loading spawn chunks, load everything else immediately
            isLoading = true;
        }
    }

    public void init(bool loadingSave = false) {
        if (side == Side.BOTH) {
            Log.info("Initializing singleplayer world...");
            player = new ClientPlayer(this, 6, 20, 6);
            player.name = Settings.instance.playerName;
            addEntity(player);
            Game.player = (ClientPlayer)player;
            Game.camera.setPlayer(player);

            // spawn cow at 3 blocks from player (only on new world)
            if (!loadingSave) {
                var cow = new Cow(this);
                cow.position = new Vector3D(9, 20, 9); // 3 blocks in +x and +z
                addEntity(cow);
            }
        }

        if (loadingSave) {
            // if loading, actually load
            if (loadingSave) {
                var tag = toBeLoadedNBT;
                worldTick = tag.has("time") ? tag.getInt("time") : 0;

                // load full player data
                if (side == Side.BOTH && tag.has("player")) {
                    player.read(tag.getCompoundTag("player"));
                }

                // load gamemode
                if (side == Side.BOTH) {
                    var gmStr = tag.getString("gamemode", "survival");
                    player.gameMode = gmStr == "survival" ? GameMode.survival : GameMode.creative;
                    player.inventoryCtx = player.gameMode == GameMode.survival
                        ? new SurvivalInventoryContext(player.inventory)
                        : new CreativeInventoryContext(player.inventory, 40);

                    player.currentCtx = player.inventoryCtx;

                    player.prevPosition = player.position;
                }

                if (true || isServer) {
                    // load lighting queues (after chunks are loaded)
                    WorldIO.loadLightingQueues(this, tag);
                }

                if (tag.has("player")) {
                    legacyPlayerNBT = tag.getCompoundTag("player");
                }

                // zero out toBeLoadedNBT to free memory
                toBeLoadedNBT = null!;
            }
        }
        else {
            // find safe spawn position with proper AABB clearance
            if (side == Side.BOTH) {
                ensurePlayerSpawnClearance();
                // give starter items
                player.inventory.initNewPlayer();

                // set spawn
                spawn = player.position;

                // set player gamemode
                player.gameMode = GameMode.creative;
            }
            // in the multiplayer server, find a safe spawn position for the player
            else if (!isClient) {
                ensureSpawnClearance();
            }
        }

        // After everything is done, SAVE THE WORLD
        // if we don't save the world, some of the chunks might get saved but no level.xnbt
        // so the world is corrupted and we have horrible chunkglitches

        if (!isMP) {
            worldIO.save(this, name, false, false);
        }


        foreach (var l in listeners) {
            l.onWorldLoad();
        }

        inited = true;
    }

    public void postInit(bool loadingSave = false) {
        // notify all entities of the chunks they're actually in
        // handled in loadChunk!
        setupAutosave();

        // process *all* the lighting so we won't get in a doomloop where we want to save, but lighting isn't done yet but saving is slow -> doomloop of constantly trying to save while game is frozen forever
        // this is because our saving is fairly slow (nbtcompound + 5 elements for each element..... multiply that by a few million and it's fucked.)

        while (skyLightQueue.Count > 0 || skyLightRemovalQueue.Count > 0 ||
               blockLightQueue.Count > 0 || blockLightRemovalQueue.Count > 0) {
            processSkyLightQueue();
            processSkyLightRemovalQueue();
            processBlockLightQueue();
            processBlockLightRemovalQueue();
        }
    }

    public void setupAutosave() {
        // setup world saving every 5 seconds
        // NOTE: this used to memory leak the ENTIRE WORLD because it was capturing the world reference in the method in Main.timerQueue.
        // to avoid that, ALWAYS MAKE SURE methods aren't overwritten!

        // SAFETY CHECK
        if (saveWorld != null) {
            Game.clearInterval(saveWorld);
        }

        // only setup autosave timer on client
        if (side == Side.BOTH) {
            // in hot reload, don't save that much!! fucking lagspikes
            var interval = Spy.enabled ? 180000 : 2000;
            saveWorld = Game.setInterval(interval, saveWorldMethod);
        }
    }

    private void saveWorldMethod() {
        if (!inited || disposing) {
            // don't save the world if it hasn't been initialized yet or is being disposed
            return;
        }

        // save async!
        autoSaveChunks();
        worldIO.saveWorldData();
    }

    public void listen(WorldListener listener) {
        listeners.Add(listener);
    }

    public void unlisten(WorldListener listener) {
        listeners.Remove(listener);
    }

    /// <summary>
    /// Autosave any chunks which haven't been saved in more than a minute.
    /// </summary>
    private void autoSaveChunks() {
        const int MAX_CHUNKS_PER_AUTOSAVE = 50;
        var currentTime = (ulong)Game.permanentStopwatch.ElapsedMilliseconds;

        var x = 0;
        foreach (var chunk in chunks) {
            if (x >= MAX_CHUNKS_PER_AUTOSAVE) {
                break; // cap at 50 chunks per autosave to prevent too much GC...
            }

            if (chunk.status >= ChunkStatus.MESHED &&
                chunk.lastSaved + 20 * 1000 < currentTime) {
                worldIO.saveChunkAsync(this, chunk);
                x++;
            }
        }
    }

    public void setBlockNeighboursDirty(Vector3I block) {
        var x = block.X;
        var y = block.Y;
        var z = block.Z;

        // calculate affected chunk range (chunks containing block +/- 1 in each direction)
        int chunkX0 = (x - 1) >> 4;
        int chunkX1 = (x + 1) >> 4;
        int chunkZ0 = (z - 1) >> 4;
        int chunkZ1 = (z + 1) >> 4;

        // cap Y to valid world height
        int y0 = Math.Max(0, y - 1);
        int y1 = Math.Min(WORLDHEIGHT - 1, y + 1);
        int subY0 = y0 >> 4;
        int subY1 = y1 >> 4;

        // batch dirty chunks to avoid repeated HashSet operations
        var rangeX = chunkX1 - chunkX0 + 1;
        var rangeZ = chunkZ1 - chunkZ0 + 1;
        var rangeY = subY1 - subY0 + 1;
        var maxCoords = rangeX * rangeZ * rangeY;

        Span<SubChunkCoord> coords = stackalloc SubChunkCoord[maxCoords];
        int coordCount = 0;

        for (int chunkX = chunkX0; chunkX <= chunkX1; chunkX++) {
            for (int chunkZ = chunkZ0; chunkZ <= chunkZ1; chunkZ++) {
                for (int subY = subY0; subY <= subY1; subY++) {
                    coords[coordCount++] = new SubChunkCoord(chunkX, subY, chunkZ);
                }
            }
        }

        dirtyChunksBatch(coords[..coordCount]);
    }

    public void dirtyChunk(SubChunkCoord coord) {
        foreach (var l in listeners) {
            l.onDirtyChunk(coord);
        }
    }

    public void dirtyChunksBatch(ReadOnlySpan<SubChunkCoord> coords) {
        foreach (var l in listeners) {
            l.onDirtyChunksBatch(coords);
        }
    }

    public void dirtyArea(Vector3I min, Vector3I max) {
        foreach (var l in listeners) {
            l.onDirtyArea(min, max);
        }
    }

    public void updatePendingLight() {
        if (lightDirtyChunks.Count == 0) {
            return;
        }

        foreach (Chunk c in lightDirtyChunks) {
            var mask = c.lightDirty;
            c.lightDirty = 0;

            if (c.destroyed) {
                continue;
            }

            while (mask != 0) {
                var sy = BitOperations.TrailingZeroCount(mask);
                mask &= (byte)(mask - 1);

                var coord = new SubChunkCoord(c.coord.x, sy, c.coord.z);
                foreach (var l in listeners) {
                    l.onLightDirty(coord);
                }
            }
        }

        lightDirtyChunks.Clear();
    }

    public void addChunk(ChunkCoord coord, Chunk chunk) {
        chunks.Set(coord.toLong(), chunk);
        chunkList.Add(chunk);

        // populate this chunk's cache and update neighbours to point to it
        chunk.getCache();
        addToCache(chunk);

        foreach (var l in listeners) {
            l.onChunkLoad(coord);
        }
    }

    /** update neighbour chunks' caches to point to the new chunk */
    private void addToCache(Chunk chunk) {
        if (chunk.cache.w != null) chunk.cache.w.cache.e = chunk;
        if (chunk.cache.e != null) chunk.cache.e.cache.w = chunk;
        if (chunk.cache.s != null) chunk.cache.s.cache.n = chunk;
        if (chunk.cache.n != null) chunk.cache.n.cache.s = chunk;
        if (chunk.cache.sw != null) chunk.cache.sw.cache.ne = chunk;
        if (chunk.cache.se != null) chunk.cache.se.cache.nw = chunk;
        if (chunk.cache.nw != null) chunk.cache.nw.cache.se = chunk;
        if (chunk.cache.ne != null) chunk.cache.ne.cache.sw = chunk;
    }

    private void loadSpawnChunks() {
        loadChunksAroundChunkImmediately(new ChunkCoord(0, 0), SPAWNCHUNKS_SIZE);
        //sortChunks();
    }

    private void ensurePlayerSpawnClearance() {
        var pos = player.position;

        // move up until we find a position with proper clearance
        while (pos.Y > WORLDHEIGHT - Player.height || !hasPlayerAABBClearance(pos)) {
            pos.Y += 1;
        }

        player.position = pos;
        // set spawn point
        spawn = pos;
    }

    private void ensureSpawnClearance() {
        var pos = spawn;

        // move up until we find a position with proper clearance
        while (pos.Y > WORLDHEIGHT - Player.height || !hasPlayerAABBClearance(pos)) {
            pos.Y += 1;
        }

        // set spawn point
        spawn = pos;
    }

    private bool hasPlayerAABBClearance(Vector3D pos) {
        const double sizehalf = Player.width / 2;
        var playerAABB = new AABB(
            new Vector3D(pos.X - sizehalf, pos.Y, pos.Z - sizehalf),
            new Vector3D(pos.X + sizehalf, pos.Y + Player.height, pos.Z + sizehalf)
        );

        // check all blocks that could potentially intersect with player AABB
        var min = playerAABB.min.toBlockPos();
        var max = playerAABB.max.toBlockPos();

        for (int x = min.X; x <= max.X; x++) {
            for (int y = min.Y; y <= max.Y; y++) {
                for (int z = min.Z; z <= max.Z; z++) {
                    var bl = getBlock(x, y, z);
                    if (bl == Block.AIR.id) {
                        continue;
                    }

                    getAABBsCollision(listAABB, x, y, z);
                    foreach (var aabb in listAABB) {
                        if (AABB.isCollision(playerAABB, aabb)) {
                            return false;
                        }
                    }
                }
            }
        }

        // check skylight at spawn position to avoid caves
        var spawnBlockPos = pos.toBlockPos();
        var skylight = getSkyLight(spawnBlockPos.X, (int)pos.Y, spawnBlockPos.Z);
        if (skylight == 0) {
            return false;
        }

        return true;
    }

    public void sortChunks() {
        if (playerPositions.Count == 0) {
            return;
        }

        // note: removal is faster from the end so we sort by the reverse - closest entries are at the end of the list
        chunkLoadQueue.Sort(new ChunkTicketComparerReverse(playerPositions));
        genCovered = 0;
    }

    public void setPlayerPosition(Vector3D pos) {
        playerPositions.Clear();
        playerPositions.Add(pos);
    }

    public void loadAroundPlayer() {
        // create terrain
        //genTerrainNoise();
        // separate loop so all data is there
        if (side == Side.BOTH) {
            player.loadChunksAroundThePlayer(Settings.instance.renderDistance);
        }
    }

    public void loadAroundPlayer(ChunkStatus status) {
        if (side == Side.BOTH) {
            player.loadChunksAroundThePlayer(Settings.instance.renderDistance, status);
        }
    }

    public int getBrightness(byte skylight, byte skyDarken) {
        // apply sky darkening to skylight only
        return Math.Max(0, skylight - skyDarken);
    }

    public float getDayPercentage(int ticks) {
        return (ticks % TICKS_PER_DAY) / (float)TICKS_PER_DAY;
    }


    /// <summary>
    /// Chunkloading and friends.
    /// </summary>
    public void renderUpdate(double dt) {
        var ctr = 0;
        updateChunkloading(loading: false, ref ctr);

        particles.update(dt);
    }

    private void processAsyncChunkLoads(double startTime, bool loading, ref int loadedChunks) {
        var limit = loading ? MAX_CHUNKLOAD_FRAMETIME_FAST : MAX_CHUNKLOAD_FRAMETIME;

        // if server, process all results without limit
        if (!isClient) {
            limit = double.MaxValue;
        }

        while (worldIO.hasChunkLoadResult()) {
            var result = worldIO.getChunkLoadResult();
            if (result == null) {
                break;
            }

            // handle error cases
            if (result.Value.error != null) {
                // log error and fall back to sync loading
                Log.error($"Async chunk load failed for {result.Value.coord}", result.Value.error);
                // re-queue for sync loading
                addToChunkLoadQueue(result.Value.coord, result.Value.targetStatus);
                continue;
            }

            // successful load: apply NBT data to existing chunk
            var coord = result.Value.coord;

            // check if chunk is still relevant before processing
            if (!isChunkRelevant(coord)) {
                continue; // skip chunks that are now too far away
            }

            if (chunks.TryGetValue(coord.toLong(), out Chunk? existingChunk) && result.Value.nbtData != null) {
                WorldIO.loadChunkDataFromNBT(existingChunk, result.Value.nbtData);
                loadedChunks++;
            }

            // re-queue for status progression (GENERATED -> POPULATED -> LIGHTED -> MESHED)
            // this will handle neighbour dependencies correctly
            addToChunkLoadQueue(result.Value.coord, result.Value.targetStatus);

            // check time AFTER processing - break if we've exceeded budget
            if (Game.permanentStopwatch.Elapsed.TotalMilliseconds - startTime >= limit) {
                break;
            }
        }
    }

    public void updateChunkloading(bool loading, ref int loadedChunks) {
        var startTime = Game.permanentStopwatch.Elapsed.TotalMilliseconds;

        // process async chunk load results first
        if (isServer) {
            processAsyncChunkLoads(startTime, loading, ref loadedChunks);
        }

        // if is loading, don't throttle
        // consume the chunk queue
        // ONLY IF THERE ARE CHUNKS
        // otherwise don't wait for nothing
        // yes I was an idiot
        var limit = loading ? (!isClient ? double.MaxValue : MAX_CHUNKLOAD_FRAMETIME_FAST)
            : !isClient ? MAX_CHUNKLOAD_TICKTIME
            : MAX_CHUNKLOAD_FRAMETIME;
        while (chunkLoadQueue.Count > 0) {
            // check time BEFORE loading - break if we've already used our budget
            if (Game.permanentStopwatch.Elapsed.TotalMilliseconds - startTime >= limit) {
                break;
            }

            // generate what the next few tickets are going to need, on the workers, before loadChunk asks for it
            if (isServer) {
                pregen();
            }

            var ticket = chunkLoadQueue[^1];
            chunkLoadQueue.RemoveAt(chunkLoadQueue.Count - 1);
            chunkLoadQueueSet.Remove(ticket);
            genCovered--;
            genExpected--;

            // check if chunk is still relevant before loading it
            if (isChunkRelevant(ticket.chunkCoord)) {
                loadChunk(ticket.chunkCoord, ticket.level);
                loadedChunks++;

                // check time AFTER loading - break if we've exceeded budget
                if (Game.permanentStopwatch.Elapsed.TotalMilliseconds - startTime >= limit) {
                    break;
                }
            }
            // if too far, just discard the ticket and continue
        }

        if (chunkLoadQueue.Count == 0) {
            isLoading = false;
        }
    }

    public void update(double dt) {
        worldTick++;
        /*if (Vector3D.DistanceSquared(player.position, player.lastSort) > 64) {
            sortedTransparentChunks.Sort(new ChunkComparer(player.camera));
            player.lastSort = player.position;
        }*/

        // execute tick actions
        if (isServer) {
            for (int i = actionQueue.Count - 1; i >= 0; i--) {
                var action = actionQueue[i];
                if (action.tick <= worldTick) {
                    action.action();
                    actionQueue.RemoveAt(i);
                }
            }

            // execute block updates
            for (int i = blockUpdateQueue.Count - 1; i >= 0; i--) {
                var update = blockUpdateQueue[i];
                if (update.tick <= worldTick) {
                    blockScheduledUpdate(update.position.X, update.position.Y, update.position.Z);
                    blockUpdateQueue.RemoveAt(i);
                }
            }
        }

        // execute lighting updates ONLY IN SP
        SuperluminalPerf.BeginEvent("light");
        //if (isServer) {
        processSkyLightRemovalQueue();
        if (skyLightRemovalQueue.Count == 0) {
            processSkyLightQueue();
        }

        processBlockLightRemovalQueue();
        if (blockLightRemovalQueue.Count == 0) {
            processBlockLightQueue();
        }
        //}
        SuperluminalPerf.EndEvent();

        if (isServer) {
            // random block updates!
            foreach (var chunk in chunks.Pairs) {
                if (playerPositions.Count == 0) {
                    break;
                }
                var c = chunk.Value;
                var cp = c.centrePos;
                var shouldTick = false;
                foreach (Vector3D ap in playerPositions) {
                    var dx = cp.X - ap.X;
                    var dz = cp.Y - ap.Z;
                    if (dx * dx + dz * dz < MAX_TICKING_DISTANCE * MAX_TICKING_DISTANCE) {
                        shouldTick = true;
                        break;
                    }
                }
                if (!shouldTick) {
                    continue;
                }

                var coord = new ChunkCoord(chunk.Key);
                for (var s = 0; s < Chunk.CHUNKHEIGHT; s++) {
                    if (!c.blocks[s].hasRandomTickingBlocks()) {
                        continue;
                    }
                    for (var i = 0; i < numTicks; i++) {
                        // I pray this is random
                        var pos = random.Next(Chunk.CHUNKSIZE * Chunk.CHUNKSIZE * Chunk.CHUNKSIZE);
                        var x = pos >> 8;
                        var y = ((pos >> 4) & 0xF) + s * Chunk.CHUNKSIZE;
                        var z = pos & 0xF;
                        tick(this, coord, c, random, x, y, z);
                    }
                }
            }
        }

        updateBlockEntities();

        if (isServer) {
            updateSpawning();
        }

        updateEntities(dt);

        updatePendingLight();
    }

    public void processSkyLightQueue() {
        //SuperluminalPerf.BeginEvent("skylight");
        processLightQueue(skyLightQueue, true);
        //SuperluminalPerf.EndEvent();
    }

    public void processSkyLightQueueLoading(int count) {
        for (int i = 0; i < count; i++) {
            if (skyLightQueue.Count == 0) {
                break; // no more nodes to process
            }

            processLightQueueOne(skyLightQueue, true);
        }
    }

    public void processSkyLightRemovalQueue() {
        processLightRemovalQueue(skyLightRemovalQueue, skyLightQueue, true);
    }

    public void processBlockLightQueue() {
        processLightQueue(blockLightQueue, false);
    }

    public void processBlockLightRemovalQueue() {
        processLightRemovalQueue(blockLightRemovalQueue, blockLightQueue, false);
    }

    /** Uses <see cref="getChunkAndRelativePos"/> to make lookups faster! since it's probably in the chunk */
    public ushort getRelativeBlock(Chunk currentChunk, int x, int y, int z, Vector3I direction) {
        // get the neighbour chunk and relative position
        var neighbourPos = getChunkAndRelativePos(currentChunk, x, y, z, direction, out var neighbourChunk);
        if (neighbourChunk == null) {
            return 0; // no chunk loaded
        }

        // return the block at the neighbour position
        return neighbourChunk.getBlock(neighbourPos.X, neighbourPos.Y, neighbourPos.Z);
    }

    /// <summary>
    /// Gets the chunk and relative position for a neighbour of a block in chunk-relative coordinates
    /// </summary>
    public Vector3I getChunkAndRelativePos(Chunk currentChunk, int x, int y, int z, Vector3I direction,
        out Chunk? chunk) {
        var neighbour = new Vector3I(x, y, z) + direction;

        if (neighbour.Y is < 0 or >= WORLDHEIGHT) {
            chunk = null;
            return Vector3I.Zero;
        }

        // Check if neighbour is within current chunk bounds (0-15 for X/Z)
        if (neighbour.X is >= 0 and < 16 && neighbour.Z is >= 0 and < 16) {
            chunk = currentChunk;
            return neighbour;
        }

        // neighbour crosses XZ boundary - try cache first
        int dx = direction.X;
        int dz = direction.Z;

        // compute cache index from direction
        // WEST=0, EAST=1, SOUTH=2, NORTH=3, SW=4, SE=5, NW=6, NE=7
        int index;
        if (dz == 0) {
            index = (dx + 1) >> 1;
        }
        else if (dx == 0) {
            index = 2 + ((dz + 1) >> 1);
        }
        else {
            index = 4 + ((dx + 1) >> 1) + ((dz + 1) & 2);
        }

        Chunk? cchunk = currentChunk.cache[index];

        // if cache hit, use it
        if (cchunk != null) {
            chunk = cchunk;
            var worldX = (currentChunk.coord.x << 4) + neighbour.X;
            var worldZ = (currentChunk.coord.z << 4) + neighbour.Z;
            return new Vector3I(worldX & 0xF, neighbour.Y, worldZ & 0xF);
        }

        // cache miss or more than 1 chunk away - fall back to dictionary lookup
        // get the chunk world coord by shifting the chunk-relative coordinates "out" of the number
        var nx = (currentChunk.coord.x << 4) + neighbour.X;
        var nz = (currentChunk.coord.z << 4) + neighbour.Z;

        // get target chunk
        // this assigns directly to the output variable! might be null, FYI
        if (!getChunkMaybe(nx, nz, out chunk) || (chunk?.destroyed ?? true)) {
            chunk = null;
            return Vector3I.Zero; // Chunk not loaded, bail
        }

        return new Vector3I(nx & 0xF, neighbour.Y, nz & 0xF);
    }


    private const int LIGHT_TIME_CHECK_INTERVAL = 64;

    private static readonly long LIGHT_BUDGET_TICKS = (long)(MAX_LIGHT_FRAMETIME * Stopwatch.Frequency / 1000.0);

    private bool lightSync;

    public void processLightQueue(Queue<LightNode> queue, bool isSkylight) {
        if (lightSync) {
            return;
        }

        lightSync = true;
        try {
            var start = Stopwatch.GetTimestamp();
            while (queue.Count > 0) {
                var n = int.Min(queue.Count, LIGHT_TIME_CHECK_INTERVAL);
                for (int i = 0; i < n; i++) {
                    processLightQueueOne(queue, isSkylight);
                }

                if (Stopwatch.GetTimestamp() - start >= LIGHT_BUDGET_TICKS) {
                    break;
                }
            }
        }
        finally {
            lightSync = false;
        }
    }

    public void processSkyLightQueueFully() {
        if (lightSync) {
            return;
        }

        lightSync = true;
        try {
            while (skyLightQueue.Count > 0) {
                processLightQueueOne(skyLightQueue, true);
            }
        }
        finally {
            lightSync = false;
        }
    }

    private readonly Chunk?[] lightChunkCache = new Chunk?[4];

    private Chunk? resolveChunk(int wx, int wz) {
        var cx = wx >> 4;
        var cz = wz >> 4;
        var slot = (cx ^ cz) & 3;

        var cached = lightChunkCache[slot];
        if (cached != null && !cached.destroyed && cached.coord.x == cx && cached.coord.z == cz) {
            return cached;
        }

        if (!chunks.TryGetValue(new ChunkCoord(cx, cz).toLong(), out var chunk) || chunk.destroyed) {
            return null;
        }

        lightChunkCache[slot] = chunk;
        return chunk;
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void processLightQueueOne(Queue<LightNode> queue, bool isSkylight) {
        var node = queue.Dequeue();

        var relX = node.x & 15;
        var relZ = node.z & 15;
        var relY = node.y;

        var chunk = resolveChunk(node.x, node.z);
        if (chunk == null) {
            return;
        }

        byte level = chunk.getLight(relX, relY, relZ);
        level = isSkylight
            ? (byte)(level & 0x0F)
            : (byte)(level >> 4);

        // if this is opaque (for skylight), don't bother
        if (isSkylight && Block.isFullBlock(chunk.getBlock(relX, relY, relZ))) {
            return;
        }

        foreach (var dir in Direction.directionsLight) {
            // Get neighbour chunk and relative position
            var neighbourRelPos = getChunkAndRelativePos(chunk, relX, relY, relZ, dir, out var neighbourChunk);
            if (neighbourChunk == null) {
                continue;
            }

            // if neighbour is opaque, don't bother either
            var neighbourBlockId = neighbourChunk.getBlock(neighbourRelPos.X, neighbourRelPos.Y, neighbourRelPos.Z);
            if (Block.isFullBlock(neighbourBlockId)) {
                continue;
            }

            byte neighbourLevel = neighbourChunk.getLight(neighbourRelPos.X, neighbourRelPos.Y, neighbourRelPos.Z);
            neighbourLevel = isSkylight
                ? (byte)(neighbourLevel & 0x0F)
                : (byte)(neighbourLevel >> 4);
            var isDown = isSkylight && level == 15 && neighbourLevel != 15 && dir == Direction.DOWN;

            var absorption = Block.lightAbsorption[neighbourBlockId];

            // apply absorption, or if no absorption: down=no decrease, sideways=decrease by 1
            var decrease = absorption > 0 ? absorption : (isDown ? 0 : 1);
            byte newLevel = (byte)Math.Max(0, level - decrease);

            if (newLevel > neighbourLevel) {
                if (isSkylight) {
                    neighbourChunk.setSkyLight(neighbourRelPos.X, neighbourRelPos.Y, neighbourRelPos.Z, newLevel);
                }
                else {
                    neighbourChunk.setBlockLight(neighbourRelPos.X, neighbourRelPos.Y, neighbourRelPos.Z, newLevel);
                }

                // convert back to world coords for queue
                int worldNX = (neighbourChunk.coord.x << 4) + neighbourRelPos.X;
                int worldNZ = (neighbourChunk.coord.z << 4) + neighbourRelPos.Z;
                queue.Enqueue(new LightNode(worldNX, neighbourRelPos.Y, worldNZ));
            }
        }
    }

    public void processLightRemovalQueue(Queue<LightRemovalNode> queue, Queue<LightNode> addQueue, bool isSkylight) {
        if (lightSync) {
            return;
        }

        lightSync = true;
        try {
            drainRemoval(queue, addQueue, isSkylight);
        }
        finally {
            lightSync = false;
        }
    }

    private void drainRemoval(Queue<LightRemovalNode> queue, Queue<LightNode> addQueue, bool isSkylight) {
        var start = Stopwatch.GetTimestamp();
        while (queue.Count > 0) {
            var n = int.Min(queue.Count, LIGHT_TIME_CHECK_INTERVAL);
            for (int i = 0; i < n; i++) {
                processLightRemovalQueueOne(queue, addQueue, isSkylight);
            }

            if (Stopwatch.GetTimestamp() - start >= LIGHT_BUDGET_TICKS) {
                break;
            }
        }
    }

    public void processLightRemovalQueueOne(Queue<LightRemovalNode> queue, Queue<LightNode> addQueue, bool isSkylight) {
        var node = queue.Dequeue();

        var level = node.value;

        var relX = node.x & 15;
        var relZ = node.z & 15;
        var relY = node.y;


        var chunk = resolveChunk(node.x, node.z);
        if (chunk == null) {
            return;
        }

        foreach (var dir in Direction.directionsLight) {
            // Get neighbour chunk and relative position
            var neighbourRelPos = getChunkAndRelativePos(chunk, relX, relY, relZ, dir, out var neighbourChunk);
            if (neighbourChunk == null) {
                continue;
            }

            byte neighbourLevel = neighbourChunk.getLight(neighbourRelPos.X, neighbourRelPos.Y, neighbourRelPos.Z);
            neighbourLevel = isSkylight
                ? (byte)(neighbourLevel & 0x0F)
                : (byte)(neighbourLevel >> 4);
            var isDownLight = isSkylight && dir == Direction.DOWN && level == 15;
            if (isDownLight || neighbourLevel != 0 && neighbourLevel < level) {
                if (isSkylight) {
                    neighbourChunk.setSkyLight(neighbourRelPos.X, neighbourRelPos.Y, neighbourRelPos.Z, 0);
                }
                else {
                    neighbourChunk.setBlockLight(neighbourRelPos.X, neighbourRelPos.Y, neighbourRelPos.Z, 0);
                }

                // convert to world coords for queue
                int worldNX = (neighbourChunk.coord.x << 4) + neighbourRelPos.X;
                int worldNZ = (neighbourChunk.coord.z << 4) + neighbourRelPos.Z;
                queue.Enqueue(new LightRemovalNode(worldNX, neighbourRelPos.Y, worldNZ, neighbourLevel));
            }
            else if (neighbourLevel >= level) {
                // Add it to the update queue, so it can propagate to fill in the gaps
                // left behind by this removal. We should update the lightBfsQueue after
                // the lightRemovalBfsQueue is empty.
                int worldNX = (neighbourChunk.coord.x << 4) + neighbourRelPos.X;
                int worldNZ = (neighbourChunk.coord.z << 4) + neighbourRelPos.Z;
                addQueue.Enqueue(new LightNode(worldNX, neighbourRelPos.Y, worldNZ));
            }
        }
    }

    public void addToChunkLoadQueue(ChunkCoord chunkCoord, ChunkStatus level) {
        // MP client gets chunks from server packets, not chunk loading
        if (!isServer) {
            SkillIssueException.throwNew("Tried to queue chunk on the MP client, wtf?");
        }

        // if server & meshed, crash
        if (!isClient && level == ChunkStatus.MESHED) {
            SkillIssueException.throwNew("Tried to queue chunk for meshing on dedicated server, wtf?");
        }

        if (chunks.TryGetValue(chunkCoord.toLong(), out var chunk)) {
            if (chunk.status < level) {
                //Log.info($"Re-queuing {chunkCoord}: current={chunk.status}, target={level}");
            }
        }

        // don't queue if chunk already exists at required status
        if (chunk?.status >= level) {
            // chunk already loaded at required status
            return;
        }

        var ticket = new ChunkLoadTicket(chunkCoord, level);
        if (chunkLoadQueueSet.Add(ticket)) {
            chunkLoadQueue.Add(ticket);
            /*if (!isClient) {
                Log.info($"Queued chunk {chunkCoord} for loading (current status: {(chunk != null ? chunk.status.ToString() : "not loaded")}, target: {level})");
            }*/
        }
    }

    /// <summary>
    /// Chunks are generated up to renderDistance + 1.
    /// Chunks are populated (tree placement, etc.) until renderDistance and meshed until renderDistance.
    /// TODO unload chunks which are renderDistance + 2 away (this is bigger to prevent chunk flicker)
    /// </summary>
    public void loadChunksAroundChunk(ChunkCoord chunkCoord, int renderDistance) {
        // meshed needs lighted around it, lighted needs generated around it, populated needs generated around it
        /*for (int x = chunkCoord.x - renderDistance - 2; x <= chunkCoord.x + renderDistance + 2; x++) {
            for (int z = chunkCoord.z - renderDistance - 2; z <= chunkCoord.z + renderDistance + 2; z++) {
                var coord = new ChunkCoord(x, z);
                if (coord.distanceSq(chunkCoord) <= (renderDistance + 2) * (renderDistance + 2)) {
                    addToChunkLoadQueue(coord, ChunkStatus.GENERATED);
                }
            }
        }

        // populated needs generated around it
        const int pr = POPULATE_REACH;
        for (int x = chunkCoord.x - renderDistance - pr; x <= chunkCoord.x + renderDistance + pr; x++) {
            for (int z = chunkCoord.z - renderDistance - pr; z <= chunkCoord.z + renderDistance + pr; z++) {
                var coord = new ChunkCoord(x, z);
                if (coord.distanceSq(chunkCoord) <= (renderDistance + pr) * (renderDistance + pr)) {
                    addToChunkLoadQueue(coord, ChunkStatus.POPULATED);
                }
            }
        }

        // lighted needs populated around it
        for (int x = chunkCoord.x - renderDistance; x <= chunkCoord.x + renderDistance; x++) {
            for (int z = chunkCoord.z - renderDistance; z <= chunkCoord.z + renderDistance; z++) {
                var coord = new ChunkCoord(x, z);
                if (coord.distanceSq(chunkCoord) <= renderDistance * renderDistance) {
                    addToChunkLoadQueue(coord, ChunkStatus.LIGHTED);
                }
            }
        }

        // finally, mesh around renderDistance
        for (int x = chunkCoord.x - renderDistance; x <= chunkCoord.x + renderDistance; x++) {
            for (int z = chunkCoord.z - renderDistance; z <= chunkCoord.z + renderDistance; z++) {
                var coord = new ChunkCoord(x, z);
                if (coord.distanceSq(chunkCoord) <= renderDistance * renderDistance) {
                    addToChunkLoadQueue(coord, ChunkStatus.MESHED);
                }
            }
        }

        // unload chunks which are far away
        if (side == Side.BOTH) {
            var playerChunk = player.getChunk();
            foreach (var chunk in chunks) {
                var coord = chunk.coord;
                // if distance is greater than renderDistance + 3, unload
                if (playerChunk.distanceSq(coord) >= (renderDistance + 3) * (renderDistance + 3)) {
                    unloadChunk(coord);
                }
            }
        }*/

        loadChunksAroundChunk(chunkCoord, renderDistance, ChunkStatus.MESHED);
    }

    public void loadChunksAroundChunk(ChunkCoord chunkCoord, int renderDistance, ChunkStatus status) {

        // finally, mesh around renderDistance
        for (int x = chunkCoord.x - renderDistance; x <= chunkCoord.x + renderDistance; x++) {
            for (int z = chunkCoord.z - renderDistance; z <= chunkCoord.z + renderDistance; z++) {
                var coord = new ChunkCoord(x, z);
                if (coord.distanceSq(chunkCoord) <= renderDistance * renderDistance) {
                    addToChunkLoadQueue(coord, status);
                }
            }
        }

        // unload chunks which are far away
        if (side == Side.BOTH) {
            var playerChunk = player.getChunk();
            var keep = renderDistance + 2 * POPULATE_REACH + 2;
            foreach (var chunk in chunks) {
                var coord = chunk.coord;
                if (playerChunk.distanceSq(coord) >= keep * keep) {
                    unloadChunk(coord);
                }
            }
        }
    }

    public void loadChunksAroundChunkImmediately(ChunkCoord chunkCoord, int renderDistance) {
        // finally, mesh around renderDistance
        for (int x = chunkCoord.x - renderDistance; x <= chunkCoord.x + renderDistance; x++) {
            for (int z = chunkCoord.z - renderDistance; z <= chunkCoord.z + renderDistance; z++) {
                var coord = new ChunkCoord(x, z);
                if (coord.distanceSq(chunkCoord) <= renderDistance * renderDistance) {
                    loadChunk(coord, isClient ? ChunkStatus.MESHED : ChunkStatus.LIGHTED, true);
                }
            }
        }

        // unload chunks which are far away
        /*foreach (var chunk in chunks.Values) {
            var playerChunk = player.getChunk();
            var coord = chunk.coord;
            // if distance is greater than renderDistance + 3, unload
            if (playerChunk.distanceSq(coord) >= (renderDistance + 3) * (renderDistance + 3)) {
                unloadChunk(coord);
            }
        }*/
    }

    public void unloadChunk(ChunkCoord coord) {
        var chunk = chunks[coord.toLong()];

        // mark all entities in this chunk as unloaded
        for (int y = 0; y < Chunk.CHUNKHEIGHT; y++) {
            foreach (var entity in chunk.entities[y]) {
                entity.inWorld = false;
                // kill these entities too IF NOT PLAYERS
                if (entity is not Player) {
                    removeEntity(entity);
                }
            }
        }

        // save chunk before unloading
        if (!isClient) {
            // dedicated server: save sync
            worldIO.saveChunk(this, chunk);
        }
        else if (!isMP) {
            // save chunk asynchronously to prevent lagspikes
            // DON'T DO IT ON THE SERVER, we don't do async there.
            // we might in the future but we'd get the save clashes so idk :\
            worldIO.saveChunkAsync(this, chunk);
        }

        foreach (var l in listeners) {
            l.onChunkUnload(coord);
        }

        // invalidate neighbour caches before removal
        chunk.removeFromCache();

        // ONLY DO THIS WHEN IT'S ALREADY SAVED
        chunkList.Remove(chunk);
        chunks.Remove(coord.toLong());
        chunk.destroyChunk();
    }

    public void unloadChunkWithHammer(ChunkCoord coord) {
        var chunk = chunks[coord.toLong()];

        // mark all entities in this chunk as unloaded
        for (int y = 0; y < Chunk.CHUNKHEIGHT; y++) {
            foreach (var entity in chunk.entities[y]) {
                entity.inWorld = false;
                // kill these entities too IF NOT PLAYERS
                if (entity is not Player) {
                    removeEntity(entity);
                }
            }
        }

        foreach (var l in listeners) {
            l.onChunkUnload(coord);
        }

        // invalidate neighbour caches before removal
        chunk.removeFromCache();

        chunkList.Remove(chunk);
        chunks.Remove(coord.toLong());
        chunk.destroyChunk();
    }

    private void ReleaseUnmanagedResources() {
        // do NOT save chunks!!! this fucks the new world
        foreach (var chunk in chunks.Pairs) {
            chunks[chunk.Key].destroyChunk();
        }

        worldIO.releaseLock();
    }

    public void unload() {
        Dispose();
    }

    public void Dispose() {
        disposing = true;

        foreach (var l in listeners) {
            l.onWorldUnload();
        }

        // stop automatic saves - don't check Net.mode as it may have changed!
        if (saveWorld != null) {
            saveWorld.enabled = false;
            Game.clearInterval(saveWorld);
            saveWorld = null!;
        }

        // stop the chunksave queue and save pending chunks
        if (!isMP) {
            worldIO.Dispose();

            // of course, we can save it here since WE call it and not the GC
            // save the whole thing

            worldIO.save(this, name);
        }


        ReleaseUnmanagedResources();

        Game.world = null;
        Game.player = null;
        //Game.renderer = null;
        GC.SuppressFinalize(this);
    }

    ~World() {
        ReleaseUnmanagedResources();
    }

    /// <summary>
    /// Check if all neighbours around a chunk have reached the specified status
    /// </summary>
    private bool areNeighboursReady(ChunkCoord chunkCoord, ChunkStatus requiredStatus, int radius = 1) {
        for (var dx = -radius; dx <= radius; dx++) {
            for (var dz = -radius; dz <= radius; dz++) {
                if (dx == 0 && dz == 0) {
                    continue;
                }

                var n = new ChunkCoord(chunkCoord.x + dx, chunkCoord.z + dz);
                if (!chunks.TryGetValue(n.toLong(), out var nc) || nc.status < requiredStatus) {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Queue neighbours for loading and ensure they reach the required status
    /// </summary>
    private void queueNeighboursForLoading(ChunkCoord chunkCoord, ChunkStatus requiredStatus) {
        Span<ChunkCoord> neighbours = [
            new(chunkCoord.x - 1, chunkCoord.z),
            new(chunkCoord.x + 1, chunkCoord.z),
            new(chunkCoord.x, chunkCoord.z - 1),
            new(chunkCoord.x, chunkCoord.z + 1),
            new(chunkCoord.x - 1, chunkCoord.z - 1),
            new(chunkCoord.x - 1, chunkCoord.z + 1),
            new(chunkCoord.x + 1, chunkCoord.z - 1),
            new(chunkCoord.x + 1, chunkCoord.z + 1)
        ];

        foreach (var neighbour in neighbours) {
            if (!chunks.TryGetValue(neighbour.toLong(), out var neighbourChunk) || neighbourChunk.status < requiredStatus) {
                addToChunkLoadQueue(neighbour, requiredStatus);
            }
        }
    }

    private void loadNeighbours(ChunkCoord chunkCoord, ChunkStatus requiredStatus, int radius = 1) {
        for (var dx = -radius; dx <= radius; dx++) {
            for (var dz = -radius; dz <= radius; dz++) {
                if (dx == 0 && dz == 0) {
                    continue;
                }

                var n = new ChunkCoord(chunkCoord.x + dx, chunkCoord.z + dz);
                if (!chunks.TryGetValue(n.toLong(), out var nc) || nc.status < requiredStatus) {
                    loadChunk(n, requiredStatus, true);
                }
            }
        }
    }

    private bool isChunkRelevant(ChunkCoord chunkCoord) {
        if (playerPositions.Count == 0) {
            // nothing to be relevant *to*
            return true;
        }

        // bit more buffer than the unload distance so we don't thrash the boundary
        var maxDistance = playerRadius + 2;
        var maxSq = maxDistance * maxDistance;

        foreach (Vector3D a in playerPositions) {
            var ac = new ChunkCoord((int)a.X >> 4, (int)a.Z >> 4);
            if (ac.distanceSq(chunkCoord) < maxSq) {
                return true;
            }
        }
        return false;
    }

    // ---- batched terrain generation ----

    // overengineering???
    private GenJob[]? genJobs;
    private readonly List<ChunkCoord> genList = [];
    private readonly HashSet<long> genSet = [];

    private readonly HashSet<long> genOnDisk = [];

    private const int PREGEN_TICKETS = 32;

    private int genCovered;
    private int genExpected;

    /**
     * Basically preloading whatever is needed for the chunks (so if you want a fully meshed chunk, you'll need the lighted neighbours, which will need the populated neighbours, which will need the generated neighbours, etc.)
     * kinda like prefetching so we don't have to rely on the recursive loading with loadChunk() which isn't batched
     * so now we can use threads!
     */
    private void pregen() {
        // still covered, and nobody appended/sorted under us
        if (genCovered > 0 && chunkLoadQueue.Count == genExpected) {
            return;
        }
        genJobs ??= makeGenJobs();
        if (genOnDisk.Count > 1 << 16) {
            genOnDisk.Clear();
        }
        var cap = genJobs.Length;

        while (true) {
            genList.Clear();
            genSet.Clear();

            // ---- scan ----
            var covered = 0;
            for (var t = chunkLoadQueue.Count - 1; t >= 0 && covered < PREGEN_TICKETS; t--) {
                var ticket = chunkLoadQueue[t];
                if (!isChunkRelevant(ticket.chunkCoord)) {
                    covered++;
                    continue;
                }

                var r = ticket.level switch {
                    >= ChunkStatus.LIGHTED => 2 * POPULATE_REACH,
                    >= ChunkStatus.POPULATED => POPULATE_REACH,
                    _ => 0
                };
                var full = true;
                for (var dx = -r; dx <= r && full; dx++) {
                    for (var dz = -r; dz <= r; dz++) {
                        if (genList.Count >= cap) {
                            full = false;
                            break;
                        }
                        var c = new ChunkCoord(ticket.chunkCoord.x + dx, ticket.chunkCoord.z + dz);
                        var key = c.toLong();
                        // present at any status - EMPTY means an async disk load owns it, leave it alone
                        if (chunks.ContainsKey(key) || !genSet.Add(key) || genOnDisk.Contains(key)) {
                            continue;
                        }
                        if (WorldIO.chunkFileExists(this, c)) {
                            genOnDisk.Add(key);
                            continue;
                        }
                        genList.Add(c);
                    }
                }
                if (!full) {
                    break;
                }
                covered++;
            }
            genCovered = covered;
            genExpected = chunkLoadQueue.Count;

            var n = genList.Count;
            if (n == 0) {
                return;
            }

            // ---- generate ----
            for (var i = 0; i < n; i++) {
                genJobs[i].chunk = new Chunk(this, genList[i].x, genList[i].z);
            }

            GenJob.pool.run(genJobs, n);

            // ---- publish ----
            var failed = false;
            for (var i = 0; i < n; i++) {
                var job = genJobs[i];
                if (job.error == null) {
                    addChunk(genList[i], job.chunk!);
                }
                else {
                    failed = true;
                }
                job.chunk = null;
            }

            if (covered > 0 || failed) {
                return;
            }
        }
    }

    private GenJob[] makeGenJobs() {
        var jobs = new GenJob[int.Max(GenJob.pool.workers, 1) * 4];
        for (var i = 0; i < jobs.Length; i++) {
            jobs[i] = new GenJob(this);
        }
        return jobs;
    }

    /**
     * Load this chunk either from disk (if exists) or generate it with the given level.
     */
    public void loadChunk(ChunkCoord chunkCoord, ChunkStatus status, bool immediately = false) {
        // TODO emergency switch! if players complain about chunk errors & lost data / crashes, flip this switch! it should make things better
        // it will make chunk loading synchronous and thus laggy / especially on shit HDDs, but it will prevent chunk errors until we can fix things:tm:
        //immediately = true;

        // on the server, all is sync! we don't have a frame yk
        // we *can* make it async later but for now it's less buggy to make it so
        if (!isClient) {
            immediately = true;
            // server never meshes - cap to LIGHTED
            if (status > ChunkStatus.LIGHTED) {
                status = ChunkStatus.LIGHTED;
            }
        }

        // on MP client, chunks come from server already LIGHTED - don't try to generate/light them
        // meshing happens via dirtyChunk, not loadChunk :(
        if (!isServer && status > ChunkStatus.LIGHTED) {
            status = ChunkStatus.LIGHTED;
        }

        // early exit if chunk is too far from player (unless forced immediate load)
        if (!immediately && !isChunkRelevant(chunkCoord)) {
            return;
        }

        // if it already exists and has the proper level, just return it
        if (chunks.TryGetValue(chunkCoord.toLong(), out var chunk) && chunk.status >= status) {
            return;
        }

        // does the chunk exist?
        bool hasChunk = chunk != null;

        Chunk c;
        bool chunkAdded = false;

        // if it exists on disk, load it asynchronously

        if (!immediately) {
            if (!hasChunk && WorldIO.chunkFileExists(this, chunkCoord)) {
                // queue for async loading - async result processing will handle status progression

                // we cheat! we only load up to GENERATED asynchronously, rest goes normally!
                worldIO.loadChunkAsync(chunkCoord, ChunkStatus.GENERATED);
                // create empty chunk that will be populated when async loading completes
                c = new Chunk(this, chunkCoord.x, chunkCoord.z);
                addChunk(chunkCoord, c);
                return;
            }
        }
        else {
            // load synchronously
            if (!hasChunk && WorldIO.chunkFileExists(this, chunkCoord)) {
                Chunk ch;
                try {
                    ch = WorldIO.loadChunkFromFile(this, chunkCoord);
                    addChunk(chunkCoord, ch);
                    // we got the chunk so set to true
                    hasChunk = true;
                    chunkAdded = true;
                }
                catch (EndOfStreamException e) {
                    // corrupted chunk file!
                    Log.error($"Corrupted chunk file for {chunkCoord}", e);
                    hasChunk = false;
                    chunkAdded = false;
                }
                catch (IOException e) {
                    // corrupted chunk file! or can't read it for some reason
                    Log.error($"IO error loading chunk file for {chunkCoord}", e);
                    hasChunk = false;
                    chunkAdded = false;
                }
            }
        }

        // save nosend state (need for recursive loadChunk calls..)
        bool oldNosend = nosend;

        // if it doesn't exist, generate it
        if (status >= ChunkStatus.GENERATED &&
            (!hasChunk || (hasChunk && chunks[chunkCoord.toLong()].status < ChunkStatus.GENERATED))) {
            if (!chunkAdded) {
                c = new Chunk(this, chunkCoord.x, chunkCoord.z);
                addChunk(chunkCoord, c);
                chunk = c;
            }
            // if we ever reach here we're fucked
            if (!isServer) {
                SkillIssueException.throwNew("fix your fucking MP chunk handling");
                return;
            }

            nosend = true; // suppress network updates during terrain generation
            chunk ??= chunks[chunkCoord.toLong()];
            generator.generate(chunk);
            chunk.recalc();
            nosend = oldNosend; // restore
        }

        if (status >= ChunkStatus.POPULATED &&
            (!hasChunk || (hasChunk && chunks[chunkCoord.toLong()].status < ChunkStatus.POPULATED))) {
            if (!areNeighboursReady(chunkCoord, ChunkStatus.GENERATED, POPULATE_REACH)) {
                // queue neighbours for loading and defer this chunk

                // DISABLE ASYNC, lighting should happen immediately too!
                if (false && !immediately) {
                    queueNeighboursForLoading(chunkCoord, ChunkStatus.GENERATED);
                    addToChunkLoadQueue(chunkCoord, status);
                    nosend = oldNosend; // restore before early return
                    return;
                }
                else {
                    loadNeighbours(chunkCoord, ChunkStatus.GENERATED, POPULATE_REACH);
                }
            }

            nosend = true; // suppress network updates during surface generation (trees, etc.)
            generator.surface(chunkCoord);
            nosend = oldNosend; // restore
        }

        if (status >= ChunkStatus.LIGHTED &&
            (!hasChunk || (hasChunk && chunks[chunkCoord.toLong()].status < ChunkStatus.LIGHTED))) {
            if (!areNeighboursReady(chunkCoord, ChunkStatus.POPULATED, POPULATE_REACH)) {
                loadNeighbours(chunkCoord, ChunkStatus.POPULATED, POPULATE_REACH);
            }

            chunks[chunkCoord.toLong()].lightChunk();

            // trigger remeshing of neighbours now that this chunk is LIGHTED
            if (isClient) {
                Span<ChunkCoord> neighbours = [
                    new(chunkCoord.x - 1, chunkCoord.z),
                    new(chunkCoord.x + 1, chunkCoord.z),
                    new(chunkCoord.x, chunkCoord.z - 1),
                    new(chunkCoord.x, chunkCoord.z + 1),
                    new(chunkCoord.x - 1, chunkCoord.z - 1),
                    new(chunkCoord.x - 1, chunkCoord.z + 1),
                    new(chunkCoord.x + 1, chunkCoord.z - 1),
                    new(chunkCoord.x + 1, chunkCoord.z + 1)
                ];

                foreach (var neighbour in neighbours) {
                    if (chunks.TryGetValue(neighbour.toLong(), out var neighbourChunk) &&
                        neighbourChunk.status >= ChunkStatus.LIGHTED) {
                        // neighbour is loaded and lighted, dirty it to trigger remesh attempt
                        for (int y = 0; y < Chunk.CHUNKHEIGHT; y++) {
                            dirtyChunk(new SubChunkCoord(neighbour.x, y, neighbour.z));
                        }
                    }
                }
            }

            // reassign any entities waiting for this chunk (needs to happen at LIGHTED for servers)
            loadEntitiesIntoChunk(chunkCoord);
        }

        if (status >= ChunkStatus.MESHED &&
            (!hasChunk || (hasChunk && chunks[chunkCoord.toLong()].status < ChunkStatus.MESHED))) {
            // check if neighbours are ready, if not defer this chunk
            if (!areNeighboursReady(chunkCoord, ChunkStatus.LIGHTED)) {
                if (!isServer) {
                    // in multiplayer client, can't generate neighbours - re-queue and wait for server to send them
                    addToChunkLoadQueue(chunkCoord, status);
                    return;
                }

                // load neighbours SYNCHRONOUSLY (singleplayer/server can generate)
                loadNeighbours(chunkCoord, ChunkStatus.LIGHTED);
            }

            if (isClient) {
                chunks[chunkCoord.toLong()].meshChunk();
            }
        }
    }

    // MAKE IT SO ONLY THE ORIGINAL BLOCK IS UPDATED
    // and the neighbours are notifiyed of this update
    // and they can decide what to do, THEY ARE NOT UPDATED THEMSELVES

    /**
     * ID is the new
     */
    public void blockScheduledUpdate(int x, int y, int z) {
        Block.get(getBlock(x, y, z)).scheduledUpdate(this, x, y, z);
    }

    public void blockUpdateNeighbours(int x, int y, int z) {

        if (!isServer) {
            return;
        }

        Block.get(getBlock(x, y, z)).update(this, x, y, z);
        foreach (var dir in Direction.directions) {
            var neighbourBlock = new Vector3I(x, y, z) + dir;
            Block.get(getBlock(neighbourBlock)).update(this, neighbourBlock.X, neighbourBlock.Y, neighbourBlock.Z);
        }
    }

    public void blockUpdateNeighboursOnly(int x, int y, int z) {

        if (!isServer) {
            return;
        }

        foreach (var dir in Direction.directions) {
            var neighbourBlock = new Vector3I(x, y, z) + dir;
            Block.get(getBlock(neighbourBlock)).update(this, neighbourBlock.X, neighbourBlock.Y, neighbourBlock.Z);
        }
    }

    public void scheduleBlockUpdate(Vector3I pos, int delay = -1) {

        // todo is this correct?
        if (!isServer) {
            return;
        }

        var blockId = getBlockRaw(pos).getID();
        var actualDelay = delay != -1 ? delay : Block.updateDelay[blockId];
        var update = new BlockUpdate(pos, worldTick + actualDelay);
        if (actualDelay > 0 && !blockUpdateQueue.Contains(update)) {
            blockUpdateQueue.Add(update);
        }
    }
}