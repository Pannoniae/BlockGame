using Silk.NET.Maths;

namespace BlockGame;

public class World {
    public const int WORLDSIZE = 12;
    public const int REGIONSIZE = 8;
    public const int WORLDHEIGHT = Chunk.CHUNKHEIGHT * Chunk.CHUNKSIZE;

    public Dictionary<ChunkCoord, Chunk> chunks;
    //public List<ChunkSection> sortedTransparentChunks = [];

    // chunk load queue
    public List<ChunkLoadTicket> chunkLoadQueue = new();
    public Queue<BlockUpdate> blockUpdateQueue = new();

    /// <summary>
    /// What needs to be meshed at the end of the frame
    /// </summary>
    public Queue<ChunkSectionCoord> meshingQueue = new();

    public WorldRenderer renderer;

    public Player player;

    public FastNoiseLite noise;
    public FastNoiseLite treenoise;

    public double worldTime;
    public int worldTick;

    public Random random;

    // max. 5 msec in each frame for chunkload
    private const long MAX_CHUNKLOAD_FRAMETIME = 5;
    private const int SPAWNCHUNKS_SIZE = 2;

    public const int RENDERDISTANCE = 32;

    /// <summary>
    /// Random ticks per chunk section per tick. Normally 3 but let's test with 50
    /// </summary>
    public const int numTicks = 3;

    public World(int seed) {
        renderer = new WorldRenderer(this);
        player = new Player(this, 6, 20, 6);

        random = new Random(seed);
        noise = new FastNoiseLite(seed);
        treenoise = new FastNoiseLite(random.Next(seed));
        noise.SetFrequency(0.003f);
        //noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        //noise.SetFractalLacunarity(2f);
        //noise.SetFractalGain(0.5f);
        treenoise.SetFrequency(1f);
        worldTime = 0;
        worldTick = 0;

        chunks = new Dictionary<ChunkCoord, Chunk>();
        // load a minimal amount of chunks so the world can get started
        loadSpawnChunks();

        renderer.meshBlockOutline();
    }

