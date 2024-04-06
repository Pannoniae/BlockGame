using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace BlockGame;

public class ChunkSection {

    public Chunk chunk;
    public ChunkSectionRenderer renderer;

    public int chunkX;
    public int chunkY;
    public int chunkZ;

    public int worldX => chunkX * CHUNKSIZE;
    public int worldY => chunkY * CHUNKSIZE;
    public int worldZ => chunkZ * CHUNKSIZE;
    public Vector3D<int> worldPos => new(worldX, worldY, worldZ);
    public Vector3D<int> centrePos => new(worldX + 8, worldY + 8, worldZ + 8);

    public AABB box;

    public World world;
    public const int CHUNKSIZE = 16;

    public ChunkSection(World world, Chunk chunk, int xpos, int ypos, int zpos) {
        this.chunk = chunk;
        renderer = new ChunkSectionRenderer(this);
        chunkX = xpos;
        chunkY = ypos;
        chunkZ = zpos;
        this.world = world;
        box = new AABB(new Vector3D<double>(chunkX * 16, chunkY * 16, chunkZ * 16), new Vector3D<double>(chunkX * 16 + 16, chunkY * 16 + 16, chunkZ * 16 + 16));

    }

    public int getBlockInChunk(int x, int y, int z) {
        return chunk.block[x, y + chunkY * CHUNKSIZE, z];
    }

    public void tick(int x, int y, int z) {
        var block = getBlockInChunk(x, y, z);
        if (block == Blocks.DIRT.id) {
            if (chunk.world.inWorld(x, y + 1, z) && getBlockInChunk(x, y + 1, z) == 0) {
                chunk.block[x, y + chunkY * CHUNKSIZE, z] = Blocks.GRASS.id;
                renderer.meshChunk();
            }
        }
    }
}