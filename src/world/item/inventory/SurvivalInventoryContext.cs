using BlockGame.ui.menu;
using BlockGame.util;

namespace BlockGame.world.item.inventory;

public class SurvivalInventoryContext : InventoryContext {
    private readonly PlayerInventory playerInv;

    private readonly CraftingGridInventory craftingGrid;

    public const int hotbarX = 47;
    public const int hotbarY = 166;
    public const int mainY = 144;
    public const int accX = 5;
    public const int accY = 84;
    public const int armourX = 15;
    public const int armourY = 12;
    public const int armourGap = 4;
    public const int craftingGridX = 148;
    public const int craftingGridY = 26;
    public const int craftingResultX = 217;
    public const int craftingResultY = 36;

    public SurvivalInventoryContext(PlayerInventory playerInv) {
        this.playerInv = playerInv;
        this.craftingGrid = new CraftingGridInventory(this, 2, 2);
        setupSlots();
    }

    public CraftingGridInventory getCraftingGrid() => craftingGrid;

    public void setupSlots() {
        slots.Clear();

        const int rows = SurvivalInventoryMenu.rows;
        const int cols = SurvivalInventoryMenu.cols;
        const int invOffsetX = SurvivalInventoryMenu.invOffsetX;
        const int invOffsetY = SurvivalInventoryMenu.invOffsetY;

        // one row for the hotbar (first 10 slots)
        for (int i = 0; i < cols; i++) {
            slots.Add(new ItemSlot(playerInv, i,
                hotbarX + i * ItemSlot.SLOTSIZE,
                hotbarY));
        }

        // 4 more rows for the main inventory
        for (int row = 0; row < rows - 1; row++) {
            for (int col = 0; col < cols; col++) {
                int slotIndex = cols + row * cols + col; // skip first 10 hotbar slots
                slots.Add(new ItemSlot(playerInv, slotIndex,
                    hotbarX + col * ItemSlot.SLOTSIZE,
                    mainY - (row) * ItemSlot.SLOTSIZE)); // mainY is the bottom row
            }
        }

        // 4 rows of accessories
        for (int row = 0; row < 4; row++) {
            for (int col = 0; col < 2; col++) {
                int slotIndex = PlayerInventory.ACCESSORIES + row * 2 + col;
                slots.Add(new AccessorySlot(playerInv, slotIndex,
                    accX + col * ItemSlot.SLOTSIZE,
                    accY + row * ItemSlot.SLOTSIZE));
            }
        }

        // The fifth row of accessories (2 slots)
        for (int col = 0; col < 2; col++) {
            int slotIndex = PlayerInventory.ACCESSORIES + 8 + col;
            slots.Add(new AccessorySlot(playerInv, slotIndex,
                accX + col * ItemSlot.SLOTSIZE,
                accY + 4 * ItemSlot.SLOTSIZE + 2)); // 2 pixel padding
        }

        // Armour slots (3 slots)
        for (int i = 0; i < 3; i++) {
            slots.Add(new ArmourSlot(playerInv, PlayerInventory.ARMOUR,
                armourX,
                armourY + i * (ItemSlot.SLOTSIZE + armourGap)));
        }

        // Crafting grid (2x2 = 4 slots)
        for (int row = 0; row < 2; row++) {
            for (int col = 0; col < 2; col++) {
                int slotIndex = row * 2 + col;
                slots.Add(new ItemSlot(craftingGrid, slotIndex,
                    craftingGridX + col * ItemSlot.SLOTSIZE,
                    craftingGridY + row * ItemSlot.SLOTSIZE));
            }
        }

        // Crafting result slot
        slots.Add(new CraftingResultSlot(craftingGrid, -1, craftingResultX, craftingResultY));
    }
}