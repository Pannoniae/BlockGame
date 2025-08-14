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
        }

        heightMap = new HeightMap(this);

        box = new AABB(new Vector3D(chunkX * CHUNKSIZE, 0, chunkZ * CHUNKSIZE),
            new Vector3D(chunkX * CHUNKSIZE + CHUNKSIZE, CHUNKHEIGHT * CHUNKSIZE, chunkZ * CHUNKSIZE + CHUNKSIZE));
    }

    public bool isVisible(BoundingFrustum frustum) {
        return !frustum.outsideCameraHorizontal(box);
    }

    public void lightChunk() {
        // set the top of the chunk to 15 if not solid
        // then propagate down
        for (int x = 0; x < CHUNKSIZE; x++) {
            for (int z = 0; z < CHUNKSIZE; z++) {
                var y = CHUNKSIZE * CHUNKHEIGHT - 1;

                // loop down until block is solid
                ushort bl = getBlock(x, y, z);
                while (!Block.isFullBlock(bl) && y > 0) {
                    // check if chunk is initialised first
                    if (blocks[y >> 4].inited) {
                        setSkyLight(x, y, z, 15);
                    }

                    y--;
                    bl = getBlock(x, y, z);
                }

                // add the last item for propagation
                world.skyLightQueue.Add(new LightNode(worldX + x, y + 1, worldZ + z, this));

                // loop from y down to the bottom of the world
                for (int yy = y - 1; yy >= 0; yy--) {
                    bl = getBlock(x, yy, z);
                    // if blocklight, propagate
                    if (Block.lightLevel[bl] > 0) {
                        world.blockLightQueue.Add(new LightNode(worldX + x, yy, worldZ + z, this));
                    }
                }
            }
        }

        // second pass: check for horizontal propagation into unlit neighbors
        for (int x = 0; x < CHUNKSIZE; x++) {
            for (int z = 0; z < CHUNKSIZE; z++) {
                for (int y = CHUNKSIZE * CHUNKHEIGHT - 1; y >= 0; y--) {
                    // if this position has skylight and is air
                    if (getSkyLight(x, y, z) == 15) {
                        // check horizontal neighbors
                        for (var i = 0; i < 4; i++) {
                            int dx;
                            int dz;
                            
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
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                            var nx = x + dx;
                            var nz = z + dz;
                            var worldnx = worldX + nx;
                            var worldnz = worldZ + nz;

                            // use world coordinates to check neighbors across chunk boundaries
                            //if (world.inWorld(worldnx, y, worldnz)) {
                            // if neighbor is air and has no skylight, add for propagation
                            if (!Block.isFullBlock(world.getBlock(worldnx, y, worldnz)) &&
                                world.getSkyLight(worldnx, y, worldnz) == 0) {
                                world.skyLightQueue.Add(new LightNode(worldX + x, y, worldZ + z, this));
                            }
                            //}
                        }
                    }
                }
            }
        }

        world.processSkyLightQueue();
        status = ChunkStatus.LIGHTED;
    }

    /*public void lightSection(SubChunk section) {

        // set the top of the chunk to 15 if not solid
        // then propagate down
        for (int x = 0; x < CHUNKSIZE; x++) {
            for (int z = 0; z < CHUNKSIZE; z++) {
                var y = section.worldY + 15;
                // loop down until block is solid
                ushort bl = getBlock(x, y, z);

                var atLeastOnce = false;
                while (!Block.isFullBlock(bl) && y > 0) {
                    atLeastOnce = true;
                    // check if chunk is initialised first
                    if (section.blocks.inited) {
                        setSkyLight(x, y, z, 15);
                    }
                    y--;
                    bl = getBlock(x, y, z);
                }
                if (atLeastOnce) {
                    // add the last item for propagation
                    world.skyLightQueue.Add(new LightNode(worldX + x, y, worldZ + z, this));
                }

                // loop from y down to the bottom of the world
                for (int yy = y - 1; yy >= 0; yy--) {
                    bl = getBlock(x, yy, z);
                    // if blocklight, propagate
                    if (Block.lightLevel[bl] > 0) {
                        world.blockLightQueue.Add(new LightNode(worldX + x, y, worldZ + z, this));
                    }
                }
            }
        }
        world.processSkyLightQueue();
        world.processBlockLightQueue();
    }*/

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
        var sectionY = y >> 4;
        var yRem = y & 0xF;

        // handle empty chunksections
        /*if (section.isEmpty && block != 0) {
            section.blocks = new ArrayBlockData(this);
            section.isEmpty = false;
        }*/
        var oldBlock = blocks[y >> 4][x, yRem, z];
        blocks[y >> 4][x, yRem, z] = block;
        var wx = coord.x * CHUNKSIZE + x;
        var wz = coord.z * CHUNKSIZE + z;

        // From there on, ONLY REMESHING STUFF

        // if block broken, add sunlight from neighbours
        if (block == 0) {
            //world.skyLightQueue.Add(new LightNode(wx, y, wz, this));
            world.skyLightQueue.Add(new LightNode(wx - 1, y, wz, this));
            world.skyLightQueue.Add(new LightNode(wx + 1, y, wz, this));
            world.skyLightQueue.Add(new LightNode(wx, y, wz - 1, this));
            world.skyLightQueue.Add(new LightNode(wx, y, wz + 1, this));
            world.skyLightQueue.Add(new LightNode(wx, y - 1, wz, this));
            world.skyLightQueue.Add(new LightNode(wx, y + 1, wz, this));

            // and blocklight too
            world.blockLightQueue.Add(new LightNode(wx - 1, y, wz, this));
            world.blockLightQueue.Add(new LightNode(wx + 1, y, wz, this));
            world.blockLightQueue.Add(new LightNode(wx, y, wz - 1, this));
            world.blockLightQueue.Add(new LightNode(wx, y, wz + 1, this));
            world.blockLightQueue.Add(new LightNode(wx, y - 1, wz, this));
            world.blockLightQueue.Add(new LightNode(wx, y + 1, wz, this));
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
            world.blockLightQueue.Add(new LightNode(wx, y, wz, this));
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
        var sectionY = y / CHUNKSIZE;
        var yRem = y % CHUNKSIZE;

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
            Game.renderer.meshChunk(subChunks[i]);
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
}

public readonly record struct SubChunkCoord(int x, int y, int z) {
    public readonly int x = x;
    public readonly int y = y;
    public readonly int z = z;
    
    public override int GetHashCode() {
        return XHash.hash(x, y, z);
    }
}

public readonly record struct RegionCoord(int x, int z) {
    public readonly int x = x;
    public readonly int z = z;

    public override int GetHashCode() {
        return XHash.hash(x, z);
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