using BlockGame.util;
using BlockGame.world.item;
using BlockGame.world.item.inventory;

namespace BlockGame.world;

public class Hotbar : Inventory {
    public ItemStack?[] slots = new ItemStack[10];
    /// <summary>
    /// Selected index
    /// </summary>
    public int selected;

    public Hotbar() {
        for (int i = 0; i < slots.Length; i++) {
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
        throw new NotImplementedException();
    }

    public ItemStack? clear(int index) {
        throw new NotImplementedException();
    }

    public void clearAll() {
        throw new NotImplementedException();
    }

    public bool add(int index, int count) {
        throw new NotImplementedException();
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
        return "Hotbar";
    }

    public void setDirty(bool dirty) {
        
    }
}