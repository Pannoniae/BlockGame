using System.Numerics;
using BlockGame.util;
using BlockGame.world.block;

namespace BlockGame.world.worldgen.feature;

public class OreFeature : Feature {

    public ushort block;
    public int minSteps;
    public int maxSteps;
    public float radius;
    public bool stoneMode = true; // only place in stone

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
        var pos = new Vector3(x, y, z);

        for (int i = 0; i < steps; i++) {
            var radius = random.NextSingle() * this.radius;
            placeSphere(world, pos, radius);
            pos += dir;
        }
    }

    private void placeSphere(World world, Vector3 center, float radius) {
        int xMin = (int)(center.X - radius);
        int xMax = (int)(center.X + radius) + 1;
        int yMin = (int)(center.Y - radius);
        int yMax = (int)(center.Y + radius) + 1;
        int zMin = (int)(center.Z - radius);
        int zMax = (int)(center.Z + radius) + 1;

        // clamp to world bounds
        yMin = Math.Max(0, yMin);
        yMax = Math.Min(World.WORLDHEIGHT, yMax);

        float radSq = radius * radius;
        for (int zz = zMin; zz < zMax; zz++) {
            for (int yy = yMin; yy < yMax; yy++) {
                for (int xx = xMin; xx < xMax; xx++) {
                    float dx = xx - center.X;
                    float dy = yy - center.Y;
                    float dz = zz - center.Z;
                    float distSq = dx * dx + dy * dy + dz * dz;

                    if (distSq <= radSq) {
                        var bl = world.getBlock(xx, yy, zz);

                        // only replace valid blocks
                        if (stoneMode && bl != Block.STONE.id) continue;
                        if (!stoneMode && bl != Block.STONE.id && bl != Block.DIRT.id && bl != Block.HELLSTONE.id) continue;

                        world.setBlockDumb(xx, yy, zz, block);
                    }
                }
            }
        }
    }
}
