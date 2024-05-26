using Silk.NET.Maths;

namespace BlockGame;

public class World {
    public const int WORLDSIZE = 12;
    public const int REGIONSIZE = 8;
    public const int WORLDHEIGHT = Chunk.CHUNKHEIGHT * Chunk.CHUNKSIZE;

    public Dictionary<ChunkCoord, Chunk> chunks;
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

    public Player player;


    public double worldTime;
    public int worldTick;

    public Random random;

    // max. 5 msec in each frame for chunkload
    private const long MAX_CHUNKLOAD_FRAMETIME = 10;
    private const long MAX_LIGHT_FRAMETIME = 10;
    private const int SPAWNCHUNKS_SIZE = 1;
    private const int MAX_TICKING_DISTANCE = 128;

    public const int RENDERDISTANCE = 16;

    /// <summary>
    /// Random ticks per chunk section per tick. Normally 3 but let's test with 50
    /// </summary>
    public const int numTicks = 3;

    public World(int seed) {
        renderer = new WorldRenderer(this);
        generator = new OverworldWorldGenerator(this);
        player = new Player(this, 6, 20, 6);

        random = new Random(seed);
        worldTime = 0;
        worldTick = 0;

        generator.setup(seed);

        chunks = new Dictionary<ChunkCoord, Chunk>();
        // load a minimal amount of chunks so the world can get started
        loadSpawnChunks();

        // teleport player to top block
        while (getBlock(player.position.toBlockPos()) != 0) {
            player.position.Y += 1;
        }

        renderer.initBlockOutline();
    }

    private void loadSpawnChunks() {
        loadChunksAroundChunkImmediately(new ChunkCoord(0, 0), SPAWNCHUNKS_SIZE);
        //sortChunks();
    }

    public void sortChunks() {
        // sort queue based on position
        // don't reorder across statuses though
        chunkLoadQueue.Sort((ticket1, ticket2) => {
            var comparison = new ChunkCoordComparer(player.position).Compare(ticket1.chunkCoord, ticket2.chunkCoord);
            int statusDiff;
            statusDiff = (int)ticket1.level - (int)ticket2.level;
            // if statusDiff > 0, chunk2 is bigger
            //Console.Out.WriteLine($"{comparison} {statusDiff * 1000}");
            return comparison + statusDiff * 1000;
        });
    }

    public void generate() {
        // create terrain
        //genTerrainNoise();
        // separate loop so all data is there
        player.loadChunksAroundThePlayer(RENDERDISTANCE);
    }

    private void genTerrainSine() {
        for (int x = 0; x < WORLDSIZE * Chunk.CHUNKSIZE; x++) {
            for (int z = 0; z < WORLDSIZE * Chunk.CHUNKSIZE; z++) {
                for (int y = 0; y < 3; y++) {
                    setBlock(x, y, z, 2, false);
                }
            }
        }

        var sinMin = 2;
        for (int x = 0; x < WORLDSIZE * Chunk.CHUNKSIZE; x++) {
            for (int z = 0; z < WORLDSIZE * Chunk.CHUNKSIZE; z++) {
                var sin = Math.Sin(x / 3f) * 2 + sinMin + 1 + Math.Cos(z / 3f) * 2 + sinMin + 1;
                for (int y = sinMin; y < sin; y++) {
                    setBlock(x, y, z, 5, false);
                }

                if (sin < 4) {
                    for (int y = 3; y < 4; y++) {
                        setBlock(x, y, z, Blocks.WATER.id, false);
                    }
                }
            }
        }
    }

