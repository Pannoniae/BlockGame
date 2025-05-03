using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using BlockGame.id;
using BlockGame.util;

namespace BlockGame;

public partial class PerlinWorldGenerator {
    // The noise is sampled every X blocks
    public const int NOISE_PER_X = 4;
    public const int NOISE_PER_Y = 4;
    public const int NOISE_PER_Z = 4;

    // mod 4 == & 3
    const int NOISE_PER_X_MASK = 3;
    const int NOISE_PER_Y_MASK = 3;
    const int NOISE_PER_Z_MASK = 3;

    // div 4 == >> 2
    const int NOISE_PER_X_SHIFT = 2;
    const int NOISE_PER_Y_SHIFT = 2;
    const int NOISE_PER_Z_SHIFT = 2;

    // x / y = x * (1 / y)
    const double NOISE_PER_X_INV = 1d / NOISE_PER_X;
    const double NOISE_PER_Y_INV = 1d / NOISE_PER_Y;
    const double NOISE_PER_Z_INV = 1d / NOISE_PER_Z;

    public const int NOISE_SIZE_X = (Chunk.CHUNKSIZE / NOISE_PER_X) + 1;
    public const int NOISE_SIZE_Y = (Chunk.CHUNKSIZE * Chunk.CHUNKHEIGHT) / NOISE_PER_Y + 1;
    public const int NOISE_SIZE_Z = (Chunk.CHUNKSIZE / NOISE_PER_Z) + 1;

    public const int WATER_LEVEL = 64;

    private readonly double[] buffer = new double[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private readonly float[] lowBuffer = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private readonly float[] highBuffer = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private readonly float[] selectorBuffer = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private readonly float[] auxBuffer = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private readonly float[] foliageBuffer = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private readonly float[] temperatureBuffer = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private readonly float[] humidityBuffer = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];

    private readonly Cave caves = new();

    public const double LOW_FREQUENCY = 1 / 292d;
    public const double HIGH_FREQUENCY = 1 / 135d;
    public const double SELECTOR_FREQUENCY = 1 / 490d;

    public const double Y_DIVIDER = 1 / 16d;

    public const double BLOCK_VARIATION_FREQUENCY = 1 / 412d;
    public const double HELLSTONE_FREQUENCY = 1 / 1.5d;

    public const double FOLIAGE_FREQUENCY = 1 / 69d;

    public void generate(ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        getDensity(buffer, coord);
        interpolate(buffer, coord);
        //Console.Out.WriteLine(coord);
        generateSurface(coord);

        chunk.recalc();
        chunk.status = ChunkStatus.GENERATED;
    }


    public void getDensity(double[] buffer, ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        // get the noise
        getNoise3DRegion(lowBuffer, lowNoise, coord, LOW_FREQUENCY, LOW_FREQUENCY * Y_DIVIDER,
            LOW_FREQUENCY, 8, 2f);
        getNoise3DRegion(highBuffer, highNoise, coord, HIGH_FREQUENCY, HIGH_FREQUENCY,
            HIGH_FREQUENCY, 8, 2f);
        getNoise3DRegion(selectorBuffer, selectorNoise, coord, SELECTOR_FREQUENCY, SELECTOR_FREQUENCY,
            SELECTOR_FREQUENCY, 2, 2f);

        for (int ny = 0; ny < NOISE_SIZE_Y; ny++) {
            for (int nz = 0; nz < NOISE_SIZE_Z; nz++) {
                for (int nx = 0; nx < NOISE_SIZE_X; nx++) {
                    // restore the actual coordinates (to see where we sample at)
                    var x = coord.x * Chunk.CHUNKSIZE + nx * NOISE_PER_X;
                    var y = ny * NOISE_PER_Y;
                    var z = coord.z * Chunk.CHUNKSIZE + nz * NOISE_PER_Z;

                    // sample lowNoise
                    double low = lowBuffer[getIndex(nx, ny, nz)];
                    // sample highNoise
                    double high = highBuffer[getIndex(nx, ny, nz)];
                    // sample selectorNoise
                    double selector = selectorBuffer[getIndex(nx, ny, nz)];
                    // make it more radical
                    // can't sqrt a negative number so sign(abs(x))
                    selector = double.Abs(selector);

                    // sin it
                    //selector = 0.5 * double.SinPi(selector - 0.5) + 0.5;

                    selector *= 2;
                    selector = double.Clamp(selector, 0, 1);

                    //Console.Out.WriteLine("b: " + selector);

                    // we only want mountains when selector is high

                    // squish selector towards zero - more flat areas, less mountains
                    //selector *= selector;

                    // squish the high noise towards zero when the selector is low - more flat areas, less mountains

                    // Reduce the density when too high above 64 and increase it when too low
                    var airBias = (y - WATER_LEVEL) / (double)World.WORLDHEIGHT;

                    // flatten out low noise
                    //low -= airBias;

                    // reduce it below ground (is water anyway, useless)
                    if (airBias < 0) {
                        airBias *= 4;
                    }

                    // only sample if actually required

                    // combine the two
                    double density = low + (high - low) * selector;
                    density -= airBias;

                    // store the value in the buffer
                    buffer[getIndex(nx, ny, nz)] = density;
                }
            }
        }
    }

