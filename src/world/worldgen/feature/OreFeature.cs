using System.Numerics;
using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.chunk;

namespace BlockGame.world.worldgen.feature;

public class OreFeature : Feature {

    public ushort block;
    public int minSteps;
    public int maxSteps;
    public float radius;
    public bool stoneMode = true; // only place in stone

    private int lx0, lx1, lz0, lz1;

    public OreFeature(ushort block, int steps, bool stoneMode = true) {
        this.block = block;
        this.stoneMode = stoneMode;

        // derive step variation
        minSteps = steps - 2;
        maxSteps = steps + 2;

        radius = 1f + steps * (1 / 24f);
    }

    public override void place(World world, XRandom random, int x, int y, int z) {
        var bl = world.getBlock(x, y, z);
        lx0 = ((x >> 4) - World.POPULATE_REACH) << 4;
        lx1 = (((x >> 4) + World.POPULATE_REACH + 1) << 4) - 1;
        lz0 = ((z >> 4) - World.POPULATE_REACH) << 4;
        lz1 = (((z >> 4) + World.POPULATE_REACH + 1) << 4) - 1;

        // only start in valid blocks
        if (stoneMode && bl != Block.STONE.id) {
            return;
        }

        if (!stoneMode && bl != Block.STONE.id && bl != Block.DIRT.id && bl != Block.HELLSTONE.id) {
            return;
        }

        // pick random direction
        var hAngle = random.NextSingle() * float.Pi * 2;
        var vAngle = (random.NextSingle() - 0.5f) * float.Pi * 0.3f;

        float vCos = MathF.Cos(vAngle);
        var dir = new Vector3(
            float.Cos(hAngle) * vCos,
            float.Sin(vAngle),
            float.Sin(hAngle) * vCos
        );

        // walk straight line with random radius at each step
        var steps = random.Next(minSteps, maxSteps + 1);

        // if we're outside, how about we dont
        var margin = radius + 1;
        var fwd = float.Min(raycast1d(x, dir.X, lx0, lx1), raycast1d(z, dir.Z, lz0, lz1)) - margin;
        var back = float.Min(raycast1d(x, -dir.X, lx0, lx1), raycast1d(z, -dir.Z, lz0, lz1)) - margin;
        var b = float.Min(steps, float.Max(fwd, 0));
        var a = float.Min(steps - b, float.Max(back, 0));
        var pos = new Vector3(x, y, z) - dir * a;
        var c = default(OreCache);

        var prev = pos;
        var prevRadSq = -1f;
        for (int i = 0; i < steps; i++) {
            var radius = random.NextSingle() * this.radius;
            placeSphere(world, pos, radius, prev, prevRadSq, ref c);
            prev = pos;
            prevRadSq = radius * radius;
            pos += dir;
        }
    }


    private static float raycast1d(float p, float d, int n, int x) {
        return d > 0 ? (x - p) / d : d < 0 ? (p - n) / -d : float.MaxValue;
    }

    private struct OreCache {
        private ChunkCoord coord;
        private Chunk? chunk;
        private bool valid;

        public Chunk? get(World world, int x, int z) {
            var c = new ChunkCoord(x >> 4, z >> 4);
            if (!valid || c != coord) {
                valid = true;
                coord = c;
                world.getChunkMaybe(c, out chunk);
            }

            return chunk;
        }
    }

    private void placeSphere(World world, Vector3 center, float radius, Vector3 prev, float prevRadSq, ref OreCache c) {
        int y0 = Math.Max(0, (int)(center.Y - radius));
        int y1 = Math.Min(World.WORLDHEIGHT, (int)(center.Y + radius) + 1);
        int z0 = int.Max(lz0, (int)(center.Z - radius));
        int z1 = int.Min(lz1 + 1, (int)(center.Z + radius) + 1);

        float radSq = radius * radius;
        for (int zz = z0; zz < z1; zz++) {
            float dz = zz - center.Z;
            float dz2 = dz * dz;
            if (dz2 > radSq) {
                continue;
            }

            float pdz = zz - prev.Z;
            float pdz2 = pdz * pdz;

            for (int yy = y0; yy < y1; yy++) {
                float dy = yy - center.Y;
                float rem = radSq - dz2 - dy * dy;
                if (rem < 0) {
                    continue;
                }

                float pdy = yy - prev.Y;
                float pdyz2 = pdz2 + pdy * pdy;

                // dx^2 <= rem
                float half = float.Sqrt(rem);
                int x0 = int.Max(lx0, (int)float.Ceiling(center.X - half));
                int x1 = int.Min(lx1, (int)float.Floor(center.X + half));
                for (int xx = x0; xx <= x1; xx++) {
                    float pdx = xx - prev.X;
                    if (pdyz2 + pdx * pdx <= prevRadSq) {
                        continue;
                    }

                    var chunk = c.get(world, xx, zz);
                    if (chunk == null) {
                        continue;
                    }

                    var bl = chunk.getBlock(xx & 0xF, yy, zz & 0xF);

                    if (bl == block) {
                        continue;
                    }

                    // only replace valid blocks
                    if (stoneMode && bl != Block.STONE.id) {
                        continue;
                    }

                    if (!stoneMode && bl != Block.STONE.id && bl != Block.DIRT.id && bl != Block.HELLSTONE.id) {
                        continue;
                    }

                    chunk.setBlockDumb(xx & 0xF, yy, zz & 0xF, block);
                }
            }
        }
    }
}
