using BlockGame.world.item.inventory;
using Molten;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.util;

/**
 * Basic slot that operates on a backing inventory.
 * All inventory interactions go through slots to enforce "business" logic. (WHO FUCKING INVENTED THAT TERM)
 *
 */
public class ItemSlot {
    public const int SLOTSIZE = 20;
    public const int PADDING = 2;
    public const int ITEMSIZE = 16;
    public const int ITEMSIZEHALF = 8;

    protected readonly Inventory? inventory;
    protected readonly int index;

    public Rectangle rect;
    public Vector2I itemPos;

    public ItemSlot(Inventory? inventory, int index, int x, int y) {
        this.inventory = inventory;
        this.index = index;
        rect = new Rectangle(x, y, SLOTSIZE, SLOTSIZE);
        itemPos = new Vector2I(rect.X + PADDING, rect.Y + PADDING);
    }

    /**
     * Returns the stack currently in this slot. May be null.
     */
    public virtual ItemStack? getStack() {
        if (inventory == null || index == -1) return null;
        return inventory.getStack(index);
    }

    /**
     * Checks if this slot can accept the given stack.
     */
    public virtual bool accept(ItemStack stack) {
        return true; // basic slots accept anything
    }

    /**
     * Attempts to take up to <i>count</i> items from this slot.
     * Returns the taken items or null if none could be taken.
    */
    public virtual ItemStack? take(int count) {
        if (inventory == null) return null;

        var current = getStack();
        if (current == null || current.quantity == 0 || count <= 0) {
            return null;
        }

        var takeAmount = Math.Min(count, current.quantity);
        var taken = new ItemStack(current.id, takeAmount, current.metadata);

        current.quantity -= takeAmount;
        if (current.quantity <= 0) {
            inventory.setStack(index, null);
        }

        return taken;
    }

    /**
     * Attempts to place the given stack in this slot.
     * Returns any items that couldn't be placed.
    */
    public virtual ItemStack? place(ItemStack stack) {
        if (inventory == null || stack == null || stack.quantity <= 0) {
            return stack;
        }

        if (!accept(stack)) {
            return stack; // slot doesn't accept this item type
        }

        var current = getStack();

        if (current == null) {
            // slot is empty, place the entire stack
            inventory.setStack(index, stack.copy());
            return null;
        }

        if (current.id == stack.id && current.metadata == stack.metadata) {
            // same item type, try to merge
            var canAdd = Inventory.MAX_STACK_SIZE - current.quantity;
            var addAmount = Math.Min(canAdd, stack.quantity);

            if (addAmount > 0) {
                current.quantity += addAmount;
                stack.quantity -= addAmount;

                if (stack.quantity <= 0) {
                    return null; // all items placed
                }
            }

            // if we still have items left, the current stack is full - swap instead
            if (stack.quantity > 0) {
                inventory.setStack(index, stack.copy());
                return current;
            }
        }
        else {
            // different item types - swap them
            inventory.setStack(index, stack.copy());
            return current;
        }

        SkillIssueException.throwNew("something is wrong in inventoryland!");
        return null;
    }

    /**
     * Swaps the contents of this slot with the given stack.
     * Returns the original contents of the slot.
     */
    public virtual ItemStack? swap(ItemStack? stack) {
        if (inventory == null) return null;

        var current = getStack();
        if (stack != null && !accept(stack)) {
            return current; // can't swap if slot doesn't accept the new stack
        }

        // simple swap for basic slots
        inventory.setStack(index, stack);
        return current;
    }
}