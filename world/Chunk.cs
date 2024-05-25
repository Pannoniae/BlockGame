using System.Numerics;
using Silk.NET.Maths;

namespace BlockGame;

public class Chunk {
    public ChunkStatus status;

    public HeightMap height;
    public ChunkCoord coord;
    public ChunkSection[] chunks;
    public World world;

    public int worldX => coord.x * CHUNKSIZE;
    public int worldZ => coord.z * CHUNKSIZE;
    public Vector2D<int> worldPos => new(worldX, worldZ);
    public Vector2D<int> centrePos => new(worldX + 8, worldZ + 8);

    public AABB box;

    public const int CHUNKHEIGHT = 8;
    public const int CHUNKSIZE = 16;
    public const int CHUNKSIZESQ = 16 * 16;
    public const int CHUNKSIZEEX = 18;
    public const int CHUNKSIZEEXSQ = 18 * 18;


    public Chunk(World world, int chunkX, int chunkZ) {
        status = ChunkStatus.EMPTY;
        this.world = world;

        chunks = new ChunkSection[CHUNKHEIGHT];
        coord = new ChunkCoord(chunkX, chunkZ);
        // TODO FIX THIS SHIT

        for (int i = 0; i < CHUNKHEIGHT; i++) {
            chunks[i] = new ChunkSection(world, this, chunkX, i, chunkZ);
        }

        height = new HeightMap(this);

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
                var y = (CHUNKSIZE * CHUNKHEIGHT) - 1;
                // loop down until block is solid
                ushort bl = getBlock(x, y, z);
                while (!Blocks.isSolid(bl)) {
                    setSkyLight(x, y, z, 15);
                    y--;
                    bl = getBlock(x, y, z);
                };

                // add the last item for propagation
                world.skyLightQueue.Add(new LightNode(worldX + x, y, worldZ + z, this));
            }
        }
        world.processSkyLightQueue();
    }

    /// <summary>
    /// Uses chunk coordinates
    /// </summary>
    public void setBlock(int x, int y, int z, ushort block, bool remesh = true) {
        var sectionY = y / CHUNKSIZE;
        var yRem = y % CHUNKSIZE;

        // handle empty chunksections
        var section = chunks[sectionY];
        /*if (section.isEmpty && block != 0) {
            section.blocks = new ArrayBlockData(this);
            section.isEmpty = false;
        }*/
        section.blocks[x, yRem, z] = block;

        if (remesh) {
            // if it needs to be remeshed, add this and neighbouring chunksections to the remesh queue

            world.mesh(new ChunkSectionCoord(coord.x, sectionY, coord.z));
            //meshChunk();

            // get global coords
            var worldPos = world.toWorldPos(coord.x, coord.z, x, y, z);
            var chunkPos = world.getChunkSectionPos(worldPos);

            foreach (var dir in Direction.directions) {
                var neighbourSection = world.getChunkSectionPos(worldPos + dir);
                if (world.isChunkSectionInWorld(neighbourSection) && neighbourSection != chunkPos) {
                    world.mesh(neighbourSection);
                }
            }
        }
    }

    /// <summary>
    /// Uses chunk coordinates
    /// </summary>
    public void setSkyLight(int x, int y, int z, byte value) {
        var sectionY = y / CHUNKSIZE;
        var yRem = y % CHUNKSIZE;

        // handle empty chunksections
        var section = chunks[sectionY];
        section.blocks.setSkylight(x, yRem, z, value);
    }

    /// <summary>
    /// Uses chunk coordinates
    /// </summary>
    public void setBlockLight(int x, int y, int z, byte value) {
        var sectionY = y / CHUNKSIZE;
        var yRem = y % CHUNKSIZE;

        // handle empty chunksections
        var section = chunks[sectionY];
        section.blocks.setBlocklight(x, yRem, z, value);
    }


    /// <summary>
    /// Uses chunk coordinates
    /// </summary>
    public ushort getBlock(int x, int y, int z) {
        var sectionY = y / CHUNKSIZE;
        var yRem = y % CHUNKSIZE;
        return chunks[sectionY].blocks[x, yRem, z];
    }

    public byte getLight(int x, int y, int z) {
        var sectionY = y / CHUNKSIZE;
        var yRem = y % CHUNKSIZE;
        return chunks[sectionY].blocks.getLight(x, yRem, z);
    }

    public byte getSkyLight(int x, int y, int z) {
        var sectionY = y / CHUNKSIZE;
        var yRem = y % CHUNKSIZE;
        return chunks[sectionY].blocks.skylight(x, yRem, z);
    }

    public byte getBlockLight(int x, int y, int z) {
        var sectionY = y / CHUNKSIZE;
        var yRem = y % CHUNKSIZE;
        return chunks[sectionY].blocks.blocklight(x, yRem, z);
    }

    public Vector3D<int> getSectionCoord(int x, int y, int z) {
        var sectionY = y / CHUNKSIZE;
        var yRem = y % CHUNKSIZE;
        return new Vector3D<int>(x, yRem, z);
    }

    public void meshChunk() {
        for (int i = 0; i < CHUNKHEIGHT; i++) {
            chunks[i].renderer.meshChunk();
        }
        status = ChunkStatus.MESHED;
    }

    public void drawOpaque(PlayerCamera camera) {
        for (int i = 0; i < CHUNKHEIGHT; i++) {
            chunks[i].renderer.drawOpaque(camera);
        }
    }

    public void drawTransparent(PlayerCamera camera) {
        for (int i = 0; i < CHUNKHEIGHT; i++) {
            chunks[i].renderer.drawTransparent(camera);
        }
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