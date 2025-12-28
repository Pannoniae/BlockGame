using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.item.inventory;

namespace BlockGame.world.item;

using static DyeItem;

// 1 dye + 1 carpet = dyed carpet
public class CarpetDyeRecipe : Recipe {
    public override bool matches(CraftingGridInventory grid) {
        int dyeCount = 0;
        int carpetCount = 0;

        foreach (var slot in grid.grid) {
            if (isEmpty(slot)) {
                continue;
            }

            if (slot.id == Item.DYE.id) {
                dyeCount++;
            } else if (slot.id == Block.CARPET.getItem().id) {
                carpetCount++;
            } else {
                // invalid item
                return false;
            }
        }

        return dyeCount == 1 && carpetCount == 1;
    }

    public override bool matchesShape(CraftingGridInventory grid) {
        return matches(grid);
    }

    public override ItemStack getResult(CraftingGridInventory grid) {
        int dyeMeta = -1;
        int carpetMeta = -1;

        foreach (var slot in grid.grid) {
            if (!isEmpty(slot)) {
                if (slot.id == Item.DYE.id) {
                    dyeMeta = slot.metadata;
                } else if (slot.id == Block.CARPET.getItem().id) {
                    carpetMeta = slot.metadata;
                }
            }
        }

        if (dyeMeta == -1 || carpetMeta == -1) {
            return ItemStack.EMPTY;
        }

        // extract carpet color and mix with dye color
        byte carpetColor = Carpet.getColor((byte)carpetMeta);
        int resultColor = mixColours(carpetColor, dyeMeta);

        // create carpet with mixed color (default floor orientation)
        byte resultMeta = 0;
        resultMeta = Carpet.setColor(resultMeta, (byte)resultColor);
        resultMeta = Carpet.setOrientation(resultMeta, Carpet.FLOOR);

        return new ItemStack(Block.CARPET.getItem(), 1, resultMeta);
    }

    public override void consumeIngredients(CraftingGridInventory grid) {
        bool dyeConsumed = false;
        bool carpetConsumed = false;

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
            } else if (!carpetConsumed && slot.id == Block.CARPET.getItem().id) {
                slot.quantity--;
                if (slot.quantity <= 0) {
                    grid.grid[i] = ItemStack.EMPTY;
                }
                carpetConsumed = true;
            }

            if (dyeConsumed && carpetConsumed) {
                break;
            }
        }
    }
}