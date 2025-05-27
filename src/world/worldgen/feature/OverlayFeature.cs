using BlockGame.util;

namespace BlockGame;

/// <summary>
/// A worldgen feature which checks itself in a grid around the populated chunk.
/// Not very efficient, but it works.:tm:
/// </summary>
public abstract class OverlayFeature {
    public readonly XRandom rand = new();

    /// <summary>
    /// How many chunks to check around the populated chunk.
    /// </summary>
    public virtual int radius => 8;

    public void place(World world, ChunkCoord coord) {
        // we want to seed this unique to the world seed but also take into account the chunk
        //var seed = coord.GetHashCode() ^ world.seed;
        //rand.Seed(seed);
        for (int xd = -radius; xd <= radius; xd++) {
            for (int zd = -radius; zd <= radius; zd++) {
                var checkCoord = new ChunkCoord(coord.x + xd, coord.z + zd);

                // reseed based on the original seed - we need this to be consistent for the generation
                rand.Seed(checkCoord.GetHashCode() ^ world.seed);
                //rand.Seed(checkCoord.GetHashCode());
                //Console.Out.WriteLine(world.seed);

                // only do it in a circle!! we don't actually need to do the whole square because the cave VERY likely won't extend diagonally that far.
                // if ppl complain about cutoff caves or whatever we can increase the radius or something
                if (xd * xd + zd * zd > radius * radius) {
                    continue;
                }
                //if (checkCoord.x == 0 && checkCoord.z == 0) {
                    // if we are at the origin chunk, we can generate in the same chunk
                generate(world, rand, checkCoord, coord);
                //}
            }
        }
    }

    /// <summary>
    /// It is YOUR responsibility to cap the actually placed blocks to the origin chunk.
    /// </summary>
    /// <param name="world"></param>
    /// <param name="rand"></param>
    /// <param name="coord">The chunk we are checking right now</param>
    /// <param name="origin">The chunk we should actually generate in</param>
    public abstract void generate(World world, XRandom rand, ChunkCoord coord, ChunkCoord origin);

}
