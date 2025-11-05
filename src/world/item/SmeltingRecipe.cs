using BlockGame.util;
using BlockGame.world.block;

namespace BlockGame.world.item;

public class SmeltingRecipe {
    public static SmeltingRecipe IRON_INGOT;
    public static SmeltingRecipe GOLD_INGOT;
    public static SmeltingRecipe COPPER_INGOT;
    public static SmeltingRecipe TIN_INGOT;
    public static SmeltingRecipe SILVER_INGOT;
    public static SmeltingRecipe BRICK;
    public static SmeltingRecipe STEAK;
    public static SmeltingRecipe FRIED_PORKCHOP;
    public static SmeltingRecipe GLASS;

    private static readonly List<SmeltingRecipe> recipes = [];

    private Item input;
    private ItemStack output;
    private int smeltTime; // ticks to smelt

    public Item getInput() => input;
    public ItemStack getOutput() => output;
    public int getSmeltTime() => smeltTime;

    public static void preLoad() {
        // ores -> ingots (8 seconds = 480 ticks)
        IRON_INGOT = register(Block.IRON_ORE.item, new ItemStack(Item.IRON_INGOT, 1), 480);
        GOLD_INGOT = register(Block.GOLD_ORE.item, new ItemStack(Item.GOLD_INGOT, 1), 480);
        COPPER_INGOT = register(Block.COPPER_ORE.item, new ItemStack(Item.COPPER_INGOT, 1), 480);
        TIN_INGOT = register(Block.TIN_ORE.item, new ItemStack(Item.TIN_INGOT, 1), 480);
        SILVER_INGOT = register(Block.SILVER_ORE.item, new ItemStack(Item.SILVER_INGOT, 1), 480);

        // clay -> brick (5 seconds = 300 ticks)
        BRICK = register(Item.CLAY, new ItemStack(Item.BRICK, 1), 300);

        // raw steak -> steak (8 seconds = 480 ticks)
        STEAK = register(Item.RAW_BEEF, new ItemStack(Item.STEAK, 1), 480);

        // porkchop -> fried porkchop (6 seconds = 360 ticks)
        FRIED_PORKCHOP = register(Item.PORKCHOP, new ItemStack(Item.FRIED_PORKCHOP, 1), 360);

        // sand -> glass (6 seconds = 360 ticks)
        GLASS = register(Block.SAND.item, new ItemStack(Block.GLASS.item, 1), 360);
    }

    private static SmeltingRecipe register(Item input, ItemStack output, int smeltTime) {
        var recipe = new SmeltingRecipe {
            input = input,
            output = output,
            smeltTime = smeltTime
        };
        recipes.Add(recipe);
        return recipe;
    }

    /** find recipe for given input item, or null */
    public static SmeltingRecipe? findRecipe(Item input) {
        foreach (var recipe in recipes) {
            if (recipe.input.id == input.id) {
                return recipe;
            }
        }
        return null;
    }
}