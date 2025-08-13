using BlockGame.GL.vertexformats;
using BlockGame.ui;
using BlockGame.util;
using Molten;

namespace BlockGame;

public partial class World : IDisposable {
    public const int WORLDSIZE = 12;
    public const int REGIONSIZE = 16;
    public const int WORLDHEIGHT = Chunk.CHUNKHEIGHT * Chunk.CHUNKSIZE;

    public string name;

    public readonly Dictionary<ChunkCoord, Chunk> chunks;
    
    public readonly List<WorldListener> listeners = [];

    // used for rendering
    public readonly List<Chunk> chunkList;

    public readonly List<Entity> entities;

    public readonly ParticleManager particleManager;
    //public List<ChunkSection> sortedTransparentChunks = [];

    // Queues
    public List<ChunkLoadTicket> chunkLoadQueue = new();
    public List<BlockUpdate> blockUpdateQueue = new();
    public List<TickAction> actionQueue = new();

    public List<LightNode> skyLightQueue = new();
    public List<LightRemovalNode> skyLightRemovalQueue = new();
    public List<LightNode> blockLightQueue = new();
    public List<LightRemovalNode> blockLightRemovalQueue = new();
    
    public WorldGenerator generator;

    public bool isLoading;

    public bool paused;
    public bool inMenu;

    public Player player;
    public WorldIO worldIO;

    public int seed;


    public int worldTick;
    
    public const int TICKS_PER_DAY = 72000;

    public XRandom random;
    private TimerAction saveWorld;
    
    // try to keep 120 FPS at least
    public const double MAX_CHUNKLOAD_FRAMETIME = 1000 / 180.0;
    public const double MAX_MESHING_FRAMETIME = 1000 / 360.0;
    
    
    // when loading the world, we can load chunks faster because fuck cares about a loading screen?
    public const double MAX_CHUNKLOAD_FRAMETIME_FAST = 1000 / 20.0;
    public const long MAX_LIGHT_FRAMETIME = 5;
    public const int SPAWNCHUNKS_SIZE = 1;
    public const int MAX_TICKING_DISTANCE = 128;

    /// <summary>
    /// Random ticks per chunk section per tick. Normally 3 but let's test with 50
    /// </summary>
    public const int numTicks = 1;

    public World(string name, int seed) {
        this.name = name;
        worldIO = new WorldIO(this);
        generator = new PerlinWorldGenerator(this);
        player = new Player(this, 6, 20, 6);
        Game.player = player;

        random = new XRandom(seed);
        worldTick = 0;

        generator.setup(seed);
        this.seed = seed;

        chunks = new Dictionary<ChunkCoord, Chunk>();
        chunkList = new List<Chunk>(2048);
        particleManager = new ParticleManager(this);
    }

    public void init(bool loadingSave = false) {
        // load a minimal amount of chunks so the world can get started
        if (!loadingSave) {
            loadSpawnChunks();


            // after loading spawn chunks, load everything else immediately
            isLoading = true;

            // teleport player to top block
            while (getBlock(player.position.toBlockPos()) != 0) {
                player.position.Y += 1;
            }
        }

        // After everything is done, SAVE THE WORLD
        // if we don't save the world, some of the chunks might get saved but no level.xnbt
        // so the world is corrupted and we have horrible chunkglitches
        worldIO.save(this, name, false);


        // setup world saving every 5 seconds
        saveWorld = Game.setInterval(5 * 1000, saveWorldMethod);
        
        foreach (var l in listeners) {
            l.onWorldLoad();
        }
    }

