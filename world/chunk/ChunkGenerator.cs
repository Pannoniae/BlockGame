namespace BlockGame;

public interface ChunkGenerator {
    public void generate(ChunkCoord coord);
    public void populate(ChunkCoord coord);
}