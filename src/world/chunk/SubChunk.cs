using System.Numerics;
using System.Runtime.CompilerServices;
using BlockGame.util;
using Silk.NET.Maths;

namespace BlockGame;

public class SubChunk : IDisposable {

    public Chunk chunk;
    public SubChunkRenderer renderer;

    public ArrayBlockData blocks;

    public int chunkX;
    public int chunkY;
    public int chunkZ;


    /// <summary>
    /// Sections start empty. If you place a block in them, they stop being empty and get array data.
    /// They won't revert to being empty if you break the blocks. (maybe a low-priority background task later? I'm not gonna bother with it atm)
    /// </summary>
    public bool isEmpty => blocks.isEmpty();

    public int worldX => chunkX * Chunk.CHUNKSIZE;
    public int worldY => chunkY * Chunk.CHUNKSIZE;
    public int worldZ => chunkZ * Chunk.CHUNKSIZE;
    public Vector3D<int> worldPos => new(worldX, worldY, worldZ);
    public Vector3D<int> centrePos => new(worldX + 8, worldY + 8, worldZ + 8);

    public AABB box;
    public BoundingBox bbbox;

    public World world;
    public ChunkSectionCoord chunkCoord => new(chunkX, chunkY, chunkZ);


    public SubChunk(World world, Chunk chunk, int xpos, int ypos, int zpos) {
        this.chunk = chunk;
        renderer = new SubChunkRenderer(this);
        chunkX = xpos;
        chunkY = ypos;
        chunkZ = zpos;
        this.world = world;
        blocks = new ArrayBlockData(chunk, this);

        box = new AABB(new Vector3D<double>(chunkX * 16, chunkY * 16, chunkZ * 16), new Vector3D<double>(chunkX * 16 + 16, chunkY * 16 + 16, chunkZ * 16 + 16));
        bbbox = new BoundingBox(box.min.toVec3(), box.max.toVec3());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort getBlockInChunk(int x, int y, int z) {
        return blocks[x, y, z];
    }

    public void tick(int x, int y, int z) {
        return;
        var block = getBlockInChunk(x, y, z);
        // todo implement proper grass spread
        if (block == Blocks.DIRT.id) {
            if (chunk.world.inWorld(x, y + 1, z) && getBlockInChunk(x, y + 1, z) == 0) {
                blocks[x, y, z] = Blocks.GRASS.id;
                renderer.meshChunk();
            }
        }

        if (block == Blocks.GRASS.id) {
            if (chunk.world.inWorld(x, y + 1, z) && getBlockInChunk(x, y + 1, z) != 0) {
                blocks[x, y, z] = Blocks.DIRT.id;
                renderer.meshChunk();
            }
        }
    }

    private void ReleaseUnmanagedResources() {
        renderer.Dispose();
        blocks.Dispose();
    }

    private void Dispose(bool disposing) {
        ReleaseUnmanagedResources();
        if (disposing) {
            renderer.Dispose();
            blocks.Dispose();
        }
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~SubChunk() {
        Dispose(false);
    }
}