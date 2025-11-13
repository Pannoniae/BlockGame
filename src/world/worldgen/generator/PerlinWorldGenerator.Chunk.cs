using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.chunk;
using BlockGame.world.worldgen.feature;

namespace BlockGame.world.worldgen.generator;

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
    const float NOISE_PER_X_INV = 1f / NOISE_PER_X;
    const float NOISE_PER_Y_INV = 1f / NOISE_PER_Y;
    const float NOISE_PER_Z_INV = 1f / NOISE_PER_Z;

    public const int NOISE_SIZE_X = (Chunk.CHUNKSIZE / NOISE_PER_X) + 1;
    public const int NOISE_SIZE_Y = (Chunk.CHUNKSIZE * Chunk.CHUNKHEIGHT) / NOISE_PER_Y + 1;
    public const int NOISE_SIZE_Z = (Chunk.CHUNKSIZE / NOISE_PER_Z) + 1;

    public const int WATER_LEVEL = 64;

    private float[] buffer = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private float[] lowBuffer = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private float[] highBuffer = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private float[] selectorBuffer = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private float[] weirdnessBuffer = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];

    private readonly Cave caves = new();
    private readonly Ravine ravines = new();
    private readonly OreFeature ironOre = new(Block.IRON_ORE.id, 6, 12);
    private readonly OreFeature coalOre = new(Block.COAL_ORE.id, 8, 16);

    public const float LOW_FREQUENCY = 1 / 167f;
    public const float HIGH_FREQUENCY = 1 / 135f;
    public const float SELECTOR_FREQUENCY = 1 / 390f;

    public const float WEIRDNESS_FREQUENCY = 1 / 590f;

    public const float Y_DIVIDER = 1;

    public const float BLOCK_VARIATION_FREQUENCY = 1 / 412f;
    public const float HELLROCK_FREQUENCY = 1 / 1.5f;

    public const float FOLIAGE_FREQUENCY = 1 / 169f;

    public void generate(ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        getDensity(buffer, coord);
        WorldgenUtil.interpolate(world, buffer, coord);
        //Console.Out.WriteLine(coord);
        generateSurface(coord);

        chunk.status = ChunkStatus.GENERATED;
    }


    public void getDensity(float[] buffer, ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        // get the noise
        WorldgenUtil.getNoise3DRegion(lowBuffer, lowNoise, coord, LOW_FREQUENCY, LOW_FREQUENCY * Y_DIVIDER,
            LOW_FREQUENCY, 12, 2f);
        WorldgenUtil.getNoise3DRegion(highBuffer, highNoise, coord, HIGH_FREQUENCY, HIGH_FREQUENCY,
            HIGH_FREQUENCY, 12, Meth.phiF);

        // get weirdness
        // only low octaves for this one, it's a glorified selector
        WorldgenUtil.getNoise3DRegion(weirdnessBuffer, weirdnessNoise, coord, WEIRDNESS_FREQUENCY, WEIRDNESS_FREQUENCY,
            WEIRDNESS_FREQUENCY, 4, Meth.phiF);


        // todo the selector could be sampled less frequently because it doesn't need to be as precise
        // also sample it less in the Y axis too because it should be 2d you know?
        // also todo migrate it to valuecubic somehow? yes the broken valuecubic octave summer combined with simplex
        // produces the actually cool results BUT we could cook something up with maths to make it more extreme and stuff
        // basically it should be mostly 0 (flat) or 1 (quirky shit) with transitions inbetween
        // im sure we can cook something up mr white
        WorldgenUtil.getNoise3DRegion(selectorBuffer, selectorNoise, coord, SELECTOR_FREQUENCY, SELECTOR_FREQUENCY,
            SELECTOR_FREQUENCY, 4, Meth.etaF);

        for (int ny = 0; ny < NOISE_SIZE_Y; ny++) {
            for (int nz = 0; nz < NOISE_SIZE_Z; nz++) {
                for (int nx = 0; nx < NOISE_SIZE_X; nx++) {
                    // restore the actual coordinates (to see where we sample at)
                    //var x = coord.x * Chunk.CHUNKSIZE + nx * NOISE_PER_X;
                    var y = ny * NOISE_PER_Y;
                    //var z = coord.z * Chunk.CHUNKSIZE + nz * NOISE_PER_Z;

                    // sample lowNoise
                    float low = lowBuffer[WorldgenUtil.getIndex(nx, ny, nz)];
                    // sample highNoise
                    float high = highBuffer[WorldgenUtil.getIndex(nx, ny, nz)];
                    // sample selectorNoise
                    float selector = selectorBuffer[WorldgenUtil.getIndex(nx, ny, nz)];

                    float weirdness = weirdnessBuffer[WorldgenUtil.getIndex(nx, ny, nz)];

                    // store the value in the buffer
                    buffer[WorldgenUtil.getIndex(nx, ny, nz)] = calculateDensity(low, high, selector, weirdness, y);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float calculateAirBias(float low, float high, float weirdness, int y) {
        // Reduce the density when too high above 64 and increase it when too low
        // range: // -0.5 (at y=128) to 0.5 (at y=0)
        var airBias = (y - WATER_LEVEL - 4) / (float)World.WORLDHEIGHT;
        // our SIGNATURE weird terrain
        //airBias *= 0.5f;
        // normalish terrain (kinda like old mc?)
        //airBias *= 1f;
        // fairly normal terrain, but its like a fucking warzone, littered with caves. if we want a normalish/realistic-looking terrain like that,
        // we should decrease the cave density in the lowlands because otherwise it will be cancer to traverse/build on
        //airBias *= 2f;

        // flatten out low noise
        // is this needed?
        //low -= airBias;

        // todo when making normal terrain, raise the "sealevel" (the midpoint of the terrain) by like ~4 blocks, so there will be actual plains instead of
        // just endless beaches and shallow water everywhere  

        // reduce it below ground (is water anyway, useless)
        if (y < WATER_LEVEL + 4) {
            airBias *= 4;
        }

        // border
        if (y is < 44 and > 36) {
            // make it more extreme
            airBias *= int.Max(44 - y, y - 36) - 2;
        }

        // under y=40
        var caveDepth = float.Max(0, float.Min(y - 6, 36 - y));

        // combine weirdness with highnoise so the actual cave isnt just slop
        var cw = weirdness + 0.45f * high;

        // if weirdness noise is high enough (>0.5), multiply the caveDepth bias by the remainder * 3
        var factor = float.Abs(cw) > 0.5f ? ((float.Abs(cw) - 0.5f) * 6f) + 1f : 0f;
        airBias += (caveDepth * 0.036f) * factor;


        // between y=120-128, taper it off to -1
        // at max should be 0.5
        // ^2 to have a better taper
        var t = float.Max((y - 120), 0) / 16f;
        airBias += t * t;

        return airBias;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float calculateDensity(float low, float high, float selector, float weirdness, int y) {
        low = float.Tan(low);
        high = float.Tan(high);

        // make it more radical
        // can't sqrt a negative number so sign(abs(x))
        selector = float.Abs(selector);

        // sin it
        //selector = 0.5 * float.SinPi(selector - 0.5) + 0.5;

        selector *= 2;
        //high *= 2;
        selector = float.Clamp(selector, 0, 1);

        //Console.Out.WriteLine("b: " + selector);

        // we only want mountains when selector is high

        // squish selector towards zero - more flat areas, less mountains
        //selector *= selector;

        // squish the high noise towards zero when the selector is low - more flat areas, less mountains
        var bias = calculateAirBias(low, high, weirdness, y);
        low -= bias;
        // combine the two with the ratio selector
        float density = WorldgenUtil.lerp(low, high, selector);
        density -= calculateAirBias(low, high, weirdness, y);

        return density;
    }

    public void generateSurface(ChunkCoord coord) {
        var chunk = world.getChunk(coord);

        for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
            for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                var worldPos = World.toWorldPos(chunk.coord.x, chunk.coord.z, x, 0, z);
                int height = chunk.heightMap.get(x, z);

                // move down until it's solid
                while (height > 0 && !Block.fullBlock[chunk.getBlock(x, height, z)]) {
                    height--;
                }

                // thickness of the soil layer
                var amt = WorldgenUtil.getNoise2D(auxNoise, worldPos.X, worldPos.Z, 1, 1) + 2.5;
                var blockVar = WorldgenUtil.getNoise3DFBm(auxNoise, worldPos.X * BLOCK_VARIATION_FREQUENCY,
                    128,
                    worldPos.Z * BLOCK_VARIATION_FREQUENCY,
                    1, 1);

                ushort topBlock = 0;
                ushort filler = 0;
                // determine the top layer
                if (height < WATER_LEVEL - 1) {
                    if (blockVar > 0) {
                        topBlock = Block.DIRT.id;
                        filler = Block.DIRT.id;
                    }
                    else {
                        topBlock = Block.SAND.id;
                        filler = Block.SAND.id;
                    }
                }

                // beaches
                else if (height > WATER_LEVEL - 3 && height < WATER_LEVEL + 1) {
                    if (blockVar > -0.2) {
                        topBlock = Block.SAND.id;
                        filler = Block.SAND.id;
                    }
                    else {
                        topBlock = Block.GRAVEL.id;
                        filler = Block.GRAVEL.id;
                    }
                }
                else {
                    topBlock = Block.GRASS.id;
                    filler = Block.DIRT.id;
                }

                // replace top layers with dirt
                // if it's rock (otherwise it's water which we don't wanna replace)
                if (chunk.getBlock(x, height, z) == Block.STONE.id) {
                    // replace top layer with topBlock
                    chunk.setBlockFast(x, height, z, topBlock);
                    for (int yy = height - 1; yy > height - 1 - amt && yy > 0; yy--) {
                        // replace stone with dirt
                        if (chunk.getBlock(x, yy, z) == Block.STONE.id) {
                            chunk.setBlockFast(x, yy, z, filler);
                        }
                    }
                }
            }
        }
    }

    public void surface(ChunkCoord coord) {
        var chunk = world.getChunk(coord);

        var xWorld = coord.x * Chunk.CHUNKSIZE;
        var zWorld = coord.z * Chunk.CHUNKSIZE;

        // set seed to chunk
        random.Seed(coord.GetHashCode());

        // place hellrock on bottom of the world
        // height should be between 1 and 4
        for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
            for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                var xs = xWorld + x;
                var zs = zWorld + z;

                var height =
                    WorldgenUtil.getNoise2D(auxNoise, -xs * HELLROCK_FREQUENCY, -zs * HELLROCK_FREQUENCY, 1, 1) * 4 + 2;
                height = float.Clamp(height, 1, 5);
                for (int y = 0; y < height; y++) {
                    chunk.setBlockFast(x, y, z, Block.HELLROCK.id);
                }
            }
        }

        surfacegen.surface(random, coord);


        chunk.status = ChunkStatus.POPULATED;
    }

    /**
     * Sample all noise buffers at a specific world position.
     * Used for /noise command debugging.
     */
    public string sample(int wx, int wy, int wz) {
        var coord = World.getChunkPos(wx, wz);

        // run getDensity to populate buffers
        getDensity(buffer, coord);

        var cx = wx - (coord.x << 4);
        var cz = wz - (coord.z << 4);
        if (cx < 0) cx += 16;
        if (cz < 0) cz += 16;

        return WorldgenUtil.sampleBuffers(this, cx, wy, cz,
            NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z,
            "perlin");
    }
}