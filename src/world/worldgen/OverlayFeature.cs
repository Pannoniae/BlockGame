using BlockGame.util;

namespace BlockGame;

/// <summary>
/// A worldgen feature which checks itself in a grid around the populated chunk.
/// Not very efficient, but it works.:tm:
/// </summary>
public abstract class OverlayFeature {
    public XRandom rand = new();
    
    /// <summary>
    /// How many chunks to check around the populated chunk.
    /// </summary>
    public int radius = 8;

    public void place(World world, ChunkCoord coord) {
        // we want to seed this unique to the world seed but also take into account the chunk
        var seed = (int)(coord.GetHashCode() + world.seed);
        rand.Seed(seed);
    }
    
    /// <summary>
    /// It is YOUR responsibility to cap the actually placed blocks to the origin chunk.
    /// </summary>
    /// <param name="world"></param>
    /// <param name="coord">The chunk we are checking right now</param>
    /// <param name="origin">The chunk we should actually generate in</param>
    public abstract void generate(World world, ChunkCoord coord, ChunkCoord origin);

}