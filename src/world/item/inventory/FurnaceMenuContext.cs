using BlockGame.ui.menu;
using BlockGame.util;

namespace BlockGame.world.item.inventory;

public class FurnaceMenuContext : InventoryContext {
    private readonly PlayerInventory playerInv;
    private readonly Inventory furnaceInv;

    public Inventory getFurnaceInventory() => furnaceInv;

    public const int hotbarX = 5;
    public const int hotbarY = 166;
    public const int mainY = 144;
    public const int inputX = 71;
    public const int inputY = 14;
    public const int fuelX = 71;
    public const int fuelY = 54;
    public const int outputX = 120;
    public const int outputY = 32;

    public FurnaceMenuContext(PlayerInventory playerInv, Inventory furnaceInv) {
        this.playerInv = playerInv;
        this.furnaceInv = furnaceInv;
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

        // furnace slots: input (0), fuel (1), output (2)
        slots.Add(new ItemSlot(furnaceInv, 0, inputX, inputY));
        slots.Add(new ItemSlot(furnaceInv, 1, fuelX, fuelY));
        slots.Add(new ItemSlot(furnaceInv, 2, outputX, outputY));
    }
}
