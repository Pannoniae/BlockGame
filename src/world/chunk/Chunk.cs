using BlockGame.main;
using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.entity;
using Molten;
using Molten.DoublePrecision;
using BoundingFrustum = BlockGame.util.meth.BoundingFrustum;

namespace BlockGame.world.chunk;

public class Chunk : IDisposable, IEquatable<Chunk> {
    public const int CHUNKHEIGHT = 8;
    public const int CHUNKSIZE = 16;
    public const int BIOMESIZE = 4;
    public const int CHUNKSIZESQ = 16 * 16;
    public const int MAXINDEX = 16 * 16 * 16;
    public const int CHUNKSIZEEX = 18;
    public const int CHUNKSIZEEXSQ = 18 * 18;
    public const int MAXINDEXEX = 18 * 18 * 18;

    public const int CHUNKSIZEMASK = CHUNKSIZE - 1;

    public ChunkStatus status;

    public readonly HeightMap heightMap;
    public readonly ChunkCoord coord;
    public readonly SubChunk[] subChunks;
    public readonly PaletteBlockData[] blocks = new PaletteBlockData[CHUNKHEIGHT];
    public readonly BiomeData biomeData = new();

    /** For now, this is fixed-size, we'll cook something better up later */
    public readonly List<Entity>[] entities = new List<Entity>[CHUNKHEIGHT];

    /** TODO implement crafting tables and stuff
     * (does that need to be a block entity? maybe? or more like a chest or something
     */
    public readonly Dictionary<Vector3I, BlockEntity> blockEntities = new();

    public readonly World world;

    public AABB box;

    public bool isRendered = false;
    public ulong lastSaved;
    public bool destroyed = false;
    private readonly List<LightNode> toPropagate = [];
    private readonly Queue<LightNode> propQueue = new();

    public ChunkCache cache = new();

    /** populate cache with all 8 neighbours */
    public void getCache() {
        cache.w = world.getChunkMaybe(new ChunkCoord(coord.x - 1, coord.z), out var w) ? w : null;
        cache.e = world.getChunkMaybe(new ChunkCoord(coord.x + 1, coord.z), out var e) ? e : null;
        cache.s = world.getChunkMaybe(new ChunkCoord(coord.x, coord.z - 1), out var s) ? s : null;
        cache.n = world.getChunkMaybe(new ChunkCoord(coord.x, coord.z + 1), out var n) ? n : null;
        cache.sw = world.getChunkMaybe(new ChunkCoord(coord.x - 1, coord.z - 1), out var sw) ? sw : null;
        cache.se = world.getChunkMaybe(new ChunkCoord(coord.x + 1, coord.z - 1), out var se) ? se : null;
        cache.nw = world.getChunkMaybe(new ChunkCoord(coord.x - 1, coord.z + 1), out var nw) ? nw : null;
        cache.ne = world.getChunkMaybe(new ChunkCoord(coord.x + 1, coord.z + 1), out var ne) ? ne : null;
    }

    /** invalidate cache entry in neighbours when this chunk is removed */
    public void removeFromCache() {
        if (cache.w != null) cache.w.cache.e = null;
        if (cache.e != null) cache.e.cache.w = null;
        if (cache.s != null) cache.s.cache.n = null;
        if (cache.n != null) cache.n.cache.s = null;
        if (cache.sw != null) cache.sw.cache.ne = null;
        if (cache.se != null) cache.se.cache.nw = null;
        if (cache.nw != null) cache.nw.cache.se = null;
        if (cache.ne != null) cache.ne.cache.sw = null;
        cache.clear();
    }

    public int worldX => coord.x << 4;
    public int worldZ => coord.z << 4;
    public Vector2I worldPos => new(worldX, worldZ);
    public Vector2I centrePos => new(worldX + 8, worldZ + 8);


