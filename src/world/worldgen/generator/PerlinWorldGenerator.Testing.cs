using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BlockGame.world.chunk;

namespace BlockGame.world.worldgen.generator;

public partial class PerlinWorldGenerator {
    private float interpolatePure(float[] buffer, ChunkCoord coord) {
        float i = 0;
        
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
                    i = value;
                }
            }
        }

        return i;
    }

    private float interpolateSIMDPure(float[] buffer, ChunkCoord coord) {
        // we do this so we don't check "is chunk initialised" every single time we set a block.
        // this cuts our asm code size by half lol from all that inlining and shit
        // TODO it doesnt work properly, I'll fix it later
        // Span<bool> initialised = stackalloc bool[Chunk.CHUNKHEIGHT];

        float i = 0;

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

                    // load the eight corner values from the buffer into a vector
                    var c000 = buffer[getIndex(x0, y0, z0)];
                    var c001 = buffer[getIndex(x0, y0, z1)];
                    var c010 = buffer[getIndex(x0, y1, z0)];
                    var c011 = buffer[getIndex(x0, y1, z1)];
                    var c100 = buffer[getIndex(x1, y0, z0)];
                    var c101 = buffer[getIndex(x1, y0, z1)];
                    var c110 = buffer[getIndex(x1, y1, z0)];
                    var c111 = buffer[getIndex(x1, y1, z1)];


                    var values1 = Vector128.Create(c000, c001, c010, c011);
                    var values2 = Vector128.Create(c100, c101, c110, c111);

                    // the two vectors contain the two halves of the cube to be interpolated.
                    // We need to interpolate element-wise.
                    var interp = lerp4(values1, values2, Vector128.Create(xd));

                    // now the vector contains the 4 interpolated values. interpolate narrower (2x2)
                    var low1 = interp[0];
                    var low2 = interp[1];
                    var high1 = interp[1];
                    var high2 = interp[2];
                    
                    // now we have 4 values, we need to interpolate them along y
                    var c0 = lerp(low1, high1, yd);
                    var c1 = lerp(low2, high2, yd);
                    // now we have 2 values, we need to interpolate them along z
                    var value = lerp(c0, c1, zd);
                    
                    i = value;
                }
            }
        }
        return i;
    }
    
    private Vector128<float> interpolateSIMDBatchPure(float[] buffer, ChunkCoord coord) {
        Vector128<float> i = Vector128<float>.Zero;
        
        // we do this so we don't check "is chunk initialised" every single time we set a block.
        // this cuts our asm code size by half lol from all that inlining and shit
        // TODO it doesnt work properly, I'll fix it later
        // Span<bool> initialised = stackalloc bool[Chunk.CHUNKHEIGHT];
        const int xc = Chunk.CHUNKSIZE / 4;

        for (int y = 0; y < Chunk.CHUNKSIZE * Chunk.CHUNKHEIGHT; y++) {
            var y0 = y >> NOISE_PER_Y_SHIFT;
            var y1 = y0 + 1;
            float yd = (y & NOISE_PER_Y_MASK) * NOISE_PER_Y_INV;
            var vyd = Vector128.Create(yd);

            for (int z = 0; z < Chunk.CHUNKSIZE; z++) {
                var z0 = z >> NOISE_PER_Z_SHIFT;
                var z1 = z0 + 1;
                float zd = (z & NOISE_PER_Z_MASK) * NOISE_PER_Z_INV;
                var vzd = Vector128.Create(zd);

                for (int xi = 0; xi < xc; xi++) {
                    // 4 per iteration
                    var x = xi << 2;
                    
                    // the grid cell that contains the point
                    //var x0 = x >> NOISE_PER_X_SHIFT;
                    //var x1 = x0 + 1;
                    var x1 = xi + 1;
                    var xs = Vector128.CreateSequence(x, 1);
                    //var vx0 = Vector128.ShiftRightArithmetic(xs, NOISE_PER_X_SHIFT);
                    //var vx1 = Vector128.Add(vx0, Vector128<int>.One);

                    // the lerp (between 0 and 1)
                    //float xd = (x & NOISE_PER_X_MASK) * NOISE_PER_X_INV;
                    
                    var vxd = Sse2.ConvertToVector128Single(Sse2.And(xs, Vector128.Create(NOISE_PER_X_MASK))) * NOISE_PER_X_INV;

                    // the eight corner values from the buffer
                    /*var c000 = buffer[getIndex(x0, y0, z0)];
                    var c001 = buffer[getIndex(x0, y0, z1)];
                    var c010 = buffer[getIndex(x0, y1, z0)];
                    var c011 = buffer[getIndex(x0, y1, z1)];
                    var c100 = buffer[getIndex(x1, y0, z0)];
                    var c101 = buffer[getIndex(x1, y0, z1)];
                    var c110 = buffer[getIndex(x1, y1, z0)];
                    var c111 = buffer[getIndex(x1, y1, z1)];*/
                    var c000 = Vector128.Create(buffer[getIndex(xi, y0, z0)]);
                    var c001 = Vector128.Create(buffer[getIndex(xi, y0, z1)]);
                    var c010 = Vector128.Create(buffer[getIndex(xi, y1, z0)]);
                    var c011 = Vector128.Create(buffer[getIndex(xi, y1, z1)]);
                    var c100 = Vector128.Create(buffer[getIndex(x1, y0, z0)]);
                    var c101 = Vector128.Create(buffer[getIndex(x1, y0, z1)]);
                    var c110 = Vector128.Create(buffer[getIndex(x1, y1, z0)]);
                    var c111 = Vector128.Create(buffer[getIndex(x1, y1, z1)]);

                    // Interpolate along x
                    var c00 = lerp4(c000, c100, vxd);
                    var c01 = lerp4(c001, c101, vxd);
                    var c10 = lerp4(c010, c110, vxd);
                    var c11 = lerp4(c011, c111, vxd);

                    // Interpolate along y
                    var c0 = lerp4(c00, c10, vyd);
                    var c1 = lerp4(c01, c11, vyd);

                    // Interpolate along z
                    var value = lerp4(c0, c1, vzd);
                    i = value;
                }
            }
        }

        return i;
    }
}