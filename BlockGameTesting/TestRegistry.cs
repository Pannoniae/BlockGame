using BlockGame.main;
using BlockGame.world.block;
using BlockGame.world.block.entity;
using BlockGame.world.entity;
using BlockGame.world.item;

namespace BlockGameTesting;

/** block/item registration is process-global and blows up if run twice. */
public static class TestRegistry {
    private static bool done;

    public static void ensure() {
        if (done) {
            return;
        }
        done = true;
        Net.mode = NetMode.DED;
        Block.preLoad();
        Item.preLoad();
        Entities.preLoad();
        BlockEntity.preLoad();
        Recipe.preLoad();
        SmeltingRecipe.preLoad();
        Block.postLoad();
    }
}
