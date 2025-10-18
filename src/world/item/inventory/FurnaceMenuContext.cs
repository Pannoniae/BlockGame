using BlockGame.ui.menu;
using BlockGame.util;

namespace BlockGame.world.item.inventory;

public class FurnaceMenuContext : InventoryContext {
    private readonly PlayerInventory playerInv;
    private readonly CraftingGridInventory craftingGrid;

    public const int hotbarX = 5;
    public const int hotbarY = 166;
    public const int mainY = 144;
    public const int craftingGridX = 71;
    public const int craftingGridY = 14;
    public const int craftingResultX = 120;
    public const int craftingResultY = 32;

    public FurnaceMenuContext(PlayerInventory playerInv) {
        this.playerInv = playerInv;
        this.craftingGrid = new CraftingGridInventory(this, 2, 1);
        setupSlots();
    }

    public void setupSlots() {
        slots.Clear();

        const int cols = 10;
        const int rows = 5;

        // hotbar (first 10 slots)
        for (int i = 0; i < cols; i++) {
            slots.Add(new ItemSlot(playerInv, i,
                hotbarX + i * ItemSlot.SLOTSIZE,
                hotbarY));
        }

        // main inventory (4 rows)
        for (int row = 0; row < rows - 1; row++) {
            for (int col = 0; col < cols; col++) {
                int slotIndex = cols + row * cols + col;
                slots.Add(new ItemSlot(playerInv, slotIndex,
                    hotbarX + col * ItemSlot.SLOTSIZE,
                    mainY - row * ItemSlot.SLOTSIZE));
            }
        }

        // 2x1 crafting grid
        for (int row = 0; row < 2; row++) {
            for (int col = 0; col < 1; col++) {
                int slotIndex = row * 2 + col;
                slots.Add(new ItemSlot(craftingGrid, slotIndex,
                    craftingGridX + col * ItemSlot.SLOTSIZE,
                    craftingGridY + row * ItemSlot.SLOTSIZE));
            }
        }

        // crafting result slot
        slots.Add(new CraftingResultSlot(craftingGrid, -1, craftingResultX, craftingResultY));
    }

    public CraftingGridInventory getCraftingGrid() => craftingGrid;
}
