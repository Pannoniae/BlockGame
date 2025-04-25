using BlockGame.util;

namespace BlockGame;

public partial class PerlinWorldGenerator {

    public const int WATER_LEVEL = 64;

    public void generate(ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        chunk.status = ChunkStatus.GENERATED;
    }

    public void populate(ChunkCoord coord) {
        var random = getRandom(coord);
        var chunk = world.getChunk(coord);
        
        chunk.status = ChunkStatus.POPULATED;
    }

    public XRandom getRandom(ChunkCoord coord) {
        return new XRandom(coord.GetHashCode());
    }
}