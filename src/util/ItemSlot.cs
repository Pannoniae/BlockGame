using BlockGame.world.item;
using BlockGame.world.item.inventory;
using Molten;

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

    public readonly Inventory? inventory;
    public readonly int index;

    public Rectangle rect;
    public Vector2I itemPos;

    public ItemSlot(Inventory? inventory, int index, int x, int y) {
        this.inventory = inventory;
        this.index = index;
        rect = new Rectangle(x, y, SLOTSIZE, SLOTSIZE);
        itemPos = new Vector2I(rect.X + PADDING, rect.Y + PADDING);
    }

    /**
     * Returns the stack currently in this slot.
     */
    public virtual ItemStack getStack() {
        if (inventory == null || index == -1) return ItemStack.EMPTY;
        return inventory.getStack(index);
    }

    /**
     * Checks if this slot allows placing items into it (bidirectional vs output-only).
     */
    public virtual bool canPlace() {
        return true; // most slots are normal..
    }

    /**
     * Checks if this slot can accept the given stack.
     */
    public virtual bool accept(ItemStack stack) {
        return true; // basic slots accept anything
    }

    /**
     * Attempts to take up to <i>count</i> items from this slot.
     * Returns the taken items or ItemStack.EMPTY if none could be taken.
    */
    public virtual ItemStack take(int count) {
        if (inventory == null) return ItemStack.EMPTY;

        var current = getStack();
        if (current == ItemStack.EMPTY || current.quantity == 0 || count <= 0) {
            return ItemStack.EMPTY;
        }

        var takeAmount = Math.Min(count, current.quantity);
        var taken = new ItemStack(current.getItem(), takeAmount, current.metadata);

        var remaining = current.copy();
        remaining.quantity -= takeAmount;
        if (remaining.quantity <= 0) {
            inventory.setStack(index, ItemStack.EMPTY);
        } else {
            inventory.setStack(index, remaining);
        }

        return taken;
    }

    /**
     * Attempts to place the given stack in this slot.
     * Returns any items that couldn't be placed.
    */
    public virtual ItemStack place(ItemStack stack) {
        if (inventory == null || stack == ItemStack.EMPTY || stack.quantity <= 0) {
            return stack;
        }

        if (!accept(stack)) {
            return stack; // slot doesn't accept this item type
        }

        var current = getStack();

        if (current == ItemStack.EMPTY) {
            // slot is empty, place the entire stack
            inventory.setStack(index, stack.copy());
            return ItemStack.EMPTY;
        }

        if (current.same(stack)) {
            // same item type, try to merge
            var canAdd = Inventory.MAX_STACK_SIZE - current.quantity;
            var addAmount = Math.Min(canAdd, stack.quantity);

            if (addAmount > 0) {
                // use setStack to trigger inventory
                var merged = current.copy();
                merged.quantity += addAmount;
                inventory.setStack(index, merged);

                if (stack.quantity <= addAmount) {
                    return ItemStack.EMPTY; // all items placed
                } else {
                    return new ItemStack(stack.getItem(), stack.quantity - addAmount, stack.metadata);
                }
            }
            else {
                // slot is full, can't merge
                return stack;
            }
        }
        else {
            // different item types - swap them
            inventory.setStack(index, stack.copy());
            return current;
        }
    }

    /**
     * Swaps the contents of this slot with the given stack.
     * Returns the original contents of the slot.
     */
    public virtual ItemStack swap(ItemStack stack) {
        if (inventory == null) return ItemStack.EMPTY;

        var current = getStack();
        if (stack != ItemStack.EMPTY && !accept(stack)) {
            return current; // can't swap if slot doesn't accept the new stack
        }

        // simple swap for basic slots
        inventory.setStack(index, stack);
        return current;
    }
}

public class ArmourSlot : ItemSlot {
    public ArmourSlot(Inventory? inventory, int index, int x, int y) : base(inventory, index, x, y) {
    }

    public override bool accept(ItemStack stack) {
        if (stack == ItemStack.EMPTY) {
            return true;
        }

        var item = stack.getItem();
        return Item.armour[item.id];
    }
}

public class AccessorySlot : ItemSlot {
    public AccessorySlot(Inventory? inventory, int index, int x, int y) : base(inventory, index, x, y) {
    }

    public override bool accept(ItemStack stack) {
        if (stack == ItemStack.EMPTY) {
            return true;
        }

        var item = stack.getItem();
        return Item.accessory[item.id];
    }
}

public class CraftingResultSlot : ItemSlot {
    private readonly CraftingGridInventory craftingGrid;

    public CraftingResultSlot(CraftingGridInventory craftingGrid, int index, int x, int y) : base(craftingGrid, index, x, y) {
        this.craftingGrid = craftingGrid;
    }

    public override bool canPlace() {
        return false; // output-only slot
    }

    public override ItemStack getStack() {
        return craftingGrid.result;
    }

    public override ItemStack take(int count) {
        var result = craftingGrid.result;
        if (result == ItemStack.EMPTY || count <= 0) {
            return ItemStack.EMPTY;
        }

        var takeAmount = Math.Min(count, result.quantity);
        var taken = new ItemStack(result.getItem(), takeAmount, result.metadata);

        // use cached recipe from updateResult() to prevent double lookup
        var recipe = craftingGrid.lastMatchedRecipe;
        if (recipe != null && recipe.matches(craftingGrid)) {
            // recipe still valid - consume ingredients
            recipe.consumeIngredients(craftingGrid);
            craftingGrid.updateResult(); // recalculate to see if we can craft again
            craftingGrid.notifyChanged();
        }
        else {
            // recipe no longer matches (grid changed or desync) - clear grid and don't give items
            craftingGrid.clearAll();
            craftingGrid.notifyChanged();
            return ItemStack.EMPTY;  // prevent item duping by returning nothing
        }

        return taken;
    }

    public override ItemStack place(ItemStack stack) {
        // Can't place items in the result slot!
        return stack;
    }
}