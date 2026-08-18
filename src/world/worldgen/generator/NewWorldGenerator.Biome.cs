using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.chunk;

namespace BlockGame.world.worldgen.generator;

public partial class NewWorldGenerator {

    public const float TEMP_FREQ = 1 / 845f;
    public const float HUM_FREQ = 1 / 799f;
    public const float AGE_FREQ = 1 / 601f;
    public const float W_FREQ = 1 / 551f;

    public const float DETAIL_FREQ = 1 / 42f;
    public const float DETAIL_STRENGTH = 0.05f;

    /** v4 density - same as v3 but generates biomes too */
    private void getDensityBiomes(float[] buffer, Chunk chunk) {
        var coord = chunk.coord;
        WorldgenUtil.getNoise3DRegion(tb, tn, coord, LOW_FREQ, LOW_FREQ * 2,
            LOW_FREQ, 8, 1 + Meth.rhoF * 2);
        WorldgenUtil.getNoise3DRegion(t2b, t2n, coord, HIGH_FREQ, HIGH_FREQ * 2,
            HIGH_FREQ, 5, 2 + Meth.rhoF);

        WorldgenUtil.getNoise3DRegion(sb, sn, coord, SELECTOR_FREQ, SELECTOR_FREQ / 2,
            SELECTOR_FREQ, 5, 2f);

        WorldgenUtil.getNoise2DRegion(eb, esn, coord, ELEVATION_FREQ, ELEVATION_FREQ, 10, 2f);
        WorldgenUtil.getNoise2DRegion(fb, fsn, coord, FRACT_FREQ, FRACT_FREQ, 8, 2f - Meth.d2r);
        // note: do NOT change the falloff from 2! BiomeData.fe()'s normaliser only works for that.
        chunk.biomeData.setChunk(chunk);

        int worldX = coord.x * Chunk.CHUNKSIZE;
        int worldZ = coord.z * Chunk.CHUNKSIZE;

        for (int nz = 0; nz < WorldgenUtil.NOISE_SIZE_Z; nz++) {
            int z = worldZ + nz * WorldgenUtil.NOISE_PER_Z;

            for (int nx = 0; nx < WorldgenUtil.NOISE_SIZE_X; nx++) {
                int x = worldX + nx * WorldgenUtil.NOISE_PER_X;

                var t = WorldgenUtil.getNoise3D(tempn, x * TEMP_FREQ, 0, z * TEMP_FREQ, 4, 2f);
                var h = WorldgenUtil.getNoise3D(humn, x * HUM_FREQ, 0, z * HUM_FREQ, 4, 2f);
                chunk.biomeData.set(nx, nz, (sbyte)(t * 127), (sbyte)(h * 127));
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
    private void generateSurfaceBiomes(Chunk chunk) {

        for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
            for (int x = 0; x < Chunk.CHUNKSIZE; x++) {
                var worldPos = World.toWorldPos(chunk.coord.x, chunk.coord.z, x, 0, z);
                int height = chunk.heightMap.get(x, z);

                while (height > 0 && !Block.fullBlock[chunk.getBlock(x, height, z)]) {
                    height--;
                }

                chunk.biomeData.sample(x, z, out var temp, out var hum);

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

                // biome-based surface selection
                var biome = Biomes.getType(temp, hum, height);
                var (topBlock, filler) = Biomes.getBlocks(biome, blockVar);

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