    public Chunk(World world, int chunkX, int chunkZ) {
        status = ChunkStatus.EMPTY;
        this.world = world;

        subChunks = new SubChunk[CHUNKHEIGHT];
        coord = new ChunkCoord(chunkX, chunkZ);
        // TODO FIX THIS SHIT

        // storage!
        for (int i = 0; i < CHUNKHEIGHT; i++) {
            subChunks[i] = new SubChunk(world, this, chunkX, i, chunkZ);
            blocks[i] = new PaletteBlockData(this, i);
            entities[i] = [];
        }


        heightMap = new HeightMap(this);

        box = new AABB(new Vector3D(chunkX * CHUNKSIZE, 0, chunkZ * CHUNKSIZE),
            new Vector3D(chunkX * CHUNKSIZE + CHUNKSIZE, CHUNKHEIGHT * CHUNKSIZE, chunkZ * CHUNKSIZE + CHUNKSIZE));
    }

    public bool isVisible(BoundingFrustum frustum) {
        return !frustum.outsideCameraHorizontal(box);
    }

    /** Don't call these, they won't update the world! */
    internal void addEntity(Entity e) {
        // get the subchunk Y coordinate
        var epos = e.position.toBlockPos();
        var scy = World.getChunkSectionPos(epos).y;

        // cap
        scy = int.Clamp(scy, 0, CHUNKHEIGHT - 1);

        entities[scy].Add(e);

        // set the entity's chunk reference
        e.subChunkCoord = new SubChunkCoord(coord.x, scy, coord.z);
    }

    /** Don't call these, they won't update the world! */
    internal void removeEntity(Entity e) {
        // get the subchunk Y coordinate
        var scy = e.subChunkCoord.y;
        // cap
        scy = int.Clamp(scy, 0, CHUNKHEIGHT - 1);

        entities[scy].Remove(e);
    }

