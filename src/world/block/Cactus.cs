using BlockGame.util;
using BlockGame.world.entity;
using BlockGame.world.item;

namespace BlockGame.world.block;

public class Cactus(string name) : Block(name) {
    /** cactus can only be placed on sand or another cactus */
    public override void update(World world, int x, int y, int z) {
        if (!canSurvive(world, x, y, z)) {
            world.setBlock(x, y, z, AIR.id);
            // drop as item
            world.spawnBlockDrop(x, y, z, getItem(), 1, 0);
        }
    }

    public bool canSurvive(World world, int x, int y, int z) {
        if (world.inWorld(x, y - 1, z)) {

            // if any block next to it is solid, cannot survive
            if (world.getBlock(x + 1, y, z) != AIR.id ||
                world.getBlock(x - 1, y, z) != AIR.id ||
                fullBlock[world.getBlock(x, y, z + 1)] ||
                fullBlock[world.getBlock(x, y, z - 1)]) {
                return false;
            }

            var below = world.getBlock(x, y - 1, z);
            return below == SAND.id || below == CACTUS.id;
        }
        return false;
    }

    /** cacti grow upward up to 3 blocks tall */
    public override void randomUpdate(World world, int x, int y, int z) {
        // check height - count how many cacti are below
        int height = 1;
        for (int yy = y - 1; yy >= 0 && world.getBlock(x, yy, z) == CACTUS.id; yy--) {
            height++;
        }

        // grow if height < 3 and space above is air
        if (height < 3 && y < World.WORLDHEIGHT - 1) {
            var above = world.getBlock(x, y + 1, z);
            if (above == AIR.id) {
                world.setBlock(x, y + 1, z, CACTUS.id);
            }
        }
    }

    public override void getDrop(List<ItemStack> drops, World world, int x, int y, int z, byte metadata, bool canBreak) {
        if (canBreak) {
            drops.Add(new ItemStack(getItem(), 1, 0));
        }
    }

    /** damage entities that touch the cactus */
    public override void interact(World world, int x, int y, int z, Entity e) {
        e.dmg(2);
    }
}