    private void loadSpawnChunks() {
        loadChunksAroundChunkImmediately(new ChunkCoord(0, 0), 2);
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

    private void genTerrainNoise() {
        // generate terrain for all loaded chunks
        foreach (var chunk in chunks.Values) {
            chunk.generator.generate();
            chunk.generator.populate();
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
        //var ctr = 0;
        // consume the chunk queue
        while (Game.permanentStopwatch.ElapsedMilliseconds - start < MAX_CHUNKLOAD_FRAMETIME) {
            if (chunkLoadQueue.Count > 0) {
                var ticket = chunkLoadQueue[0];
                chunkLoadQueue.RemoveAt(0);
                loadChunk(ticket.chunkCoord, ticket.level);
                //ctr++;
            }
        }
        //Console.Out.WriteLine(Game.permanentStopwatch.ElapsedMilliseconds - start);
        //Console.Out.WriteLine(ctr);

        // random block updates!
        foreach (var chunk in chunks) {
            foreach (var chunksection in chunk.Value.chunks) {
                for (int i = 0; i < numTicks; i++) {
                    var x = random.Next(16);
                    var y = random.Next(16);
                    var z = random.Next(16);
                    chunksection.tick(x, y, z);
                }
            }
        }

        // empty the meshing queue
        while (meshingQueue.TryDequeue(out var sectionCoord)) {
            var section = getChunkSection(sectionCoord);
            section.renderer.meshChunk();
        }
    }

    private void addToChunkLoadQueue(ChunkCoord chunkCoord, ChunkStatus level) {
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
                if (coord.distanceSq(chunkCoord) <= renderDistance * renderDistance) {
                    addToChunkLoadQueue(new ChunkCoord(x, z), ChunkStatus.GENERATED);
                }
            }
        }
        // populate around renderDistance
        for (int x = chunkCoord.x - renderDistance; x <= chunkCoord.x + renderDistance; x++) {
            for (int z = chunkCoord.z - renderDistance; z <= chunkCoord.z + renderDistance; z++) {
                var coord = new ChunkCoord(x, z);
                if (coord.distanceSq(chunkCoord) <= renderDistance * renderDistance) {
                    addToChunkLoadQueue(new ChunkCoord(x, z), ChunkStatus.POPULATED);
                }
            }
        }
        // finally, mesh around renderDistance
        for (int x = chunkCoord.x - renderDistance; x <= chunkCoord.x + renderDistance; x++) {
            for (int z = chunkCoord.z - renderDistance; z <= chunkCoord.z + renderDistance; z++) {
                var coord = new ChunkCoord(x, z);
                if (coord.distanceSq(chunkCoord) <= renderDistance * renderDistance) {
                    addToChunkLoadQueue(new ChunkCoord(x, z), ChunkStatus.MESHED);
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
                if (coord.distanceSq(chunkCoord) <= renderDistance * renderDistance) {
                    loadChunk(new ChunkCoord(x, z), ChunkStatus.GENERATED);
                }
            }
        }
        // populate around renderDistance
        for (int x = chunkCoord.x - renderDistance; x <= chunkCoord.x + renderDistance; x++) {
            for (int z = chunkCoord.z - renderDistance; z <= chunkCoord.z + renderDistance; z++) {
                var coord = new ChunkCoord(x, z);
                if (coord.distanceSq(chunkCoord) <= renderDistance * renderDistance) {
                    loadChunk(new ChunkCoord(x, z), ChunkStatus.POPULATED);
                }
            }
        }
        // finally, mesh around renderDistance
        for (int x = chunkCoord.x - renderDistance; x <= chunkCoord.x + renderDistance; x++) {
            for (int z = chunkCoord.z - renderDistance; z <= chunkCoord.z + renderDistance; z++) {
                var coord = new ChunkCoord(x, z);
                if (coord.distanceSq(chunkCoord) <= renderDistance * renderDistance) {
                    loadChunk(new ChunkCoord(x, z), ChunkStatus.MESHED);
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
            chunks[chunkCoord].generator.generate();
        }
        if (status >= ChunkStatus.POPULATED && !(hasChunk && chunk!.status > ChunkStatus.POPULATED)) {
            chunks[chunkCoord].generator.populate();
        }
        if (status >= ChunkStatus.MESHED && !(hasChunk && chunk!.status > ChunkStatus.MESHED)) {
            chunks[chunkCoord].meshChunk();
        }
        return chunks[chunkCoord];
    }

    public void mesh(ChunkSectionCoord coord) {
        meshingQueue.Enqueue(coord);
    }

    public Vector3D<int> getWorldSize() {
        var c = Chunk.CHUNKSIZE;
        return new Vector3D<int>(c * WORLDSIZE, c * WORLDHEIGHT, c * WORLDSIZE);
    }

    public bool isBlock(int x, int y, int z) {
        if (!inWorld(x, y, z)) {
            return false;
        }

        var blockPos = getPosInChunk(x, y, z);
        var chunk = getChunk(x, z);
        return chunk.blocks[blockPos.X, y, blockPos.Z] != 0;
    }

    public ushort getBlock(int x, int y, int z) {
        if (!inWorld(x, y, z)) {
            return 0;
        }

        var blockPos = getPosInChunk(x, y, z);
        var chunk = getChunk(x, z);
        return chunk.blocks[blockPos.X, y, blockPos.Z];
    }

    /// <summary>
    /// getBlock but returns -1 if OOB
    /// </summary>
    public int getBlockUnsafe(int x, int y, int z) {
        if (!inWorld(x, y, z)) {
            return -1;
        }

        var blockPos = getPosInChunk(x, y, z);
        var chunk = getChunk(x, z);
        return chunk.blocks[blockPos.X, y, blockPos.Z];
    }

    public ushort getBlock(Vector3D<int> pos) {
        return getBlock(pos.X, pos.Y, pos.Z);
    }

    public AABB? getAABB(int x, int y, int z, ushort id) {
        if (id == 0) {
            return null;
        }

        var block = Blocks.get(id);
        var aabb = block.aabb;
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

    public void blockUpdate(Vector3D<int> pos) {
        Blocks.get(getBlock(pos)).update(this, pos);
        foreach (var dir in Direction.directions) {
            var neighbourBlock = pos + dir;
            Blocks.get(getBlock(neighbourBlock)).update(this, neighbourBlock);
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
        return chunks.ContainsKey(chunkpos) && chunks[chunkpos].status >= ChunkStatus.GENERATED;
    }

    public ChunkSectionCoord getChunkSectionPos(Vector3D<int> pos) {
        return new ChunkSectionCoord(
            (int)MathF.Floor(pos.X / (float)Chunk.CHUNKSIZE),
            (int)MathF.Floor(pos.Y / (float)Chunk.CHUNKSIZE),
            (int)MathF.Floor(pos.Z / (float)Chunk.CHUNKSIZE));
    }

    public ChunkCoord getChunkPos(Vector2D<int> pos) {
        return new ChunkCoord(
            (int)MathF.Floor(pos.X / (float)Chunk.CHUNKSIZE),
            (int)MathF.Floor(pos.Y / (float)Chunk.CHUNKSIZE));
    }

    public ChunkCoord getChunkPos(int x, int z) {
        return new ChunkCoord(
            (int)MathF.Floor(x / (float)Chunk.CHUNKSIZE),
            (int)MathF.Floor(z / (float)Chunk.CHUNKSIZE));
    }

    public RegionCoord getRegionPos(ChunkCoord pos) {
        return new RegionCoord(
            (int)MathF.Floor(pos.x / (float)World.REGIONSIZE),
            (int)MathF.Floor(pos.z / (float)World.REGIONSIZE));
    }

    public Vector3D<int> getPosInChunk(int x, int y, int z) {
        return new Vector3D<int>(
            Utils.mod(x, Chunk.CHUNKSIZE),
            y,
            Utils.mod(z, Chunk.CHUNKSIZE));
    }

    public Vector3D<int> getPosInChunk(Vector3D<int> pos) {
        return new Vector3D<int>(
            Utils.mod(pos.X, Chunk.CHUNKSIZE),
            pos.Y,
            Utils.mod(pos.Z, Chunk.CHUNKSIZE));
    }

    public bool isChunkSectionInWorld(ChunkSectionCoord pos) {
        return chunks.ContainsKey(new ChunkCoord(pos.x, pos.z)) && pos.y >= 0 && pos.y < Chunk.CHUNKHEIGHT;
    }

    public Chunk getChunk(int x, int z) {
        var pos = getChunkPos(x, z);
        return chunks[pos];
    }

    public ChunkSection getChunkSection(int x, int y, int z) {
        var pos = getChunkSectionPos(new Vector3D<int>(x, y, z));
        return chunks[new ChunkCoord(pos.x, pos.z)].chunks[pos.y];
    }

    public ChunkSection getChunkSection(Vector3D<int> coord) {
        var pos = getChunkSectionPos(coord);
        return chunks[new ChunkCoord(pos.x, pos.z)].chunks[pos.y];
    }

    public ChunkSection getChunkSection(ChunkSectionCoord sectionCoord) {
        return chunks[new ChunkCoord(sectionCoord.x, sectionCoord.z)].chunks[sectionCoord.y];
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
    public Vector3D<int> toWorldPos(int chunkX, int chunkY, int chunkZ, int x, int y, int z) {
        return new Vector3D<int>(chunkX * Chunk.CHUNKSIZE + x,
            chunkY * Chunk.CHUNKSIZE + y,
            chunkZ * Chunk.CHUNKSIZE + z);
    }

    /// <summary>
    /// For chunks
    /// </summary>
    public Vector3D<int> toWorldPos(int chunkX, int chunkZ, int x, int y, int z) {
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

        previous = toBlockPos(currentPos);
        for (int i = 0; i < 1 / Constants.RAYCASTSTEP * Constants.RAYCASTDIST; i++) {
            currentPos += cameraForward * Constants.RAYCASTSTEP;
            var blockPos = toBlockPos(currentPos);
            if (isBlock(blockPos.X, blockPos.Y, blockPos.Z) && Blocks.get(getBlock(blockPos)).selection) {
                //Console.Out.WriteLine("getblock:" + getBlock(blockPos.X, blockPos.Y, blockPos.Z));
                return blockPos;
            }

            previous = blockPos;
        }

        previous = null;
        return null;
    }

    public Vector3D<int> toBlockPos(Vector3D<double> currentPos) {
        return new Vector3D<int>((int)Math.Floor(currentPos.X), (int)Math.Floor(currentPos.Y),
            (int)Math.Floor(currentPos.Z));
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