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

    public virtual void handleSlotClick(ItemSlot slot, ClickType click) {
        var player = Game.player;
        var cursor = player.inventory.cursor;

        if (click == ClickType.LEFT) {
            if (cursor == ItemStack.EMPTY) {
                // try to take from slot
                var currentStack = slot.getStack();
                var taken = slot.take(currentStack == ItemStack.EMPTY ? 1 : currentStack.quantity);
                if (taken != ItemStack.EMPTY && taken.id != Items.AIR) {
                    player.inventory.cursor = taken;
                }
            }
            else {
                // place cursor into slot - this handles merging, swapping, etc.
                var remaining = slot.place(cursor);
                player.inventory.cursor = remaining;
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
                        player.inventory.cursor = taken;
                    }
                }
            }
            else {
                // right-click only places 1 item if slot is empty or can merge (no swapping!)
                var slotStack = slot.getStack();

                if (!slot.accept(cursor)) {
                    // slot doesn't accept this item type - do nothing
                    return;
                }

                if (slotStack == ItemStack.EMPTY) {
                    // empty slot - place 1 item
                    var singleItem = new ItemStack(cursor.id, 1, cursor.metadata);
                    slot.place(singleItem);
                    cursor.quantity--;
                    if (cursor.quantity <= 0) {
                        player.inventory.cursor = ItemStack.EMPTY;
                    }
                }
                else if (slotStack.same(cursor)) {
                    // same item - try to add 1 if there's room
                    if (slotStack.quantity < Inventory.MAX_STACK_SIZE) {
                        slotStack.quantity++;
                        cursor.quantity--;
                        if (cursor.quantity <= 0) {
                            player.inventory.cursor = ItemStack.EMPTY;
                        }
                    }
                    // else slot is full, do nothing
                }
                // else different item - do nothing (no swap on right-click)
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