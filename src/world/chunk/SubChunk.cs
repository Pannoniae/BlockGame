using BlockGame.GL;
using BlockGame.util;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame;

public class SubChunk {

    public Chunk chunk;
    public ArrayBlockData blocks => chunk.blocks[coord.y];
    public SubChunkCoord coord;
    public AABB box;
    
    
    public bool isRendered = false;
    public bool hasRenderOpaque = false;
    public bool hasRenderTranslucent = false;
    
    public SharedBlockVAO? vao;
    public SharedBlockVAO? watervao;
    
    

    /// <summary>
    /// Sections start empty. If you place a block in them, they stop being empty and get array data.
    /// They won't revert to being empty if you break the Block. (maybe a low-priority background task later? I'm not gonna bother with it atm)
    /// </summary>
    public bool isEmpty => blocks.isEmpty();

    public int worldX => coord.x << 4;
    public int worldY => coord.y << 4;
    public int worldZ => coord.z << 4;
    public Vector3I worldPos => new(worldX, worldY, worldZ);
    public Vector3I centrePos => new(worldX + 8, worldY + 8, worldZ + 8);
    

    public SubChunk(World world, Chunk chunk, int xpos, int ypos, int zpos) {
        this.chunk = chunk;
        coord = new SubChunkCoord(xpos, ypos, zpos);
        box = new AABB(new Vector3D(xpos * 16, ypos * 16, zpos * 16), new Vector3D(xpos * 16 + 16, ypos * 16 + 16, zpos * 16 + 16));
    }

    public void tick(World world, XRandom random, int x, int y, int z) {
        var block = blocks[x, y, z];
        var worldPos = World.toWorldPos(coord.x, coord.y, coord.z, x, y, z);

        if (block == Blocks.GRASS) {
            if (y < 127 && Block.isFullBlock(world.getBlock(worldPos.X, worldPos.Y + 1, worldPos.Z))) {
                blocks[x, y, z] = Blocks.DIRT;
                // dirty block
                world.setBlockNeighboursDirty(worldPos);
            }
            // spread grass to nearby dirt blocks
            // in a 3x3x3 area
            var r = Meth.getRandomCoord(random, 6, 6, 6);
            var coord = World.toWorldPos(this.coord.x, this.coord.y, this.coord.z, r.X, r.Y, r.Z);
            var x1 = coord.X;
            var y1 = coord.Y;
            var z1 = coord.Z;
            // if dirt + air above
            if (world.getBlock(x + x1 - 3, y + y1 - 3, z + z1 - 3) == Blocks.DIRT && world.getBlock(x + x1 - 3, y + y1 - 2, z + z1 - 3) == Blocks.AIR) {
                world.setBlockDumb(x + x1 - 3, y + y1 - 3, z + z1 - 3, Blocks.GRASS);
                // dirty this chunk
                world.dirtyChunk(this.coord);
            }
        }
    }
}