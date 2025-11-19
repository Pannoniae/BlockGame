using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.chunk;

namespace BlockGame.world.worldgen.generator;

public partial class NewWorldGenerator {

    public const float TEMP_FREQ = 1 / 845f;
    public const float HUM_FREQ = 1 / 799f;
    public const float AGE_FREQ = 1 / 601f;
    public const float W_FREQ = 1 / 551f;

    /** v4 density - same as v3 but generates biomes too */
    private void getDensityBiomes(float[] buffer, ChunkCoord coord) {
        WorldgenUtil.getNoise3DRegion(tb, tn, coord, LOW_FREQ, LOW_FREQ * 2,
            LOW_FREQ, 8, 1 + Meth.rhoF * 2);
        WorldgenUtil.getNoise3DRegion(t2b, t2n, coord, HIGH_FREQ, HIGH_FREQ * 2,
            HIGH_FREQ, 8, 2 + Meth.rhoF);

        WorldgenUtil.getNoise3DRegion(sb, sn, coord, SELECTOR_FREQ, SELECTOR_FREQ / 2,
            SELECTOR_FREQ, 6, 2f);

        WorldgenUtil.getNoise2DRegion(eb, esn, coord, ELEVATION_FREQ, ELEVATION_FREQ, 10, 2f);
        WorldgenUtil.getNoise2DRegion(fb, fsn, coord, FRACT_FREQ, FRACT_FREQ, 8, 2f - Meth.d2r);

        // generate biome noise (5x33x5 grid - 16 blocks sampled every 4 = 5 points, 128 blocks = 33 points)
        const int biomeNX = 5;
        const int biomeNY = 33; // 0,4,8,...,124,128
        const int biomeNZ = 5;

        WorldgenUtil.getNoise3DRegionBiome(tempb, tempn, coord, TEMP_FREQ, TEMP_FREQ, TEMP_FREQ, 6, 2f, biomeNX, biomeNY, biomeNZ);
        WorldgenUtil.getNoise3DRegionBiome(humb, humn, coord, HUM_FREQ, HUM_FREQ, HUM_FREQ, 6, 1.5f, biomeNX, biomeNY, biomeNZ);
        WorldgenUtil.getNoise3DRegionBiome(ageb, agen, coord, AGE_FREQ, AGE_FREQ, AGE_FREQ, 6, Meth.phiF, biomeNX, biomeNY, biomeNZ);
        WorldgenUtil.getNoise3DRegionBiome(wb, wn, coord, W_FREQ, W_FREQ, W_FREQ, 6, Meth.etaF, biomeNX, biomeNY, biomeNZ);

        // fill chunk biome data
        var chunk = world.getChunk(coord);
        for (int by = 0; by < 32; by++) {
            for (int bz = 0; bz < 4; bz++) {
                for (int bx = 0; bx < 4; bx++) {
                    var idx = (by * biomeNZ + bz) * biomeNX + bx;
                    chunk.biomeData.setTemp(bx, by, bz, (sbyte)(tempb[idx] * 127));
                    chunk.biomeData.setHum(bx, by, bz, (sbyte)(humb[idx] * 127));
                    chunk.biomeData.setAge(bx, by, bz, (sbyte)(ageb[idx] * 127));
                    chunk.biomeData.setW(bx, by, bz, (sbyte)(wb[idx] * 127));
                }
            }
        }

        // terrain density calculation (same as v3)
        for (int ny = 0; ny < WorldgenUtil.NOISE_SIZE_Y; ny++) {
            for (int nz = 0; nz < WorldgenUtil.NOISE_SIZE_Z; nz++) {
                for (int nx = 0; nx < WorldgenUtil.NOISE_SIZE_X; nx++) {
                    var y = ny * WorldgenUtil.NOISE_PER_Y;

                    float t = tb[WorldgenUtil.getIndex(nx, ny, nz)];
                    float t2 = t2b[WorldgenUtil.getIndex(nx, ny, nz)];
                    float s = sb[WorldgenUtil.getIndex(nx, ny, nz)];
                    float e = eb[WorldgenUtil.getIndex(nx, ny, nz)];
                    float f = fb[WorldgenUtil.getIndex(nx, ny, nz)];

                    s = float.Clamp((s * 6 + 0.5f), 0, 1);
                    float density = WorldgenUtil.lerp(t, t2, s);

                    e = float.Max(0.25f * e, e) + 0.02f;
                    e *= (1 / 7f);
                    e = e < 0 ? e * 5 : e;
                    e = float.Min(e, (1 / 5f));

                    var m = ((f - 0.05f) * 16) + 0.5f;
                    m = e switch {
                        < 0f and > -0.055f => Meth.lerp(m, 0f, (0f - e) / 0.055f),
                        < -0.055f => 0f,
                        _ => m
                    };
                    m = f < 0f ? 0f : m;
                    m = 1 / (m + 0.5f);

                    e *= World.WORLDHEIGHT;
                    var airBias = (y - ((WATER_LEVEL + 4) + e)) / (float)World.WORLDHEIGHT * 10 * m;

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

    /** v4 surface - uses biome data */
    private void generateSurfaceBiomes(ChunkCoord coord) {
        var chunk = world.getChunk(coord);

        for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
            for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                var worldPos = World.toWorldPos(chunk.coord.x, chunk.coord.z, x, 0, z);
                int height = chunk.heightMap.get(x, z);

                while (height > 0 && !Block.fullBlock[chunk.getBlock(x, height, z)]) {
                    height--;
                }

                // get biome data at this column
                var temp = chunk.biomeData.getTemp(x, height, z);
                var hum = chunk.biomeData.getHum(x, height, z);

                // soil thickness
                var amt = WorldgenUtil.getNoise2D(auxn, worldPos.X, worldPos.Z, 1, 1) + 4f;
                var e = WorldgenUtil.sample2D(eb, x, z);
                e = float.Abs(float.Max(0.25f * -e, e)) - 0.121f;
                e *= (1 / 7f);

                amt = e >= 0.06 ? (amt - 2f) : amt;
                amt = float.Max(amt, 0);

                var blockVar = WorldgenUtil.getNoise3D(auxn, worldPos.X * FREQAUX,
                    128,
                    worldPos.Z * FREQAUX,
                    1, 1);

                ushort topBlock;
                ushort filler;

                // simple biome selection based on temp/humidity
                if (height < WATER_LEVEL - 1) {
                    // underwater
                    if (blockVar > 0) {
                        topBlock = Block.DIRT.id;
                        filler = Block.DIRT.id;
                    }
                    else {
                        topBlock = Block.SAND.id;
                        filler = Block.SAND.id;
                    }
                }
                else if (height is > WATER_LEVEL - 3 and < WATER_LEVEL + 1) {
                    // beaches
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
                    // above water - use biomes
                    // cold + dry = plains/tundra
                    // cold + wet = forest
                    // hot + dry = desert
                    // hot + wet = jungle

                    switch (temp) {
                        case < -0.25f: {
                            // cold biomes
                            if (hum > 0.25f) {
                                // snowy forest
                                topBlock = Block.SNOW_GRASS.id;
                                filler = Block.DIRT.id;
                            }
                            else {
                                // tundra/plains
                                topBlock = Block.SNOW_GRASS.id;
                                filler = Block.DIRT.id;
                            }

                            break;
                        }
                        case > 0.25f: {
                            // hot biomes
                            if (hum > 0.25f) {
                                // jungle todo
                                topBlock = Block.GRASS.id;
                                filler = Block.DIRT.id;
                            }
                            else {
                                // desert
                                topBlock = Block.SAND.id;
                                filler = Block.SAND.id;
                            }

                            break;
                        }
                        default:
                            // temperate
                            topBlock = Block.GRASS.id;
                            filler = Block.DIRT.id;
                            break;
                    }
                }

                if (chunk.getBlock(x, height, z) == Block.STONE.id && amt >= 1f) {
                    for (int yy = height; yy > height - amt && yy > 0; yy--) {
                        if (yy == height) {
                            chunk.setBlockFast(x, height, z, topBlock);
                        }
                        else {
                            if (chunk.getBlock(x, yy, z) == Block.STONE.id) {
                                chunk.setBlockFast(x, yy, z, filler);
                            }
                        }
                    }
                }
            }
        }
    }
}