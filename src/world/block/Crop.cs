using BlockGame.main;
using BlockGame.util;
using BlockGame.world.item;

namespace BlockGame.world.block;

public class Crop : Block {
    public int stages;
    public Item product;
    public Item seedItem;

    public Crop(string name, int i) : base(name) {
        stages = i;
    }

    protected override void onRegister(int id) {
        base.onRegister(id);

        customAABB[id] = true;
        partialBlock();
        noCollision();
        transparency();
        itemLike();
        // we need randomtick to grow!
        tick();
    }

    public static bool canSurvive(World world, int x, int y, int z) {
        var blockBelow = world.getBlock(x, y - 1, z);
        return blockBelow == FARMLAND.id;
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
        byte metadata = world.getBlockMetadata(x, y, z);

        // already fully grown?
        if (metadata >= stages - 1) {
            return;
        }

        // need light level 9+
        if (world.getLight(x, y, z) < 9) {
            return;
        }

        // count water blocks nearby (4 block radius)
        int waterCount = 0;
        for (int dx = -4; dx <= 4; dx++) {
            for (int dz = -4; dz <= 4; dz++) {
                for (int dy = 0; dy <= 1; dy++) {
                    if (world.getBlock(x + dx, y + dy, z + dz) == WATER.id) {
                        waterCount++;
                    }
                }
            }
        }

        // count same crop neighbours (3x3 area, same Y)
        int cropNeighbours = 0;
        for (int dx = -1; dx <= 1; dx++) {
            for (int dz = -1; dz <= 1; dz++) {
                if (dx == 0 && dz == 0) continue; // skip self
                if (world.getBlock(x + dx, y, z + dz) == id) {
                    cropNeighbours++;
                }
            }
        }

        // base growth chance (50%)
        float growthChance = 0.5f;

        // more water = faster
        growthChance += waterCount * (1 / 16f);

        // crop social distancing:tm:: too many neighbours = overcrowded
        var neighboursStat = (1 - (cropNeighbours * (1 / 8f)));
        growthChance *= neighboursStat;
        // 0-2 neighbours = no penalty (crops like a little personal space)

        // roll the dice
        if (world.random.NextDouble() >= growthChance) {
            return;
        }

        // grow
        world.setBlockMetadata(x, y, z, ((uint)id).setMetadata((byte)(metadata + 1)));
    }

    public override void getDrop(List<ItemStack> drops, World world, int x, int y, int z, byte metadata, bool canBreak) {
        // if fully grown, drop the product and seeds
        if (metadata >= stages - 1) {
            var seedCount = world.random.Next(1, 4);
            var productCount = canBreak ? Game.random.Next(1, 2) : 1;
            drops.Add(new ItemStack(product, productCount, 0));
            drops.Add(new ItemStack(seedItem, seedCount, 0));
            return;
        }

        // otherwise just drop seeds
        drops.Add(new ItemStack(seedItem, 1, 0));
    }


    public override UVPair getTexture(int faceIdx, int metadata) {
        return uvs[Math.Clamp(metadata, 0, stages - 1)];
    }

    public override ItemStack getActualItem(byte metadata) {
        return new ItemStack(product, 1, 0);
    }

    public override void getAABBs(World world, int x, int y, int z, byte metadata, List<AABB> aabbs) {
        float h = 0.125f + (metadata / (float)(stages - 1)) * 0.875f;
        aabbs.Clear();
        aabbs.Add(new AABB(x, y, z, x + 1f, y + h, z + 1f));
    }

    public override byte maxValidMetadata() {
        return (byte)(stages - 1);
    }
}