    public void lightChunk() {
        // set the top of the chunk to 15 if not solid
        // then propagate down
        for (int x = 0; x < CHUNKSIZE; x++) {
            for (int z = 0; z < CHUNKSIZE; z++) {
                var y = CHUNKSIZE * CHUNKHEIGHT - 1;

                // loop down until block is solid
                ushort bl = getBlock(x, y, z);
                while (bl == 0 && y > 0) {
                    // check if chunk is initialised first
                    //if (blocks[y >> 4].inited) {
                    setSkyLightDumb(x, y, z, 15);
                    //}

                    y--;
                    bl = getBlock(x, y, z);
                }

                // add the last item for propagation
                if (y + 1 >= CHUNKSIZE * CHUNKHEIGHT) {
                    goto blockLoop;
                }

                // convert to world coords
                int worldX = coord.x * CHUNKSIZE + x;
                int worldZ = coord.z * CHUNKSIZE + z;
                world.skyLightQueue.Enqueue(new LightNode(worldX, y + 1, worldZ, this));

                // y + 1 is air
                // y is water or solid block

                // SPECIAL CASE:
                // if the block on the bottom is water, DON'T START TO PROPAGATE
                // it will eat ALL the performance.
                // instead, what we will do is, we'll fast forward to the bottom of the water, lighting it up as we go (decreasing the light level obviously THEN add that to the propagation)

                var ll = getSkyLight(x, y + 1, z);
                if (bl ==  Block.WATER.id) {
                    // if the block is water, we need to propagate downwards
                    // but we need to do it manually, because otherwise it will add 7 million entries to the queue
                    while (y > 0 && bl ==  Block.WATER.id) {
                        ll -= Block.lightAbsorption[bl];
                        if (ll <= 0) break;
                        y--;
                        setSkyLightDumb(x, y, z, ll);
                        bl = getBlock(x, y, z);
                    }

                    // add it to the queue for propagation
                    world.skyLightQueue.Enqueue(new LightNode(worldX, y + 1, worldZ, this));
                }

                blockLoop: ;
                worldX = coord.x * CHUNKSIZE + x;
                worldZ = coord.z * CHUNKSIZE + z;
                // loop from y down to the bottom of the world
                for (int yy = y - 1; yy >= 0; yy--) {
                    bl = getBlock(x, yy, z);
                    // if blocklight, propagate
                    if (Block.lightLevel[bl] > 0) {
                        world.blockLightQueue.Enqueue(new LightNode(worldX, yy, worldZ, this));
                    }
                }
            }
        }

        // we collect, then we propagate!
        toPropagate.Clear();

        // second pass: check for horizontal propagation into unlit neighbours
        for (int x = 0; x < CHUNKSIZE; x++) {
            for (int z = 0; z < CHUNKSIZE; z++) {
                for (int y = CHUNKSIZE * CHUNKHEIGHT - 1; y >= 0; y--) {
                    // if this position has skylight and is air
                    if (getSkyLight(x, y, z) == 15) {
                        // check horizontal neighbours

                        bool propagateThis = false;
                        bool propagateBelow = false;
                        foreach (var d in Direction.directionsHorizontal) {

                            var nx = x + d.X;
                            var nz = z + d.Z;

                            // if neighbour is air and has no skylight, add for propagation
                            //if (!Block.isFullBlock(world.getRelativeBlock(this, x, y, z, new Vector3I(dx, 0, dz))) {
                            // if full skylight there, nothing to do....
                            //world.getSkyLight(worldnx, y, worldnz) != 15) {
                            //world.skyLightQueue.Add(new LightNode(x, y, z, this));

                            // what if we propagated manually? let's find out!
                            //}


                            // if at least one neighbour is solid, add this to the propagation and the block below it too! (for overhangs)
                            var relPos = world.getChunkAndRelativePos(this, x, y, z, new Vector3I(d.X, 0, d.Z),
                                out var neighbourChunk);

                            if (neighbourChunk.destroyed) {
                                Console.Out.WriteLine(neighbourChunk.coord);
                            }

                            var neighbourBlock = neighbourChunk?.getBlock(relPos.X, relPos.Y, relPos.Z) ?? 0;
                            if (Block.isFullBlock(neighbourBlock)) {
                                // if the neighbour is solid, we can propagate skylight from this position
                                propagateThis = true;
                                // also add the block below it for propagation
                                if (y > 0) {
                                    // only add if the block below is not solid
                                    var belowBlock = neighbourChunk?.getBlock(relPos.X, relPos.Y - 1, relPos.Z) ?? 0;
                                    if (!Block.isFullBlock(belowBlock)) {
                                        // add the block below for propagation
                                        propagateBelow = true;
                                    }
                                }
                            }
                        }

                        // we only propagate *once* per position, so if we found an empty neighbour, we propagate
                        // we don't propagate inside the loop lol
                        //if (propagateThis) {
                        //toPropagate.Add(new LightNode(x, y, z, this));
                        //}

                        // if we found a block below, we also propagate it
                        if (propagateBelow) {
                            toPropagate.Add(new LightNode(x, y - 1, z, this));
                        }
                    }
                }
            }
        }

        //print length of queue

        //Console.Out.WriteLine($"Found {toPropagate.Count} positions to propagate skylight from");
        // now we propagate all the skylight
        foreach (var lightNode in toPropagate) {
            // manually propagate skylight from this position
            manuallyPropagate(lightNode.x, lightNode.y, lightNode.z);
        }

        world.processSkyLightQueueNoUpdate();
        status = ChunkStatus.LIGHTED;
    }

