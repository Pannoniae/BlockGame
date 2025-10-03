using BlockGame.main;
using BlockGame.render;
using BlockGame.ui;
using BlockGame.util;
using BlockGame.util.log;
using BlockGame.util.xNBT;
using BlockGame.world.block;
using BlockGame.world.chunk;
using BlockGame.world.worldgen.generator;
using Molten;
using Molten.DoublePrecision;
using Silk.NET.GLFW;
using Silk.NET.Windowing.Glfw;

namespace BlockGame.world;

public partial class World : IDisposable {
    public const int WORLDSIZE = 12;
    public const int REGIONSIZE = 16;
    public const int WORLDHEIGHT = Chunk.CHUNKHEIGHT * Chunk.CHUNKSIZE;

    // try to keep 120 FPS at least
    public const double MAX_CHUNKLOAD_FRAMETIME = 1000 / 180.0;
    public const double MAX_MESHING_FRAMETIME = 1000 / 360.0;


    // when loading the world, we can load chunks faster because fuck cares about a loading screen?
    public const double MAX_CHUNKLOAD_FRAMETIME_FAST = 1000 / 10.0;

    // this applies to the queues *separately* so it's lower
    public const double MAX_LIGHT_FRAMETIME = 1000 / 480.0;
    public const int SPAWNCHUNKS_SIZE = 1;
    public const int MAX_TICKING_DISTANCE = 128;

    /// <summary>
    /// Random ticks per chunk section per tick. Normally 3 but let's test with 50
    /// </summary>
    public const int numTicks = 1;

    public string name;


    public readonly List<WorldListener> listeners = [];

    public readonly Dictionary<ChunkCoord, Chunk> chunks;

    // used for rendering
    public readonly List<Chunk> chunkList;

    //public List<ChunkSection> sortedTransparentChunks = [];


    // Queues
    public List<ChunkLoadTicket> chunkLoadQueue = new();
    //public HashSet<ChunkLoadTicket> chunkLoadQueueSet = new();

    public List<BlockUpdate> blockUpdateQueue = new();
    public HashSet<BlockUpdate> blockUpdateQueueSet = new();


    public List<TickAction> actionQueue = new();

    public List<LightNode> skyLightQueue = new();
    public List<LightRemovalNode> skyLightRemovalQueue = new();
    public List<LightNode> blockLightQueue = new();
    public List<LightRemovalNode> blockLightRemovalQueue = new();

    public WorldGenerator generator;

    public bool isLoading;

    /**
     * Tracking for stuck queue detection (used for shuffling the chunkload queue only when stuck)
     */
    private int lastQueueSize = -1;
    private int stuckIterations = 0;

    /**
     * True if the world has actually been initialized, false if the init method hasn't been called yet.
     */
    public bool inited;

    public bool paused;
    public bool inMenu;

    public WorldIO worldIO;

    public int seed;


    public int worldTick;

    public const int TICKS_PER_DAY = 72000;

    public XRandom random;
    private TimerAction saveWorld;
    public NBTCompound toBeLoadedNBT;
    private static readonly List<AABB> listAABB = [];

    public World(string name, int seed) {
        this.name = name;
        inited = false;
        worldIO = new WorldIO(this);
        generator = new PerlinWorldGenerator(this);

        random = new XRandom(seed);
        worldTick = 0;

        generator.setup(seed);
        this.seed = seed;

        chunks = new Dictionary<ChunkCoord, Chunk>();
        chunkList = new List<Chunk>(2048);

        entities = [];
        particles = new Particles(this);

        // setup world saving every 5 seconds
        // NOTE: this used to memory leak the ENTIRE WORLD because it was capturing the world reference in the method in Main.timerQueue.
        // to avoid that, ALWAYS MAKE SURE methods aren't overwritten!

        // SAFETY CHECK
        if (saveWorld != null) {
            Game.clearInterval(saveWorld);
        }

        saveWorld = Game.setInterval(5 * 1000, saveWorldMethod);
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
        player = new ClientPlayer(this, 6, 20, 6);
        addEntity(player);
        Game.player = player as ClientPlayer;
        Game.camera.setPlayer(player);

        if (!loadingSave) {
            // find safe spawn position with proper AABB clearance
            ensurePlayerSpawnClearance();
        }

        // if loading, actually load
        if (loadingSave) {
            var tag = toBeLoadedNBT;
            Log.debug(tag.getDouble("posX"));
            Log.debug(tag.getDouble("posY"));
            Log.debug(tag.getDouble("posZ"));
            player.position.X = tag.getDouble("posX");
            player.position.Y = tag.getDouble("posY");
            player.position.Z = tag.getDouble("posZ");
            worldTick = tag.has("time") ? tag.getInt("time") : 0;

            player.prevPosition = player.position;
        }

        // After everything is done, SAVE THE WORLD
        // if we don't save the world, some of the chunks might get saved but no level.xnbt
        // so the world is corrupted and we have horrible chunkglitches
        worldIO.save(this, name, false);


        foreach (var l in listeners) {
            l.onWorldLoad();
        }

        inited = true;
    }

