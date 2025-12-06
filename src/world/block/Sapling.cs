using BlockGame.util;
using BlockGame.world.entity;
using BlockGame.world.worldgen;
using Molten;

namespace BlockGame.world.block;

/**
 * Sapling that grows into a tree.
 * Growth is affected by quantum observation - slows down when player is nearby.
 */
public class Sapling : Block {
    private readonly SaplingType treeType;

    /** Base growth chance per tick (1/chance) */
    private const int GROWTH_CHANCE = 20;

    private const double RADIUS = 16.0;

    private const int OBSERVE = 4;

    public Sapling(string name, SaplingType type) : base(name) {
        this.treeType = type;
    }

    protected override void onRegister(int id) {
        tick(); // enable random ticking
        AABB[id] = new AABB(0.25f, 0.0f, 0.25f, 0.75f, 0.75f, 0.75f);
    }

    public override bool canPlace(World world, int x, int y, int z, Placement info) {
        return canSurvive(world, x, y, z);
    }

    public override void update(World world, int x, int y, int z) {
        if (!canSurvive(world, x, y, z)) {
            world.setBlock(x, y, z, AIR.id);
        }
    }


    public override void randomUpdate(World world, int x, int y, int z) {
        // check if there's enough space above
        if (!canGrow(world, x, y, z)) {
            return;
        }

        int growth = GROWTH_CHANCE;
        // shit SP code
        //var dist = Vector3D.Distance(Game.player.position, new Vector3D(x + 0.5, y + 0.5, z + 0.5));

        var player = findNearestPlayer(world, new Vector3I(x, y, z), RADIUS, out var sq);

        if (sq < RADIUS * RADIUS) {
            growth *= OBSERVE;
        }

        if (world.random.Next(growth) != 0) {
            return;
        }

        world.setBlock(x, y, z, AIR.id);
        growTree(world, world.random, x, y, z);
    }

    /** Check if sapling can grow (needs space above and valid ground) */
    private static bool canGrow(World world, int x, int y, int z) {
        // check ground block below
        if (y <= 0 || !world.inWorld(x, y - 1, z)) {
            return false;
        }

        var below = world.getBlock(x, y - 1, z);
        if (below != GRASS.id && below != DIRT.id && below != SNOW_GRASS.id) {
            return false;
        }

        int minHeight = 6;
        for (int dy = 1; dy <= minHeight; dy++) {
            if (!world.inWorld(x, y + dy, z)) {
                return false;
            }
            var blockAbove = world.getBlock(x, y + dy, z);
            if (fullBlock[blockAbove]) {
                return false;
            }
        }

        return true;
    }

    /** Grow the appropriate tree type */
    private void growTree(World world, XRandom random, int x, int y, int z) {
        switch (treeType) {
            case SaplingType.OAK:
                // 20% fancy, 80% normal
                if (random.Next(5) == 0) {
                    TreeGenerator.placeFancyTree(world, random, x, y, z);
                } else {
                    TreeGenerator.placeOakTree(world, random, x, y, z);
                }
                break;

            case SaplingType.MAPLE:
                TreeGenerator.placeMapleTree(world, random, x, y, z);
                break;

            case SaplingType.MAHOGANY:
                TreeGenerator.placeMahoganyTree(world, random, x, y, z);
                break;

            case SaplingType.PINE:
                TreeGenerator.placePineTree(world, random, x, y, z);
                break;
        }
    }

    /** checks if sapling can survive at this position */
    public static bool canSurvive(World world, int x, int y, int z) {
        if (y <= 0 || !world.inWorld(x, y - 1, z)) {
            return false;
        }

        var below = world.getBlock(x, y - 1, z);
        if (below != GRASS.id && below != DIRT.id && below != SNOW_GRASS.id) {
            return false;
        }

        return true;
    }

    public static Player? findNearestPlayer(World world, Vector3I pos, double radius, out double nearestDistSq) {
        Player? nearest = null;
        nearestDistSq = radius * radius;

        foreach (var entity in world.players) {
            if (entity.gameMode == null) {
                // todo this is a horrible hack, find out why it's not initialised fully yet
                continue;
            }


            if (entity.gameMode.gameplay) {
                var distSq = Vector3I.DistanceSquared(pos, entity.position.toBlockPos());
                if (distSq < nearestDistSq) {
                    nearest = entity;
                    nearestDistSq = distSq;
                }
            }
        }

        return nearest;
    }
}

/** Tree types for saplings */
public enum SaplingType {
    OAK,
    MAPLE,
    MAHOGANY,
    PINE,
    PALM,
    REDWOOD
}