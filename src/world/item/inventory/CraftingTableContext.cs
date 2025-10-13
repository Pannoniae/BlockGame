using BlockGame.ui.menu;
using BlockGame.util;

namespace BlockGame.world.item.inventory;

public class CraftingTableContext : InventoryContext {
    private readonly PlayerInventory playerInv;
    private readonly CraftingGridInventory craftingGrid;

    public const int hotbarX = 5;
    public const int hotbarY = 191;
    public const int mainY = 144;
    public const int craftingGridX = 51;
    public const int craftingGridY = 14;
    public const int craftingResultX = 140;
    public const int craftingResultY = 33;

    public CraftingTableContext(PlayerInventory playerInv) {
        this.playerInv = playerInv;
        this.craftingGrid = new CraftingGridInventory(this, 3, 3);
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

        // 3x3 crafting grid
        for (int row = 0; row < 3; row++) {
            for (int col = 0; col < 3; col++) {
                int slotIndex = row * 3 + col;
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