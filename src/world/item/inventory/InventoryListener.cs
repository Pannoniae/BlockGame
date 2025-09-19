using BlockGame.util;

namespace BlockGame.world.item.inventory;

public interface InventoryListener {
    /**
     * Called when a specific slot in an inventory changes.
     */
    void onSlotChanged(Inventory inventory, int slot, ItemStack oldStack, ItemStack newStack);

    /**
     * Called when the entire inventory is reloaded.
     */
    void refresh(Inventory inventory, List<ItemStack> items);
}