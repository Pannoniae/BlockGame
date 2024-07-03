using System.Numerics;
using BlockGame.util;
using Silk.NET.Maths;

namespace BlockGame;

public class Chunk : IDisposable {
    public ChunkStatus status;

    public HeightMap heightMap;
    public ChunkCoord coord;
    public SubChunk[] subChunks;
    public World world;

    public int worldX => coord.x * CHUNKSIZE;
    public int worldZ => coord.z * CHUNKSIZE;
    public Vector2D<int> worldPos => new(worldX, worldZ);
    public Vector2D<int> centrePos => new(worldX + 8, worldZ + 8);

    public AABB box;

    public const int CHUNKHEIGHT = 8;
    public const int CHUNKSIZE = 16;
    public const int CHUNKSIZESQ = 16 * 16;
    public const int MAXINDEX = 16 * 16 * 16;
    public const int CHUNKSIZEEX = 18;
    public const int CHUNKSIZEEXSQ = 18 * 18;
    public const int MAXINDEXEX = 18 * 18 * 18;


    public Chunk(World world, int chunkX, int chunkZ) {
        status = ChunkStatus.EMPTY;
        this.world = world;

        subChunks = new SubChunk[CHUNKHEIGHT];
        coord = new ChunkCoord(chunkX, chunkZ);
        // TODO FIX THIS SHIT

        for (int i = 0; i < CHUNKHEIGHT; i++) {
            subChunks[i] = new SubChunk(world, this, chunkX, i, chunkZ);
        }

        heightMap = new HeightMap(this);

        box = new AABB(new Vector3D<double>(chunkX * CHUNKSIZE, 0, chunkZ * CHUNKSIZE), new Vector3D<double>(chunkX * CHUNKSIZE + CHUNKSIZE, CHUNKHEIGHT * CHUNKSIZE, chunkZ * CHUNKSIZE + CHUNKSIZE));
    }

    public bool isVisible(BoundingFrustum frustum) {
        return frustum.Contains(new BoundingBox(box.min.toVec3(), box.max.toVec3())) != ContainmentType.Disjoint;
    }

    public void lightChunk() {
        // set the top of the chunk to 15 if not solid
        // then propagate down
        for (int x = 0; x < CHUNKSIZE; x++) {
            for (int z = 0; z < CHUNKSIZE; z++) {
                var y = CHUNKSIZE * CHUNKHEIGHT - 1;
                // loop down until block is solid
                ushort bl = getBlock(x, y, z);
                while (!Blocks.isFullBlock(bl)) {
                    // check if chunk is initialised first
                    if (subChunks[y >> 4].blocks.inited) {
                        setSkyLight(x, y, z, 15);
                    }
                    y--;
                    bl = getBlock(x, y, z);
                }

                // add the last item for propagation
                world.skyLightQueue.Add(new LightNode(worldX + x, y, worldZ + z, this));
            }
        }
        world.processSkyLightQueue();
        status = ChunkStatus.LIGHTED;
    }

    public void lightSection(SubChunk section) {

        // set the top of the chunk to 15 if not solid
        // then propagate down
        for (int x = 0; x < CHUNKSIZE; x++) {
            for (int z = 0; z < CHUNKSIZE; z++) {
                var y = section.worldY + 15;
                // loop down until block is solid
                ushort bl = getBlock(x, y, z);
                var atLeastOnce = false;
                while (!Blocks.isFullBlock(bl) && y > 0) {
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
            }
        }
        world.processSkyLightQueue();
    }

    /// <summary>
    /// Uses chunk coordinates
    /// </summary>
    public void setBlock(int x, int y, int z, ushort block) {
        subChunks[y >> 4].blocks[x, y & 0xF, z] = block;
    }

    public void setBlockRemesh(int x, int y, int z, ushort block) {
        var sectionY = y >> 4;
        var yRem = y & 0xF;

        // handle empty chunksections
        var section = subChunks[sectionY];
        /*if (section.isEmpty && block != 0) {
            section.blocks = new ArrayBlockData(this);
            section.isEmpty = false;
        }*/
        var oldBlock = section.blocks[x, yRem, z];
        section.blocks[x, yRem, z] = block;
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
        }

        // if the new block has light, add the light
        if (Blocks.get(block).lightLevel > 0) {
            // add lightsource
            setBlockLight(x, y, z, Blocks.get(block).lightLevel);
            //Console.Out.WriteLine(Blocks.get(block).lightLevel);
            world.blockLightQueue.Add(new LightNode(wx, y, wz, this));
        }
        // if the old block had light, remove the light
        if (block == 0 && Blocks.get(oldBlock).lightLevel > 0) {
            // remove lightsource
            world.removeBlockLightAndPropagate(wx, y, wz);
        }

        // if it needs to be remeshed, add this and neighbouring chunksections to the remesh queue

        // mesh this section
        world.mesh(new ChunkSectionCoord(coord.x, sectionY, coord.z));

        // get global coords
        var chunkPos = World.getChunkSectionPos(wx, y, wz);

        // TODO only remesh neighbours if on the edge of the chunk
        // we need to mesh EVERYTHING in the 3x3x3 area because AO is a bitch / affects non-adjacent blocks too
        foreach (var dir in Direction.directionsAll) {
            var neighbourSection = World.getChunkSectionPos(new Vector3D<int>(wx, y, wz) + dir);
            if (world.isChunkSectionInWorld(neighbourSection) && neighbourSection != chunkPos) {
                world.mesh(neighbourSection);
            }
        }
    }