    /**
     * The task is simple: propagate light under underhangs without adding 7 million entries to the lighting queue (where it will choke).
     * SO HOW ABOUT DOING IT MANUALLY?
     * we know that the light level at the given position is 15, and we only need to propagate skylight.
     * This makes it much easier!
     */
    private void manuallyPropagate(int x, int y, int z) {
        propQueue.Clear();
        int worldX = coord.x * CHUNKSIZE + x;
        int worldZ = coord.z * CHUNKSIZE + z;
        propQueue.Enqueue(new LightNode(worldX, y, worldZ, this));

        while (propQueue.Count > 0) {
            var (wx, wy, wz, chunk) = propQueue.Dequeue();

            // world -> chunkrel
            var relX = wx & 15;
            var relZ = wz & 15;
            var currentLight = chunk.getSkyLight(relX, wy, relZ);

            // propagate to all 6 neighbours
            foreach (var dir in Direction.directions) {
                var neighbourPos = world.getChunkAndRelativePos(chunk, relX, wy, relZ, dir, out var neighbourChunk);
                if (neighbourChunk == null) {
                    continue;
                }

                var nx = neighbourPos.X;
                var ny = neighbourPos.Y;
                var nz = neighbourPos.Z;

                // skip if neighbour is solid
                if (Block.fullBlock[neighbourChunk.getBlock(nx, ny, nz)]) continue;

                var neighbourLight = neighbourChunk.getSkyLight(nx, ny, nz);
                byte newLevel;

                // special case for skylight downward propagation
                if (dir == Direction.DOWN && currentLight == 15) {
                    newLevel = 15;
                }
                else {
                    newLevel = (byte)(currentLight - 1);
                }

                // only propagate if we can improve the light level
                if (newLevel > 0 && newLevel > neighbourLight &&
                    (neighbourLight + 2 <= currentLight || dir == Direction.DOWN)) {
                    neighbourChunk.setSkyLightDumb(nx, ny, nz, newLevel);
                    // convert back to world coords
                    int worldNX = (neighbourChunk.coord.x << 4) + nx;
                    int worldNZ = (neighbourChunk.coord.z << 4) + nz;
                    propQueue.Enqueue(new LightNode(worldNX, ny, worldNZ, neighbourChunk));
                }
            }
        }
    }

    /// <summary>
    /// Uses chunk coordinates
    /// </summary>
    public void setBlockDumb(int x, int y, int z, ushort block) {
        blocks[y >> 4][x, y & 0xF, z] = block;
        if (Block.fullBlock[block]) {
            blocks[y >> 4].setSkylight(x, y & 0xF, z, 0);
        }
        //else {
        //    subChunks[y >> 4].blocks.setSkylight(x, y & 0xF, z, 15);
        //}
    }

    public void setBlockFast(int x, int y, int z, ushort block) {
        blocks[y >> 4].fastSet(x, y & 0xF, z, block);
        if (Block.fullBlock[block]) {
            blocks[y >> 4].setSkylight(x, y & 0xF, z, 0);
        }
        //else {
        //    subChunks[y >> 4].blocks.setSkylight(x, y & 0xF, z, 15);
        //}
    }

    public void recalc() {
        for (int i = 0; i < CHUNKHEIGHT; i++) {
            blocks[i].refreshCounts();
        }
    }

    public void recalc(int i) {
        blocks[i].refreshCounts();
    }

