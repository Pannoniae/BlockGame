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
    private float[] auxBuffer = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private float[] foliageBuffer = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private float[] temperatureBuffer = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private float[] humidityBuffer = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private float[] weirdnessBuffer = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];

    private readonly Cave caves = new();
    private readonly Ravine ravines = new();
    private readonly OreFeature ironOre = new(Blocks.RED_ORE, 6, 12);
    private readonly OreFeature coalOre = new(Blocks.TITANIUM_ORE, 8, 16);

    public const float LOW_FREQUENCY = 1 / 367f;
    public const float HIGH_FREQUENCY = 1 / 135f;
    public const float SELECTOR_FREQUENCY = 1 / 490f;
    
    public const float WEIRDNESS_FREQUENCY = 1 / 590f;

    public const float Y_DIVIDER = 1;
    public const float Y_DIVIDER_INV = 16f;

    public const float BLOCK_VARIATION_FREQUENCY = 1 / 412f;
    public const float HELLROCK_FREQUENCY = 1 / 1.5f;

    public const float FOLIAGE_FREQUENCY = 1 / 69f;

    public void generate(ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        getDensity(buffer, coord);
        interpolate(buffer, coord);
        //Console.Out.WriteLine(coord);
        generateSurface(coord);

        chunk.recalc();
        chunk.status = ChunkStatus.GENERATED;
    }


    public void getDensity(float[] buffer, ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        // get the noise
        getNoise3DRegion(lowBuffer, lowNoise, coord, LOW_FREQUENCY, LOW_FREQUENCY * Y_DIVIDER,
            LOW_FREQUENCY, 12, 2f);
        getNoise3DRegion(highBuffer, highNoise, coord, HIGH_FREQUENCY, HIGH_FREQUENCY,
            HIGH_FREQUENCY, 12, Meth.phiF);
        
        // get weirdness
        // only low octaves for this one, it's a glorified selector
        getNoise3DRegion(weirdnessBuffer, weirdnessNoise, coord, WEIRDNESS_FREQUENCY, WEIRDNESS_FREQUENCY,
            WEIRDNESS_FREQUENCY, 4, Meth.phiF);


        // todo the selector could be sampled less frequently because it doesn't need to be as precise
        // also sample it less in the Y axis too because it should be 2d you know?
        // also todo migrate it to valuecubic somehow? yes the broken valuecubic octave summer combined with simplex
        // produces the actually cool results BUT we could cook something up with maths to make it more extreme and stuff
        // basically it should be mostly 0 (flat) or 1 (quirky shit) with transitions inbetween
        // im sure we can cook something up mr white
        getNoise3DRegion(selectorBuffer, selectorNoise, coord, SELECTOR_FREQUENCY, SELECTOR_FREQUENCY,
            SELECTOR_FREQUENCY, 4, Meth.etaF);

        for (int ny = 0; ny < NOISE_SIZE_Y; ny++) {
            for (int nz = 0; nz < NOISE_SIZE_Z; nz++) {
                for (int nx = 0; nx < NOISE_SIZE_X; nx++) {
                    // restore the actual coordinates (to see where we sample at)
                    //var x = coord.x * Chunk.CHUNKSIZE + nx * NOISE_PER_X;
                    var y = ny * NOISE_PER_Y;
                    //var z = coord.z * Chunk.CHUNKSIZE + nz * NOISE_PER_Z;

                    // sample lowNoise
                    float low = lowBuffer[getIndex(nx, ny, nz)];
                    // sample highNoise
                    float high = highBuffer[getIndex(nx, ny, nz)];
                    // sample selectorNoise
                    float selector = selectorBuffer[getIndex(nx, ny, nz)];
                    
                    float weirdness = weirdnessBuffer[getIndex(nx, ny, nz)];

                    // store the value in the buffer
                    buffer[getIndex(nx, ny, nz)] = calculateDensity(low, high, selector, weirdness, y);
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
        float density = lerp(low, high, selector);
        density -= calculateAirBias(low, high, weirdness, y);

        return density;
    }

    private void interpolate(float[] buffer, ChunkCoord coord) {
        var chunk = world.getChunk(coord);

        // we do this so we don't check "is chunk initialised" every single time we set a block.
        // this cuts our asm code size by half lol from all that inlining and shit
        // TODO it doesnt work properly, I'll fix it later
        // Span<bool> initialised = stackalloc bool[Chunk.CHUNKHEIGHT];

        for (int y = 0; y < Chunk.CHUNKSIZE * Chunk.CHUNKHEIGHT; y++) {
            var y0 = y >> NOISE_PER_Y_SHIFT;
            var y1 = y0 + 1;
            float yd = (y & NOISE_PER_Y_MASK) * NOISE_PER_Y_INV;

            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                var z0 = z >> NOISE_PER_Z_SHIFT;
                var z1 = z0 + 1;
                float zd = (z & NOISE_PER_Z_MASK) * NOISE_PER_Z_INV;

                for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                    // the grid cell that contains the point
                    var x0 = x >> NOISE_PER_X_SHIFT;
                    var x1 = x0 + 1;

                    // the lerp (between 0 and 1)
                    // float xd = (x % NOISE_PER_X) / (float)NOISE_PER_X;
                    float xd = (x & NOISE_PER_X_MASK) * NOISE_PER_X_INV;
                    //var ys = y >> 4;

                    float value;

                    /*if (Avx2.IsSupported && false) {
                        unsafe {
                            // load the eight corner values from the buffer into a vector
                            var c000 = buffer[getIndex(x0, y0, z0)];
                            var c001 = buffer[getIndex(x0, y0, z1)];
                            var c010 = buffer[getIndex(x0, y1, z0)];
                            var c011 = buffer[getIndex(x0, y1, z1)];
                            var c100 = buffer[getIndex(x1, y0, z0)];
                            var c101 = buffer[getIndex(x1, y0, z1)];
                            var c110 = buffer[getIndex(x1, y1, z0)];
                            var c111 = buffer[getIndex(x1, y1, z1)];


                            var values1 = Vector256.Create(c000, c001, c010, c011);
                            var values2 = Vector256.Create(c100, c101, c110, c111);

                            // the two vectors contain the two halves of the cube to be interpolated.
                            // We need to interpolate element-wise.
                            var interp = lerp4(values1, values2, Vector256.Create(xd));

                            // now the vector contains the 4 interpolated values. interpolate narrower (2x2)
                            var low = interp.GetLower();
                            var high = interp.GetUpper();
                            var interp2 = lerp2(low, high, Vector128.Create(yd));

                            // now the vector contains the 2 interpolated values. interpolate again (1x2)
                            var lower = interp2.GetElement(0);
                            var higher = interp2.GetElement(1);
                            value = lerp(lower, higher, zd);
                        }
                    }
                    else */
                    {
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
                        value = lerp(c0, c1, zd);
                    }

                    // if we haven't initialised the chunk yet, do so
                    // if below sea level, water
                    if (value > 0) {
                        chunk.setBlockFast(x, y, z, Blocks.STONE);
                        chunk.addToHeightMap(x, y, z);
                    }
                    else {
                        if (y is < WATER_LEVEL and >= 40) {
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

        for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
            for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                var worldPos = World.toWorldPos(chunk.coord.x, chunk.coord.z, x, 0, z);
                int height = chunk.heightMap.get(x, z);

                // move down until it's solid
                while (height > 0 && !Block.fullBlock[chunk.getBlock(x, height, z)]) {
                    height--;
                }

                // thickness of the soil layer layer
                var amt = getNoise2D(auxNoise, worldPos.X, worldPos.Z, 1, 1) + 2.5;
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
        // THE PARENTHESES ARE IMPORTANT
        // otherwise it does 5 * 5 for some reason??? completely useless multiplication
        return x + z * NOISE_SIZE_X + y * (NOISE_SIZE_X * NOISE_SIZE_Z);
    }

    private static Vector128<int> getIndex4(int x, int y, int z) {
        return Vector128.Create(x) + Vector128.Create(z) * NOISE_SIZE_X +
               Vector128.Create(y) * NOISE_SIZE_X * NOISE_SIZE_Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float lerp(float a, float b, float t) {
        return a + t * (b - a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<double> lerp4(Vector256<double> a, Vector256<double> b, Vector256<double> t) {
        return a + t * (b - a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<float> lerp4(Vector128<float> a, Vector128<float> b, Vector128<float> t) {
        return a + t * (b - a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<float> lerp2(Vector128<float> a, Vector128<float> b, Vector128<float> t) {
        return a + t * (b - a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector64<float> lerp2(Vector64<float> a, Vector64<float> b, Vector64<float> t) {
        return a + t * (b - a);
    }

    public void populate(ChunkCoord coord) {
        var chunk = world.getChunk(coord);

        var xWorld = coord.x * Chunk.CHUNKSIZE;
        var zWorld = coord.z * Chunk.CHUNKSIZE;

        // set seed to chunk
        random.Seed(coord.GetHashCode());

        var xChunk = coord.x * Chunk.CHUNKSIZE;
        var zChunk = coord.z * Chunk.CHUNKSIZE;

        // place hellrock on bottom of the world
        // height should be between 1 and 4
        for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
            for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                var xs = xWorld + x;
                var zs = zWorld + z;

                var height =
                    getNoise2D(auxNoise, -xs * HELLROCK_FREQUENCY, -zs * HELLROCK_FREQUENCY, 1, 1) * 4 + 2;
                height = float.Clamp(height, 1, 5);
                for (int y = 0; y < height; y++) {
                    chunk.setBlockFast(x, y, z, Blocks.HELLROCK);
                }
            }
        }

        // Do ore
        for (int i = 0; i < 16; i++) {
            var x = xWorld + random.Next(0, Chunk.CHUNKSIZE);
            var z = zWorld + random.Next(0, Chunk.CHUNKSIZE);
            var y = random.Next(0, World.WORLDHEIGHT);
            coalOre.place(world, random, x, y, z);
        }

        for (int i = 0; i < 16; i++) {
            var x = xWorld + random.Next(0, Chunk.CHUNKSIZE);
            var z = zWorld + random.Next(0, Chunk.CHUNKSIZE);
            var y = random.Next(0, World.WORLDHEIGHT / 2);
            ironOre.place(world, random, x, y, z);
        }

        var foliage = getNoise2D(foliageNoise, xChunk * FOLIAGE_FREQUENCY, zChunk * FOLIAGE_FREQUENCY, 2, 2);
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
        // Do ravines
        ravines.place(world, coord);

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
        if (chunk.getBlock(x, y, z) != Blocks.GRASS) {
            return;
        }

        var xWorld = coord.x * Chunk.CHUNKSIZE + x;
        var zWorld = coord.z * Chunk.CHUNKSIZE + z;

        // if there's stuff in the bounding box, don't place a tree
        for (int yd = 1; yd < 8; yd++) {
            for (int zd = -2; zd <= 2; zd++) {
                for (int xd = -2; xd <= 2; xd++) {
                    if (world.getBlock(xWorld, y + yd, zWorld) != Blocks.AIR) {
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
            world.setBlockDumb(x, y + i, z, Blocks.LOG);
            // leaves, thick
            for (int x1 = -2; x1 <= 2; x1++) {
                for (int z1 = -2; z1 <= 2; z1++) {
                    // don't overwrite the trunk
                    if (x1 == 0 && z1 == 0) {
                        continue;
                    }

                    for (int y1 = randomNumber - 2; y1 <= randomNumber - 1; y1++) {
                        world.setBlockDumb(x + x1, y + y1, z + z1, Blocks.LEAVES);
                    }
                }
            }

            // leaves, thin on top
            for (int x1 = -1; x1 <= 1; x1++) {
                for (int z1 = -1; z1 <= 1; z1++) {
                    for (int y1 = randomNumber; y1 <= randomNumber + 1; y1++) {
                        world.setBlockDumb(x + x1, y + y1, z + z1, Blocks.LEAVES);
                    }
                }
            }
        }
    }
}