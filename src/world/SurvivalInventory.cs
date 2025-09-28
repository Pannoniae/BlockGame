using BlockGame.util;
using BlockGame.world.item;
using BlockGame.world.item.inventory;

namespace BlockGame.world;

public class SurvivalInventory : Inventory {
    public readonly ItemStack[] slots = new ItemStack[50].fill();
    
    public ItemStack cursor = ItemStack.EMPTY;
    
    /**
     * The index of the selected item.
     */
    public int selected;

    public SurvivalInventory() {
        for (int i = 0; i < 10; i++) {
            slots[i] = new ItemStack(Item.blockID(i + 1), Random.Shared.Next(15));
        }
        // replace water with something useful
        //slots[Block.WATER.id - 1] = new ItemStack(Block.LEAVES.id, 1);
    }

    public ItemStack getSelected() {
        return slots[selected];
    }

    public int size() {
        return slots.Length;
    }

    public ItemStack getStack(int index) {
        return slots[index];
    }

    public void setStack(int index, ItemStack stack) {
        slots[index] = stack;
    }

    public ItemStack removeStack(int index, int count) {
        if (index < 0 || index >= slots.Length) return ItemStack.EMPTY;
        var stack = slots[index];
        if (stack == ItemStack.EMPTY || count <= 0) return ItemStack.EMPTY;

        var removeAmount = Math.Min(count, stack.quantity);
        var removed = new ItemStack(stack.id, removeAmount, stack.metadata);

        stack.quantity -= removeAmount;
        if (stack.quantity <= 0) {
            slots[index] = ItemStack.EMPTY;
        }

        return removed;
    }

    public ItemStack clear(int index) {
        if (index < 0 || index >= slots.Length) return ItemStack.EMPTY;
        var stack = slots[index];
        slots[index] = ItemStack.EMPTY;
        return stack;
    }

    public void clearAll() {
        for (int i = 0; i < slots.Length; i++) {
            slots[i] = ItemStack.EMPTY;
        }
    }

    public bool add(int index, int count) {
        if (index < 0 || index >= slots.Length || count <= 0) return false;
        var stack = slots[index];
        if (stack == ItemStack.EMPTY) return false;

        stack.quantity += count;
        return true;
    }

    public bool isEmpty() {
        foreach (var slot in slots) {
            if (slot != ItemStack.EMPTY && slot.quantity != 0) {
                return false;
            }
        }
        return true;
    }

    public string name() {
        return "Inventory";
    }

    public void setDirty(bool dirty) {

    }

    /**
     * Try to add an ItemStack to the inventory. Returns true if successful, false if full.
     */
    public bool addItem(ItemStack stack) {
        if (stack == ItemStack.EMPTY || stack.quantity <= 0) {
            return true;
        }

        var remaining = stack.quantity;
        var maxStackSize = stack.getItem().getMaxStackSize();

        // first, try to add to existing stacks of the same item
        for (int i = 0; i < slots.Length; i++) {
            var slot = slots[i];
            if (slot != ItemStack.EMPTY && slot.same(stack)) {
                var canAdd = Math.Min(remaining, maxStackSize - slot.quantity);
                if (canAdd > 0) {
                    slot.quantity += canAdd;
                    remaining -= canAdd;
                    if (remaining <= 0) {
                        return true;
                    }
                }
            }
        }

        // if we still have items left, try to place in empty slots
        for (int i = 0; i < slots.Length; i++) {
            if (slots[i] == ItemStack.EMPTY) {
                var canAdd = Math.Min(remaining, maxStackSize);
                slots[i] = new ItemStack(stack.id, canAdd, stack.metadata);
                remaining -= canAdd;
                if (remaining <= 0) {
                    return true;
                }
            }
        }

        // if we get here, the inventory is full
        return false;
    }
}