    public void setBlock(int x, int y, int z, ushort block) {
        var oldBlock = blocks[y >> 4][x, y & 0xF, z];
        blocks[y >> 4][x, y & 0xF, z] = block;
        var wx = coord.x * CHUNKSIZE + x;
        var wz = coord.z * CHUNKSIZE + z;

        // call onBreak callback for old block if being replaced
        if (!Net.mode.isMPC()) {
            if (oldBlock != 0 && oldBlock != block) {
                Block.get(oldBlock).onBreak(world, wx, y, wz, 0);
            }
        }

        // if block broken, add sunlight from neighbours
        if (block == 0) {
            // Queue all 6 neighbours for light propagation
            // The propagation algorithm will handle cross-chunk boundaries
            foreach (var dir in Direction.directions) {
                var neighbourPos = world.getChunkAndRelativePos(this, x, y, z, dir, out var neighbourChunk);

                // is nullcheck needed?
                if (neighbourChunk != null) {
                    // Only queue if neighbour has light to propagate
                    var light = neighbourChunk.getLight(neighbourPos.X, neighbourPos.Y, neighbourPos.Z);
                    var skyLight = light.skylight();
                    var blockLight = light.blocklight();

                    if (skyLight > 0) {
                        int worldNX = (neighbourChunk.coord.x << 4) + neighbourPos.X;
                        int worldNZ = (neighbourChunk.coord.z << 4) + neighbourPos.Z;
                        world.skyLightQueue.Enqueue(new LightNode(worldNX, neighbourPos.Y, worldNZ,
                            neighbourChunk));
                    }

                    if (blockLight > 0) {
                        int worldNX = (neighbourChunk.coord.x << 4) + neighbourPos.X;
                        int worldNZ = (neighbourChunk.coord.z << 4) + neighbourPos.Z;
                        world.blockLightQueue.Enqueue(new LightNode(worldNX, neighbourPos.Y, worldNZ,
                            neighbourChunk));
                    }
                }
            }
        }
        else {
            world.removeSkyLightAndPropagate(wx, y, wz);
            world.removeBlockLightAndPropagate(wx, y, wz);
        }

        // if the old block had light, remove the light
        var newLightLevel = block == 0 ? 0 : Block.lightLevel[block];
        if (newLightLevel != Block.lightLevel[oldBlock]) {
            // remove lightsource
            world.removeBlockLightAndPropagate(wx, y, wz);
        }

        // if the new block has light, add the light
        if (Block.lightLevel[block] > 0) {
            // add lightsource
            setBlockLightDumb(x, y, z, Block.lightLevel[block]);
            //Console.Out.WriteLine(Block.get(block).lightLevel);
            world.blockLightQueue.Enqueue(new LightNode(wx, y, wz, this));
        }

        if (!Net.mode.isMPC()) {
            // call onPlace callback for new block if being placed
            if (block != 0 && oldBlock != block) {
                Block.get(block).onPlace(world, wx, y, wz, 0);
            }
        }

        // todo remove this hack when we handle shit properly - we need to dirtyArea on the server because we use dirtyChunksBatch in setBlockNeighboursDirty
        //  which isn't tracked by the chunk tracking
        //  "the spec is what happens"

        if (Net.mode.isDed()) {
            // notify listeners
            var pos = new Vector3I(x, y, z);
            world.dirtyArea(pos, pos);
        }

        // set neighbours dirty
        world.setBlockNeighboursDirty(new Vector3I(wx, y, wz));
    }

