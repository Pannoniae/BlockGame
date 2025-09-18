using BlockGame.util;
using BlockGame.world.item;
using BlockGame.world.item.inventory;

namespace BlockGame.world;

public class SurvivalInventory : Inventory {
    public ItemStack?[] slots = new ItemStack[50];
    
    public ItemStack? cursor = null;
    
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

    public ItemStack? getStack(int index) {
        return slots[index];
    }

    public void setStack(int index, ItemStack? stack) {
        slots[index] = stack;
    }

    public ItemStack? removeStack(int index, int count) {
        if (index < 0 || index >= slots.Length) return null;
        var stack = slots[index];
        if (stack == null || count <= 0) return null;

        var removeAmount = Math.Min(count, stack.quantity);
        var removed = new ItemStack(stack.id, removeAmount, stack.metadata);

        stack.quantity -= removeAmount;
        if (stack.quantity <= 0) {
            slots[index] = null;
        }

        return removed;
    }

    public ItemStack? clear(int index) {
        if (index < 0 || index >= slots.Length) return null;
        var stack = slots[index];
        slots[index] = null;
        return stack;
    }

    public void clearAll() {
        for (int i = 0; i < slots.Length; i++) {
            slots[i] = null;
        }
    }

    public bool add(int index, int count) {
        if (index < 0 || index >= slots.Length || count <= 0) return false;
        var stack = slots[index];
        if (stack == null) return false;

        stack.quantity += count;
        return true;
    }

    public bool isEmpty() {
        foreach (var slot in slots) {
            if (slot != null && slot.quantity != 0) {
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
}