using System.Numerics;

namespace BlockGame;

public class Chunk {
    public ChunkStatus status;

    public ushort[,,] block;
    public int x;
    public int z;
    public ChunkSection[] chunks;
    public World world;


    public Chunk(World world, int x, int z) {
        block = new ushort[ChunkSection.CHUNKSIZE, ChunkSection.CHUNKSIZE * CHUNKHEIGHT, ChunkSection.CHUNKSIZE];
        this.world = world;
        chunks = new ChunkSection[CHUNKHEIGHT];
        this.x = x;
        this.z = z;

        for (int i = 0; i < CHUNKHEIGHT; i++) {
            chunks[i] = new ChunkSection(world, this, x, i, z);
        }
    }

    public void meshChunk() {
        for (int i = 0; i < CHUNKHEIGHT; i++) {
            chunks[i].renderer.meshChunk();
        }
    }

    public const int CHUNKHEIGHT = 8;

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

public record struct ChunkCoord(int x, int z);

public record struct ChunkSectionCoord(int x, int y, int z);

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