    public void setBlockMetadata(int x, int y, int z, uint block) {
        var oldBlockRaw = blocks[y >> 4].getRaw(x, y & 0xF, z);
        var oldBlock = oldBlockRaw.getID();
        var oldMeta = oldBlockRaw.getMetadata();
        blocks[y >> 4].setRaw(x, y & 0xF, z, block);
        var wx = coord.x * CHUNKSIZE + x;
        var wz = coord.z * CHUNKSIZE + z;

        var id = block.getID();
        var newMeta = block.getMetadata();

        // call onBreak callback for old block if being replaced
        if (oldBlock != 0 && oldBlock != id) {
            Block.get(oldBlock).onBreak(world, wx, y, wz, 0);
        }

        // if block broken, add sunlight from neighbours
        if (id == 0) {
            // Queue all 6 neighbours for light propagation
            // The propagation algorithm will handle cross-chunk boundaries
            foreach (var dir in Direction.directions) {
                var neighbourPos = world.getChunkAndRelativePos(this, x, y, z, dir, out var neighbourChunk);

                // is nullcheck needed?
                if (neighbourChunk != null) {
                    // Only queue if neighbour has light to propagate
                    var light = neighbourChunk.getLight(neighbourPos.X, neighbourPos.Y, neighbourPos.Z);
                    var skyLight = light.skylight();
                    var blockLight = light.blocklight();

                    if (skyLight > 0) {
                        int worldNX = (neighbourChunk.coord.x << 4) + neighbourPos.X;
                        int worldNZ = (neighbourChunk.coord.z << 4) + neighbourPos.Z;
                        world.skyLightQueue.Enqueue(new LightNode(worldNX, neighbourPos.Y, worldNZ,
                            neighbourChunk));
                    }

                    if (blockLight > 0) {
                        int worldNX = (neighbourChunk.coord.x << 4) + neighbourPos.X;
                        int worldNZ = (neighbourChunk.coord.z << 4) + neighbourPos.Z;
                        world.blockLightQueue.Enqueue(new LightNode(worldNX, neighbourPos.Y, worldNZ,
                            neighbourChunk));
                    }
                }
            }
        }
        else {
            world.removeSkyLightAndPropagate(wx, y, wz);
            world.removeBlockLightAndPropagate(wx, y, wz);
        }

        // if the new block has light, add the light
        if (Block.lightLevel[id] > 0) {
            // add lightsource
            setBlockLightDumb(x, y, z, Block.lightLevel[id]);
            //Console.Out.WriteLine(Block.get(block).lightLevel);
            world.blockLightQueue.Enqueue(new LightNode(wx, y, wz, this));
        }

        // if the old block had light, remove the light
        if (id == 0 && Block.lightLevel[oldBlock] > 0) {
            // remove lightsource
            world.removeBlockLightAndPropagate(wx, y, wz);
        }

        // call onPlace callback for new block if being placed
        if (id != 0 && oldBlock != id) {
            Block.get(id).onPlace(world, wx, y, wz, newMeta);
        }

        // todo remove this hack when we handle shit properly - we need to dirtyArea on the server because we use dirtyChunksBatch in setBlockNeighboursDirty
        //  which isn't tracked by the chunk tracking
        //  "the spec is what happens"

        if (Net.mode.isDed()) {
            // notify listeners
            var pos = new Vector3I(x, y, z);
            world.dirtyArea(pos, pos);
        }

        // set neighbours dirty
        world.setBlockNeighboursDirty(new Vector3I(wx, y, wz));
    }

    public void setBlockMetadataDumb(int x, int y, int z, uint block) {
        blocks[y >> 4].setRaw(x, y & 0xF, z, block);
    }

    public void setMetadata(int x, int y, int z, byte metadata) {
        var oldBlockRaw = blocks[y >> 4].getRaw(x, y & 0xF, z);
        blocks[y >> 4].setMetadata(x, y & 0xF, z, metadata);
        var wx = coord.x * CHUNKSIZE + x;
        var wz = coord.z * CHUNKSIZE + z;

        // todo remove this hack when we handle shit properly - we need to dirtyArea on the server because we use dirtyChunksBatch in setBlockNeighboursDirty
        //  which isn't tracked by the chunk tracking
        //  "the spec is what happens"

        if (Net.mode.isDed()) {
            // notify listeners
            var pos = new Vector3I(x, y, z);
            world.dirtyArea(pos, pos);
        }

        // set neighbours dirty
        world.setBlockNeighboursDirty(new Vector3I(wx, y, wz));
    }

    /// <summary>
    /// Uses chunk coordinates
    /// </summary>
    public void setLight(int x, int y, int z, byte value) {
        blocks[y >> 4].setLight(x, y & 0xF, z, value);
    }

    /// <summary>
    /// Uses chunk coordinates
    /// </summary>
    public void setSkyLightDumb(int x, int y, int z, byte value) {
        blocks[y >> 4].setSkylight(x, y & 0xF, z, value);
    }

    public void setSkyLight(int x, int y, int z, byte value) {
        var sectionY = y >> 4;
        var yRem = y & 0xF;

        // handle empty chunksections
        blocks[sectionY].setSkylight(x, yRem, z, value);
        var wx = coord.x * CHUNKSIZE + x;
        var wz = coord.z * CHUNKSIZE + z;

        // todo remove this hack when we handle shit properly - we need to dirtyArea on the server because we use dirtyChunksBatch in setBlockNeighboursDirty
        //  which isn't tracked by the chunk tracking
        //  "the spec is what happens"

        if (Net.mode.isDed()) {
            // notify listeners
            var pos = new Vector3I(x, y, z);
            world.dirtyArea(pos, pos);
        }


        world.setBlockNeighboursDirty(new Vector3I(wx, y, wz));
    }

