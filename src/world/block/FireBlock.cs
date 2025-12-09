using BlockGame.util;
using BlockGame.world.item;

namespace BlockGame.world.block;

public class FireBlock(string name) : Block(name) {

    public override void update(World world, int x, int y, int z) {
        if (!canSurvive(world, x, y, z)) {
            world.setBlock(x, y, z, AIR.id);
        }
    }

    public override bool canPlace(World world, int x, int y, int z, Placement info) {
        return canSurvive(world, x, y, z);
    }

    public override void randomUpdate(World world, int x, int y, int z) {
        byte age = world.getBlockMetadata(x, y, z);

        age++;

        if (age > 15) {
            world.setBlock(x, y, z, AIR.id);
            return;
        }

        world.setBlockMetadata(x, y, z, ((uint)id).setMetadata(age));

        // try to burn blocks beneath/adjacent
        tryBurnBlock(world, x, y - 1, z);

        // try to spread fire to flammable neighbours
        int attempts = 3 + (age / 4); // younger fire spreads more aggressively
        for (int i = 0; i < attempts; i++) {
            var dir = Direction.directionsAll[world.random.Next(27)];

            if (dir.X == 0 && dir.Y == 0 && dir.Z == 0) continue; // skip self

            int nx = x + dir.X;
            int ny = y + dir.Y;
            int nz = z + dir.Z;

            ushort target = world.getBlock(nx, ny, nz);

            if (target == AIR.id || !waterSolid[target]) {
                // can place fire here, check if adjacent to flammable
                if (hasFlammableNeighbour(world, nx, ny, nz)) {
                    double spreadChance = world.random.NextDouble();
                    // upward spread is 2x more likely
                    double multiplier = (dir.Y > 0) ? 2.0 : 1.0;

                    if (spreadChance < 0.15 * multiplier) {
                        world.setBlock(nx, ny, nz, FIRE.id);
                    }
                }
            } else {
                tryBurnBlock(world, nx, ny, nz);
            }
        }
    }

    /** tries to burn a flammable block, replacing it with air */
    private static void tryBurnBlock(World world, int x, int y, int z) {
        ushort block = world.getBlock(x, y, z);
        if (block == AIR.id) {
            return;
        }

        double flammability = flammable[block];
        if (flammability > 0) {
            double burnChance = flammability / 100.0;
            if (world.random.NextDouble() < burnChance * 2.5) { // scaled chance lol
                world.setBlock(x, y, z, AIR.id);
                // TODO: drop nothing or drop with reduced chance
            }
        }
    }

    /** checks if any neighbour is flammable */
    private static bool hasFlammableNeighbour(World world, int x, int y, int z) {
        // check 6 faces
        foreach (var dir in Direction.directions) {
            if (flammable[world.getBlock(x + dir.X, y + dir.Y, z + dir.Z)] > 0) {
                return true;
            }
        }

        // check if on top of hellstone/similar (infinite fire)
        ushort blockBelow = world.getBlock(x, y - 1, z);
        if (blockBelow == HELLSTONE.id || blockBelow == HELLROCK.id) return true;

        return false;
    }

    /** checks if fire can survive at this position */
    public static bool canSurvive(World world, int x, int y, int z) {
        // always in air
        ushort block = world.getBlock(x, y, z);
        if (block != AIR.id) {
            return false;
        }

        // can't be in water
        if (liquid[block]) {
            return false;
        }


        // fire needs either a solid block beneath or adjacent flammable
        var blockBelow = world.getBlock(x, y - 1, z);

        // infinite fire on hellstone-type blocks
        // needs solid support or flammable neighbour
        return blockBelow == HELLSTONE.id || blockBelow == HELLROCK.id || collision[blockBelow] || hasFlammableNeighbour(world, x, y, z);
    }

    public override void getDrop(List<ItemStack> drops, World world, int x, int y, int z, byte metadata, bool canBreak) {
        // fire drops nothing
    }
}