    public void postInit(bool loadingSave = false) {
        // notify all entities of the chunks they're actually in
        // handled in loadChunk!
    }

    private void saveWorldMethod() {
        if (!inited) {
            // don't save the world if it hasn't been initialized yet
            return;
        }

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
        var x = 0;
        foreach (var chunk in chunks.Values) {
            if (chunk.status >= ChunkStatus.MESHED &&
                chunk.lastSaved + 15 * 1000 < (ulong)Game.permanentStopwatch.ElapsedMilliseconds) {
                worldIO.saveChunkAsync(this, chunk);
                x++;
            }
        }

        if (x > 0) {
            Log.info($"Queued {x} chunks for async save");
        }
    }

    public void startMeshing() {
        foreach (var chunk in chunks.Values) {
            if (chunk.status < ChunkStatus.MESHED) {
                addToChunkLoadQueue(chunk.coord, ChunkStatus.MESHED);
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

    public void addChunk(ChunkCoord coord, Chunk chunk) {
        chunks[coord] = chunk;
        chunkList.Add(chunk);
        foreach (var l in listeners) {
            l.onChunkLoad(coord);
        }
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
                    if (bl == Blocks.AIR) {
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

        return true;
    }

    public void sortChunks() {
        // sort queue based on position
        // don't reorder across statuses though

        // note: removal is faster from the end so we sort by the reverse - closest entries are at the end of the list
        chunkLoadQueue.Sort(new ChunkTicketComparerReverse(player.position.toBlockPos()));
    }

    public void sortChunksRandom() {
        // randomize the chunk load queue so chunks can load in a more random order
        // this helps with not getting deadlocked
        var rnd = new XRandom();
        chunkLoadQueue = chunkLoadQueue.OrderBy(x => rnd.Next()).ToList();
    }

    public void loadAroundPlayer() {
        // create terrain
        //genTerrainNoise();
        // separate loop so all data is there
        player.loadChunksAroundThePlayer(Settings.instance.renderDistance);
    }

    public void loadAroundPlayer(ChunkStatus status) {
        player.loadChunksAroundThePlayer(Settings.instance.renderDistance, status);
    }

    public int getBrightness(byte skylight, byte skyDarken) {
        // apply sky darkening to skylight only
        return Math.Max(0, skylight - skyDarken);
    }

    public float getDayPercentage(int ticks) {
        return (ticks % TICKS_PER_DAY) / (float)TICKS_PER_DAY;
    }

    private const float TWILIGHT_ANGLE = -12f * MathF.PI / 180f; // -6 degrees WHICH WE DON'T HAVE
    private const float SUNRISE_ANGLE = 0f;
    private const float SOLAR_NOON_ANGLE = MathF.PI / 2f;
    private const float SUNSET_ANGLE = MathF.PI;

    public float getSunAngle(int ticks) {
        float dayPercent = getDayPercentage(ticks);
        // Maps 0-1 to 0-2π (full rotation)
        return dayPercent * MathF.PI * 2f;
    }

    public float getSunElevation(int ticks) {
        float angle = getSunAngle(ticks);
        return MathF.Sin(angle);
    }

    public Color4b getSkyColour(int ticks) {
        float e = getSunElevation(ticks);
        float angle = getSunAngle(ticks);

        var nightSky = new Color4b(5, 5, 15);
        var daySky = new Color4b(100, 180, 255);

        if (e < TWILIGHT_ANGLE) {
            // night
            return nightSky;
        }
        else if (e < SUNRISE_ANGLE) {
            // civil twilight
            float t = (e - TWILIGHT_ANGLE) / (SUNRISE_ANGLE - TWILIGHT_ANGLE);
            return Color4b.Lerp(nightSky, new Color4b(20, 35, 80), t);
        }
        else if (e < MathF.PI / 12f) {
            // 15 deg, sunrise/sunset
            float t = e / (MathF.PI / 12f);
            return Color4b.Lerp(new Color4b(20, 35, 80), daySky, t);
        }
        else {
            // Full day
            return daySky;
        }
    }

    public Color4b getHorizonColour(int ticks) {
        float e = getSunElevation(ticks);
        float angle = getSunAngle(ticks);

        var nightHorizon = new Color4b(15, 15, 40);
        var dayHorizon = new Color4b(135, 206, 235);

        const float NIGHT_START = -18f * MathF.PI / 180f;
        const float TWILIGHT_START = -12f * MathF.PI / 180f;
        const float GOLDEN_START = -0f * MathF.PI / 180f;
        const float GOLDEN_END = 10f * MathF.PI / 180f;
        const float DAY_START = 30f * MathF.PI / 180f;

        // Smooth blend between sunrise/sunset colors based on time of day
        float isSunset;
        switch (angle) {
            // morning
            case < MathF.PI / 2f:
                isSunset = angle / (MathF.PI / 2f) * 0.5f;
                break;
            // evening
            case < MathF.PI:
                isSunset = 0.5f + (angle - MathF.PI / 2f) / (MathF.PI / 2f) * 0.5f;
                break;
            // night
            case < 3f * MathF.PI / 2f:
                isSunset = 1f - (angle - MathF.PI) / (MathF.PI / 2f) * 0.5f;
                break;
            // sunrise
            default:
                isSunset = 0.5f - (angle - 3f * MathF.PI / 2f) / (MathF.PI / 2f) * 0.5f;
                break;
        }

        var twilightColor = Color4b.Lerp(
            new Color4b(80, 40, 100), // dawn purple
            new Color4b(120, 50, 90), // sunset purple=pink
            isSunset);

        var goldenColor = Color4b.Lerp(
            new Color4b(255, 140, 80), // dawn orange
            new Color4b(255, 80, 50), // sunset red-orange-ish thingie
            isSunset);

        switch (e) {
            case <= NIGHT_START:
                return nightHorizon;
            case <= TWILIGHT_START: {
                float t = (e - NIGHT_START) / (TWILIGHT_START - NIGHT_START);
                return Color4b.Lerp(nightHorizon, twilightColor, t);
            }
            case <= GOLDEN_START: {
                float t = (e - TWILIGHT_START) / (GOLDEN_START - TWILIGHT_START);
                return Color4b.Lerp(twilightColor, goldenColor, t);
            }
            case <= GOLDEN_END: {
                float t = (e - GOLDEN_START) / (GOLDEN_END - GOLDEN_START);
                // ???
                return goldenColor;
            }
            case <= DAY_START: {
                float t = (e - GOLDEN_END) / (DAY_START - GOLDEN_END);
                return Color4b.Lerp(goldenColor, dayHorizon, t);
            }
            default:
                return dayHorizon;
        }
    }

    public Color4b getFogColour(int ticks) {
        var horizon = getHorizonColour(ticks);
        var gray = new Color4b(180, 180, 180);
        return Color4b.Lerp(horizon, gray, 0.3f);
    }

    public float getSkyDarkenFloat(int ticks) {
        float elevation = getSunElevation(ticks);

        float darken;

        switch (elevation) {
            case > SUNRISE_ANGLE:
                darken = 0f;
                break;
            case > TWILIGHT_ANGLE: {
                float t = (elevation - TWILIGHT_ANGLE) / (SUNRISE_ANGLE - TWILIGHT_ANGLE);
                darken = 11f * (1f - t);
                break;
            }
            default:
                darken = 11f;
                break;
        }

        return !Settings.instance.smoothDayNight ? (float)Math.Round(darken) : darken;
    }


    /// <summary>
    /// Chunkloading and friends.
    /// </summary>
    public void renderUpdate(double dt) {
        var start = Game.permanentStopwatch.Elapsed.TotalMilliseconds;
        var ctr = 0;
        updateChunkloading(start, loading: false, ref ctr);

        particles.update(dt);
    }

    private void processAsyncChunkLoads(double startTime, bool loading, ref int loadedChunks) {
        var limit = loading ? MAX_CHUNKLOAD_FRAMETIME_FAST : MAX_CHUNKLOAD_FRAMETIME;

        while (worldIO.hasChunkLoadResult() && Game.permanentStopwatch.Elapsed.TotalMilliseconds - startTime < limit) {
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

            if (chunks.TryGetValue(coord, out Chunk? existingChunk) && result.Value.nbtData != null) {
                WorldIO.loadChunkDataFromNBT(existingChunk, result.Value.nbtData);
                loadedChunks++;
            }

            // re-queue for status progression (GENERATED -> POPULATED -> LIGHTED -> MESHED)
            // this will handle neighbor dependencies correctly
            addToChunkLoadQueue(result.Value.coord, result.Value.targetStatus);
        }
    }

    /** This is separate so this can be called from the outside without updating the whole (still nonexistent) world. */
    public void updateChunkloading(double startTime, bool loading, ref int loadedChunks) {
        // process async chunk load results first
        processAsyncChunkLoads(startTime, loading, ref loadedChunks);

        // if is loading, don't throttle
        // consume the chunk queue
        // ONLY IF THERE ARE CHUNKS
        // otherwise don't wait for nothing
        // yes I was an idiot
        var limit = loading ? MAX_CHUNKLOAD_FRAMETIME_FAST : MAX_CHUNKLOAD_FRAMETIME;
        while (Game.permanentStopwatch.Elapsed.TotalMilliseconds - startTime < limit) {
            // check if queue is stuck and shuffle only when needed
            var currentQueueSize = chunkLoadQueue.Count;
            if (lastQueueSize == currentQueueSize) {
                stuckIterations++;
                // only shuffle if stuck for a while (queue size not changing)
                if (stuckIterations > 20) {
                    sortChunksRandom();
                    stuckIterations = 0;
                }
            }
            else {
                // queue is making progress, reset stuck counter
                stuckIterations = 0;
                lastQueueSize = currentQueueSize;
            }

            if (chunkLoadQueue.Count > 0) {
                var ticket = chunkLoadQueue[chunkLoadQueue.Count - 1];
                chunkLoadQueue.RemoveAt(chunkLoadQueue.Count - 1);

                // check if chunk is still relevant before loading it
                if (isChunkRelevant(ticket.chunkCoord)) {
                    loadChunk(ticket.chunkCoord, ticket.level);
                    loadedChunks++;
                }
                // if too far, just discard the ticket and continue
            }
            else {
                // chunk queue empty, don't loop more
                isLoading = false;
                break;
            }
        }

        if (!loading) {
            return;
        }

        // if we're loading, we can also mesh chunks
        // empty the meshing queue
        while (Game.renderer.meshingQueue.TryDequeue(out var sectionCoord)) {
            // if this chunk doesn't exist anymore (because we unloaded it)
            // then don't mesh! otherwise we'll fucking crash
            if (!isChunkSectionInWorld(sectionCoord)) {
                continue;
            }

            var section = getSubChunk(sectionCoord);
            Game.blockRenderer.meshChunk(section);
        }

        // debug
        /*Console.Out.WriteLine("---BEGIN---");
        foreach (var chunk in chunkLoadQueue) {
            Console.Out.WriteLine(chunk.level);
        }
        Console.Out.WriteLine("---END---");*/
        //Console.Out.WriteLine(Game.permanentStopwatch.ElapsedMilliseconds - start);
        //Console.Out.WriteLine($"{ctr} chunks loaded");
    }

    public void update(double dt) {
        worldTick++;
        /*if (Vector3D.DistanceSquared(player.position, player.lastSort) > 64) {
            sortedTransparentChunks.Sort(new ChunkComparer(player.camera));
            player.lastSort = player.position;
        }*/

        // execute tick actions
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
                blockUpdateQueueSet.Remove(update);
            }
        }

        // execute lighting updates
        SuperluminalPerf.BeginEvent("light");
        processSkyLightRemovalQueue();
        processSkyLightQueue();
        processBlockLightRemovalQueue();
        processBlockLightQueue();
        SuperluminalPerf.EndEvent();

        // random block updates!
        foreach (var chunk in chunks) {
            // distance check
            if (Vector2I.DistanceSquared(chunk.Value.centrePos,
                    new Vector2I((int)player.position.X, (int)player.position.Z)) <
                MAX_TICKING_DISTANCE * MAX_TICKING_DISTANCE) {
                foreach (var chunksection in chunk.Value.subChunks) {
                    if (!chunksection.blocks.hasRandomTickingBlocks()) {
                        continue;
                    }

                    for (int i = 0; i < numTicks; i++) {
                        // I pray this is random
                        var coord = random.Next(16 * 16 * 16);
                        var x = (coord >> 8);
                        var y = (coord >> 4) & 0xF;
                        var z = coord & 0xF;
                        chunksection.tick(this, random, x, y, z);
                    }
                }
            }
        }

        updateEntities(dt);
    }

    public void processSkyLightQueue() {
        //SuperluminalPerf.BeginEvent("skylight");
        processLightQueue(skyLightQueue, true);
        //SuperluminalPerf.EndEvent();
    }

    public void processSkyLightQueueNoUpdate() {
        processLightQueue(skyLightQueue, true, true);
    }

    public void processSkyLightQueueLoading() {
        // this is used when loading the world, so we don't remesh the chunks, we only process one at a time!
        processLightQueueOne(skyLightQueue, true, true);
    }

    public void processSkyLightQueueLoading(int count) {
        // this is used when loading the world, so we don't remesh the chunks, we only process one at a time!

        for (int i = 0; i < count; i++) {
            if (skyLightQueue.Count == 0) {
                break; // no more nodes to process
            }

            processLightQueueOne(skyLightQueue, true, true);
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
        // get the neighbor chunk and relative position
        var neighbourPos = getChunkAndRelativePos(currentChunk, x, y, z, direction, out var neighbourChunk);
        if (neighbourChunk == null) {
            return 0; // no chunk loaded
        }

        // return the block at the neighbour position
        return neighbourChunk.getBlock(neighbourPos.X, neighbourPos.Y, neighbourPos.Z);
    }

    /// <summary>
    /// Gets the chunk and relative position for a neighbor of a block in chunk-relative coordinates
    /// </summary>
    public Vector3I getChunkAndRelativePos(Chunk currentChunk, int x, int y, int z, Vector3I direction,
        out Chunk? chunk) {
        var neighbour = new Vector3I(x, y, z) + direction;

        if (neighbour.Y is < 0 or >= WORLDHEIGHT) {
            chunk = null;
            return Vector3I.Zero;
        }

        // Check if neighbor is within current chunk bounds (0-15 for X/Z)
        if (neighbour.X is >= 0 and < 16 && neighbour.Z is >= 0 and < 16) {
            chunk = currentChunk;
            return neighbour;
        }


        // todo this could be way simpler but it was buggy so im leaving the optimisation for later
        // neighbour crosses XZ boundary - calculate global position and find target chunk
        //var neighbourGlobal = toWorldPos(currentChunk.coord.x, currentChunk.coord.z, neighbourX, neighbourY, neighbourZ);

        // get the chunk world coord by shifting the chunk-relative coordinates "out" of the number
        var newX = (currentChunk.coord.x << 4) + neighbour.X;
        var newZ = (currentChunk.coord.z << 4) + neighbour.Z;

        // get target chunk
        // this assigns directly to the output variable! might be null, FYI
        if (!getChunkMaybe(newX, newZ, out var testChunk)) {
            chunk = null;
            return Vector3I.Zero; // Chunk not loaded, bail
        }


        chunk = testChunk;
        return new Vector3I(newX & 0xF, neighbour.Y, newZ & 0xF);
    }


    /**
     * If noUpdate, we're loading, don't bother invalidating chunks, they'll get remeshed *anyway*
     */
    public void processLightQueue(List<LightNode> queue, bool isSkylight, bool noUpdate = false) {
        var start = Game.permanentStopwatch.Elapsed.TotalMilliseconds;
        while (queue.Count > 0 && Game.permanentStopwatch.Elapsed.TotalMilliseconds - start < MAX_LIGHT_FRAMETIME) {
            processLightQueueOne(queue, isSkylight, noUpdate);
        }
    }


    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void processLightQueueOne(List<LightNode> queue, bool isSkylight, bool noUpdate) {
        var cnt = queue.Count;
        //Console.Out.WriteLine(cnt);
        var node = queue[cnt - 1];
        queue.RemoveAt(cnt - 1);

        // if null or destroyed, chunk got unloaded meanwhile
        if (node.chunk == null! || node.chunk.destroyed) {
            return;
        }

        //var blockPos = new Vector3I(node.x, node.y, node.z);
        byte level = isSkylight
            ? node.chunk.getSkyLight(node.x, node.y, node.z)
            : node.chunk.getBlockLight(node.x, node.y, node.z);

        // if this is opaque (for skylight), don't bother
        if (isSkylight && Block.isFullBlock(node.chunk.getBlock(node.x, node.y, node.z))) {
            return;
        }

        //Console.Out.WriteLine(blockPos);

        foreach (var dir in Direction.directionsLight) {
            // Get neighbor chunk and relative position
            var neighborRelPos = getChunkAndRelativePos(node.chunk, node.x, node.y, node.z, dir, out var neighborChunk);
            if (neighborChunk == null) {
                continue;
            }

            // if neighbour is opaque, don't bother either
            if (Block.isFullBlock(neighborChunk.getBlock(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z))) {
                continue;
            }

            byte neighbourLevel = isSkylight
                ? neighborChunk.getSkyLight(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z)
                : neighborChunk.getBlockLight(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z);
            //var neighbourBlock = getBlock(neighbour);
            var isDown = isSkylight && level == 15 && neighbourLevel != 15 && dir == Direction.DOWN;

            if (neighbourLevel + 2 <= level || isDown) {
                var neighborBlockId = neighborChunk.getBlock(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z);
                var absorption = Block.lightAbsorption[neighborBlockId];

                // apply absorption, or if no absorption: down=no decrease, sideways=decrease by 1
                var decrease = absorption > 0 ? absorption : (isDown ? 0 : 1);
                byte newLevel = (byte)Math.Max(0, level - decrease);
                if (isSkylight) {
                    if (noUpdate) {
                        neighborChunk.setSkyLightDumb(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z, newLevel);
                    }
                    else {
                        neighborChunk.setSkyLight(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z,
                            newLevel);
                    }
                }
                else {
                    if (noUpdate) {
                        neighborChunk.setBlockLightDumb(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z, newLevel);
                    }
                    else {
                        neighborChunk.setBlockLight(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z,
                            newLevel);
                    }
                }

                queue.Add(new LightNode(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z, neighborChunk));
            }
        }
    }

    public void processLightRemovalQueue(List<LightRemovalNode> queue, List<LightNode> addQueue, bool isSkylight) {
        var start = Game.permanentStopwatch.Elapsed.TotalMilliseconds;
        while (queue.Count > 0 && Game.permanentStopwatch.Elapsed.TotalMilliseconds - start < MAX_LIGHT_FRAMETIME) {
            processLightRemovalQueueOne(queue, addQueue, isSkylight);
        }
    }

    public void processLightRemovalQueueOne(List<LightRemovalNode> queue, List<LightNode> addQueue, bool isSkylight) {
        var cnt = queue.Count;
        var node = queue[cnt - 1];
        queue.RemoveAt(cnt - 1);

        //var blockPos = new Vector3I(node.x, node.y, node.z);
        var level = node.value;

        // if null or destroyed, chunk got unloaded meanwhile
        if (node.chunk == null! || node.chunk.destroyed) {
            return;
        }

        foreach (var dir in Direction.directionsLight) {
            // Get neighbor chunk and relative position
            var neighborRelPos = getChunkAndRelativePos(node.chunk, node.x, node.y, node.z, dir, out var neighborChunk);
            if (neighborChunk == null) {
                continue;
            }

            byte neighbourLevel = isSkylight
                ? neighborChunk.getSkyLight(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z)
                : neighborChunk.getBlockLight(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z);
            var isDownLight = isSkylight && dir == Direction.DOWN && level == 15;
            if (isDownLight || neighbourLevel != 0 && neighbourLevel < level) {
                if (isSkylight) {
                    neighborChunk.setSkyLight(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z, 0);
                }
                else {
                    neighborChunk.setBlockLight(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z, 0);
                }

                // Emplace new node to queue. (could use push as well)
                queue.Add(new LightRemovalNode(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z, neighbourLevel,
                    neighborChunk));
            }
            else if (neighbourLevel >= level) {
                // Add it to the update queue, so it can propagate to fill in the gaps
                // left behind by this removal. We should update the lightBfsQueue after
                // the lightRemovalBfsQueue is empty.
                addQueue.Add(new LightNode(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z, neighborChunk));
            }
        }
    }

    public void addToChunkLoadQueue(ChunkCoord chunkCoord, ChunkStatus level) {
        var ticket = new ChunkLoadTicket(chunkCoord, level);
        if (!chunkLoadQueue.Contains(ticket)) {
            chunkLoadQueue.Add(ticket);
        }
    }

    /// <summary>
    /// Chunks are generated up to renderDistance + 1.
    /// Chunks are populated (tree placement, etc.) until renderDistance and meshed until renderDistance.
    /// TODO unload chunks which are renderDistance + 2 away (this is bigger to prevent chunk flicker)
    /// </summary>
    public void loadChunksAroundChunk(ChunkCoord chunkCoord, int renderDistance) {
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

        var playerChunk = player.getChunk();
        // unload chunks which are far away
        foreach (var chunk in chunks.Values) {
            var coord = chunk.coord;
            // if distance is greater than renderDistance + 3, unload
            if (playerChunk.distanceSq(coord) >= (renderDistance + 3) * (renderDistance + 3)) {
                unloadChunk(coord);
            }
        }
    }

    public void loadChunksAroundChunkImmediately(ChunkCoord chunkCoord, int renderDistance) {
        // finally, mesh around renderDistance
        for (int x = chunkCoord.x - renderDistance; x <= chunkCoord.x + renderDistance; x++) {
            for (int z = chunkCoord.z - renderDistance; z <= chunkCoord.z + renderDistance; z++) {
                var coord = new ChunkCoord(x, z);
                if (coord.distanceSq(chunkCoord) <= renderDistance * renderDistance) {
                    loadChunk(coord, ChunkStatus.MESHED, true);
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
        var chunk = chunks[coord];
        // save chunk asynchronously to prevent lagspikes
        worldIO.saveChunkAsync(this, chunk);

        foreach (var l in listeners) {
            l.onChunkUnload(coord);
        }

        // ONLY DO THIS WHEN IT'S ALREADY SAVED
        chunkList.Remove(chunk);
        chunks.Remove(coord);
        chunk.destroyChunk();
    }

    private void ReleaseUnmanagedResources() {
        // do NOT save chunks!!! this fucks the new world
        foreach (var chunk in chunks) {
            chunks[chunk.Key].destroyChunk();
        }
    }

    public void unload() {
        Dispose();
    }

    public void Dispose() {
        foreach (var l in listeners) {
            l.onWorldUnload();
        }

        // stop automatic saves
        saveWorld.enabled = false;
        Game.clearInterval(saveWorld);
        saveWorld = null!;

        // stop the chunksave queue and save pending chunks
        worldIO.Dispose();

        // of course, we can save it here since WE call it and not the GC
        // save the whole thing
        worldIO.save(this, name);


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
    /// Check if all neighbors around a chunk have reached the specified status
    /// </summary>
    private bool areNeighboursReady(ChunkCoord chunkCoord, ChunkStatus requiredStatus) {
        var neighbours = new[] {
            new ChunkCoord(chunkCoord.x - 1, chunkCoord.z),
            new ChunkCoord(chunkCoord.x + 1, chunkCoord.z),
            new ChunkCoord(chunkCoord.x, chunkCoord.z - 1),
            new ChunkCoord(chunkCoord.x, chunkCoord.z + 1),
            new ChunkCoord(chunkCoord.x - 1, chunkCoord.z - 1),
            new ChunkCoord(chunkCoord.x - 1, chunkCoord.z + 1),
            new ChunkCoord(chunkCoord.x + 1, chunkCoord.z - 1),
            new ChunkCoord(chunkCoord.x + 1, chunkCoord.z + 1)
        };

        foreach (var neighbour in neighbours) {
            if (!chunks.TryGetValue(neighbour, out var neighbourChunk) || neighbourChunk.status < requiredStatus) {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Queue neighbors for loading and ensure they reach the required status
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
            if (!chunks.TryGetValue(neighbour, out var neighbourChunk) || neighbourChunk.status < requiredStatus) {
                addToChunkLoadQueue(neighbour, requiredStatus);
            }
        }
    }

    private void loadNeighbours(ChunkCoord chunkCoord, ChunkStatus requiredStatus) {
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
            if (!chunks.TryGetValue(neighbour, out var neighbourChunk) || neighbourChunk.status < requiredStatus) {
                loadChunk(neighbour, requiredStatus, true);
            }
        }
    }

    /// <summary>
    /// Check if a chunk is still within loading distance of the player
    /// </summary>
    private bool isChunkRelevant(ChunkCoord chunkCoord) {
        var playerChunk = player.getChunk();
        var maxDistance = Settings.instance.renderDistance + 2; // bit more buffer than unload distance
        return playerChunk.distanceSq(chunkCoord) < maxDistance * maxDistance;
    }

    /// <summary>
    /// Load this chunk either from disk (if exists) or generate it with the given level.
    /// </summary>
    public void loadChunk(ChunkCoord chunkCoord, ChunkStatus status, bool immediately = false) {

        // TODO emergency switch! if players complain about chunk errors & lost data / crashes, flip this switch! it should make things better
        // it will make chunk loading synchronous and thus laggy / especially on shit HDDs, but it will prevent chunk errors until we can fix things:tm:
        //immediately = true;

        // early exit if chunk is too far from player (unless forced immediate load)
        if (!immediately && !isChunkRelevant(chunkCoord)) {
            return;
        }

        // if it already exists and has the proper level, just return it
        if (chunks.TryGetValue(chunkCoord, out var chunk) && chunk.status >= status) {
            return;
        }

        // does the chunk exist?
        bool hasChunk = chunk != null;

        Chunk c;
        bool chunkAdded = false;

        // if it exists on disk, load it asynchronously

        if (!immediately) {
            if (!hasChunk && WorldIO.chunkFileExists(name, chunkCoord)) {
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
            if (!hasChunk && WorldIO.chunkFileExists(name, chunkCoord)) {
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

        // if it doesn't exist, generate it
        if (status >= ChunkStatus.GENERATED &&
            (!hasChunk || (hasChunk && chunks[chunkCoord].status < ChunkStatus.GENERATED))) {
            if (!chunkAdded) {
                c = new Chunk(this, chunkCoord.x, chunkCoord.z);
                addChunk(chunkCoord, c);
            }

            generator.generate(chunkCoord);
        }

        if (status >= ChunkStatus.POPULATED &&
            (!hasChunk || (hasChunk && chunks[chunkCoord].status < ChunkStatus.POPULATED))) {
            // check if neighbors are ready, if not defer this chunk
            if (!areNeighboursReady(chunkCoord, ChunkStatus.GENERATED)) {
                // queue neighbors for loading and defer this chunk

                // DISABLE ASYNC, lighting should happen immediately too!
                if (false && !immediately) {
                    queueNeighboursForLoading(chunkCoord, ChunkStatus.GENERATED);
                    addToChunkLoadQueue(chunkCoord, status);
                    return;
                }
                else {
                    loadNeighbours(chunkCoord, ChunkStatus.GENERATED);
                }
            }

            generator.populate(chunkCoord);
        }

        if (status >= ChunkStatus.LIGHTED &&
            (!hasChunk || (hasChunk && chunks[chunkCoord].status < ChunkStatus.LIGHTED))) {
            chunks[chunkCoord].lightChunk();
        }

        if (status >= ChunkStatus.MESHED &&
            (!hasChunk || (hasChunk && chunks[chunkCoord].status < ChunkStatus.MESHED))) {
            // check if neighbors are ready, if not defer this chunk
            if (!areNeighboursReady(chunkCoord, ChunkStatus.LIGHTED)) {
                // load neighbors SYNCHRONOUSLY
                loadNeighbours(chunkCoord, ChunkStatus.LIGHTED);
                // DON'T DO THIS, IT MESSES THE QUEUE UP
                //addToChunkLoadQueue(chunkCoord, status);
                //return;
            }

            chunks[chunkCoord].meshChunk();
        }

        // reassign any entities waiting for this chunk
        loadEntitiesIntoChunk(chunkCoord);
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
        Block.get(getBlock(x, y, z)).update(this, x, y, z);
        foreach (var dir in Direction.directions) {
            var neighbourBlock = new Vector3I(x, y, z) + dir;
            Block.get(getBlock(neighbourBlock)).update(this, neighbourBlock.X, neighbourBlock.Y, neighbourBlock.Z);
        }
    }

    public void blockUpdateNeighboursOnly(int x, int y, int z) {
        foreach (var dir in Direction.directions) {
            var neighbourBlock = new Vector3I(x, y, z) + dir;
            Block.get(getBlock(neighbourBlock)).update(this, neighbourBlock.X, neighbourBlock.Y, neighbourBlock.Z);
        }
    }

    public void scheduleBlockUpdate(Vector3I pos, int delay = -1) {
        var blockId = getBlockRaw(pos).getID();
        var actualDelay = delay != -1 ? delay : Block.updateDelay[blockId];
        var update = new BlockUpdate(pos, worldTick + actualDelay);
        if (actualDelay > 0 && !blockUpdateQueue.Contains(update)) {
            blockUpdateQueue.Add(update);
            blockUpdateQueueSet.Add(update);
        }
    }
}