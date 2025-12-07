using BlockGame.GL;
using BlockGame.util;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.world.chunk;

public class SubChunk {

    public Chunk chunk;
    public PaletteBlockData blocks => chunk.blocks[coord.y];
    public SubChunkCoord coord;
    public AABB box;
    
    
    public bool isRendered = false;
    public bool hasRenderOpaque = false;
    public bool hasRenderTranslucent = false;
    
    public SharedBlockVAO? vao;
    public SharedBlockVAO? watervao;

    public XUList<Vector3I> renderedBlockEntities = [];
    
    

    /// <summary>
    /// Sections start empty. If you place a block in them, they stop being empty and get array data.
    /// They won't revert to being empty if you break the Block. (maybe a low-priority background task later? I'm not gonna bother with it atm)
    /// </summary>
    public bool isEmpty => blocks.isEmpty();

    /** Returns true if this subchunk has been meshed (has VAO data). Or if we have nothing to mesh. */
    public bool isMeshed() => (vao != null || watervao != null) || isEmpty;

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
}