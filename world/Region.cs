namespace BlockGame;

public class Region {
    public RegionCoord coord;

    public Dictionary<ChunkCoord, Chunk> chunks = new();
}