using BlockGame.util;
using BlockGame.world.entity;
using Molten;

namespace BlockGame.world.block;

public class Farmland : Block {
    public Farmland(string name) : base(name) {

    }

    protected override void onRegister(int id) {
        // custom AABB
        customAABB[id] = true;
        partialBlock();
        tick(); // for hydration
    }

    public override void onBreak(World world, int x, int y, int z, byte metadata) {
        // break crop above if present
        var above = world.getBlock(x, y + 1, z);
        if (above != 0 && get(above) is Crop) {
            world.setBlock(x, y + 1, z, AIR.id);
        }
    }

    public override void onPlace(World world, int x, int y, int z, byte metadata) {
        // schedule update after 2 ticks to check for nearby water
        world.scheduleBlockUpdate(new Vector3I(x, y, z), 2);
    }

    public override void scheduledUpdate(World world, int x, int y, int z) {
        byte currentMeta = world.getBlockMetadata(x, y, z);
        bool hasWater = isNearWater(world, x, y, z);

        if (hasWater && currentMeta == 0) {
            // hydrate
            world.setBlockMetadata(x, y, z, ((uint)id).setMetadata(1));
        } else if (!hasWater && currentMeta > 0) {
            // dry out
            world.setBlockMetadata(x, y, z, ((uint)id).setMetadata(0));
        }
    }

    public override void randomUpdate(World world, int x, int y, int z) {
        byte currentMeta = world.getBlockMetadata(x, y, z);
        bool hasWater = isNearWater(world, x, y, z);

        if (hasWater && currentMeta == 0) {
            // hydrate
            world.setBlockMetadata(x, y, z, ((uint)id).setMetadata(1));
        } else if (!hasWater && currentMeta > 0) {
            // dry out
            world.setBlockMetadata(x, y, z, ((uint)id).setMetadata(0));
        }
    }

    public override void onStepped(World world, int x, int y, int z, Entity entity) {
        // only if mob! we don't want items and shit to trample
        if (entity is not Mob) {
            return;
        }

        // trample (unless sneaking)
        if (!entity.sneaking) {
            if (world.random.NextDouble() < 1 / 16f) {
                world.setBlock(x, y, z, DIRT.id);
            }
        }
    }

    /** check if water is within 4 blocks horizontally */
    private static bool isNearWater(World world, int x, int y, int z) {
        for (int dx = -4; dx <= 4; dx++) {
            for (int dz = -4; dz <= 4; dz++) {
                for (int dy = 0; dy <= 1; dy++) {
                    var block = world.getBlock(x + dx, y + dy, z + dz);
                    if (block == WATER.id) {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public override void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        aabbs.Clear();
        aabbs.Add(new AABB(x + 0, y + 0, z + 0, x + 1, y + 15 / 16f, z + 1)); // 15/16 height
    }

    public override UVPair getTexture(int faceIdx, int metadata) {
        var top = metadata > 0 ? uvs[6] : uvs[5];
        return faceIdx switch {
            5 => top,    // top
            _ => uvs[faceIdx]
        };
    }

    public override byte maxValidMetadata() {
        return 1;
    }
}