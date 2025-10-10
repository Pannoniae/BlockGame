using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.chunk;
using BlockGame.world.worldgen.feature;

namespace BlockGame.world.worldgen.generator;

public partial class NewWorldGenerator {
    // The noise is sampled every X blocks
    public const int NOISE_PER_X = 4;
    public const int NOISE_PER_Y = 4;
    public const int NOISE_PER_Z = 4;

    // mod 4 == & 3
    private const int NOISE_PER_X_MASK = 3;
    private const int NOISE_PER_Y_MASK = 3;
    private const int NOISE_PER_Z_MASK = 3;

    // div 4 == >> 2
    private const int NOISE_PER_X_SHIFT = 2;
    private const int NOISE_PER_Y_SHIFT = 2;
    private const int NOISE_PER_Z_SHIFT = 2;

    // x / y = x * (1 / y)
    private const float NOISE_PER_X_INV = 1f / NOISE_PER_X;
    private const float NOISE_PER_Y_INV = 1f / NOISE_PER_Y;
    private const float NOISE_PER_Z_INV = 1f / NOISE_PER_Z;

    public const int NOISE_SIZE_X = (Chunk.CHUNKSIZE / NOISE_PER_X) + 1;
    public const int NOISE_SIZE_Y = (Chunk.CHUNKSIZE * Chunk.CHUNKHEIGHT) / NOISE_PER_Y + 1;
    public const int NOISE_SIZE_Z = (Chunk.CHUNKSIZE / NOISE_PER_Z) + 1;

    public const int WATER_LEVEL = 64;

