using System.Runtime.CompilerServices;
using BlockGame.GL.vertexformats;
using BlockGame.ui;
using BlockGame.util;
using Molten;

namespace BlockGame;

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

    public WorldIO worldIO;

    public int seed;


    public int worldTick;
    
    public const int TICKS_PER_DAY = 72000;

    public XRandom random;
    private TimerAction saveWorld;

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

        entities = new List<Entity>();
        particles = new Particles(this);
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
                worldIO.saveChunkAsync(this, chunk);
                x++;
            }
        }
        if (x > 0) {
            Console.Out.WriteLine($"Queued {x} chunks for async save");
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
    
    public void loadAroundPlayer(ChunkStatus status) {
        player.loadChunksAroundThePlayer(Settings.instance.renderDistance, status);
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
        particles.update(dt);
        
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
        SuperluminalPerf.BeginEvent("light");
        processSkyLightRemovalQueue();
        processSkyLightQueue();
        processBlockLightRemovalQueue();
        processBlockLightQueue();
        SuperluminalPerf.EndEvent();

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
                        var x = coord >> 8;
                        var y = coord >> 4 & 0xF;
                        var z = coord & 0xF;
                        chunksection.tick(this, random, x, y, z);
                    }
                }
            }
        }
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
    public Vector3I getChunkAndRelativePos(Chunk currentChunk, int x, int y, int z, Vector3I direction, out Chunk? chunk) {
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
                        neighborChunk.setSkyLight(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z, newLevel);
                    }
                    else {
                        neighborChunk.setSkyLightRemesh(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z,
                            newLevel);
                    }
                }
                else {
                    if (noUpdate) {
                        neighborChunk.setBlockLight(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z, newLevel);
                    }
                    else {
                        neighborChunk.setBlockLightRemesh(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z,
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

        foreach (var dir in Direction.directionsLight) {
            // Get neighbor chunk and relative position
            var neighborRelPos = getChunkAndRelativePos(node.chunk, node.x, node.y, node.z, dir, out var neighborChunk);
            if (neighborChunk == null) {
                continue;
            }

            byte neighbourLevel = isSkylight ? neighborChunk.getSkyLight(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z) : neighborChunk.getBlockLight(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z);
            var isDownLight = isSkylight && dir == Direction.DOWN && level == 15;
            if (isDownLight || neighbourLevel != 0 && neighbourLevel < level) {
                if (isSkylight) {
                    neighborChunk.setSkyLightRemesh(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z, 0);
                }
                else {
                    neighborChunk.setBlockLightRemesh(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z, 0);
                }

                // Emplace new node to queue. (could use push as well)
                queue.Add(new LightRemovalNode(neighborRelPos.X, neighborRelPos.Y, neighborRelPos.Z, neighbourLevel, neighborChunk));
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
        chunkLoadQueue.Add(new ChunkLoadTicket(chunkCoord, level));
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
        // save chunk asynchronously to prevent lagspikes
        worldIO.saveChunkAsync(this, chunks[coord]);
        
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
        
        // dispose worldIO to ensure all pending saves complete
        worldIO.Dispose();
        
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
}