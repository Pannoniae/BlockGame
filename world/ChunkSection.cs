using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.Maths;

namespace BlockGame;

public class ChunkSection {

    public Chunk chunk;
    public ChunkSectionRenderer renderer;

    public BlockData blocks;

    public int chunkX;
    public int chunkY;
    public int chunkZ;


    /// <summary>
    /// Sections start empty. If you place a block in them, they stop being empty and get array data.
    /// They won't revert to being empty if you break the blocks. (maybe a low-priority background task later? I'm not gonna bother with it atm)
    /// </summary>
    public bool isEmpty = false;

    /// <summary>
    /// isEmpty but for transparent blocks
    /// </summary>
    public bool isEmptyTransparent = false;

    public int worldX => chunkX * Chunk.CHUNKSIZE;
    public int worldY => chunkY * Chunk.CHUNKSIZE;
    public int worldZ => chunkZ * Chunk.CHUNKSIZE;
    public Vector3D<int> worldPos => new(worldX, worldY, worldZ);
    public Vector3D<int> centrePos => new(worldX + 8, worldY + 8, worldZ + 8);

    public AABB box;
    public BoundingBox bbbox;

    public World world;
    public ChunkSectionCoord chunkCoord => new(chunkX, chunkY, chunkZ);


    public ChunkSection(World world, Chunk chunk, int xpos, int ypos, int zpos) {
        this.chunk = chunk;
        renderer = new ChunkSectionRenderer(this);
        chunkX = xpos;
        chunkY = ypos;
        chunkZ = zpos;
        this.world = world;
        blocks = new ArrayBlockData(chunk);

        box = new AABB(new Vector3D<double>(chunkX * 16, chunkY * 16, chunkZ * 16), new Vector3D<double>(chunkX * 16 + 16, chunkY * 16 + 16, chunkZ * 16 + 16));
        bbbox = new BoundingBox(box.min.toVec3(), box.max.toVec3());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort getBlockInChunk(int x, int y, int z) {
        return blocks[x, y, z];
    }

    public byte getLight(int x, int y, int z) {
        return blocks.getLight(x, y, z);
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
}