    public void update(double dt) {
        worldTime += dt;
        worldTick++;
        /*if (Vector3D.DistanceSquared(player.position, player.lastSort) > 64) {
            sortedTransparentChunks.Sort(new ChunkComparer(player.camera));
            player.lastSort = player.position;
        }*/

        var start = Game.permanentStopwatch.ElapsedMilliseconds;
        var ctr = 0;
        // consume the chunk queue
        // ONLY IF THERE ARE CHUNKS
        // otherwise don't wait for nothing
        // yes I was an idiot

        // debug
        /*Console.Out.WriteLine("---BEGIN---");
        foreach (var chunk in chunkLoadQueue) {
            Console.Out.WriteLine(chunk.level);
        }
        Console.Out.WriteLine("---END---");*/

        while (Game.permanentStopwatch.ElapsedMilliseconds - start < MAX_CHUNKLOAD_FRAMETIME) {
            if (chunkLoadQueue.Count > 0) {
                var ticket = chunkLoadQueue[0];
                chunkLoadQueue.RemoveAt(0);
                loadChunk(ticket.chunkCoord, ticket.level);
                ctr++;
            }
            else {
                // chunk queue empty, don't loop more
                break;
            }
        }
        //Console.Out.WriteLine(Game.permanentStopwatch.ElapsedMilliseconds - start);
        //Console.Out.WriteLine($"{ctr} chunks loaded");
        // execute block updates
        for (int i = 0; i < blockUpdateQueue.Count; i++) {
            var update = blockUpdateQueue[i];
            if (update.tick <= worldTick) {
                blockUpdate(update.position);
                blockUpdateQueue.RemoveAt(i);
            }
        }

        // execute tick actions
        for (int i = 0; i < actionQueue.Count; i++) {
            var action = actionQueue[i];
            if (action.tick <= worldTick) {
                action.action();
                actionQueue.RemoveAt(i);
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
            if (Vector2D.DistanceSquared(chunk.Value.centrePos, new Vector2D<int>((int)player.position.X, (int)player.position.Z)) < MAX_TICKING_DISTANCE * MAX_TICKING_DISTANCE) {
                foreach (var chunksection in chunk.Value.chunks) {
                    for (int i = 0; i < numTicks; i++) {
                        // I pray this is random
                        var coord = random.Next(16 * 16 * 16);
                        var x = coord / (16 * 16) % 16;
                        var y = coord / (16) % 16;
                        var z = coord % 16;
                        chunksection.tick(x, y, z);
                    }
                }
            }
        }

        // empty the meshing queue
        while (meshingQueue.TryDequeue(out var sectionCoord)) {
            var section = getChunkSection(sectionCoord);
            section.renderer.meshChunk();
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

            var blockPos = new Vector3D<int>(node.x, node.y, node.z);
            byte level = isSkylight ? getSkyLight(node.x, node.y, node.z) : getBlockLight(node.x, node.y, node.z);

            // if this is opaque, don't bother
            if (Blocks.isSolid(getBlock(node.x, node.y, node.z))) {
                continue;
            }

            //Console.Out.WriteLine(blockPos);

            foreach (var dir in Direction.directionsLight) {
                var neighbour = blockPos + dir;
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
                        setSkyLight(neighbour.X, neighbour.Y, neighbour.Z, newLevel);
                    }
                    else {
                        setBlockLight(neighbour.X, neighbour.Y, neighbour.Z, newLevel);
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

            var blockPos = new Vector3D<int>(node.x, node.y, node.z);
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
                        setSkyLight(neighbour.X, neighbour.Y, neighbour.Z, 0);
                    }
                    else {
                        setBlockLight(neighbour.X, neighbour.Y, neighbour.Z, 0);
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
        chunkLoadQueue.Insert(0, new ChunkLoadTicket(chunkCoord, level));
    }

    /// <summary>
    /// Chunks are generated up to renderDistance + 1.
    /// Chunks are populated (tree placement, etc.) until renderDistance and meshed until renderDistance.
    /// TODO unload chunks which are renderDistance + 2 away (this is bigger to prevent chunk flicker)
    /// </summary>
    public void loadChunksAroundChunk(ChunkCoord chunkCoord, int renderDistance) {
        // load +1 chunks around renderdistance
        for (int x = chunkCoord.x - renderDistance - 1; x <= chunkCoord.x + renderDistance + 1; x++) {
            for (int z = chunkCoord.z - renderDistance - 1; z <= chunkCoord.z + renderDistance + 1; z++) {
                // restrict it to a circle
                var coord = new ChunkCoord(x, z);
                if (coord.distanceSq(chunkCoord) <= (renderDistance + 1) * (renderDistance + 1)) {
                    addToChunkLoadQueue(coord, ChunkStatus.GENERATED);
                }
            }
        }
        // populate around renderDistance
        for (int x = chunkCoord.x - renderDistance; x <= chunkCoord.x + renderDistance; x++) {
            for (int z = chunkCoord.z - renderDistance; z <= chunkCoord.z + renderDistance; z++) {
                var coord = new ChunkCoord(x, z);
                if (coord.distanceSq(chunkCoord) <= renderDistance * renderDistance) {
                    addToChunkLoadQueue(coord, ChunkStatus.POPULATED);
                }
            }
        }
        // light around renderDistance
        for (int x = chunkCoord.x - renderDistance; x <= chunkCoord.x + renderDistance; x++) {
            for (int z = chunkCoord.z - renderDistance; z <= chunkCoord.z + renderDistance; z++) {
                var coord = new ChunkCoord(x, z);
                if (coord.distanceSq(chunkCoord) <= renderDistance * renderDistance) {
                    addToChunkLoadQueue(coord, ChunkStatus.LIGHTED);
                }
            }
        }
        // finally, mesh around renderDistance
        for (int x = chunkCoord.x - renderDistance + 1; x <= chunkCoord.x + renderDistance - 1; x++) {
            for (int z = chunkCoord.z - renderDistance + 1; z <= chunkCoord.z + renderDistance - 1; z++) {
                var coord = new ChunkCoord(x, z);
                if (coord.distanceSq(chunkCoord) <= (renderDistance - 1) * (renderDistance - 1)) {
                    addToChunkLoadQueue(coord, ChunkStatus.MESHED);
                }
            }
        }

        // unload chunks which are far away
        foreach (var chunk in chunks.Values) {
            var playerChunk = player.getChunk();
            var coord = chunk.coord;
            // if distance is greater than renderDistance + 2, unload
            if (playerChunk.distance(coord) >= renderDistance + 2) {
                unloadChunk(coord);
            }
        }
    }

    public void loadChunksAroundChunkImmediately(ChunkCoord chunkCoord, int renderDistance) {
        // load +1 chunks around renderdistance
        for (int x = chunkCoord.x - renderDistance - 1; x <= chunkCoord.x + renderDistance + 1; x++) {
            for (int z = chunkCoord.z - renderDistance - 1; z <= chunkCoord.z + renderDistance + 1; z++) {
                var coord = new ChunkCoord(x, z);
                if (coord.distanceSq(chunkCoord) <= (renderDistance + 1) * (renderDistance + 1)) {
                    loadChunk(coord, ChunkStatus.GENERATED);
                }
            }
        }
        // populate around renderDistance
        for (int x = chunkCoord.x - renderDistance; x <= chunkCoord.x + renderDistance; x++) {
            for (int z = chunkCoord.z - renderDistance; z <= chunkCoord.z + renderDistance; z++) {
                var coord = new ChunkCoord(x, z);
                if (coord.distanceSq(chunkCoord) <= renderDistance * renderDistance) {
                    loadChunk(coord, ChunkStatus.POPULATED);
                }
            }
        }
        // light around renderDistance
        for (int x = chunkCoord.x - renderDistance; x <= chunkCoord.x + renderDistance; x++) {
            for (int z = chunkCoord.z - renderDistance; z <= chunkCoord.z + renderDistance; z++) {
                var coord = new ChunkCoord(x, z);
                if (coord.distanceSq(chunkCoord) <= renderDistance * renderDistance) {
                    loadChunk(coord, ChunkStatus.LIGHTED);
                }
            }
        }
        // finally, mesh around renderDistance
        for (int x = chunkCoord.x - renderDistance + 1; x <= chunkCoord.x + renderDistance - 1; x++) {
            for (int z = chunkCoord.z - renderDistance + 1; z <= chunkCoord.z + renderDistance - 1; z++) {
                var coord = new ChunkCoord(x, z);
                if (coord.distanceSq(chunkCoord) <= (renderDistance - 1) * (renderDistance - 1)) {
                    loadChunk(coord, ChunkStatus.MESHED);
                }
            }
        }

        // unload chunks which are far away
        foreach (var chunk in chunks.Values) {
            var playerChunk = player.getChunk();
            var coord = chunk.coord;
            // if distance is greater than renderDistance + 2, unload
            if (playerChunk.distanceSq(coord) >= (renderDistance + 2) * (renderDistance + 2)) {
                unloadChunk(coord);
            }
        }
    }

    public void unloadChunk(ChunkCoord coord) {
        chunks.Remove(coord);
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

        // right now we only generate, not load
        // if it's already generated, don't do it again
        if (status >= ChunkStatus.GENERATED && !(hasChunk && chunk!.status > ChunkStatus.GENERATED)) {
            chunks[chunkCoord] = new Chunk(this, chunkCoord.x, chunkCoord.z);
            generator.generate(chunkCoord);
        }
        if (status >= ChunkStatus.POPULATED && !(hasChunk && chunk!.status > ChunkStatus.POPULATED)) {
            generator.populate(chunkCoord);
        }
        if (status >= ChunkStatus.LIGHTED && !(hasChunk && chunk!.status > ChunkStatus.LIGHTED)) {
            chunks[chunkCoord].lightChunk();
        }
        if (status >= ChunkStatus.MESHED && !(hasChunk && chunk!.status > ChunkStatus.MESHED)) {
            chunks[chunkCoord].meshChunk();
        }
        return chunks[chunkCoord];
    }

    public void mesh(ChunkSectionCoord coord) {
        if (!meshingQueue.Contains(coord)) {
            meshingQueue.Enqueue(coord);
        }
    }

    public bool isMisalignedBlock(Vector3D<int> position) {
        return position.X < 0 || position.X > 15 || position.Z < 0 || position.Z > 15;
    }

    // get a block of a chunk position where the position is *outside* the chunk
    public ushort getMisalignedBlock(Vector3D<int> position, Chunk chunk, out Chunk actualChunk) {
        var pos = toWorldPos(chunk.worldX, chunk.worldZ, position.X, position.Y, position.Z);
        var blockPos = getPosInChunk(pos);
        var success = getChunkMaybe(pos.X, pos.Z, out actualChunk);
        return success ? actualChunk!.getBlock(blockPos.X, pos.Y, blockPos.Z) : (ushort)0;
    }

    // normalise a block position into the proper chunk and return the chunk it actually belongs to
    public Vector3D<int> alignBlock(Vector3D<int> position, Chunk chunk, out Chunk actualChunk) {
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

    public void setSkyLight(int x, int y, int z, byte level, bool remesh = true) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var success = getChunkMaybe(x, z, out var chunk);
        if (success) {
            chunk!.setSkyLight(blockPos.X, blockPos.Y, blockPos.Z, level);
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

    public ushort getBlock(Vector3D<int> pos) {
        return getBlock(pos.X, pos.Y, pos.Z);
    }

    public AABB? getAABB(int x, int y, int z, Block block) {
        var aabb = block.aabb;
        if (aabb == null) {
            return null;
        }
        return new AABB(new Vector3D<double>(x + aabb.minX, y + aabb.minY, z + aabb.minZ),
            new Vector3D<double>(x + aabb.maxX, y + aabb.maxY, z + aabb.maxZ));
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
        return new AABB(new Vector3D<double>(x + aabb.minX, y + aabb.minY, z + aabb.minZ),
            new Vector3D<double>(x + aabb.maxX, y + aabb.maxY, z + aabb.maxZ));
    }

    public void setBlock(int x, int y, int z, ushort block, bool remesh = true) {
        if (!inWorld(x, y, z)) {
            return;
        }

        var blockPos = getPosInChunk(x, y, z);
        var chunk = getChunk(x, z);
        chunk.setBlock(blockPos.X, blockPos.Y, blockPos.Z, block, remesh);
    }

    public void runLater(Action action, int tick) {
        actionQueue.Add(new TickAction(action, worldTick + tick));
    }

    public void blockUpdate(Vector3D<int> pos) {
        Blocks.get(getBlock(pos)).update(this, pos);
        foreach (var dir in Direction.directions) {
            var neighbourBlock = pos + dir;
            Blocks.get(getBlock(neighbourBlock)).update(this, neighbourBlock);
        }
    }

    public void blockUpdate(Vector3D<int> pos, int tick) {
        blockUpdateQueue.Add(new BlockUpdate(pos, worldTick + tick));
    }

    /// <summary>
    /// This checks whether it's at least generated.
    /// </summary>
    public bool inWorld(int x, int y, int z) {
        if (y is < 0 or >= WORLDHEIGHT) {
            return false;
        }
        var chunkpos = getChunkPos(x, z);
        return chunks.ContainsKey(chunkpos) && chunks[chunkpos].status >= ChunkStatus.GENERATED;
    }

    public bool inWorldY(int x, int y, int z) {
        return y is >= 0 and < WORLDHEIGHT;
    }

    public static ChunkSectionCoord getChunkSectionPos(Vector3D<int> pos) {
        return new ChunkSectionCoord(
            (int)MathF.Floor((float)pos.X / Chunk.CHUNKSIZE),
            (int)MathF.Floor((float)pos.Y / Chunk.CHUNKSIZE),
            (int)MathF.Floor((float)pos.Z / Chunk.CHUNKSIZE));
    }

    public static ChunkSectionCoord getChunkSectionPos(int x, int y, int z) {
        return new ChunkSectionCoord(
            (int)MathF.Floor((float)x / Chunk.CHUNKSIZE),
            (int)MathF.Floor((float)y / Chunk.CHUNKSIZE),
            (int)MathF.Floor((float)z / Chunk.CHUNKSIZE));
    }

    public static ChunkCoord getChunkPos(Vector2D<int> pos) {
        return new ChunkCoord(
            (int)MathF.Floor(pos.X / (float)Chunk.CHUNKSIZE),
            (int)MathF.Floor(pos.Y / (float)Chunk.CHUNKSIZE));
    }

    public static ChunkCoord getChunkPos(int x, int z) {
        return new ChunkCoord(
            (int)MathF.Floor(x / (float)Chunk.CHUNKSIZE),
            (int)MathF.Floor(z / (float)Chunk.CHUNKSIZE));
    }

    public static RegionCoord getRegionPos(ChunkCoord pos) {
        return new RegionCoord(
            (int)MathF.Floor(pos.x / (float)World.REGIONSIZE),
            (int)MathF.Floor(pos.z / (float)World.REGIONSIZE));
    }

    public static Vector3D<int> getPosInChunk(int x, int y, int z) {
        return new Vector3D<int>(
            Utils.mod(x, Chunk.CHUNKSIZE),
            y,
            Utils.mod(z, Chunk.CHUNKSIZE));
    }

    public static Vector3D<int> getPosInChunk(Vector3D<int> pos) {
        return new Vector3D<int>(
            Utils.mod(pos.X, Chunk.CHUNKSIZE),
            pos.Y,
            Utils.mod(pos.Z, Chunk.CHUNKSIZE));
    }

    public static Vector3D<int> getPosInChunkSection(int x, int y, int z) {
        return new Vector3D<int>(
            Utils.mod(x, Chunk.CHUNKSIZE),
            Utils.mod(y, Chunk.CHUNKSIZE),
            Utils.mod(z, Chunk.CHUNKSIZE));
    }

    public static Vector3D<int> getPosInChunkSection(Vector3D<int> pos) {
        return new Vector3D<int>(
            Utils.mod(pos.X, Chunk.CHUNKSIZE),
            Utils.mod(pos.Y, Chunk.CHUNKSIZE),
            Utils.mod(pos.Z, Chunk.CHUNKSIZE));
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

    public ChunkSection getChunkSection(int x, int y, int z) {
        var pos = getChunkSectionPos(new Vector3D<int>(x, y, z));
        return chunks[new ChunkCoord(pos.x, pos.z)].chunks[pos.y];
    }

    public bool getChunkSectionMaybe(int x, int y, int z, out ChunkSection? section) {
        var pos = getChunkSectionPos(x, y, z);
        var c = chunks.TryGetValue(new ChunkCoord(pos.x, pos.z), out var chunk);
        if (!c || y is < 0 or >= WORLDHEIGHT) {
            section = null;
            return false;
        }
        section = chunk!.chunks[pos.y];
        return true;
    }

    public ChunkSection getChunkSection(Vector3D<int> coord) {
        var pos = getChunkSectionPos(coord);
        return chunks[new ChunkCoord(pos.x, pos.z)].chunks[pos.y];
    }

    public ChunkSection getChunkSection(ChunkSectionCoord sectionCoord) {
        return chunks[new ChunkCoord(sectionCoord.x, sectionCoord.z)].chunks[sectionCoord.y];
    }

    public bool getChunkSectionMaybe(ChunkSectionCoord pos, out ChunkSection? section) {
        var c = chunks.TryGetValue(new ChunkCoord(pos.x, pos.z), out var chunk);
        if (!c || pos.y is < 0 or >= Chunk.CHUNKHEIGHT) {
            section = null;
            return false;
        }
        section = chunk!.chunks[pos.y];
        return true;
    }

    public Chunk getChunk(Vector2D<int> position) {
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
    public static Vector3D<int> toWorldPos(int chunkX, int chunkY, int chunkZ, int x, int y, int z) {
        return new Vector3D<int>(chunkX * Chunk.CHUNKSIZE + x,
            chunkY * Chunk.CHUNKSIZE + y,
            chunkZ * Chunk.CHUNKSIZE + z);
    }

    /// <summary>
    /// For chunks
    /// </summary>
    public static Vector3D<int> toWorldPos(int chunkX, int chunkZ, int x, int y, int z) {
        return new Vector3D<int>(chunkX * Chunk.CHUNKSIZE + x,
            y,
            chunkZ * Chunk.CHUNKSIZE + z);
    }

    /// <summary>
    /// This piece of shit raycast breaks when the player goes outside the world. Solution? Don't go outside the world (will be prevented in the future with barriers)
    /// </summary>
    /// <param name="previous">The previous block (used for placing)</param>
    /// <returns></returns>
    public Vector3D<int>? naiveRaycastBlock(out Vector3D<int>? previous) {
        // raycast
        var cameraPos = player.camera.position;
        var forward = player.camera.forward;
        var cameraForward = new Vector3D<double>(forward.X, forward.Y, forward.Z);
        var currentPos = new Vector3D<double>(cameraPos.X, cameraPos.Y, cameraPos.Z);

        // don't round!!
        //var blockPos = toBlockPos(currentPos);

        previous = currentPos.toBlockPos();
        for (int i = 0; i < 1 / Constants.RAYCASTSTEP * Constants.RAYCASTDIST; i++) {
            currentPos += cameraForward * Constants.RAYCASTSTEP;
            var blockPos = currentPos.toBlockPos();
            var block = Blocks.get(getBlock(blockPos));
            if (isBlock(blockPos.X, blockPos.Y, blockPos.Z) && block.selection) {
                // we also need to check if it's inside the selection of the block
                if (AABB.isCollision(getSelectionAABB(blockPos.X, blockPos.Y, blockPos.Z, block) ?? AABB.empty, currentPos)) {
                    //Console.Out.WriteLine("getblock:" + getBlock(blockPos.X, blockPos.Y, blockPos.Z));
                    return blockPos;
                }
            }

            previous = blockPos;
        }

        previous = null;
        return null;
    }

    public List<Vector3D<int>> getBlocksInBox(Vector3D<int> min, Vector3D<int> max) {
        var l = new List<Vector3D<int>>();
        for (int x = min.X; x <= max.X; x++) {
            for (int y = min.Y; y <= max.Y; y++) {
                for (int z = min.Z; z <= max.Z; z++) {
                    l.Add(new Vector3D<int>(x, y, z));
                }
            }
        }

        return l;
    }
}