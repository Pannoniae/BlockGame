using BlockGame.block;
using BlockGame.util;
using Molten;
using Molten.DoublePrecision;
using BoundingFrustum = System.Numerics.BoundingFrustum;

namespace BlockGame;

public class Chunk : IDisposable, IEquatable<Chunk> {
    public const int CHUNKHEIGHT = 8;
    public const int CHUNKSIZE = 16;
    public const int CHUNKSIZESQ = 16 * 16;
    public const int MAXINDEX = 16 * 16 * 16;
    public const int CHUNKSIZEEX = 18;
    public const int CHUNKSIZEEXSQ = 18 * 18;
    public const int MAXINDEXEX = 18 * 18 * 18;

    public const int CHUNKSIZEMASK = CHUNKSIZE - 1;

    public ChunkStatus status;

    public HeightMap heightMap;
    public readonly ChunkCoord coord;
    public SubChunk[] subChunks;
    public ArrayBlockData[] blocks = new ArrayBlockData[CHUNKHEIGHT];

    /** For now, this is fixed-size, we'll cook something better up later */
    public List<Entity>[] entities = new List<Entity>[CHUNKHEIGHT];

    /** TODO implement crafting tables and stuff
     * (does that need to be a block entity? maybe? or more like a chest or something
     */
    public Dictionary<Vector3I, BlockEntity> blockEntities = new();

    public World world;

    public AABB box;

