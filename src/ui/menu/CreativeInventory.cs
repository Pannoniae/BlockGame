using BlockGame.item;
using BlockGame.item.inventory;
using BlockGame.util;

namespace BlockGame.ui;

public class CreativeInventory : Inventory {
    private readonly ItemStack?[] slots = new ItemStack[InventoryMenu.ITEMS_PER_PAGE];

    public int size() {
        return slots.Length;
    }

    public ItemStack? getStack(int index) {
        if (index < 0 || index >= slots.Length) return null;
        return slots[index];
    }

    public void setStack(int index, ItemStack? stack) {
        if (index >= 0 && index < slots.Length) {
            slots[index] = stack;
        }
    }

    public ItemStack? removeStack(int index, int count) {
        return null; // read-only for creative
    }

    public ItemStack? clear(int index) {
        setStack(index, null);
        return null;
    }

    public void clearAll() {
        for (int i = 0; i < slots.Length; i++) {
            slots[i] = null;
        }
    }

    public bool add(int index, int count) {
        return false; // read-only for creative
    }

    public bool isEmpty() {
        foreach (var slot in slots) {
            if (slot != null && slot.quantity > 0) {
                return false;
            }
        }
        return true;
    }

    public string name() {
        return "Creative";
    }

    public void setDirty(bool dirty) {
        // no-op for creative
    }
}