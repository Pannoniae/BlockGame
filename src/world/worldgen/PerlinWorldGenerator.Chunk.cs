using BlockGame.util;

namespace BlockGame;

public partial class PerlinWorldGenerator {
    // The noise is sampled every X blocks
    public const int NOISE_PER_X = 4;
    public const int NOISE_PER_Y = 4;
    public const int NOISE_PER_Z = 4;

    public const int NOISE_SIZE_X = (Chunk.CHUNKSIZE / NOISE_PER_X) + 1;
    public const int NOISE_SIZE_Y = (Chunk.CHUNKSIZE * Chunk.CHUNKHEIGHT) / NOISE_PER_Y + 1;
    public const int NOISE_SIZE_Z = (Chunk.CHUNKSIZE / NOISE_PER_Z) + 1;

    public const int WATER_LEVEL = 64;

    private readonly double[] buffer = new double[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];

    public void generate(ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        getDensity(buffer, coord);
        interpolate(buffer, coord);

        chunk.recalc();
        chunk.status = ChunkStatus.GENERATED;
    }


    public void getDensity(double[] buffer, ChunkCoord coord) {
        var chunk = world.getChunk(coord);

        for (int nx = 0; nx < NOISE_SIZE_X; nx++) {
            for (int ny = 0; ny < NOISE_SIZE_Y; ny++) {
                for (int nz = 0; nz < NOISE_SIZE_Z; nz++) {
                    // restore the actual coordinates (to see where we sample at)
                    var x = coord.x * Chunk.CHUNKSIZE + nx * NOISE_PER_X;
                    var y = ny * NOISE_PER_Y;
                    var z = coord.z * Chunk.CHUNKSIZE + nz * NOISE_PER_Z;

                    // sample lowNoise
                    var low = get3DNoise(lowNoise, x, y, z, 4, 0.5);
                    // sample highNoise
                    var high = get3DNoise(highNoise, x, y, z, 4, 0.5);
                    // sample selectorNoise
                    var selector = get3DNoise(selectorNoise, x, y, z, 4, 0.5);
                    // combine the two
                    var value = low + high * selector;
                    // store the value in the buffer
                    buffer[getIndex(nx, ny, nz)] = value;
                }
            }
        }
    }

    private void interpolate(double[] buffer, ChunkCoord coord) {
        var chunk = world.getChunk(coord);

        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            for (int y = 0; y < Chunk.CHUNKSIZE * Chunk.CHUNKHEIGHT; y++) {
                for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                    // the grid cell that contains the point
                    var x0 = x / NOISE_PER_X;
                    var y0 = y / NOISE_PER_Y;
                    var z0 = z / NOISE_PER_Z;

                    var x1 = x0 + 1;
                    var y1 = y0 + 1;
                    var z1 = z0 + 1;

                    // the lerp (between 0 and 1)
                    double xd = (x % NOISE_PER_X) / (double)NOISE_PER_X;
                    double yd = (y % NOISE_PER_Y) / (double)NOISE_PER_Y;
                    double zd = (z % NOISE_PER_Z) / (double)NOISE_PER_Z;

                    // the eight corner values from the buffer
                    var c000 = buffer[getIndex(x0, y0, z0)];
                    var c001 = buffer[getIndex(x0, y0, z1)];
                    var c010 = buffer[getIndex(x0, y1, z0)];
                    var c011 = buffer[getIndex(x0, y1, z1)];
                    var c100 = buffer[getIndex(x1, y0, z0)];
                    var c101 = buffer[getIndex(x1, y0, z1)];
                    var c110 = buffer[getIndex(x1, y1, z0)];
                    var c111 = buffer[getIndex(x1, y1, z1)];

                    // Interpolate along x
                    var c00 = lerp(c000, c100, xd);
                    var c01 = lerp(c001, c101, xd);
                    var c10 = lerp(c010, c110, xd);
                    var c11 = lerp(c011, c111, xd);

                    // Interpolate along y
                    var c0 = lerp(c00, c10, yd);
                    var c1 = lerp(c01, c11, yd);

                    // Interpolate along z
                    var value = lerp(c0, c1, zd);

                    if (value > 0) {
                        chunk.setBlockFast(x, y, z, Block.STONE.id);
                    }
                }
            }
        }
    }

    private int getIndex(int x, int y, int z) {
        return x + y * NOISE_SIZE_X + z * NOISE_SIZE_X * NOISE_SIZE_Y;
    }

    private double lerp(double a, double b, double t) {
        return a + t * (b - a);
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