    public bool isRendered = false;
    public ulong lastSaved;

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
            blocks[i] = new ArrayBlockData(this, i);
            entities[i] = [];
        }
        

        heightMap = new HeightMap(this);

        box = new AABB(new Vector3D(chunkX * CHUNKSIZE, 0, chunkZ * CHUNKSIZE),
            new Vector3D(chunkX * CHUNKSIZE + CHUNKSIZE, CHUNKHEIGHT * CHUNKSIZE, chunkZ * CHUNKSIZE + CHUNKSIZE));
    }

    public bool isVisible(BoundingFrustum frustum) {
        return !frustum.outsideCameraHorizontal(box);
    }
    
    public void addEntity(Entity entity) {
        // check if the entity is in the chunk
        if (entity.position.X < worldX || entity.position.X >= worldX + CHUNKSIZE ||
            entity.position.Z < worldZ || entity.position.Z >= worldZ + CHUNKSIZE) {
            SkillIssueException.throwNew($"Entity position is out of chunk bounds: {entity.position} in chunk {coord}");
        }

        // get the Y coordinate in the chunk
        int y = (int)entity.position.Y;
        if (y < 0 || y >= CHUNKHEIGHT * CHUNKSIZE) {
            return; // out of bounds
        }

        // add the entity to the list
        entities[y].Add(entity);
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
                    setSkyLight(x, y, z, 15);
                    //}

                    y--;
                    bl = getBlock(x, y, z);
                }
                
                // add the last item for propagation
                if (y + 1 >= CHUNKSIZE * CHUNKHEIGHT) {
                    goto blockLoop;
                }
                
                world.skyLightQueue.Add(new LightNode(x, y + 1, z, this));

                // y + 1 is air
                // y is water or solid block

                // SPECIAL CASE:
                // if the block on the bottom is water, DON'T START TO PROPAGATE
                // it will eat ALL the performance.
                // instead, what we will do is, we'll fast forward to the bottom of the water, lighting it up as we go (decreasing the light level obviously THEN add that to the propagation)

                var ll = getSkyLight(x, y + 1, z);
                if (bl == Blocks.WATER) {
                    // if the block is water, we need to propagate downwards
                    // but we need to do it manually, because otherwise it will add 7 million entries to the queue
                    while (y > 0 && bl == Blocks.WATER) {
                        ll -= Block.lightAbsorption[bl];
                        if (ll <= 0) break;
                        y--;
                        setSkyLight(x, y, z, ll);
                        bl = getBlock(x, y, z);
                    }

                    // add it to the queue for propagation
                    world.skyLightQueue.Add(new LightNode(x, y + 1, z, this));
                }

                blockLoop: ;
                // loop from y down to the bottom of the world
                for (int yy = y - 1; yy >= 0; yy--) {
                    bl = getBlock(x, yy, z);
                    // if blocklight, propagate
                    if (Block.lightLevel[bl] > 0) {
                        world.blockLightQueue.Add(new LightNode(x, yy, z, this));
                    }
                }
            }
        }

        // we collect, then we propagate!
        List<LightNode> toPropagate = new();

        // second pass: check for horizontal propagation into unlit neighbors
        for (int x = 0; x < CHUNKSIZE; x++) {
            for (int z = 0; z < CHUNKSIZE; z++) {
                for (int y = CHUNKSIZE * CHUNKHEIGHT - 1; y >= 0; y--) {
                    // if this position has skylight and is air
                    if (getSkyLight(x, y, z) == 15) {
                        // check horizontal neighbors

                        bool propagateThis = false;
                        bool propagateBelow = false;
                        for (var i = 0; i < 4; i++) {
                            int dx = 0;
                            int dz = 0;

                            switch (i) {
                                case 0: // left
                                    dx = -1;
                                    dz = 0;
                                    break;
                                case 1: // right
                                    dx = 1;
                                    dz = 0;
                                    break;
                                case 2: // front
                                    dx = 0;
                                    dz = -1;
                                    break;
                                case 3: // back
                                    dx = 0;
                                    dz = 1;
                                    break;
                                // yes i know the switch isn't exhaustive SHUT UP
                            }

                            var nx = x + dx;
                            var nz = z + dz;
                            var worldnx = worldX + nx;
                            var worldnz = worldZ + nz;

                            // if neighbor is air and has no skylight, add for propagation
                            //if (!Block.isFullBlock(world.getRelativeBlock(this, x, y, z, new Vector3I(dx, 0, dz))) {
                            // if full skylight there, nothing to do....
                            //world.getSkyLight(worldnx, y, worldnz) != 15) {
                            //world.skyLightQueue.Add(new LightNode(x, y, z, this));

                            // what if we propagated manually? let's find out!
                            //}


                            // if at least one neighbour is solid, add this to the propagation and the block below it too! (for overhangs)
                            var relPos = world.getChunkAndRelativePos(this, x, y, z, new Vector3I(dx, 0, dz),
                                out var neighborChunk);
                            var neighborBlock = neighborChunk?.getBlock(relPos.X, relPos.Y, relPos.Z) ?? 0;
                            if (Block.isFullBlock(neighborBlock)) {
                                // if the neighbor is solid, we can propagate skylight from this position
                                propagateThis = true;
                                // also add the block below it for propagation
                                if (y > 0) {
                                    // only add if the block below is not solid
                                    var belowBlock = neighborChunk?.getBlock(relPos.X, relPos.Y - 1, relPos.Z) ?? 0;
                                    if (!Block.isFullBlock(belowBlock)) {
                                        // add the block below for propagation
                                        propagateBelow = true;
                                    }
                                }
                            }
                        }

                        // we only propagate *once* per position, so if we found an empty neighbor, we propagate
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

        //world.processSkyLightQueueNoUpdate();
        status = ChunkStatus.LIGHTED;
    }

    /**
     * The task is simple: propagate light under underhangs without adding 7 million entries to the lighting queue (where it will choke).
     * SO HOW ABOUT DOING IT MANUALLY?
     * we know that the light level at the given position is 15, and we only need to propagate skylight.
     * This makes it much easier!
     */
    private void manuallyPropagate(int x, int y, int z) {
        var queue = new Queue<LightNode>();
        queue.Enqueue(new LightNode(x, y, z, this));

        while (queue.Count > 0) {
            var (cx, cy, cz, chunk) = queue.Dequeue();
            var currentLight = chunk.getSkyLight(cx, cy, cz);

            // propagate to all 6 neighbors
            foreach (var dir in Direction.directions) {
                var neighborPos = world.getChunkAndRelativePos(chunk, cx, cy, cz, dir, out var neighborChunk);
                if (neighborChunk == null) {
                    continue;
                }

                var nx = neighborPos.X;
                var ny = neighborPos.Y;
                var nz = neighborPos.Z;

                // skip if neighbor is solid
                if (Block.fullBlock[neighborChunk.getBlock(nx, ny, nz)]) continue;

                var neighborLight = neighborChunk.getSkyLight(nx, ny, nz);
                byte newLevel;

                // special case for skylight downward propagation
                if (dir == Direction.DOWN && currentLight == 15) {
                    newLevel = 15;
                }
                else {
                    newLevel = (byte)(currentLight - 1);
                }

                // only propagate if we can improve the light level
                if (newLevel > 0 && newLevel > neighborLight &&
                    (neighborLight + 2 <= currentLight || dir == Direction.DOWN)) {
                    neighborChunk.setSkyLight(nx, ny, nz, newLevel);
                    queue.Enqueue(new LightNode(nx, ny, nz, neighborChunk));
                }
            }
        }
    }

    /// <summary>
    /// Uses chunk coordinates
    /// </summary>
    public void setBlock(int x, int y, int z, ushort block) {
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

    /// <summary>
    /// It's like setBlockFast but doesn't check if the chunk is initialised.
    /// </summary>
    public void setBlockFastUnsafe(int x, int y, int z, ushort block) {
        blocks[y >> 4].fastSetUnsafe(x, y & 0xF, z, block);
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

    public void setBlockRemesh(int x, int y, int z, ushort block) {
        // handle empty chunksections
        /*if (section.isEmpty && block != 0) {
            section.blocks = new ArrayBlockData(this);
            section.isEmpty = false;
        }*/
        var oldBlock = blocks[y >> 4][x, y & 0xF, z];
        blocks[y >> 4][x, y & 0xF, z] = block;
        var wx = coord.x * CHUNKSIZE + x;
        var wz = coord.z * CHUNKSIZE + z;

        // From there on, ONLY REMESHING STUFF

        // if block broken, add sunlight from neighbours
        if (block == 0) {
            // Queue all 6 neighbors for light propagation
            // The propagation algorithm will handle cross-chunk boundaries
            foreach (var dir in Direction.directions) {
                var neighborPos = world.getChunkAndRelativePos(this, x, y, z, dir, out var neighborChunk);

                // is nullcheck needed?
                if (neighborChunk != null) {
                    // Only queue if neighbor has light to propagate
                    //var skyLight = neighborChunk.getSkyLight(neighborPos.X, neighborPos.Y, neighborPos.Z);
                    //var blockLight = neighborChunk.getBlockLight(neighborPos.X, neighborPos.Y, neighborPos.Z);

                    //if (skyLight > 0) {
                    world.skyLightQueue.Add(new LightNode(neighborPos.X, neighborPos.Y, neighborPos.Z, neighborChunk));
                    //}
                    //if (blockLight > 0) {
                    world.blockLightQueue.Add(new LightNode(neighborPos.X, neighborPos.Y, neighborPos.Z,
                        neighborChunk));
                }
                //}
            }
        }
        else {
            world.removeSkyLightAndPropagate(wx, y, wz);
            world.removeBlockLightAndPropagate(wx, y, wz);
        }

        // if the new block has light, add the light
        if (Block.lightLevel[block] > 0) {
            // add lightsource
            setBlockLight(x, y, z, Block.lightLevel[block]);
            //Console.Out.WriteLine(Block.get(block).lightLevel);
            world.blockLightQueue.Add(new LightNode(x, y, z, this));
        }

        // if the old block had light, remove the light
        if (block == 0 && Block.lightLevel[oldBlock] > 0) {
            // remove lightsource
            world.removeBlockLightAndPropagate(wx, y, wz);
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
    public void setSkyLight(int x, int y, int z, byte value) {
        blocks[y >> 4].setSkylight(x, y & 0xF, z, value);
    }

    public void setSkyLightRemesh(int x, int y, int z, byte value) {
        var sectionY = y >> 4;
        var yRem = y & 0xF;

        // handle empty chunksections
        blocks[sectionY].setSkylight(x, yRem, z, value);
        var wx = coord.x * CHUNKSIZE + x;
        var wz = coord.z * CHUNKSIZE + z;


        world.setBlockNeighboursDirty(new Vector3I(wx, y, wz));
    }

    /// <summary>
    /// Uses chunk coordinates
    /// </summary>
    public void setBlockLight(int x, int y, int z, byte value) {
        // handle empty chunksections
        blocks[y >> 4].setBlocklight(x, y & 0xF, z, value);
    }

    public void setBlockLightRemesh(int x, int y, int z, byte value) {
        // handle empty chunksections
        blocks[y >> 4].setBlocklight(x, y & 0xF, z, value);

        var wx = coord.x * CHUNKSIZE + x;
        var wz = coord.z * CHUNKSIZE + z;

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

    public void meshChunk() {
        for (int i = 0; i < CHUNKHEIGHT; i++) {
            Game.blockRenderer.meshChunk(subChunks[i]);
        }

        status = ChunkStatus.MESHED;
    }

    public void destroyChunk() {
        Dispose();
    }

    private void ReleaseUnmanagedResources() {
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