    private void interpolate(double[] buffer, ChunkCoord coord) {
        var chunk = world.getChunk(coord);

        for (int y = 0; y < Chunk.CHUNKSIZE * Chunk.CHUNKHEIGHT; y++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                    // the grid cell that contains the point
                    var x0 = x >> NOISE_PER_X_SHIFT;
                    var y0 = y >> NOISE_PER_Y_SHIFT;
                    var z0 = z >> NOISE_PER_Z_SHIFT;

                    var x1 = x0 + 1;
                    var y1 = y0 + 1;
                    var z1 = z0 + 1;

                    // the lerp (between 0 and 1)
                    // double xd = (x % NOISE_PER_X) / (double)NOISE_PER_X;
                    double xd = (x & NOISE_PER_X_MASK) * NOISE_PER_X_INV;
                    double yd = (y & NOISE_PER_X_MASK) * NOISE_PER_Y_INV;
                    double zd = (z & NOISE_PER_X_MASK) * NOISE_PER_Z_INV;

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

                    // if below sea level, water
                    if (value > 0) {
                        chunk.setBlockFast(x, y, z, Blocks.STONE);
                        chunk.addToHeightMap(x, y, z);
                    }
                    else {
                        if (y < WATER_LEVEL) {
                            chunk.setBlockFast(x, y, z, Blocks.WATER);
                            chunk.addToHeightMap(x, y, z);
                        }
                    }
                }
            }
        }
    }

    public void generateSurface(ChunkCoord coord) {
        var chunk = world.getChunk(coord);

        for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                var worldPos = World.toWorldPos(chunk.coord.x, chunk.coord.z, x, 0, z);
                int height = chunk.heightMap.get(x, z);

                // move down until it's solid
                while (height > 0 && !Block.fullBlock[chunk.getBlock(x, height, z)]) {
                    height--;
                }

                // thickness of the soil layer layer
                var amt = getNoise(auxNoise, worldPos.X, worldPos.Z, 1, 1) + 2.5;
                var blockVar = getNoise3D(auxNoise, worldPos.X * BLOCK_VARIATION_FREQUENCY,
                    128,
                    worldPos.Z * BLOCK_VARIATION_FREQUENCY,
                    1, 1);

                ushort topBlock = 0;
                ushort filler = 0;
                // determine the top layer
                if (height < WATER_LEVEL - 1) {
                    if (blockVar > 0) {
                        topBlock = Blocks.DIRT;
                        filler = Blocks.DIRT;
                    }
                    else {
                        topBlock = Blocks.SAND;
                        filler = Blocks.SAND;
                    }
                }

                // beaches
                else if (height > WATER_LEVEL - 3 && height < WATER_LEVEL + 1) {
                    if (blockVar > -0.2) {
                        topBlock = Blocks.SAND;
                        filler = Blocks.SAND;
                    }
                    else {
                        topBlock = Blocks.GRAVEL;
                        filler = Blocks.GRAVEL;
                    }
                }
                else {
                    topBlock = Blocks.GRASS;
                    filler = Blocks.DIRT;
                }

                // replace top layers with dirt
                // if it's rock (otherwise it's water which we don't wanna replace)
                if (chunk.getBlock(x, height, z) == Blocks.STONE) {
                    // replace top layer with topBlock
                    chunk.setBlockFast(x, height, z, topBlock);
                    for (int yy = height - 1; yy > height - 1 - amt && yy > 0; yy--) {
                        // replace stone with dirt
                        if (chunk.getBlock(x, yy, z) == Blocks.STONE) {
                            chunk.setBlockFast(x, yy, z, filler);
                        }
                    }
                }
            }
        }
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int getIndex(int x, int y, int z) {
        return x + z * NOISE_SIZE_X + y * NOISE_SIZE_X * NOISE_SIZE_Z;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double lerp(double a, double b, double t) {
        return a + t * (b - a);
    }

    private static Vector256<double> lerp4(Vector256<double> a, Vector256<double> b, Vector256<double> t) {
        return a + t * (b - a);
    }

    public void populate(ChunkCoord coord) {
        var random = getRandom(coord);
        var chunk = world.getChunk(coord);

        var xChunk = coord.x * Chunk.CHUNKSIZE;
        var zChunk = coord.z * Chunk.CHUNKSIZE;

        // place hellstone on bottom of the world
        // height should be between 1 and 4
        for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
            for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                
                var xWorld = coord.x * Chunk.CHUNKSIZE + x;
                var zWorld = coord.z * Chunk.CHUNKSIZE + z;
                
                var height =
                    getNoise(auxNoise, -xWorld * HELLSTONE_FREQUENCY, -zWorld * HELLSTONE_FREQUENCY, 1, 1) * 4 + 2;
                height = float.Clamp(height, 1, 5);
                for (int y = 0; y < height; y++) {

                    chunk.setBlockFast(x, y, z, Block.HELLSTONE.id);
                    chunk.setBlockLight(x, y, z, 15);
                }
            }
        }

        var foliage = getNoise(foliageNoise, xChunk * FOLIAGE_FREQUENCY, zChunk * FOLIAGE_FREQUENCY, 2, 2);
        var treeCount = foliage;
        if (foliage < 0) {
            treeCount = 0;
        }

        treeCount *= 10;
        treeCount *= treeCount;

        for (int i = 0; i < treeCount; i++) {
            placeTree(random, coord);
        }

        // Do caves
        caves.place(world, coord);

        chunk.status = ChunkStatus.POPULATED;
    }

    private void placeTree(XRandom random, ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        var x = random.Next(0, Chunk.CHUNKSIZE);
        var z = random.Next(0, Chunk.CHUNKSIZE);
        var y = chunk.heightMap.get(x, z);

        if (y > 120) {
            return;
        }

        // if not on dirt, don't bother
        if (chunk.getBlock(x, y, z) != Block.GRASS.id) {
            return;
        }

        var xWorld = coord.x * Chunk.CHUNKSIZE + x;
        var zWorld = coord.z * Chunk.CHUNKSIZE + z;

        // if there's stuff in the bounding box, don't place a tree
        for (int yd = 1; yd < 8; yd++) {
            for (int zd = -2; zd <= 2; zd++) {
                for (int xd = -2; xd <= 2; xd++) {
                    if (world.getBlock(xWorld, y + yd, zWorld) != Block.AIR.id) {
                        return;
                    }
                }
            }
        }

        placeOakTree(random, x + coord.x * Chunk.CHUNKSIZE, y + 1, z + coord.z * Chunk.CHUNKSIZE);
    }

    public void placeOakTree(XRandom random, int x, int y, int z) {
        int randomNumber = random.Next(5, 8);
        for (int i = 0; i < randomNumber; i++) {
            world.setBlock(x, y + i, z, Block.LOG.id);
            // leaves, thick
            for (int x1 = -2; x1 <= 2; x1++) {
                for (int z1 = -2; z1 <= 2; z1++) {
                    // don't overwrite the trunk
                    if (x1 == 0 && z1 == 0) {
                        continue;
                    }

                    for (int y1 = randomNumber - 2; y1 <= randomNumber - 1; y1++) {
                        world.setBlock(x + x1, y + y1, z + z1, Block.LEAVES.id);
                    }
                }
            }

            // leaves, thin on top
            for (int x1 = -1; x1 <= 1; x1++) {
                for (int z1 = -1; z1 <= 1; z1++) {
                    for (int y1 = randomNumber; y1 <= randomNumber + 1; y1++) {
                        world.setBlock(x + x1, y + y1, z + z1, Block.LEAVES.id);
                    }
                }
            }
        }
    }

    public XRandom getRandom(ChunkCoord coord) {
        return new XRandom(coord.GetHashCode());
    }
}