    private readonly float[] b = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private readonly float[] tb = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private readonly float[] t2b = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private readonly float[] sb = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private readonly float[] eb = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private readonly float[] fb = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private readonly float[] gb = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private readonly float[] mb = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private readonly float[] ob = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private readonly float[] auxb = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private readonly float[] foliageb = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private readonly float[] tempb = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private readonly float[] humb = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];
    private readonly float[] wb = new float[NOISE_SIZE_X * NOISE_SIZE_Y * NOISE_SIZE_Z];

    private readonly Cave caves = new();
    private readonly Ravine ravines = new();
    private readonly OreFeature ironOre = new(Blocks.REALGAR, 6, 12);
    private readonly OreFeature coalOre = new(Blocks.TITANIUM_ORE, 8, 16);

    /**
     * Everyone knows the meaning of life, don't they?
     */
    public const float FREQ1 = 1 / (42f + Meth.sqrt2F);

    public const float FREQ2 = 1 / (54f - Meth.kappaF * Meth.phiF);
    public const float FREQS = 1 / 390f;
    public const float FREQE = 1 / 390f;
    public const float FREQW = 1 / 590f;

    public const float FREQAUX = 1 / 42f;
    public const float HELLROCK_FREQUENCY = 1 / 1.5f;

    public const float FREQFOLIAGE = 1 / 169f;

    public void generate(ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        getDensity(b, coord);
        interpolate(b, coord);
        //Console.Out.WriteLine(coord);
        generateSurface(coord);

        chunk.recalc();
        chunk.status = ChunkStatus.GENERATED;
    }


    public void getDensity(float[] buffer, ChunkCoord coord) {
        var chunk = world.getChunk(coord);
        // get the noise
        // todo cleanup this shit and give it proper constants
        getNoise3DRegion(tb, tn, coord, 1 / (42f * 2), 1 / (42f * 2),
            1 / (42f * 2), 8, 1 + Meth.rhoF * 2);
        getNoise3DRegion(t2b, t2n, coord, 1 / (42f * 2), 1 / (42f * 2),
            1 / (42f * 2), 8, 2 + Meth.rhoF);


        getNoise3DRegion(sb, sn, coord, 1 / 59f, 1 / 29f,
            1 / 29f, 4, 2f);

        getNoise2DRegion(eb, en, coord, 1 / 342f, 1 / 342f, 8, 2f);
        getNoise2DRegion(fb, fn, coord, 1 / 342f, 1 / 342f, 8, 2f - Meth.d2r);

        //getNoise2DRegion(gb, gn, coord, 1 / 342f, 1 / 342f, 8, 2f);

        //getNoise2DRegion(ob, on, coord, 1 / (754 * 300f), 1 / (754 * 300f), 4, 1.81f);

        getNoise2DRegion(mb, mn, coord, 1 / 354f, 1 / 354f, 6, 1.81f);

        //getNoise3DRegion(wb, wn, coord, FREQW, FREQW, FREQW, 4, Meth.phiF);

        for (int ny = 0; ny < NOISE_SIZE_Y; ny++) {
            for (int nz = 0; nz < NOISE_SIZE_Z; nz++) {
                for (int nx = 0; nx < NOISE_SIZE_X; nx++) {
                    // restore the actual coordinates (to see where we sample at)
                    //var x = coord.x * Chunk.CHUNKSIZE + nx * NOISE_PER_X;
                    var y = ny * NOISE_PER_Y;
                    //var z = coord.z * Chunk.CHUNKSIZE + nz * NOISE_PER_Z;


                    float t = tb[getIndex(nx, ny, nz)];
                    float t2 = t2b[getIndex(nx, ny, nz)];
                    float s = sb[getIndex(nx, ny, nz)];

                    float e = eb[getIndex(nx, ny, nz)];
                    float f = fb[getIndex(nx, ny, nz)];
                    float g = gb[getIndex(nx, ny, nz)];

                    float mm = mb[getIndex(nx, ny, nz)];
                    //float o = ob[getIndex(nx, ny, nz)];

                    float w = wb[getIndex(nx, ny, nz)];

                    // store the value in the buffer
                    t = float.Tan(t);
                    t2 = float.Tan(t2);


                    // we select and we win
                    // maybe something like -0.08 to 0.24?
                    s = float.Clamp((s * 6 + 0.5f) * 6f, 0, 1);

                    //s = 0f;

                    //s *= 2;
                    //s = float.Clamp(s, 0, 1);

                    float density = lerp(t, t2, s);

                    e = float.Clamp(e, 0, 1);
                    //e = e < 0.05f ? 0 : e;

                    //Console.Out.WriteLine(density);
                    var a = 0;
                    var c = 8f;
                    // high m = flat, low m = not
                    var sh = (e / 2f) + 1f;

                    var dd = float.Abs(e - 0.09f);
                    //var m = sh * sh * c;
                    //var m = 1 / (sh * (e / 2f));
                    var m = ((float.Clamp(f, 0, 1) * 16) + 0.5f);
                    //m *= float.Clamp(g, 0, 1);

                    //m *= (1 / e * e);
                    //m *= e;

                    // leave the broken to the d
                    //m *= (e - 0.2f);

                    //e = float.Clamp(e, 0, 1);

                    var h = 1 / (0.1f + float.Pow(float.E, -5 * (e - 0.3f)));

                    // if mountains, add a bit
                    m = e > 0.4f ? m + ((e - 0.4f) * 2f) : m;
                    m *= h;

                    m = 1 / (m + 0.5f);

                    // follow the fucking terrain shape goddamnit
                    //e -= (0.05f / 4f);
                    //var d = (float.Abs(e * e)) * World.WORLDHEIGHT * 2.4f;

                    // if you touch these values ill murder you
                    var d = (dd - 0.05f);

                    d = (d is < 0.04f and > 0f) ? d * 0.4f : d;
                    d = (d < 0f) ? d * 1.5f : d;

                    d = e < 0.05f ? 0 : d;
                    d = e is < 0.05f and > 0f ? ((0.25f * (0.05f - e)) - 0.03f) : d;
                    // cheat
                    //d = (d > -0.03 && d < 0.03f) ? d + 0.02f : d;
                    //var d2 = float.Sqrt(float.Abs(0.08f * (e - 0.04f))) - 0.065f;
                    //d = float.Max(d, d2);

                    // OR it together with the 25%
                    //d = o is > 0.5f and < 0.75f ? (float.Abs(o - 0.625f) - 0.125f) : d;
                    var od = float.Abs((0.5f) * (mm + 0.8f)) - 0.1f;
                    //od = (od > -0.1 && od < 0.03f) ? od + 0.07f : od;
                    d = mm < -0.6f && e < 0.2f ? od : d;
                    //d = mm < -0.2f ? 1f : d;

                    d *= World.WORLDHEIGHT;
                    // increase bias by default, mod it above
                    var airBias = (y - ((WATER_LEVEL + 4) + d)) / (float)World.WORLDHEIGHT * 10f * m;

                    //Console.Out.WriteLine(airBias);

                    // reduce it below ground (is water anyway, useless)
                    if (y < WATER_LEVEL + 4) {
                        airBias *= 4;
                    }


                    // between y=120-128, taper it off to -1
                    // at max should be 0.5
                    // ^2 to have a better taper
                    var mt = float.Max((y - 120), 0) / 16f;
                    airBias += mt * mt;
                    density -= airBias;
                    buffer[getIndex(nx, ny, nz)] = density;
                }
            }
        }
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

                // thickness of the soil layer (1 to 5)
                var amt = getNoise2D(auxn, worldPos.X, worldPos.Z, 1, 1) + 4f;

                var e = sample2D(eb, x, z);
                var f = sample2D(fb, x, z);

                e = float.Clamp(e, 0, 1);

                //amt -= float.Max(0, (f * 2f + 1.5f));
                amt -= float.Max(0, (e >= 0.3 ? float.Sqrt(e - 0.3f) : 0f) * 30f);

                amt = float.Max(amt, 0);

                var blockVar = getNoise3D(auxn, worldPos.X * FREQAUX,
                    128,
                    worldPos.Z * FREQAUX,
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

                if (chunk.getBlock(x, height, z) == Blocks.STONE && amt >= 1f) {
                    for (int yy = height; yy > height - amt && yy > 0; yy--) {
                        if (yy == height) {
                            chunk.setBlockFast(x, height, z, topBlock);
                        }
                        else {
                            if (chunk.getBlock(x, yy, z) == Blocks.STONE) {
                                chunk.setBlockFast(x, yy, z, filler);
                            }
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
    private static float sample2D(float[] buffer, int x, int z) {
        var x0 = x >> NOISE_PER_X_SHIFT;
        var x1 = x0 + 1;
        var z0 = z >> NOISE_PER_Z_SHIFT;
        var z1 = z0 + 1;
        float xd = (x & NOISE_PER_X_MASK) * NOISE_PER_X_INV;
        float zd = (z & NOISE_PER_Z_MASK) * NOISE_PER_Z_INV;

        var v00 = buffer[getIndex(x0, 0, z0)];
        var v01 = buffer[getIndex(x0, 0, z1)];
        var v10 = buffer[getIndex(x1, 0, z0)];
        var v11 = buffer[getIndex(x1, 0, z1)];

        var v0 = lerp(v00, v10, xd);
        var v1 = lerp(v01, v11, xd);
        return lerp(v0, v1, zd);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float sample3D(float[] buffer, int x, int y, int z) {
        var x0 = x >> NOISE_PER_X_SHIFT;
        var x1 = x0 + 1;
        var y0 = y >> NOISE_PER_Y_SHIFT;
        var y1 = y0 + 1;
        var z0 = z >> NOISE_PER_Z_SHIFT;
        var z1 = z0 + 1;
        float xd = (x & NOISE_PER_X_MASK) * NOISE_PER_X_INV;
        float yd = (y & NOISE_PER_Y_MASK) * NOISE_PER_Y_INV;
        float zd = (z & NOISE_PER_Z_MASK) * NOISE_PER_Z_INV;

        var c000 = buffer[getIndex(x0, y0, z0)];
        var c001 = buffer[getIndex(x0, y0, z1)];
        var c010 = buffer[getIndex(x0, y1, z0)];
        var c011 = buffer[getIndex(x0, y1, z1)];
        var c100 = buffer[getIndex(x1, y0, z0)];
        var c101 = buffer[getIndex(x1, y0, z1)];
        var c110 = buffer[getIndex(x1, y1, z0)];
        var c111 = buffer[getIndex(x1, y1, z1)];

        var c00 = lerp(c000, c100, xd);
        var c01 = lerp(c001, c101, xd);
        var c10 = lerp(c010, c110, xd);
        var c11 = lerp(c011, c111, xd);

        var c0 = lerp(c00, c10, yd);
        var c1 = lerp(c01, c11, yd);

        return lerp(c0, c1, zd);
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
                    getNoise2D(auxn, -xs * HELLROCK_FREQUENCY, -zs * HELLROCK_FREQUENCY, 1, 1) * 4 + 2;
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

        var foliage = getNoise2D(foliagen, xChunk * FREQFOLIAGE, zChunk * FREQFOLIAGE, 2, 2);
        var treeCount = foliage;
        if (foliage < 0) {
            treeCount = 0;
        }

        treeCount *= 10;
        treeCount *= treeCount;

        for (int i = 0; i < treeCount; i++) {
            placeTree(world, random, coord);
        }

        // get e
        //var e = getNoise2D(en, 1 / 342f, 1 / 342f, 8, 2f);

        //e = float.Clamp(e, 0, 1);

        // Do caves
        //caves.freq = e;
        caves.place(world, coord);
        // Do ravines
        //ravines.freq = e;
        ravines.place(world, coord);

        // place grass
        var grassDensity = float.Abs(getNoise2D(foliagen, xChunk * FREQFOLIAGE, zChunk * FREQFOLIAGE, 2, 1.5f));
        var grassCount = grassDensity * World.WORLDHEIGHT;

        if (grassDensity < 0) {
            grassCount = 0;
        }

        grassCount *= grassCount;

        for (int i = 0; i < grassCount; i++) {
            var x = random.Next(0, Chunk.CHUNKSIZE);
            var z = random.Next(0, Chunk.CHUNKSIZE);
            // var y = chunk.heightMap.get(x, z);
            // the problem with the heightmap approach is that you get chunks FULL of grass vs. literally nothing elsewhere
            // STOCHASTIC RANDOMISATION FOR THE LULZ
            var y = random.Next(0, World.WORLDHEIGHT - 1);

            if (chunk.getBlock(x, y, z) == Blocks.GRASS && y < World.WORLDHEIGHT - 1) {
                if (chunk.getBlock(x, y + 1, z) == Blocks.AIR) {
                    var grassType = random.NextSingle() > 0.7f ? Blocks.TALL_GRASS : Blocks.SHORT_GRASS;
                    chunk.setBlockFast(x, y + 1, z, grassType);
                }
            }
        }

        chunk.status = ChunkStatus.POPULATED;
    }

    private static void placeTree(World world, XRandom random, ChunkCoord coord) {
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

        placeOakTree(world, random, x + coord.x * Chunk.CHUNKSIZE, y + 1, z + coord.z * Chunk.CHUNKSIZE);
    }

    private static void placeOakTree(World world, XRandom random, int x, int y, int z) {
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