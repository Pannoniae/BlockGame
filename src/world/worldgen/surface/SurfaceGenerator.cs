using BlockGame.util;
using BlockGame.world.chunk;

namespace BlockGame.world.worldgen.surface;

public interface SurfaceGenerator {
    public void surface(XRandom random, ChunkCoord coord);
    void setup(XRandom random, int seed);
}