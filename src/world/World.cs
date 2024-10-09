using BlockGame.ui;
using BlockGame.util;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame;

public class World : IDisposable {
    public const int WORLDSIZE = 12;
    public const int REGIONSIZE = 16;
    public const int WORLDHEIGHT = Chunk.CHUNKHEIGHT * Chunk.CHUNKSIZE;

    public string name;

    public readonly Dictionary<ChunkCoord, Chunk> chunks;

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

    /// <summary>
    /// What needs to be meshed at the end of the frame
    /// </summary>
    public Queue<ChunkSectionCoord> meshingQueue = new();

    public WorldRenderer renderer;
    public WorldGenerator generator;

    public bool isLoading;

    public bool paused;
    public bool inMenu;

    public Player player;
    public WorldIO worldIO;

    public int seed;


    public int worldTick;

    public Random random;
    private readonly TimerAction saveWorld;

    private const long MAX_CHUNKLOAD_FRAMETIME = 20;
    private const long MAX_LIGHT_FRAMETIME = 10;
    private const int SPAWNCHUNKS_SIZE = 1;
    private const int MAX_TICKING_DISTANCE = 128;

    /// <summary>
    /// Random ticks per chunk section per tick. Normally 3 but let's test with 50
    /// </summary>
    public const int numTicks = 3;

    public World(string name, int seed, bool loadingSave = false) {
        this.name = name;
        worldIO = new WorldIO(this);
        generator = new OverworldWorldGenerator(this);
        player = new Player(this, 6, 20, 6);
        renderer = new WorldRenderer(this);

        random = new Random(seed);
        worldTick = 0;

        generator.setup(seed);
        this.seed = seed;

        chunks = new Dictionary<ChunkCoord, Chunk>();
        chunkList = new List<Chunk>(2048);
        particleManager = new ParticleManager(this);
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

        renderer.initBlockOutline();

        // After everything is done, SAVE THE WORLD
        // if we don't save the world, some of the chunks might get saved but no level.xnbt
        // so the world is corrupted and we have horrible chunkglitches
        worldIO.save(this, name, false);


        // setup world saving every 5 mins
        saveWorld = Game.setInterval(5 * 60 * 1000, saveWorldMethod);
    }

    private void saveWorldMethod() {
        worldIO.saveWorldData();
    }

    public void startMeshing() {
        foreach (var chunk in chunks.Values) {
            if (chunk.status < ChunkStatus.MESHED) {
                addToChunkLoadQueue(chunk.coord, ChunkStatus.MESHED);
            }
        }
    }

