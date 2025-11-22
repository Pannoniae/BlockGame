using BlockGame.util;

namespace BlockGame.world.item;

using inventory;

/** Recipe for mixing any 2 dyes into their average colour */
public class DyeRecipe : Recipe {
    public override bool matches(CraftingGridInventory grid) {
        return countDyes(grid) == 2 && countNonEmpty(grid) == 2;
    }

    public override bool matchesShape(CraftingGridInventory grid) {
        return matches(grid);
    }

    public override ItemStack getResult(CraftingGridInventory grid) {
        var metas = new List<int>(2);

        foreach (var slot in grid.grid) {
            if (!isEmpty(slot) && slot.id == Item.DYE.id) {
                metas.Add(slot.metadata);
            }
        }

        if (metas.Count != 2) return ItemStack.EMPTY;

        int resultMeta = DyeItem.mixColours(metas[0], metas[1]);
        return new ItemStack(Item.DYE, 1, resultMeta);
    }

    public override void consumeIngredients(CraftingGridInventory grid) {
        int consumed = 0;
        for (int i = 0; i < grid.grid.Length && consumed < 2; i++) {
            var slot = grid.grid[i];
            if (!isEmpty(slot) && slot.id == Item.DYE.id) {
                slot.quantity--;
                if (slot.quantity <= 0) {
                    grid.grid[i] = ItemStack.EMPTY;
                }
                consumed++;
            }
        }
    }

    private static int countDyes(CraftingGridInventory grid) {
        int cnt = 0;
        foreach (var slot in grid.grid) {
            if (!isEmpty(slot) && slot.id == Item.DYE.id) cnt++;
        }
        return cnt;
    }

    private static int countNonEmpty(CraftingGridInventory grid) {
        int cnt = 0;
        foreach (var slot in grid.grid) {
            if (!isEmpty(slot)) cnt++;
        }
        return cnt;
    }

    private static bool isEmpty(ItemStack slot) => slot == ItemStack.EMPTY || slot.quantity <= 0;
}