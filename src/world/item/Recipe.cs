using BlockGame.world.block;

namespace BlockGame.world.item;

public class Recipe {
    public static Recipe WOOD_PICKAXE;

    public static Recipe[] recipes;

    public static void preLoad() {
        WOOD_PICKAXE = register(Item.AIR);
        WOOD_PICKAXE.shape(111_020_020);
        WOOD_PICKAXE.ingredients(
            Item.block(Blocks.PLANKS),
            Item.block(Blocks.PLANKS));
    }
    
    // todo add ItemStack result too for metadata items, maybe NBT items in the future too...

    public static Recipe register(Item result) {
        // blah blah blah
        return new Recipe();
    }

    public Recipe shape(int shape) {
        return this;
    }

    public Recipe ingredients(params ReadOnlySpan<Item> ingredients) {
        return this;
    }
}