using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.item.inventory;

namespace BlockGame.world.item;

// 1 dye + 1 candy = dyed candy
public class CandyDyeRecipe : Recipe {
    public override bool matches(CraftingGridInventory grid) {
        int dyeCount = 0;
        int candyCount = 0;

        foreach (var slot in grid.grid) {
            if (isEmpty(slot)) {
                continue;
            }

            if (slot.id == Item.DYE.id) {
                dyeCount++;
            } else if (slot.id == Block.CANDY.getItem().id) {
                candyCount++;
            } else {
                // invalid item
                return false;
            }
        }

        return dyeCount == 1 && candyCount == 1;
    }

    public override bool matchesShape(CraftingGridInventory grid) {
        return matches(grid);
    }

    public override ItemStack getResult(CraftingGridInventory grid) {
        int dyeMeta = -1;

        foreach (var slot in grid.grid) {
            if (!isEmpty(slot) && slot.id == Item.DYE.id) {
                dyeMeta = slot.metadata;
                break;
            }
        }

        if (dyeMeta == -1) {
            return ItemStack.EMPTY;
        }

        // return dyed candy
        return new ItemStack(Block.CANDY.getItem(), 1, dyeMeta);
    }

    public override void consumeIngredients(CraftingGridInventory grid) {
        bool dyeConsumed = false;
        bool candyConsumed = false;

        for (int i = 0; i < grid.grid.Length; i++) {
            var slot = grid.grid[i];

            if (isEmpty(slot)) {
                continue;
            }

            if (!dyeConsumed && slot.id == Item.DYE.id) {
                slot.quantity--;
                if (slot.quantity <= 0) {
                    grid.grid[i] = ItemStack.EMPTY;
                }
                dyeConsumed = true;
            } else if (!candyConsumed && slot.id == Block.CANDY.getItem().id) {
                slot.quantity--;
                if (slot.quantity <= 0) {
                    grid.grid[i] = ItemStack.EMPTY;
                }
                candyConsumed = true;
            }

            if (dyeConsumed && candyConsumed) {
                break;
            }
        }
    }
}