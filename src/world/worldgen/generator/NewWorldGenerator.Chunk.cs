using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.chunk;
using BlockGame.world.worldgen.feature;

namespace BlockGame.world.worldgen.generator;

public partial class NewWorldGenerator {
    public const int WATER_LEVEL = 64;

    private readonly float[] b = new float[WorldgenUtil.NOISE_SIZE_X * WorldgenUtil.NOISE_SIZE_Y * WorldgenUtil.NOISE_SIZE_Z];
    private readonly float[] tb = new float[WorldgenUtil.NOISE_SIZE_X * WorldgenUtil.NOISE_SIZE_Y * WorldgenUtil.NOISE_SIZE_Z];
    private readonly float[] t2b = new float[WorldgenUtil.NOISE_SIZE_X * WorldgenUtil.NOISE_SIZE_Y * WorldgenUtil.NOISE_SIZE_Z];
    private readonly float[] sb = new float[WorldgenUtil.NOISE_SIZE_X * WorldgenUtil.NOISE_SIZE_Y * WorldgenUtil.NOISE_SIZE_Z];
    private readonly float[] eb = new float[WorldgenUtil.NOISE_SIZE_X * WorldgenUtil.NOISE_SIZE_Y * WorldgenUtil.NOISE_SIZE_Z];
    private readonly float[] fb = new float[WorldgenUtil.NOISE_SIZE_X * WorldgenUtil.NOISE_SIZE_Y * WorldgenUtil.NOISE_SIZE_Z];
    private readonly float[] gb = new float[WorldgenUtil.NOISE_SIZE_X * WorldgenUtil.NOISE_SIZE_Y * WorldgenUtil.NOISE_SIZE_Z];
    private readonly float[] mb = new float[WorldgenUtil.NOISE_SIZE_X * WorldgenUtil.NOISE_SIZE_Y * WorldgenUtil.NOISE_SIZE_Z];
    private readonly float[] ob = new float[WorldgenUtil.NOISE_SIZE_X * WorldgenUtil.NOISE_SIZE_Y * WorldgenUtil.NOISE_SIZE_Z];
    private readonly float[] auxb = new float[WorldgenUtil.NOISE_SIZE_X * WorldgenUtil.NOISE_SIZE_Y * WorldgenUtil.NOISE_SIZE_Z];
    private readonly float[] foliageb = new float[WorldgenUtil.NOISE_SIZE_X * WorldgenUtil.NOISE_SIZE_Y * WorldgenUtil.NOISE_SIZE_Z];
    private readonly float[] tempb = new float[WorldgenUtil.NOISE_SIZE_X * WorldgenUtil.NOISE_SIZE_Y * WorldgenUtil.NOISE_SIZE_Z];
    private readonly float[] humb = new float[WorldgenUtil.NOISE_SIZE_X * WorldgenUtil.NOISE_SIZE_Y * WorldgenUtil.NOISE_SIZE_Z];
    private readonly float[] wb = new float[WorldgenUtil.NOISE_SIZE_X * WorldgenUtil.NOISE_SIZE_Y * WorldgenUtil.NOISE_SIZE_Z];


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

    public const float LOW_FREQ = 1 / 262f;
    public const float HIGH_FREQ = 1 / 219f;
    public const float SELECTOR_FREQ = 1 / 69f; // funny number
    public const float ELEVATION_FREQ = 1 / 422f;

    // yes this is hugely increased compared to v2 and v1. we use this to control plains vs. mountains INSTEAD of e now for the most part, e provides the base elevation
    public const float FRACT_FREQ = 1 / 2101f;

    // (tightly capped so the base doesn't overflow the heightlimit)
    //public const float LAKE_FREQ = 1 / 226200f;
    //public const float LAKE_FREQ = 1 / 2262.00f;
    public const float LAKE_FREQ = 1 / 762.00f;
    //public const float LAKE_FREQ = 1 / 4f;

    public void generate(ChunkCoord coord) {
        var chunk = world.getChunk(coord);

        switch (version) {
            case 1:
                getDensity(b, coord);
                break;
            case 2:
                getDensityv2(b, coord);
                break;
            case 3:
                getDensityv3(b, coord);
                break;
        }

        WorldgenUtil.interpolate(world, b, coord);
        //Console.Out.WriteLine(coord);


        switch (version) {
            case 1:
            case 2:
                generateSurface(coord);
                break;
            case 3:
                generateSurfacev3(coord);
                break;
        }


        chunk.status = ChunkStatus.GENERATED;
    }


