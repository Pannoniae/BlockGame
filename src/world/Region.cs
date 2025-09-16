using BlockGame.world.chunk;

namespace BlockGame.world;

public class Region {
    public RegionCoord coord;

    public Dictionary<ChunkCoord, Chunk> chunks = new();
}