    private void saveWorldMethod() {
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
            if (chunk.status >= ChunkStatus.MESHED && chunk.lastSaved + 60 * 1000 < (ulong)Game.permanentStopwatch.ElapsedMilliseconds) {
                worldIO.saveChunk(this, chunk);
                x++;
            }
        }
        if (x > 0) {
            Console.Out.WriteLine($"Saved {x} chunks");
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
        // dirty the block and its neighbours
        dirtyChunk(new SubChunkCoord(block.X >> 4, block.Y >> 4, block.Z >> 4));
        foreach (var dir in Direction.directions) {
            var neighbour = block + dir;
            dirtyChunk(new SubChunkCoord(neighbour.X >> 4, neighbour.Y >> 4, neighbour.Z >> 4));
        }
    }

    public void dirtyChunk(SubChunkCoord coord) {
        foreach (var l in listeners) {
            l.onDirtyChunk(coord);
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

    public void sortChunks() {
        // sort queue based on position
        // don't reorder across statuses though

        // note: removal is faster from the end so we sort by the reverse - closest entries are at the end of the list
        chunkLoadQueue.Sort(new ChunkTicketComparerReverse(player.position.toBlockPos()));
    }

    public void loadAroundPlayer() {
        // create terrain
        //genTerrainNoise();
        // separate loop so all data is there
        player.loadChunksAroundThePlayer(Settings.instance.renderDistance);
    }

    public int getBrightness(byte skylight, byte skyDarken) {
        // apply sky darkening to skylight only
        return  Math.Max(0, skylight - skyDarken);
    }

    public float getDayPercentage(int ticks) {
        return (ticks % TICKS_PER_DAY) / (float)TICKS_PER_DAY;
    }

    public Color4b getHorizonColour(int ticks) {
        float dayPercent = getDayPercentage(ticks);
        
        // 0.0-0.1: sunrise, 0.1-0.5: day, 0.5-0.6: sunset, 0.6-1.0: night
        if (dayPercent < 0.1f) {
            // sunrise
            float t = dayPercent / 0.1f;
            return Color4b.Lerp(new Color4b(15, 15, 40), new Color4b(135, 206, 235), t); // dark blue to day blue
        }
        else if (dayPercent < 0.5f) {
            // day
            return new Color4b(135, 206, 235); // sky blue
        }
        else if (dayPercent < 0.6f) {
            // sunset
            float t = (dayPercent - 0.5f) / 0.1f;
            return Color4b.Lerp(new Color4b(135, 206, 235), new Color4b(15, 15, 40), t); // sky blue to night
        }
        else {
            // night
            return new Color4b(15, 15, 40); // dark blue
        }
    }

    public Color4b getFogColour(int ticks) {
        float dayPercent = getDayPercentage(ticks);
        
        // 0.0-0.1: sunrise, 0.1-0.5: day, 0.5-0.6: sunset, 0.6-1.0: night
        if (dayPercent < 0.1f) {
            // sunrise fog
            float t = dayPercent / 0.1f;
            return Color4b.Lerp(new Color4b(10, 10, 25), new Color4b(255, 255, 255), t);
        }
        else if (dayPercent < 0.5f) {
            // day fog - light
            return new Color4b(255, 255, 255);
        }
        else if (dayPercent < 0.6f) {
            // sunset fog
            float t = (dayPercent - 0.5f) / 0.1f;
            return Color4b.Lerp(new Color4b(255, 255, 255), new Color4b(10, 10, 25), t);
        }
        else {
            // night fog
            return new Color4b(10, 10, 25);
        }
    }

    public Color4b getSkyColour(int ticks) {
        float dayPercent = getDayPercentage(ticks);
        
        // 0.0-0.1: sunrise, 0.1-0.5: day, 0.5-0.6: sunset, 0.6-1.0: night
        if (dayPercent < 0.1f) {
            // sunrise sky
            float t = dayPercent / 0.1f;
            return Color4b.Lerp(new Color4b(5, 5, 15), new Color4b(100, 180, 255), t);
        }
        else if (dayPercent < 0.5f) {
            // day sky - bright blue
            return new Color4b(100, 180, 255);
        }
        else if (dayPercent < 0.6f) {
            // sunset sky
            float t = (dayPercent - 0.5f) / 0.1f;
            return Color4b.Lerp(new Color4b(100, 180, 255), new Color4b(5, 5, 15), t);
        }
        else {
            // night sky
            return new Color4b(5, 5, 15);
        }
    }

    /** effective skylight */
    public byte getSkyDarken(int ticks) {
        float dayPercent = getDayPercentage(ticks);
        
        // remapped: 0.0-0.1: sunrise, 0.1-0.5: day, 0.5-0.6: sunset, 0.6-1.0: night
        if (dayPercent < 0.1f) {
            // sunrise - gradually getting brighter
            float t = dayPercent / 0.1f;
            return (byte)(11 * (1 - t)); // 11 to 0
        }
        else if (dayPercent < 0.5f) {
            // day - full brightness
            return 0;
        }
        else if (dayPercent < 0.6f) {
            // sunset - gradually getting darker
            float t = (dayPercent - 0.5f) / 0.1f;
            return (byte)(11 * t); // 0 to 11
        }
        else {
            // night - maximum darkness (11 levels down from 15)
            return 11;
        }
    }

    /** effective skylight (float version for rendering)
     * 0 = daylight, 11 = night
     * 16 = black
     */
    public float getSkyDarkenFloat(int ticks) {
        float dayPercent = getDayPercentage(ticks);
        
        if (Settings.instance.smoothDayNight) {
            if (dayPercent < 0.1f) {
                // sunrise - gradually getting brighter
                float t = dayPercent / 0.1f;
                return 11f * (1f - t); // 11 to 0
            }
            else if (dayPercent < 0.5f) {
                // day - full brightness
                return 0f;
            }
            else if (dayPercent < 0.6f) {
                // sunset - gradually getting darker
                float t = (dayPercent - 0.5f) / 0.1f;
                return 11f * t; // 0 to 11
            }
            else {
                // night - maximum darkness (11 levels down from 15)
                return 11f;
            }
        }
        else {
            // classic mode
            return getSkyDarken(ticks);
        }
    }
    
    


    /// <summary>
    /// Chunkloading and friends.
    /// </summary>
    public void renderUpdate(double dt) {
        var start = Game.permanentStopwatch.Elapsed.TotalMilliseconds;
        var ctr = 0;
        updateChunkloading(start, loading: false, ref ctr);
        particleManager.update(dt);
        
    }

    /** This is separate so this can be called from the outside without updating the whole (still nonexistent) world. */
    public void updateChunkloading(double startTime, bool loading, ref int loadedChunks) {
        // if is loading, don't throttle
        // consume the chunk queue
        // ONLY IF THERE ARE CHUNKS
        // otherwise don't wait for nothing
        // yes I was an idiot
        var limit = loading ? MAX_CHUNKLOAD_FRAMETIME_FAST : MAX_CHUNKLOAD_FRAMETIME;
        while (Game.permanentStopwatch.Elapsed.TotalMilliseconds - startTime < limit) {
            if (chunkLoadQueue.Count > 0) {
                var ticket = chunkLoadQueue[chunkLoadQueue.Count - 1];
                chunkLoadQueue.RemoveAt(chunkLoadQueue.Count - 1);
                loadChunk(ticket.chunkCoord, ticket.level);
                loadedChunks++;
            }
            else {
                // chunk queue empty, don't loop more
                isLoading = false;
                break;
            }
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
            Game.renderer.meshChunk(section);
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
                blockUpdate(update.position);
                blockUpdateQueue.RemoveAt(i);
            }
        }

        // execute lighting updates
        processSkyLightRemovalQueue();
        processSkyLightQueue();
        processBlockLightRemovalQueue();
        processBlockLightQueue();

        // random block updates!
        foreach (var chunk in chunks) {
            // distance check
            if (Vector2I.DistanceSquared(chunk.Value.centrePos, new Vector2I((int)player.position.X, (int)player.position.Z)) < MAX_TICKING_DISTANCE * MAX_TICKING_DISTANCE) {
                foreach (var chunksection in chunk.Value.subChunks) {
                    if (!chunksection.blocks.hasRandomTickingBlocks()) {
                        continue;
                    }
                    for (int i = 0; i < numTicks; i++) {
                        // I pray this is random
                        var coord = random.Next(16 * 16 * 16);
                        var x = coord / (16 * 16);
                        var y = coord / 16 % 16;
                        var z = coord % 16;
                        chunksection.tick(this, random, x, y, z);
                    }
                }
            }
        }
    }

    public void processSkyLightQueue() {
        processLightQueue(skyLightQueue, true);
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

    public void processLightQueue(List<LightNode> queue, bool isSkylight) {
        while (queue.Count > 0) {
            var cnt = queue.Count;
            //Console.Out.WriteLine(cnt);
            var node = queue[cnt - 1];
            queue.RemoveAt(cnt - 1);

            var blockPos = new Vector3I(node.x, node.y, node.z);
            byte level = isSkylight ? getSkyLight(node.x, node.y, node.z) : getBlockLight(node.x, node.y, node.z);

            // if this is opaque (for skylight), don't bother
            if (isSkylight && Block.isFullBlock(getBlock(node.x, node.y, node.z))) {
                continue;
            }

            //Console.Out.WriteLine(blockPos);

            foreach (var dir in Direction.directionsLight) {
                var neighbour = blockPos + dir;
                // if neighbour is opaque, don't bother either
                if (Block.isFullBlock(getBlock(neighbour))) {
                    continue;
                }
                byte neighbourLevel = isSkylight ? getSkyLight(neighbour.X, neighbour.Y, neighbour.Z) : getBlockLight(neighbour.X, neighbour.Y, neighbour.Z);
                // if not in world, forget it
                if (!inWorldY(neighbour.X, neighbour.Y, neighbour.Z)) {
                    continue;
                }
                //var neighbourBlock = getBlock(neighbour);
                var isDown = isSkylight && level == 15 && neighbourLevel != 15 && dir == Direction.DOWN;
                if (neighbourLevel + 2 <= level || isDown) {
                    byte newLevel = (byte)(isDown ? level : level - 1);
                    if (isSkylight) {
                        setSkyLightRemesh(neighbour.X, neighbour.Y, neighbour.Z, newLevel);
                    }
                    else {
                        setBlockLightRemesh(neighbour.X, neighbour.Y, neighbour.Z, newLevel);
                    }
                    queue.Add(new LightNode(neighbour.X, neighbour.Y, neighbour.Z, node.chunk));
                }
            }
        }
    }

    public void processLightRemovalQueue(List<LightRemovalNode> queue, List<LightNode> addQueue, bool isSkylight) {
        while (queue.Count > 0) {
            var cnt = queue.Count;
            var node = queue[cnt - 1];
            queue.RemoveAt(cnt - 1);

            var blockPos = new Vector3I(node.x, node.y, node.z);
            var level = node.value;

            foreach (var dir in Direction.directionsLight) {
                var neighbour = blockPos + dir;
                // if not in world, forget it
                if (!inWorldY(neighbour.X, neighbour.Y, neighbour.Z)) {
                    continue;
                }
                byte neighbourLevel = isSkylight ? getSkyLight(neighbour.X, neighbour.Y, neighbour.Z) : getBlockLight(neighbour.X, neighbour.Y, neighbour.Z);
                var isDownLight = isSkylight && dir == Direction.DOWN && level == 15;
                if (isDownLight || neighbourLevel != 0 && neighbourLevel < level) {
                    if (isSkylight) {
                        setSkyLightRemesh(neighbour.X, neighbour.Y, neighbour.Z, 0);
                    }
                    else {
                        setBlockLightRemesh(neighbour.X, neighbour.Y, neighbour.Z, 0);
                    }

                    // Emplace new node to queue. (could use push as well)
                    queue.Add(new LightRemovalNode(neighbour.X, neighbour.Y, neighbour.Z, neighbourLevel, node.chunk));
                }
                else if (neighbourLevel >= level) {
                    // Add it to the update queue, so it can propagate to fill in the gaps
                    // left behind by this removal. We should update the lightBfsQueue after
                    // the lightRemovalBfsQueue is empty.
                    addQueue.Add(new LightNode(neighbour.X, neighbour.Y, neighbour.Z, node.chunk));

                }
            }
        }
    }

    public void addToChunkLoadQueue(ChunkCoord chunkCoord, ChunkStatus level) {
        chunkLoadQueue.Add(new ChunkLoadTicket(chunkCoord, level));
    }

    /// <summary>
    /// Chunks are generated up to renderDistance + 1.
    /// Chunks are populated (tree placement, etc.) until renderDistance and meshed until renderDistance.
    /// TODO unload chunks which are renderDistance + 2 away (this is bigger to prevent chunk flicker)
    /// </summary>
    public void loadChunksAroundChunk(ChunkCoord chunkCoord, int renderDistance) {
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
        foreach (var chunk in chunks.Values) {
            var playerChunk = player.getChunk();
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
                    loadChunk(coord, ChunkStatus.MESHED);
                }
            }
        }

        // unload chunks which are far away
        foreach (var chunk in chunks.Values) {
            var playerChunk = player.getChunk();
            var coord = chunk.coord;
            // if distance is greater than renderDistance + 3, unload
            if (playerChunk.distanceSq(coord) >= (renderDistance + 3) * (renderDistance + 3)) {
                unloadChunk(coord);
            }
        }
    }