    /// <summary>
    /// Uses chunk coordinates
    /// </summary>
    public void setBlockLightDumb(int x, int y, int z, byte value) {
        // handle empty chunksections
        blocks[y >> 4].setBlocklight(x, y & 0xF, z, value);
    }

    public void setBlockLight(int x, int y, int z, byte value) {
        // handle empty chunksections
        blocks[y >> 4].setBlocklight(x, y & 0xF, z, value);

        var wx = coord.x * CHUNKSIZE + x;
        var wz = coord.z * CHUNKSIZE + z;

        // todo remove this hack when we handle shit properly - we need to dirtyArea on the server because we use dirtyChunksBatch in setBlockNeighboursDirty
        //  which isn't tracked by the chunk tracking
        //  "the spec is what happens"

        if (Net.mode.isDed()) {
            // notify listeners
            var pos = new Vector3I(x, y, z);
            world.dirtyArea(pos, pos);
        }

        world.setBlockNeighboursDirty(new Vector3I(wx, y, wz));
    }

    /// <summary>
    /// Add the selected block to the heightmap.
    /// </summary>
    public void addToHeightMap(int x, int y, int z) {
        var height = heightMap.get(x, z);
        if (height < y && y < CHUNKHEIGHT * CHUNKSIZE) {
            heightMap.set(x, z, (byte)y);
        }
    }


    /// <summary>
    /// Remove the selected block from the heightmap and finds the block below it to add to the heightmap.
    /// </summary>
    public void removeFromHeightMap(int x, int y, int z) {
        var height = heightMap.get(x, z);
        // if the block is the highest block in the column
        if (height == y) {
            // find the block below
            for (int yy = y - 1; yy >= 0; yy--) {
                if (Block.isFullBlock(getBlock(x, yy, z))) {
                    heightMap.set(x, z, (byte)yy);
                    return;
                }
            }

            heightMap.set(x, z, 0);
        }
    }


    /// <summary>
    /// Uses chunk coordinates
    /// </summary>
    public ushort getBlock(int x, int y, int z) {
        return blocks[y >> 4][x, y & 0xF, z];
    }

    public byte getMetadata(int x, int y, int z) {
        return blocks[y >> 4].getMetadata(x, y & 0xF, z);
    }

    public uint getBlockRaw(int x, int y, int z) {
        return blocks[y >> 4].getRaw(x, y & 0xF, z);
    }

    public ushort getBlockFast(int x, int y, int z) {
        return blocks[y >> 4].fastGet(x, y & 0xF, z);
    }

    public byte getLight(int x, int y, int z) {
        return blocks[y >> 4].getLight(x, y & 0xF, z);
    }

    public byte getSkyLight(int x, int y, int z) {
        return blocks[y >> 4].skylight(x, y & 0xF, z);
    }

    public byte getBlockLight(int x, int y, int z) {
        return blocks[y >> 4].blocklight(x, y & 0xF, z);
    }

    public Vector3I getCoordInSection(int x, int y, int z) {
        return new Vector3I(x, y & 0xF, z);
    }

    public void setBlockEntity(int x, int y, int z, BlockEntity be) {
        blockEntities[new Vector3I(x, y, z)] = be;
    }

    public void removeBlockEntity(int x, int y, int z) {
        blockEntities.Remove(new Vector3I(x, y, z));
    }

    public BlockEntity? getBlockEntity(int x, int y, int z) {
        blockEntities.TryGetValue(new Vector3I(x, y, z), out var be);
        return be;
    }