    public void addChunk(ChunkCoord coord, Chunk chunk) {
        chunks[coord] = chunk;
        chunkList.Add(chunk);
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


    /// <summary>
    /// Chunkloading and friends.
    /// </summary>
    public void renderUpdate(double dt) {
        var start = Game.permanentStopwatch.ElapsedMilliseconds;
        var ctr = 0;
        // if is loading, don't throttle
        // consume the chunk queue
        // ONLY IF THERE ARE CHUNKS
        // otherwise don't wait for nothing
        // yes I was an idiot
        var limit = MAX_CHUNKLOAD_FRAMETIME;
        while (Game.permanentStopwatch.ElapsedMilliseconds - start < limit) {
            if (chunkLoadQueue.Count > 0) {
                var ticket = chunkLoadQueue[chunkLoadQueue.Count - 1];
                chunkLoadQueue.RemoveAt(chunkLoadQueue.Count - 1);
                loadChunk(ticket.chunkCoord, ticket.level);
                ctr++;
            }
            else {
                // chunk queue empty, don't loop more
                isLoading = false;
                break;
            }
        }
        // debug
        /*Console.Out.WriteLine("---BEGIN---");
        foreach (var chunk in chunkLoadQueue) {
            Console.Out.WriteLine(chunk.level);
        }
        Console.Out.WriteLine("---END---");*/
        //Console.Out.WriteLine(Game.permanentStopwatch.ElapsedMilliseconds - start);
        //Console.Out.WriteLine($"{ctr} chunks loaded");

        // empty the meshing queue
        while (meshingQueue.TryDequeue(out var sectionCoord)) {
            var section = getChunkSection(sectionCoord);
            section.renderer.meshChunk();
        }
        particleManager.update(dt);
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
                        chunksection.tick(random, x, y, z);
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
            if (isSkylight && Blocks.isFullBlock(getBlock(node.x, node.y, node.z))) {
                continue;
            }

            //Console.Out.WriteLine(blockPos);

            foreach (var dir in Direction.directionsLight) {
                var neighbour = blockPos + dir;
                // if neighbour is opaque, don't bother either
                if (Blocks.isFullBlock(getBlock(neighbour))) {
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

    public void Dispose() {
        // of course, we can save it here since WE call it and not the GC
        worldIO.save(this, name);
        saveWorld.enabled = false;
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
        bool hasChunk = chunk != default;

        Chunk c;
        bool chunkAdded = false;

        // if it exists on disk, load it
        if (!hasChunk && WorldIO.chunkFileExists(name, chunkCoord)) {
            var ch = WorldIO.loadChunkFromFile(this, chunkCoord);
            addChunk(chunkCoord, ch);
            // we got the chunk so set to true
            hasChunk = true;
            chunkAdded = true;
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

    public void mesh(ChunkSectionCoord coord) {
        if (!meshingQueue.Contains(coord)) {
            meshingQueue.Enqueue(coord);
        }
    }

    public static bool isMisalignedBlock(Vector3I position) {
        return position.X < 0 || position.X > 15 || position.Z < 0 || position.Z > 15;
    }

    // get a block of a chunk position where the position is *outside* the chunk
    public ushort getMisalignedBlock(Vector3I position, Chunk chunk, out Chunk actualChunk) {
        var pos = toWorldPos(chunk.worldX, chunk.worldZ, position.X, position.Y, position.Z);
        var blockPos = getPosInChunk(pos);
        var success = getChunkMaybe(pos.X, pos.Z, out actualChunk);
        return success ? actualChunk!.getBlock(blockPos.X, pos.Y, blockPos.Z) : (ushort)0;
    }

    // normalise a block position into the proper chunk and return the chunk it actually belongs to
    public Vector3I alignBlock(Vector3I position, Chunk chunk, out Chunk actualChunk) {
        var pos = toWorldPos(chunk.worldX, chunk.worldZ, position.X, position.Y, position.Z);
        var blockPos = getPosInChunk(pos);
        var success = getChunkMaybe(pos.X, pos.Z, out actualChunk);
        return blockPos;
    }

    public bool isBlock(int x, int y, int z) {
        if (!inWorld(x, y, z)) {
            return false;
        }

        var blockPos = getPosInChunk(x, y, z);
        var chunk = getChunk(x, z);
        return chunk.getBlock(blockPos.X, y, blockPos.Z) != 0;
    }

    public bool isSelectableBlock(int x, int y, int z) {
        if (!inWorld(x, y, z)) {
            return false;
        }

        var blockPos = getPosInChunk(x, y, z);
        var chunk = getChunk(x, z);
        return Blocks.get(chunk.getBlock(blockPos.X, y, blockPos.Z)).selection;
    }

    public ushort getBlock(int x, int y, int z) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return 0;
        }
        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        return success ? chunk!.getBlock(blockPos.X, y, blockPos.Z) : (ushort)0;
    }

    public byte getLight(int x, int y, int z) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return 0;
        }
        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        return success ? chunk!.getLight(blockPos.X, blockPos.Y, blockPos.Z) : (byte)0;
    }

    public byte getSkyLight(int x, int y, int z) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return 0;
        }
        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        return success ? chunk!.getSkyLight(blockPos.X, blockPos.Y, blockPos.Z) : (byte)0;
    }