    public void unloadChunk(ChunkCoord coord) {
        // save chunk first
        worldIO.saveChunk(this, chunks[coord]);
        
        foreach (var l in listeners) {
            l.onChunkUnload(coord);
        }
        
        chunkList.Remove(chunks[coord]);
        chunks[coord].destroyChunk();
        chunks.Remove(coord);
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
        // of course, we can save it here since WE call it and not the GC
        worldIO.save(this, name);
        
        foreach (var l in listeners) {
            l.onWorldUnload();
        }
        
        saveWorld.enabled = false;
        Game.world = null;
        Game.player = null;
        //Game.renderer = null;
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~World() {
        ReleaseUnmanagedResources();
    }

    /// <summary>
    /// Load this chunk either from disk (if exists) or generate it with the given level.
    /// </summary>
    public Chunk loadChunk(ChunkCoord chunkCoord, ChunkStatus status) {
        // if it already exists and has the proper level, just return it
        if (chunks.TryGetValue(chunkCoord, out var chunk) && chunk.status >= status) {
            return chunk;
        }

        // does the chunk exist?
        bool hasChunk = chunk != null;

        Chunk c;
        bool chunkAdded = false;

        // if it exists on disk, load it
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
                Console.Error.WriteLine($"Corrupted chunk file for {chunkCoord}: {e.Message}");
                hasChunk = false;
                chunkAdded = false;
            }
        }

