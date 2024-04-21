using Silk.NET.Maths;

namespace BlockGame;

public class Chunk {
    public ChunkStatus status;

    public ushort[,,] blocks;
    public LightMap lightMap;
    public ChunkCoord coord;
    public ChunkSection[] chunks;
    public World world;

    public int worldX => coord.x * CHUNKSIZE;
    public int worldZ => coord.z * CHUNKSIZE;
    public Vector2D<int> worldPos => new(worldX, worldZ);

    public ChunkGenerator generator;

    public const int CHUNKHEIGHT = 8;
    public const int CHUNKSIZE = 16;


    public Chunk(World world, int chunkX, int chunkZ) {
        status = ChunkStatus.EMPTY;
        this.world = world;

        blocks = new ushort[CHUNKSIZE, CHUNKSIZE * CHUNKHEIGHT, CHUNKSIZE];
        chunks = new ChunkSection[CHUNKHEIGHT];
        coord = new ChunkCoord(chunkX, chunkZ);
        generator = new TechDemoChunkGenerator(this);

        for (int i = 0; i < CHUNKHEIGHT; i++) {
            chunks[i] = new ChunkSection(world, this, chunkX, i, chunkZ);
        }

        lightMap = new LightMap(this);
    }

    /// <summary>
    /// Uses chunk coordinates
    /// </summary>
    public void setBlock(int x, int y, int z, ushort block, bool remesh = true) {
        blocks[x, y, z] = block;

        if (remesh) {
            // if it needs to be remeshed, add this and neighbouring chunksections to the remesh queue
            var sectionY = (int)MathF.Floor(y / (float)CHUNKSIZE);
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

    public void meshChunk() {
        for (int i = 0; i < CHUNKHEIGHT; i++) {
            chunks[i].renderer.meshChunk();
        }
        status = ChunkStatus.MESHED;
    }

    public void drawChunk(PlayerCamera camera) {
        for (int i = 0; i < CHUNKHEIGHT; i++) {
            chunks[i].renderer.drawChunk(camera);
        }
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

public readonly record struct ChunkCoord(int x, int z) {
    public double distance(ChunkCoord chunkCoord) {
        int dx = x - chunkCoord.x;
        int dz = z - chunkCoord.z;
        return Math.Sqrt(dx * dx + dz * dz);
    }
}

public readonly record struct ChunkSectionCoord(int x, int y, int z);
public readonly record struct RegionCoord(int x, int z);

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
    /// Chunk has been meshed
    /// </summary>
    MESHED
}