using BlockGame.main;
using BlockGame.util;

namespace BlockGame.world.item.inventory;

/**
 * Groups related slots that work together as a unit.
 * Handles complex interactions and coordinates between different inventory areas.
 *
 * This is needed because the player can interact with multiple inventories at once, and we can't just yeet this onto the menu itself (there will be server code!)
 */
public abstract class InventoryContext {
    protected readonly List<ItemSlot> slots = [];

    public List<ItemSlot> getSlots() => slots;

    /**
     * TODO this should be reused to avoid code duplication with SurvivalInventoryContext and other stuff!
     */
    public virtual void handleSlotClick(ItemSlot slot, ClickType click) {
        var player = Game.player;
        var cursor = player.survivalInventory.cursor;

        if (click == ClickType.LEFT) {
            if (cursor == ItemStack.EMPTY) {
                // try to take from slot
                var currentStack = slot.getStack();
                var taken = slot.take(currentStack == ItemStack.EMPTY ? 1 : currentStack.quantity);
                if (taken != ItemStack.EMPTY && taken.id != Items.AIR) {
                    player.survivalInventory.cursor = taken;
                }
            }
            else {
                // try to place in slot
                var remaining = slot.place(cursor);
                player.survivalInventory.cursor = remaining;
            }
        }
        else if (click == ClickType.RIGHT) {
            if (cursor == ItemStack.EMPTY) {
                // try to take half
                var currentStack = slot.getStack();
                if (currentStack != ItemStack.EMPTY && currentStack.quantity > 0) {
                    var halfQuantity = (currentStack.quantity + 1) / 2;
                    var taken = slot.take(halfQuantity);
                    if (taken != ItemStack.EMPTY) {
                        player.survivalInventory.cursor = taken;
                    }
                }
            }
            else {
                // try to place one item
                var singleItem = new ItemStack(cursor.id, 1, cursor.metadata);
                var remaining = slot.place(singleItem);

                if (remaining == ItemStack.EMPTY) {
                    // Successfully placed 1 item through merging or into empty slot
                    cursor.quantity--;
                    if (cursor.quantity <= 0) {
                        player.survivalInventory.cursor = ItemStack.EMPTY;
                    }
                }
                else {
                    // Swap occurred - either different item type or same type but slot was full
                    // The slot now contains our 1 item, cursor should become what was returned
                    player.survivalInventory.cursor = remaining;
                }
            }
        }
    }
}

public enum ClickType {
    LEFT,
    RIGHT,
    SHIFT_LEFT,
    SHIFT_RIGHT
}