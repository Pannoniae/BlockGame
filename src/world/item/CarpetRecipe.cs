using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.item.inventory;

namespace BlockGame.world.item;

using static DyeItem;

/** Recipe for mixing any 2 carpets into their average colour */
public class CarpetRecipe : Recipe {
    public override bool matches(CraftingGridInventory grid) {
        return countCarpets(grid) == 2 && countNonEmpty(grid) == 2;
    }

    public override bool matchesShape(CraftingGridInventory grid) {
        return matches(grid);
    }

    public override ItemStack getResult(CraftingGridInventory grid) {
        var metas = new List<int>(2);

        foreach (var slot in grid.grid) {
            if (!isEmpty(slot) && slot.id == Block.CARPET.item.id) {
                // extract color from metadata (carpets have orientation too)
                byte color = Carpet.getColor((byte)slot.metadata);
                metas.Add(color);
            }
        }

        if (metas.Count != 2) return ItemStack.EMPTY;

        int resultColor = mixColours(metas[0], metas[1]);

        // create carpet with mixed color (default floor orientation)
        byte resultMeta = 0;
        resultMeta = Carpet.setColor(resultMeta, (byte)resultColor);
        resultMeta = Carpet.setOrientation(resultMeta, Carpet.FLOOR);

        return new ItemStack(Block.CARPET.item, 1, resultMeta);
    }

    public override void consumeIngredients(CraftingGridInventory grid) {
        int consumed = 0;
        for (int i = 0; i < grid.grid.Length && consumed < 2; i++) {
            var slot = grid.grid[i];
            if (!isEmpty(slot) && slot.id == Block.CARPET.item.id) {
                slot.quantity--;
                if (slot.quantity <= 0) {
                    grid.grid[i] = ItemStack.EMPTY;
                }
                consumed++;
            }
        }
    }

    private static int countCarpets(CraftingGridInventory grid) {
        int cnt = 0;
        foreach (var slot in grid.grid) {
            if (!isEmpty(slot) && slot.id == Block.CARPET.item.id) cnt++;
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
