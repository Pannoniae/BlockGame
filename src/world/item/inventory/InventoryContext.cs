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
    protected readonly List<ItemSlot> slots = new();

    public List<ItemSlot> getSlots() => slots;

    /**
     * TODO this should be reused to avoid code duplication with SurvivalInventoryContext and other stuff!
     */
    public virtual void handleSlotClick(ItemSlot slot, ClickType click) {
        var player = Game.player;
        var cursor = player.survivalInventory.cursor;

        if (click == ClickType.LEFT) {
            if (cursor == null) {
                // try to take from slot
                var taken = slot.take(slot.getStack()?.quantity ?? 1);
                if (taken != null && taken.id != Items.AIR) {
                    player.survivalInventory.cursor = taken;
                }
            } else {
                // try to place in slot
                var remaining = slot.place(cursor);
                player.survivalInventory.cursor = remaining;
            }
        } else if (click == ClickType.RIGHT) {
            if (cursor == null) {
                // try to take half
                var currentStack = slot.getStack();
                if (currentStack != null && currentStack.quantity > 0) {
                    var halfQuantity = (currentStack.quantity + 1) / 2;
                    var taken = slot.take(halfQuantity);
                    if (taken != null) {
                        player.survivalInventory.cursor = taken;
                    }
                }
            } else {
                // try to place one item
                var singleItem = new ItemStack(cursor.id, 1, cursor.metadata);
                var remaining = slot.place(singleItem);
                if (remaining == null) {
                    cursor.quantity--;
                    if (cursor.quantity <= 0) {
                        player.survivalInventory.cursor = null;
                    }
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