    public void updateBlockEntities() {
        foreach (var be in blockEntities.Values) {
            be.update(world, be.pos.X, be.pos.Y, be.pos.Z);
        }
    }

    public void meshChunk() {
        for (int i = 0; i < CHUNKHEIGHT; i++) {
            Game.blockRenderer.meshChunk(subChunks[i]);
        }

        status = ChunkStatus.MESHED;
    }

    public void destroyChunk() {
        destroyed = true;
        Dispose();
    }

    private void ReleaseUnmanagedResources() {
        destroyed = true;

        foreach (var blocks in blocks) {
            blocks.Dispose();
        }

        // dispose SubChunk VAOs to prevent memory leaks
        // (read: you'll suffer)
        foreach (var subChunk in subChunks) {
            subChunk.vao?.Dispose();
            subChunk.watervao?.Dispose();
        }

        heightMap.Dispose();
    }

    private void Dispose(bool disposing) {
        ReleaseUnmanagedResources();
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~Chunk() {
        Dispose(false);
    }

    public bool Equals(Chunk? other) {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return coord.Equals(other.coord);
    }

    public override bool Equals(object? obj) {
        if (obj is null)
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((Chunk)obj);
    }

    public override int GetHashCode() {
        return coord.GetHashCode();
    }

    public static bool operator ==(Chunk? left, Chunk? right) {
        return Equals(left, right);
    }

    public static bool operator !=(Chunk? left, Chunk? right) {
        return !Equals(left, right);
    }
}

// we are declaring the fields manually because the language designers thought that generating *properties* is a good idea,
// especially when I care about access performance.
public readonly record struct ChunkCoord(int x, int z) {
    public readonly int x = x;
    public readonly int z = z;

    public ChunkCoord(long chunkKey) : this((int)(chunkKey >> 32), (int)chunkKey) {

    }

    public double distance(ChunkCoord chunkCoord) {
        int dx = x - chunkCoord.x;
        int dz = z - chunkCoord.z;
        return Math.Sqrt(dx * dx + dz * dz);
    }

    public double distanceSq(ChunkCoord chunkCoord) {
        int dx = x - chunkCoord.x;
        int dz = z - chunkCoord.z;
        return dx * dx + dz * dz;
    }

    /** pack coords into long */
    public long toLong() {
        return ((long)x) << 32 | (uint)z;
    }

    /** unpack coords from long */
    public static ChunkCoord fromLong(long packed) {
        int x = (int)(packed >> 32);
        int z = (int)packed;
        return new ChunkCoord(x, z);
    }

    public override int GetHashCode() {
        return XHash.hash(x, z);
    }

    public bool Equals(ChunkCoord other) {
        return x == other.x && z == other.z;
    }
}

public readonly record struct SubChunkCoord(int x, int y, int z) {
    public readonly int x = x;
    public readonly int y = y;
    public readonly int z = z;

    public ChunkCoord toChunk() {
        return new ChunkCoord(x, z);
    }

    public override int GetHashCode() {
        return XHash.hash(x, y, z);
    }

    public bool Equals(SubChunkCoord other) {
        return x == other.x && y == other.y && z == other.z;
    }
}

public readonly record struct RegionCoord(int x, int z) {
    public readonly int x = x;
    public readonly int z = z;

    public override int GetHashCode() {
        return XHash.hash(x, z);
    }

    public bool Equals(RegionCoord other) {
        return x == other.x && z == other.z;
    }
}

public enum ChunkStatus : byte {
    /// <summary>
    /// No data
    /// </summary>
    EMPTY,

    /// <summary>
    /// Terrain is generated
    /// </summary>
    GENERATED,

    /// <summary>
    /// Trees, ores, features etc. are added in the chunk
    /// </summary>
    POPULATED,

    /// <summary>
    /// Lightmap has been initialised (skylight + blocklist)
    /// </summary>
    LIGHTED,

    /// <summary>
    /// Chunk has been meshed
    /// </summary>
    MESHED
}