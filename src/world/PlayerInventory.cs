using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.item.inventory;

namespace BlockGame.world;

public class PlayerInventory : Inventory {

    public const int HOTBAR_SIZE = 10;
    public const int MAIN_SIZE = 40;
    public const int ARMOUR_SIZE = 3;
    public const int ACCESSORY_SIZE = 10;
    public const int TOTAL_SIZE = HOTBAR_SIZE + MAIN_SIZE + ARMOUR_SIZE + ACCESSORY_SIZE;

    public const int ARMOUR = 50;
    public const int ARMOUR_HELMET = 50;
    public const int ARMOUR_CHESTPLATE = 51;
    public const int ARMOUR_BOOTS = 52;

    public const int ACCESSORIES = 53;
    public const int ACCESSORIES_END = 62;

    /** 0-49 */
    public readonly ItemStack[] slots = new ItemStack[HOTBAR_SIZE + MAIN_SIZE].fill();
    /** 50-52 */
    public readonly ItemStack[] armour = new ItemStack[ARMOUR_SIZE].fill();
    /** 53-62 */
    public readonly ItemStack[] accessories = new ItemStack[ACCESSORY_SIZE].fill();
    public readonly float[] shiny = new float[TOTAL_SIZE];
    
    public ItemStack cursor = ItemStack.EMPTY;
    
    /**
     * The index of the selected item.
     */
    public int selected;

    public PlayerInventory() {
        
    }

   
    public void initNewPlayer() {
        for (int i = 0; i < 10; i++) {
            var block = Block.get(i + 1);
            if (block != null) {
                slots[i] = new ItemStack(block.item, Random.Shared.Next(15));
            }
        }
    }

    public ItemStack getSelected() {
        return slots[selected];
    }

    public int size() {
        return slots.Length + armour.Length + accessories.Length;
    }

    public ItemStack getStack(int index) {
        if (index < slots.Length) {
            return slots[index];
        }
        if (index < slots.Length + armour.Length) {
            return armour[index - slots.Length];
        }
        if (index < slots.Length + armour.Length + accessories.Length) {
            return accessories[index - slots.Length - armour.Length];
        }
        return ItemStack.EMPTY;
    }

    public void setStack(int index, ItemStack stack) {
        if (index < slots.Length) {
            slots[index] = stack;
        }
        else if (index < slots.Length + armour.Length) {
            armour[index - slots.Length] = stack;
        }
        else if (index < slots.Length + armour.Length + accessories.Length) {
            accessories[index - slots.Length - armour.Length] = stack;
        }
    }

    public ItemStack removeStack(int index, int count) {
        if (index < 0 || index >= size()) return ItemStack.EMPTY;
        var stack = getStack(index);
        if (stack == ItemStack.EMPTY || count <= 0) return ItemStack.EMPTY;

        var removeAmount = Math.Min(count, stack.quantity);
        var removed = new ItemStack(stack.getItem(), removeAmount, stack.metadata);

        stack.quantity -= removeAmount;
        if (stack.quantity <= 0) {
            setStack(index, ItemStack.EMPTY);
        }

        return removed;
    }

    public ItemStack clear(int index) {
        if (index < 0 || index >= size()) return ItemStack.EMPTY;
        var stack = getStack(index);
        setStack(index, ItemStack.EMPTY);
        return stack;
    }

    public void clearAll() {
        for (int i = 0; i < slots.Length; i++) {
            slots[i] = ItemStack.EMPTY;
        }
        for (int i = 0; i < armour.Length; i++) {
            armour[i] = ItemStack.EMPTY;
        }
        for (int i = 0; i < accessories.Length; i++) {
            accessories[i] = ItemStack.EMPTY;
        }
    }

    public bool add(int index, int count) {
        if (index < 0 || index >= size() || count <= 0) return false;
        var stack = getStack(index);
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
                    shiny[i] = 1.0f; // trigger pop animation
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
                slots[i] = new ItemStack(stack.getItem(), canAdd, stack.metadata);
                shiny[i] = 1.0f; // trigger pop animation
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