    public byte getBlockLight(int x, int y, int z) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return 0;
        }
        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        return success ? chunk!.getBlockLight(blockPos.X, blockPos.Y, blockPos.Z) : (byte)0;
    }

    public void setSkyLight(int x, int y, int z, byte level) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        if (success) {
            chunk!.setSkyLight(blockPos.X, blockPos.Y, blockPos.Z, level);
        }
    }

    public void setSkyLightRemesh(int x, int y, int z, byte level) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        if (success) {
            chunk!.setSkyLightRemesh(blockPos.X, blockPos.Y, blockPos.Z, level);
        }
    }

    public void setSkyLightAndPropagate(int x, int y, int z, byte level) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        if (success) {
            chunk!.setSkyLight(blockPos.X, blockPos.Y, blockPos.Z, level);
            skyLightQueue.Add(new LightNode(x, y, z, chunk));
            //processSkyLightQueue();
        }
    }

    public void removeSkyLightAndPropagate(int x, int y, int z) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        if (success) {
            var value = getSkyLight(x, y, z);
            skyLightRemovalQueue.Add(new LightRemovalNode(x, y, z, value, chunk!));
            chunk!.setSkyLight(blockPos.X, blockPos.Y, blockPos.Z, 0);
        }
    }

    public void setBlockLight(int x, int y, int z, byte level) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        if (success) {
            chunk!.setBlockLight(blockPos.X, blockPos.Y, blockPos.Z, level);
        }
    }

    public void setBlockLightRemesh(int x, int y, int z, byte level) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        if (success) {
            chunk!.setBlockLightRemesh(blockPos.X, blockPos.Y, blockPos.Z, level);
        }
    }

    public void removeBlockLightAndPropagate(int x, int y, int z) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        if (success) {
            var value = getBlockLight(x, y, z);
            blockLightRemovalQueue.Add(new LightRemovalNode(x, y, z, value, chunk!));
            chunk!.setBlockLight(blockPos.X, blockPos.Y, blockPos.Z, 0);
        }
    }

    /// <summary>
    /// getBlock but returns -1 if OOB
    /// </summary>
    public int getBlockUnsafe(int x, int y, int z) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return -1;
        }
        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        return success ? chunk!.getBlock(blockPos.X, y, blockPos.Z) : -1;
    }

    public ushort getBlock(Vector3I pos) {
        return getBlock(pos.X, pos.Y, pos.Z);
    }

    public AABB? getAABB(int x, int y, int z, Block block) {
        var aabb = block.aabb;
        if (aabb == null) {
            return null;
        }
        return new AABB(new Vector3D(x + aabb.Value.minX, y + aabb.Value.minY, z + aabb.Value.minZ),
            new Vector3D(x + aabb.Value.maxX, y + aabb.Value.maxY, z + aabb.Value.maxZ));
    }

    public AABB? getAABB(int x, int y, int z, ushort id) {
        if (id == 0) {
            return null;
        }

        var block = Blocks.get(id);
        return getAABB(x, y, z, block);
    }

    public AABB? getSelectionAABB(int x, int y, int z, ushort id) {
        if (id == 0) {
            return null;
        }

        var block = Blocks.get(id);
        return getSelectionAABB(x, y, z, block);
    }

    public AABB? getSelectionAABB(int x, int y, int z, Block block) {

        var aabb = block.selectionAABB;
        if (aabb == null) {
            return null;
        }
        return new AABB(new Vector3D(x + aabb.Value.minX, y + aabb.Value.minY, z + aabb.Value.minZ),
            new Vector3D(x + aabb.Value.maxX, y + aabb.Value.maxY, z + aabb.Value.maxZ));
    }

    public void setBlock(int x, int y, int z, ushort block) {
        if (!inWorld(x, y, z)) {
            //Console.Out.WriteLine($"was? {x} {y} {z} {getChunkPos(x, z)} {chunks[getChunkPos(x, z)]}");
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var chunk = getChunk(x, z);
        chunk.setBlock(blockPos.X, blockPos.Y, blockPos.Z, block);
    }

    public void setBlockRemesh(int x, int y, int z, ushort block) {
        if (!inWorld(x, y, z)) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var chunk = getChunk(x, z);
        chunk.setBlockRemesh(blockPos.X, blockPos.Y, blockPos.Z, block);
    }

    public void runLater(Vector3I pos, Action action, int tick) {
        var tickAction = new TickAction(pos, action, worldTick + tick);
        if (!actionQueue.Contains(tickAction)) {
            actionQueue.Add(tickAction);
        }
    }

    public void blockUpdateWithNeighbours(Vector3I pos) {
        Blocks.get(getBlock(pos)).update(this, pos);
        foreach (var dir in Direction.directions) {
            var neighbourBlock = pos + dir;
            Blocks.get(getBlock(neighbourBlock)).update(this, neighbourBlock);
        }
    }

    public void blockUpdate(Vector3I pos) {
        Blocks.get(getBlock(pos)).update(this, pos);
    }

    public void blockUpdate(Vector3I pos, int tick) {
        var update = new BlockUpdate(pos, worldTick + tick);
        if (!blockUpdateQueue.Contains(update)) {
            blockUpdateQueue.Add(update);
        }
    }

    /// <summary>
    /// This checks whether it's at least generated.
    /// </summary>
    public bool inWorld(int x, int y, int z) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return false;
        }
        var chunkpos = getChunkPos(x, z);
        return chunks.ContainsKey(chunkpos);
    }

    public static bool inWorldY(int x, int y, int z) {
        return y is >= 0 and < WORLDHEIGHT;
    }

    public static ChunkSectionCoord getChunkSectionPos(Vector3I pos) {
        return new ChunkSectionCoord(
            pos.X >> 4,
            pos.Y >> 4,
            pos.Z >> 4);
    }

    public static ChunkSectionCoord getChunkSectionPos(int x, int y, int z) {
        return new ChunkSectionCoord(
            x >> 4,
            y >> 4,
            z >> 4);
    }

    public static ChunkCoord getChunkPos(Vector2I pos) {
        return new ChunkCoord(
            pos.X >> 4,
            pos.Y >> 4);
    }

    public static ChunkCoord getChunkPos(int x, int z) {
        return new ChunkCoord(
            x >> 4,
            z >> 4);
    }

    public static RegionCoord getRegionPos(ChunkCoord pos) {
        return new RegionCoord(
            pos.x >> 4,
            pos.z >> 4);
    }

    public static Vector3I getPosInChunk(int x, int y, int z) {
        return new Vector3I(
            x & 0xF,
            y,
            z & 0xF);
    }

    public static Vector3I getPosInChunk(Vector3I pos) {
        return new Vector3I(
            pos.X & 0xF,
            pos.Y,
            pos.Z & 0xF);
    }

    public static Vector3I getPosInChunkSection(int x, int y, int z) {
        return new Vector3I(
            x & 0xF,
            y & 0xF,
            z & 0xF);
    }

    public static Vector3I getPosInChunkSection(Vector3I pos) {
        return new Vector3I(
            pos.X & 0xF,
            pos.Y & 0xF,
            pos.Z & 0xF);
    }

    public bool isChunkSectionInWorld(ChunkSectionCoord pos) {
        return chunks.ContainsKey(new ChunkCoord(pos.x, pos.z)) && pos.y >= 0 && pos.y < Chunk.CHUNKHEIGHT;
    }

    public Chunk getChunk(int x, int z) {
        var pos = getChunkPos(x, z);
        return chunks[pos];
    }

    public bool getChunkMaybe(int x, int z, out Chunk? chunk) {
        var pos = getChunkPos(x, z);
        var c = chunks.TryGetValue(pos, out chunk);
        return c;
    }

    public bool getChunkMaybe(ChunkCoord coord, out Chunk? chunk) {
        var c = chunks.TryGetValue(coord, out chunk);
        return c;
    }

    public SubChunk getChunkSection(int x, int y, int z) {
        var pos = getChunkSectionPos(new Vector3I(x, y, z));
        return chunks[new ChunkCoord(pos.x, pos.z)].subChunks[pos.y];
    }

    public bool getChunkSectionMaybe(int x, int y, int z, out SubChunk? section) {
        var pos = getChunkSectionPos(x, y, z);
        var c = chunks.TryGetValue(new ChunkCoord(pos.x, pos.z), out var chunk);
        if (!c || y is < 0 or >= WORLDHEIGHT) {
            section = null;
            return false;
        }
        section = chunk!.subChunks[pos.y];
        return true;
    }

    public SubChunk getChunkSection(Vector3I coord) {
        var pos = getChunkSectionPos(coord);
        return chunks[new ChunkCoord(pos.x, pos.z)].subChunks[pos.y];
    }

    public SubChunk getChunkSection(ChunkSectionCoord sectionCoord) {
        return chunks[new ChunkCoord(sectionCoord.x, sectionCoord.z)].subChunks[sectionCoord.y];
    }

    public bool getChunkSectionMaybe(ChunkSectionCoord pos, out SubChunk? section) {
        var c = chunks.TryGetValue(new ChunkCoord(pos.x, pos.z), out var chunk);
        if (!c || pos.y is < 0 or >= Chunk.CHUNKHEIGHT) {
            section = null;
            return false;
        }
        section = chunk!.subChunks[pos.y];
        return true;
    }

    public SubChunk? getChunkSectionUnsafe(ChunkSectionCoord pos) {
        if (pos.y is < 0 or >= Chunk.CHUNKHEIGHT) {
            return null;
        }
        bool c = chunks.TryGetValue(new ChunkCoord(pos.x, pos.z), out var chunk);
        return !c ? null : chunk!.subChunks[pos.y];
    }

    public Chunk getChunk(Vector2I position) {
        var pos = getChunkPos(position);
        return chunks[pos];
    }

    public Chunk getChunk(ChunkCoord position) {
        return chunks[position];
    }

    public void mesh() {
        foreach (var chunk in chunks) {
            chunk.Value.meshChunk();
        }
    }

    /// <summary>
    /// For sections
    /// </summary>
    public static Vector3I toWorldPos(int chunkX, int chunkY, int chunkZ, int x, int y, int z) {
        return new Vector3I(chunkX * Chunk.CHUNKSIZE + x,
            chunkY * Chunk.CHUNKSIZE + y,
            chunkZ * Chunk.CHUNKSIZE + z);
    }

    public static Vector3I toWorldPos(ChunkSectionCoord coord, int x, int y, int z) {
        return new Vector3I(coord.x * Chunk.CHUNKSIZE + x,
            coord.y * Chunk.CHUNKSIZE + y,
            coord.z * Chunk.CHUNKSIZE + z);
    }

    public static Vector3I toWorldPos(ChunkSectionCoord coord, Vector3I c) {
        return new Vector3I(coord.x * Chunk.CHUNKSIZE + c.X,
            coord.y * Chunk.CHUNKSIZE + c.Y,
            coord.z * Chunk.CHUNKSIZE + c.Z);
    }

    /// <summary>
    /// For chunks
    /// </summary>
    public static Vector3I toWorldPos(int chunkX, int chunkZ, int x, int y, int z) {
        return new Vector3I(chunkX * Chunk.CHUNKSIZE + x,
            y,
            chunkZ * Chunk.CHUNKSIZE + z);
    }

    public static Vector3I toWorldPos(ChunkCoord coord, int x, int y, int z) {
        return new Vector3I(coord.x * Chunk.CHUNKSIZE + x,
            y,
            coord.z * Chunk.CHUNKSIZE + z);
    }

    public static Vector3I toWorldPos(ChunkCoord coord, Vector3I c) {
        return new Vector3I(coord.x * Chunk.CHUNKSIZE + c.X,
            c.Y,
            coord.z * Chunk.CHUNKSIZE + c.Z);
    }

    public List<Vector3I> getBlocksInBox(Vector3I min, Vector3I max) {
        var l = new List<Vector3I>();
        for (int x = min.X; x <= max.X; x++) {
            for (int y = min.Y; y <= max.Y; y++) {
                for (int z = min.Z; z <= max.Z; z++) {
                    l.Add(new Vector3I(x, y, z));
                }
            }
        }

        return l;
    }
}