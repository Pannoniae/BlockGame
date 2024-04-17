using System.Numerics;
using Silk.NET.Maths;

namespace BlockGame;

public class Chunk {
    public ChunkStatus status;

    public ushort[,,] blocks;
    public int chunkX;
    public int chunkZ;
    public ChunkSection[] chunks;
    public World world;

    public ChunkGenerator generator;

    public const int CHUNKHEIGHT = 8;
    public const int CHUNKSIZE = 16;


    public Chunk(World world, int chunkX, int chunkZ) {
        blocks = new ushort[CHUNKSIZE, CHUNKSIZE * CHUNKHEIGHT, CHUNKSIZE];
        this.world = world;
        chunks = new ChunkSection[CHUNKHEIGHT];
        this.chunkX = chunkX;
        this.chunkZ = chunkZ;
        generator = new ChunkGenerator(this);

        for (int i = 0; i < CHUNKHEIGHT; i++) {
            chunks[i] = new ChunkSection(world, this, chunkX, i, chunkZ);
        }
    }

    /// <summary>
    /// Uses chunk coordinates
    /// </summary>
    public void setBlock(int x, int y, int z, ushort block, bool remesh = true) {
        blocks[x, y, z] = block;

        if (remesh) {
            meshChunk();
            
            // get global coords
            var worldPos = world.toWorldPos(chunkX, 0, chunkZ, x, y, z);
            var chunkPos = world.getChunkSectionPos(worldPos);

            foreach (var dir in Direction.directions) {
                var neighbourSection = world.getChunkSectionPos(worldPos + dir);
                if (world.isChunkSectionInWorld(neighbourSection) && neighbourSection != chunkPos) {
                    world.getChunkByChunkPos(new ChunkCoord(neighbourSection.x, neighbourSection.z)).chunks[neighbourSection.y].renderer.meshChunk();
                }
            }
        }
    }

    public void meshChunk() {
        for (int i = 0; i < CHUNKHEIGHT; i++) {
            chunks[i].renderer.meshChunk();
        }
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