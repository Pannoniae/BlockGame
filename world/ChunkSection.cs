using Silk.NET.Maths;

namespace BlockGame;

public class ChunkSection {

    public Chunk chunk;
    public ChunkSectionRenderer renderer;

    public int chunkX;
    public int chunkY;
    public int chunkZ;

    public int worldX => chunkX * Chunk.CHUNKSIZE;
    public int worldY => chunkY * Chunk.CHUNKSIZE;
    public int worldZ => chunkZ * Chunk.CHUNKSIZE;
    public Vector3D<int> worldPos => new(worldX, worldY, worldZ);
    public Vector3D<int> centrePos => new(worldX + 8, worldY + 8, worldZ + 8);

    public AABB box;

    public World world;

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
        return chunk.blocks[x, y + chunkY * Chunk.CHUNKSIZE, z];
    }

    public void tick(int x, int y, int z) {
        var block = getBlockInChunk(x, y, z);
        if (block == Blocks.DIRT.id) {
            if (chunk.world.inWorld(x, y + 1, z) && getBlockInChunk(x, y + 1, z) == 0) {
                chunk.blocks[x, y + chunkY * Chunk.CHUNKSIZE, z] = Blocks.GRASS.id;
                renderer.meshChunk();
            }
        }

        if (block == Blocks.GRASS.id) {
            if (chunk.world.inWorld(x, y + 1, z) && getBlockInChunk(x, y + 1, z) != 0) {
                chunk.blocks[x, y + chunkY * Chunk.CHUNKSIZE, z] = Blocks.DIRT.id;
                renderer.meshChunk();
            }
        }
    }
}