    public void getDensity(float[] buffer, ChunkCoord coord) {
        // get the noise
        // todo cleanup this shit and give it proper constants
        WorldgenUtil.getNoise3DRegion(tb, tn, coord, 1 / (42f * 2), 1 / (42f * 2),
            1 / (42f * 2), 8, 1 + Meth.rhoF * 2);
        WorldgenUtil.getNoise3DRegion(t2b, t2n, coord, 1 / (42f * 2), 1 / (42f * 2),
            1 / (42f * 2), 8, 2 + Meth.rhoF);


        WorldgenUtil.getNoise3DRegion(sb, sn, coord, 1 / 29f, 1 / 29f,
            1 / 29f, 4, 2f);

        WorldgenUtil.getNoise2DRegion(eb, en, coord, 1 / 342f, 1 / 342f, 8, 2f);
        WorldgenUtil.getNoise2DRegion(fb, fn, coord, 1 / 342f, 1 / 342f, 8, 2f - Meth.d2r);

        WorldgenUtil.getNoise2DRegion(mb, mn, coord, 1 / 354f, 1 / 354f, 6, 1.81f);


        for (int ny = 0; ny < WorldgenUtil.NOISE_SIZE_Y; ny++) {
            for (int nz = 0; nz < WorldgenUtil.NOISE_SIZE_Z; nz++) {
                for (int nx = 0; nx < WorldgenUtil.NOISE_SIZE_X; nx++) {
                    // restore the actual coordinates (to see where we sample at)
                    //var x = coord.x * Chunk.CHUNKSIZE + nx * NOISE_PER_X;
                    var y = ny * WorldgenUtil.NOISE_PER_Y;
                    //var z = coord.z * Chunk.CHUNKSIZE + nz * NOISE_PER_Z;


                    float t = tb[WorldgenUtil.getIndex(nx, ny, nz)];
                    float t2 = t2b[WorldgenUtil.getIndex(nx, ny, nz)];
                    float s = sb[WorldgenUtil.getIndex(nx, ny, nz)];

                    float e = eb[WorldgenUtil.getIndex(nx, ny, nz)];
                    float f = fb[WorldgenUtil.getIndex(nx, ny, nz)];

                    float mm = mb[WorldgenUtil.getIndex(nx, ny, nz)];
                    t = float.Tan(t);
                    t2 = float.Tan(t2);
                    s = float.Clamp((s * 6 + 0.5f) * 6f, 0, 1);
                    float density = WorldgenUtil.lerp(t, t2, s);

                    e = float.Clamp(e, 0, 1);

                    var dd = float.Abs(e - 0.09f);
                    var m = ((float.Clamp(f, 0, 1) * 16) + 0.5f);

                    var h = 1 / (0.1f + float.Pow(float.E, -5 * (e - 0.3f)));
                    m = e > 0.4f ? m + ((e - 0.4f) * 2f) : m;
                    m *= h;

                    m = 1 / (m + 0.5f);
                    var d = (dd - 0.05f);

                    d = (d is < 0.04f and > 0f) ? d * 0.4f : d;
                    d = (d < 0f) ? d * 1.5f : d;
                    var od = float.Abs((0.5f) * (mm + 0.8f)) - 0.1f;
                    d = mm < -0.6f && e < 0.2f ? od : d;

                    d *= World.WORLDHEIGHT;
                    var airBias = (y - ((WATER_LEVEL + 4) + d)) / (float)World.WORLDHEIGHT * 10f * m;
                    if (y < WATER_LEVEL + 4) {
                        airBias *= 4;
                    }

                    var mt = float.Max((y - 120), 0) / 16f;
                    airBias += mt * mt;
                    density -= airBias;
                    buffer[WorldgenUtil.getIndex(nx, ny, nz)] = density;
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
                var amt = WorldgenUtil.getNoise2D(auxn, worldPos.X, worldPos.Z, 1, 1) + 4f;

                var e = WorldgenUtil.sample2D(eb, x, z);
                var f = WorldgenUtil.sample2D(fb, x, z);

                e = float.Abs(e);
                f = float.Abs(f);

                //amt -= float.Max(0, (f * 2f + 1.5f));
                //amt -= float.Max(0, (e >= 0.3 ? float.Sqrt(e - 0.3f) : 0f) * 6f);
                amt = e >= 0.3 ? (amt - 2f) : amt;
                amt -= float.Max(0, (f >= 0.3 ? float.Sqrt(e - 0.3f) : 0f) * 12f);

                amt = float.Max(amt, 0);

                var blockVar = WorldgenUtil.getNoise3D(auxn, worldPos.X * FREQAUX,
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
                else if (height is > WATER_LEVEL - 3 and < WATER_LEVEL + 1) {
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
                    WorldgenUtil.getNoise2D(auxn, -xs * HELLROCK_FREQUENCY, -zs * HELLROCK_FREQUENCY, 1, 1) * 4 + 2;
                height = float.Clamp(height, 1, 5);
                for (int y = 0; y < height; y++) {
                    chunk.setBlockFast(x, y, z, Blocks.HELLROCK);
                }
            }
        }

        surfacegen.surface(random, coord);

        chunk.status = ChunkStatus.POPULATED;
    }

    public void getDensityv2(float[] buffer, ChunkCoord coord) {
        WorldgenUtil.getNoise3DRegion(tb, tn, coord, LOW_FREQ, LOW_FREQ * 2,
            LOW_FREQ, 10, 1 + Meth.rhoF * 2);
        WorldgenUtil.getNoise3DRegion(t2b, t2n, coord, HIGH_FREQ, HIGH_FREQ * 2,
            HIGH_FREQ, 10, 2 + Meth.rhoF);


        WorldgenUtil.getNoise3DRegion(sb, sn, coord, SELECTOR_FREQ, SELECTOR_FREQ / 2,
            SELECTOR_FREQ, 6, 2f);

        WorldgenUtil.getNoise2DRegion(eb, en, coord, ELEVATION_FREQ, ELEVATION_FREQ, 10, 2f);
        WorldgenUtil.getNoise2DRegion(fb, fn, coord, FRACT_FREQ, FRACT_FREQ, 10, 2f - Meth.d2r);

        WorldgenUtil.getNoise2DRegion(mb, mn, coord, 1 / 354f, 1 / 354f, 6, 1.81f);


        for (int ny = 0; ny < WorldgenUtil.NOISE_SIZE_Y; ny++) {
            for (int nz = 0; nz < WorldgenUtil.NOISE_SIZE_Z; nz++) {
                for (int nx = 0; nx < WorldgenUtil.NOISE_SIZE_X; nx++) {
                    // restore the actual coordinates (to see where we sample at)
                    //var x = coord.x * Chunk.CHUNKSIZE + nx * NOISE_PER_X;
                    var y = ny * WorldgenUtil.NOISE_PER_Y;
                    //var z = coord.z * Chunk.CHUNKSIZE + nz * NOISE_PER_Z;


                    float t = tb[WorldgenUtil.getIndex(nx, ny, nz)];
                    float t2 = t2b[WorldgenUtil.getIndex(nx, ny, nz)];
                    float s = sb[WorldgenUtil.getIndex(nx, ny, nz)];

                    float e = eb[WorldgenUtil.getIndex(nx, ny, nz)];
                    float f = fb[WorldgenUtil.getIndex(nx, ny, nz)];
                    float g = gb[WorldgenUtil.getIndex(nx, ny, nz)];

                    float mm = mb[WorldgenUtil.getIndex(nx, ny, nz)];
                    //float o = ob[getIndex(nx, ny, nz)];

                    float w = wb[WorldgenUtil.getIndex(nx, ny, nz)];
                    s = float.Clamp((s * 6 + 0.5f), 0, 1);

                    float density = WorldgenUtil.lerp(t, t2, s);
                    e = e < 0f ? float.Abs(float.Max(-0.5f - e, 0)) : e;

                    var dd = float.Abs(e - 0.09f);
                    var m = ((float.Abs(f) * 16) + 0.5f);

                    var h = 1 / (0.1f + float.Pow(float.E, -5 * (e - 0.3f)));
                    m = e > 0.4f ? m + ((e - 0.4f) * 2f) : m;
                    m *= h;

                    m = 1 / (m + 0.5f);
                    var d = (dd - 0.05f);

                    d = (d is < 0.04f and > 0f) ? d * 0.4f : d;
                    d = (d < 0f) ? d * 1.5f : d;

                    d = e < 0.05f ? 0 : d;
                    d = e is < 0.05f and > 0f ? ((0.25f * (0.05f - e)) - 0.03f) : d;
                    var od = float.Abs((0.5f) * (mm + 0.8f)) - 0.1f;
                    d = mm < -0.6f && e < 0.2f ? od : d;

                    d *= World.WORLDHEIGHT * 0.7f;
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
                    buffer[WorldgenUtil.getIndex(nx, ny, nz)] = density;
                }
            }
        }
    }

    public void getDensityv3(float[] buffer, ChunkCoord coord) {
        // get the noise
        // todo cleanup this shit and give it proper constants
        WorldgenUtil.getNoise3DRegion(tb, tn, coord, LOW_FREQ, LOW_FREQ * 2,
            LOW_FREQ, 8, 1 + Meth.rhoF * 2);
        WorldgenUtil.getNoise3DRegion(t2b, t2n, coord, HIGH_FREQ, HIGH_FREQ * 2,
            HIGH_FREQ, 8, 2 + Meth.rhoF);


        WorldgenUtil.getNoise3DRegion(sb, sn, coord, SELECTOR_FREQ, SELECTOR_FREQ / 2,
            SELECTOR_FREQ, 6, 2f);

        WorldgenUtil.getNoise2DRegion(eb, esn, coord, ELEVATION_FREQ, ELEVATION_FREQ, 10, 2f);
        WorldgenUtil.getNoise2DRegion(fb, fsn, coord, FRACT_FREQ, FRACT_FREQ, 8, 2f - Meth.d2r);

        //getNoise2DRegion(gb, gn, coord, 1 / 342f, 1 / 342f, 8, 2f);

        //getNoise2DRegion(ob, on, coord, 1 / (754 * 300f), 1 / (754 * 300f), 4, 1.81f);

        WorldgenUtil.getNoise2DRegion(mb, mn, coord, LAKE_FREQ, LAKE_FREQ, 4, 2f);


        //getNoise3DRegion(wb, wn, coord, FREQW, FREQW, FREQW, 4, Meth.phiF);

        // print all noise resolutions
        //WorldgenUtil.printNoiseResolution(1 / (42f * 2), 8, 1 + Meth.rhoF * 2, 1 / (1 + Meth.rhoF * 2));
        //WorldgenUtil.printNoiseResolution(1 / 29f, 4, 2f,0.5f);


        for (int ny = 0; ny < WorldgenUtil.NOISE_SIZE_Y; ny++) {
            for (int nz = 0; nz < WorldgenUtil.NOISE_SIZE_Z; nz++) {
                for (int nx = 0; nx < WorldgenUtil.NOISE_SIZE_X; nx++) {
                    // restore the actual coordinates (to see where we sample at)
                    //var x = coord.x * Chunk.CHUNKSIZE + nx * NOISE_PER_X;
                    var y = ny * WorldgenUtil.NOISE_PER_Y;
                    //var z = coord.z * Chunk.CHUNKSIZE + nz * NOISE_PER_Z;


                    float t = tb[WorldgenUtil.getIndex(nx, ny, nz)];
                    float t2 = t2b[WorldgenUtil.getIndex(nx, ny, nz)];
                    float s = sb[WorldgenUtil.getIndex(nx, ny, nz)];

                    float e = eb[WorldgenUtil.getIndex(nx, ny, nz)];
                    float f = fb[WorldgenUtil.getIndex(nx, ny, nz)];
                    //float g = gb[WorldgenUtil.getIndex(nx, ny, nz)];

                    float mm = mb[WorldgenUtil.getIndex(nx, ny, nz)];
                    //float o = ob[getIndex(nx, ny, nz)];

                    //float w = wb[WorldgenUtil.getIndex(nx, ny, nz)];

                    // store the value in the buffer
                    //t = float.Tan(t);
                    //t2 = float.Tan(t2);


                    // we select and we win
                    // maybe something like -0.08 to 0.24?
                    s = float.Clamp((s * 6 + 0.5f), 0, 1);

                    //s = 0f;

                    //s *= 2;
                    //s = float.Clamp(s, 0, 1);

                    float density = WorldgenUtil.lerp(t, t2, s);

                    //e = float.Clamp(e, 0, 1);
                    //e = float.Abs(e);
                    var ee = e;
                    e = float.Abs(float.Max(0.25f * -e, e)) - 0.121f;
                    //e = e * 2 + 0.4f;
                    //e = e > 0 ? e * (1 / 9f) : e * (1/ 4f);
                    e *= (1 / 7f);
                    e = ee < 0 ? e * 1.5f : e;

                    //e = 0f;
                    //e += 0.018f;

                    //e = 0.1f;
                    //e = e < 0.05f ? 0 : e;

                    //Console.Out.WriteLine(density);
                    //var a = 0;
                    //var c = 8f;
                    // high m = flat, low m = not
                    //var sh = (e / 2f) + 1f;

                    //var dd = float.Abs(e - 0.09f);
                    //var m = sh * sh * c;
                    //var m = 1 / (sh * (e / 2f));
                    var m = float.Abs(f * 18) + 0.5f;
                    //m *= float.Clamp(g, 0, 1);

                    //m *= (1 / e * e);
                    //m *= e;

                    // leave the broken to the d
                    //m *= (e - 0.2f);

                    //e = float.Clamp(e, 0, 1);

                    //var h = 1 / (0.1f + float.Pow(float.E, -5 * (e - 0.3f)));

                    // if mountains, add a bit
                    //m = e > 0.4f ? m + ((e - 0.4f) * 2f) : m;
                    //m *= h;

                    // follow the fucking terrain shape goddamnit
                    //e -= (0.05f / 4f);
                    //var d = (float.Abs(e * e)) * World.WORLDHEIGHT * 2.4f;

                    // if you touch these values ill murder you
                    /*var d = (dd - 0.05f);

                    d = (d is < 0.04f and > 0f) ? d * 0.4f : d;
                    d = (d < 0f) ? d * 1.5f : d;

                    d = e < 0.05f ? 0 : d;
                    d = e is < 0.05f and > 0f ? ((0.25f * (0.05f - e)) - 0.03f) : d;*/
                    // cheat
                    //d = (d > -0.03 && d < 0.03f) ? d + 0.02f : d;
                    //var d2 = float.Sqrt(float.Abs(0.08f * (e - 0.04f))) - 0.065f;
                    //d = float.Max(d, d2);

                    // OR it together with the 25%
                    //d = o is > 0.5f and < 0.75f ? (float.Abs(o - 0.625f) - 0.125f) : d;
                    //var od = float.Max(3 * float.Abs(0.5f * (mm + 0.8f)) - 0.3f, -0.15f);
                    //od = (od > -0.1 && od < 0.03f) ? od + 0.07f : od;

                    // merge
                    //e = mm < -0.6f && e < 0.1f ? od - 0.2f : e;
                    //e = mm < -0.6f && e < 0.1f ? -9f : e;
                    //e = mm < -0.2f && e < 0.071f ? e - ((float.Abs(mm + 0.6f) - 0.4f)) * (1 / 2f) : e;
                    //e = mm < -0.4f && e < 0.071f ? -0.2f : e;
                    //d = mm < -0.2f ? 1f : d;

                    // IF LAKE SHIT don't go crazy
                    //m = (mm < -0.4f && e < 0.065f) ? 0 : m;
                    //m = (e < 0f) ? 0.5f : m;

                    m = 1 / (m + 0.5f);

                    //e = -0.2f;

                    e *= World.WORLDHEIGHT;
                    // increase bias by default, mod it above
                    var airBias = (y - ((WATER_LEVEL + 4) + e)) / (float)World.WORLDHEIGHT * 16f * m;

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
                    buffer[WorldgenUtil.getIndex(nx, ny, nz)] = density;
                }
            }
        }
    }

    public void generateSurfacev3(ChunkCoord coord) {
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
                var amt = WorldgenUtil.getNoise2D(auxn, worldPos.X, worldPos.Z, 1, 1) + 4f;

                var e = WorldgenUtil.sample2D(eb, x, z);
                var f = WorldgenUtil.sample2D(fb, x, z);

                e = float.Abs(float.Max(0.25f * -e, e)) - 0.121f;
                e *= (1 / 7f);

                //f = float.Abs(f);

                //amt -= float.Max(0, (f * 2f + 1.5f));
                //amt -= float.Max(0, (e >= 0.3 ? float.Sqrt(e - 0.3f) : 0f) * 6f);
                amt = e >= 0.06 ? (amt - 2f) : amt;
                //amt -= float.Max(0, (f >= 0.3 ? float.Sqrt(e - 0.3f) : 0f) * 12f);

                amt = float.Max(amt, 0);

                var blockVar = WorldgenUtil.getNoise3D(auxn, worldPos.X * FREQAUX,
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
                else if (height is > WATER_LEVEL - 3 and < WATER_LEVEL + 1) {
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
}