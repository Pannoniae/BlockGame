using BlockGame.util;
using BlockGame.world.entity;

namespace BlockGame.world.block;

/** small bush that grows into a mature bush */
public class BushSapling : Block {
    private readonly Block matureBush;
    private const int GROWTH_CHANCE = 20;

    public BushSapling(string name, Block matureBush) : base(name) {
        this.matureBush = matureBush;
    }

    protected override void onRegister(int id) {
        transparency();
        tick(); // enable random ticking
        material(Material.ORGANIC);
        setHardness(0.1);
        noCollision();
        itemLike();
    }

    public override bool canPlace(World world, int x, int y, int z, Placement info) {
        // can be placed on grass, dirt, or snow grass
        if (y <= 0 || !world.inWorld(x, y - 1, z)) {
            return false;
        }

        var below = world.getBlock(x, y - 1, z);
        return below == GRASS.id || below == DIRT.id || below == SNOW_GRASS.id;
    }

    public override void update(World world, int x, int y, int z) {
        // break if no ground below
        if (world.inWorld(x, y - 1, z) && world.getBlock(x, y - 1, z) == 0) {
            world.setBlock(x, y, z, AIR.id);
        }
    }

    public override void randomUpdate(World world, int x, int y, int z) {
        // grow into mature bush with 1/20 chance
        if (world.random.Next(GROWTH_CHANCE) == 0) {
            world.setBlock(x, y, z, matureBush.id);
            // set mature bush metadata to 2 (mature, ready to harvest)
            world.setMetadata(x, y, z, 2);
        }
    }

    public override void getDrop(List<ItemStack> drops, World world, int x, int y, int z, byte metadata, bool canBreak) {
        // drop the mature bush's seeds when broken
        if (matureBush is Bush bush && bush.seed != null && canBreak) {
            drops.Add(new ItemStack(bush.seed, 1, 0));
        }
    }
}