        // right now we only generate, not load
        // if it's already generated, don't do it again
        if (status >= ChunkStatus.GENERATED && (!hasChunk || (hasChunk && chunks[chunkCoord].status < ChunkStatus.GENERATED))) {
            if (!chunkAdded) {
                c = new Chunk(this, chunkCoord.x, chunkCoord.z);
                addChunk(chunkCoord, c);
            }
            generator.generate(chunkCoord);
        }
        if (status >= ChunkStatus.POPULATED && (!hasChunk || (hasChunk && chunks[chunkCoord].status < ChunkStatus.POPULATED))) {
            // load adjacent first
            loadChunk(new ChunkCoord(chunkCoord.x - 1, chunkCoord.z), ChunkStatus.GENERATED);
            loadChunk(new ChunkCoord(chunkCoord.x + 1, chunkCoord.z), ChunkStatus.GENERATED);
            loadChunk(new ChunkCoord(chunkCoord.x, chunkCoord.z - 1), ChunkStatus.GENERATED);
            loadChunk(new ChunkCoord(chunkCoord.x, chunkCoord.z + 1), ChunkStatus.GENERATED);


            loadChunk(new ChunkCoord(chunkCoord.x - 1, chunkCoord.z - 1), ChunkStatus.GENERATED);
            loadChunk(new ChunkCoord(chunkCoord.x - 1, chunkCoord.z + 1), ChunkStatus.GENERATED);
            loadChunk(new ChunkCoord(chunkCoord.x + 1, chunkCoord.z - 1), ChunkStatus.GENERATED);
            loadChunk(new ChunkCoord(chunkCoord.x + 1, chunkCoord.z + 1), ChunkStatus.GENERATED);

            generator.populate(chunkCoord);
        }
        if (status >= ChunkStatus.LIGHTED && (!hasChunk || (hasChunk && chunks[chunkCoord].status < ChunkStatus.LIGHTED))) {
            chunks[chunkCoord].lightChunk();
        }
        if (status >= ChunkStatus.MESHED && (!hasChunk || (hasChunk && chunks[chunkCoord].status < ChunkStatus.MESHED))) {
            // load adjacent first
            loadChunk(new ChunkCoord(chunkCoord.x - 1, chunkCoord.z), ChunkStatus.LIGHTED);
            loadChunk(new ChunkCoord(chunkCoord.x + 1, chunkCoord.z), ChunkStatus.LIGHTED);
            loadChunk(new ChunkCoord(chunkCoord.x, chunkCoord.z - 1), ChunkStatus.LIGHTED);
            loadChunk(new ChunkCoord(chunkCoord.x, chunkCoord.z + 1), ChunkStatus.LIGHTED);

            loadChunk(new ChunkCoord(chunkCoord.x - 1, chunkCoord.z - 1), ChunkStatus.LIGHTED);
            loadChunk(new ChunkCoord(chunkCoord.x - 1, chunkCoord.z + 1), ChunkStatus.LIGHTED);
            loadChunk(new ChunkCoord(chunkCoord.x + 1, chunkCoord.z - 1), ChunkStatus.LIGHTED);
            loadChunk(new ChunkCoord(chunkCoord.x + 1, chunkCoord.z + 1), ChunkStatus.LIGHTED);
            chunks[chunkCoord].meshChunk();
        }
        return chunks[chunkCoord];
    }
    
    public void runLater(Vector3I pos, Action action, int tick) {
        var tickAction = new TickAction(pos, action, worldTick + tick);
        if (!actionQueue.Contains(tickAction)) {
            actionQueue.Add(tickAction);
        }
    }

    public void blockUpdateWithNeighbours(Vector3I pos) {
        Block.get(getBlock(pos)).update(this, pos);
        foreach (var dir in Direction.directions) {
            var neighbourBlock = pos + dir;
            Block.get(getBlock(neighbourBlock)).update(this, neighbourBlock);
        }
    }

    public void blockUpdate(Vector3I pos) {
        Block.get(getBlock(pos)).update(this, pos);
    }

    public void blockUpdate(Vector3I pos, int tick) {
        var update = new BlockUpdate(pos, worldTick + tick);
        if (!blockUpdateQueue.Contains(update)) {
            blockUpdateQueue.Add(update);
        }
    }

    public static List<Vector3I> getBlocksInBox(Vector3I min, Vector3I max) {
        var result = new List<Vector3I>();
        getBlocksInBox(result, min, max);
        return result;
    }
    
    public static void getBlocksInBox(List<Vector3I> result, Vector3I min, Vector3I max) {
        result.Clear();
        for (int x = min.X; x <= max.X; x++) {
            for (int y = min.Y; y <= max.Y; y++) {
                for (int z = min.Z; z <= max.Z; z++) {
                    result.Add(new Vector3I(x, y, z));
                }
            }
        }
    }
}