    /// <summary>
    /// Uses chunk coordinates
    /// </summary>
    public void setLight(int x, int y, int z, byte value) {
        subChunks[y >> 4].blocks.setLight(x, y & 0xF, z, value);
    }

    /// <summary>
    /// Uses chunk coordinates
    /// </summary>
    public void setSkyLight(int x, int y, int z, byte value) {
        subChunks[y >> 4].blocks.setSkylight(x, y & 0xF, z, value);
    }

    public void setSkyLightRemesh(int x, int y, int z, byte value) {

        var sectionY = y / CHUNKSIZE;
        var yRem = y % CHUNKSIZE;

        // handle empty chunksections
        var section = subChunks[sectionY];
        section.blocks.setSkylight(x, yRem, z, value);
        var wx = coord.x * CHUNKSIZE + x;
        var wz = coord.z * CHUNKSIZE + z;

        world.mesh(new ChunkSectionCoord(coord.x, sectionY, coord.z));
        var chunkPos = World.getChunkSectionPos(wx, y, wz);

        // TODO only remesh neighbours if on the edge of the chunk
        foreach (var dir in Direction.directions) {
            var neighbourSection = World.getChunkSectionPos(new Vector3D<int>(wx, y, wz) + dir);
            if (world.isChunkSectionInWorld(neighbourSection) && neighbourSection != chunkPos) {
                world.mesh(neighbourSection);
            }
        }
    }

    /// <summary>
    /// Uses chunk coordinates
    /// </summary>
    public void setBlockLight(int x, int y, int z, byte value) {
        // handle empty chunksections
        subChunks[y >> 4].blocks.setBlocklight(x, y & 0xF, z, value);

    }

    public void setBlockLightRemesh(int x, int y, int z, byte value) {

        // handle empty chunksections
        subChunks[y >> 4].blocks.setBlocklight(x, y & 0xF, z, value);

        var wx = coord.x * CHUNKSIZE + x;
        var wz = coord.z * CHUNKSIZE + z;

        world.mesh(new ChunkSectionCoord(coord.x, y >> 4, coord.z));
        var chunkPos = World.getChunkSectionPos(wx, y, wz);

        // TODO only remesh neighbours if on the edge of the chunk
        foreach (var dir in Direction.directions) {
            var neighbourSection = World.getChunkSectionPos(new Vector3D<int>(wx, y, wz) + dir);
            if (world.isChunkSectionInWorld(neighbourSection) && neighbourSection != chunkPos) {
                world.mesh(neighbourSection);
            }
        }
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
                if (Blocks.isFullBlock(getBlock(x, yy, z))) {
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
        return subChunks[y >> 4].blocks[x, y & 0xF, z];
    }

    public byte getLight(int x, int y, int z) {
        return subChunks[y >> 4].blocks.getLight(x, y & 0xF, z);
    }

    public byte getSkyLight(int x, int y, int z) {
        return subChunks[y >> 4].blocks.skylight(x, y & 0xF, z);
    }

    public byte getBlockLight(int x, int y, int z) {
        return subChunks[y >> 4].blocks.blocklight(x, y & 0xF, z);
    }

    public Vector3D<int> getCoordInSection(int x, int y, int z) {
        return new Vector3D<int>(x, y & 0xF, z);
    }

    public void meshChunk() {
        for (int i = 0; i < CHUNKHEIGHT; i++) {
            subChunks[i].renderer.meshChunk();
        }
        status = ChunkStatus.MESHED;
    }

    public void drawOpaque() {
        for (int i = 0; i < CHUNKHEIGHT; i++) {
            subChunks[i].renderer.drawOpaque();
        }
    }

    public void drawTransparent(bool dummy = false) {
        for (int i = 0; i < CHUNKHEIGHT; i++) {
            subChunks[i].renderer.drawTransparent(dummy);
        }
    }

    public void destroyChunk() {
        Dispose();
    }

    private void ReleaseUnmanagedResources() {
        foreach (var chunk in subChunks) {
            chunk.Dispose();
        }
        heightMap.Dispose();
    }

    private void Dispose(bool disposing) {
        ReleaseUnmanagedResources();
        if (disposing) {
            heightMap.Dispose();
        }
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~Chunk() {
        Dispose(false);
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
}

public readonly record struct ChunkSectionCoord(int x, int y, int z) {
    public readonly int x = x;
    public readonly int y = y;
    public readonly int z = z;
}

public readonly record struct RegionCoord(int x, int z) {
    public readonly int x = x;
    public readonly int z = z;
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