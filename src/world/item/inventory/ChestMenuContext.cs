using BlockGame.util;

namespace BlockGame.world.item.inventory;

public class ChestMenuContext : InventoryContext {
    private readonly PlayerInventory playerInv;
    public readonly Inventory chestInv;

    public const int hotbarX = 5;
    public const int hotbarY = 179;
    public const int mainY = 157;
    public const int chestX = 5;
    public const int chestY = 13;

    public ChestMenuContext(PlayerInventory playerInv, Inventory chestInv) {
        this.playerInv = playerInv;
        this.chestInv = chestInv;
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

        // chest inventory (4 rows of 10)
        for (int row = 0; row < rows - 1; row++) {
            for (int col = 0; col < cols; col++) {
                int slotIndex = row * cols + col;
                slots.Add(new ItemSlot(chestInv, slotIndex,
                    chestX + col * ItemSlot.SLOTSIZE,
                    chestY + row * ItemSlot.SLOTSIZE));
            }
        }
    }
}