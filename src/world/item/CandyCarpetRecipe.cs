using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.item.inventory;

namespace BlockGame.world.item;

/** Recipe for converting 1 candy block ↔ 16 carpets (bidirectional, preserves color) */
public class CandyCarpetRecipe : Recipe {
    public override bool matches(CraftingGridInventory grid) {
        int totalCandies = 0;
        int totalCarpets = 0;
        byte? carpetColor = null;

        foreach (var slot in grid.grid) {
            if (isEmpty(slot)) continue;

            if (slot.id == Block.CANDY.item.id) {
                totalCandies += slot.quantity;
            } else if (slot.id == Block.CARPET.item.id) {
                totalCarpets += slot.quantity;

                // check all carpets are same color
                byte slotColor = Carpet.getColor((byte)slot.metadata);
                if (carpetColor == null) {
                    carpetColor = slotColor;
                } else if (carpetColor.Value != slotColor) {
                    return false; // mixed colors
                }
            } else {
                return false; // invalid item
            }
        }

        bool result = (totalCandies == 1 && totalCarpets == 0) ||
                      (totalCandies == 0 && totalCarpets == 16);
        Console.WriteLine($"[CandyCarpetRecipe.matches] candies={totalCandies}, carpets={totalCarpets}, color={carpetColor}, result={result}");
        return result;
    }

    public override bool matchesShape(CraftingGridInventory grid) {
        return matches(grid);
    }

    public override ItemStack getResult(CraftingGridInventory grid) {
        int totalCandies = 0;
        int totalCarpets = 0;
        int candyMeta = -1;
        byte? carpetColor = null;

        foreach (var slot in grid.grid) {
            if (isEmpty(slot)) continue;

            if (slot.id == Block.CANDY.item.id) {
                totalCandies += slot.quantity;
                if (candyMeta == -1) {
                    candyMeta = slot.metadata;
                }
            } else if (slot.id == Block.CARPET.item.id) {
                totalCarpets += slot.quantity;
                if (carpetColor == null) {
                    carpetColor = Carpet.getColor((byte)slot.metadata);
                }
            }
        }

        Console.WriteLine($"[CandyCarpetRecipe.getResult] candies={totalCandies}, carpets={totalCarpets}, carpetColor={carpetColor}");

        // 1 candy → 16 carpets
        if (totalCandies == 1 && totalCarpets == 0) {
            Console.WriteLine("[CandyCarpetRecipe.getResult] Returning 16 carpets from 1 candy");
            byte color = (byte)candyMeta;
            byte carpetMeta = 0;
            carpetMeta = Carpet.setColor(carpetMeta, color);
            carpetMeta = Carpet.setOrientation(carpetMeta, Carpet.FLOOR);
            return new ItemStack(Block.CARPET.item, 16, carpetMeta);
        }

        // 16 carpets → 1 candy
        if (totalCandies == 0 && totalCarpets == 16 && carpetColor.HasValue) {
            Console.WriteLine("[CandyCarpetRecipe.getResult] Returning 1 candy from 16 carpets");
            byte color = carpetColor.Value;
            return new ItemStack(Block.CANDY.item, 1, color);
        }

        Console.WriteLine("[CandyCarpetRecipe.getResult] No match, returning EMPTY");
        return ItemStack.EMPTY;
    }

    public override void consumeIngredients(CraftingGridInventory grid) {
        int toConsume = 0;
        ushort targetId = 0;

        Console.WriteLine("[CandyCarpetRecipe.consumeIngredients] CALLED");

        // determine what we're consuming
        foreach (var slot in grid.grid) {
            if (isEmpty(slot)) continue;

            if (slot.id == Block.CANDY.item.id) {
                toConsume = 1;
                targetId = (ushort)Block.CANDY.item.id;
                break;
            } else if (slot.id == Block.CARPET.item.id) {
                toConsume = 16;
                targetId = (ushort)Block.CARPET.item.id;
                break;
            }
        }

        Console.WriteLine($"[CandyCarpetRecipe.consumeIngredients] toConsume={toConsume}, targetId={targetId}");

        // consume the required amount
        int consumed = 0;
        for (int i = 0; i < grid.grid.Length && consumed < toConsume; i++) {
            var slot = grid.grid[i];
            if (isEmpty(slot) || slot.id != targetId) continue;

            Console.WriteLine($"[CandyCarpetRecipe.consumeIngredients] Slot {i}: qty={slot.quantity}, taking {Math.Min(slot.quantity, toConsume - consumed)}");

            int take = Math.Min(slot.quantity, toConsume - consumed);
            slot.quantity -= take;
            consumed += take;

            // write back to array (ItemStack is a struct)
            if (slot.quantity <= 0) {
                grid.grid[i] = ItemStack.EMPTY;
            } else {
                grid.grid[i] = slot;
            }
        }

        Console.WriteLine($"[CandyCarpetRecipe.consumeIngredients] Total consumed: {consumed}");
    }

    private static bool isEmpty(ItemStack slot) => slot == ItemStack.EMPTY || slot.quantity <= 0;
}
