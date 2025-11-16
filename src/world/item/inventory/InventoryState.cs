using BlockGame.util;

namespace BlockGame.world.item.inventory;

/**
 * Tracks inventory state changes for efficient syncing.
 * Similar to EntityState but for inventories.
 */
public class InventoryState {
    private readonly ItemStack[] lastSynced;
    private readonly bool[] dirtyFlags;

    public InventoryState(int size) {
        lastSynced = new ItemStack[size];
        dirtyFlags = new bool[size];
        for (int i = 0; i < size; i++) {
            lastSynced[i] = ItemStack.EMPTY;
        }
    }

    /** mark a specific slot as dirty (changed) */
    public void markDirty(int slot) {
        if (slot >= 0 && slot < dirtyFlags.Length) {
            dirtyFlags[slot] = true;
        }
    }

    /** mark all slots as dirty (for full resync) */
    public void markAllDirty() {
        Array.Fill(dirtyFlags, true);
    }

    /** get list of changed slots since last sync */
    public List<(int slot, ItemStack stack)> getChanges(ItemStack[] current) {
        var changes = new List<(int, ItemStack)>();
        for (int i = 0; i < Math.Min(current.Length, lastSynced.Length); i++) {
            if (dirtyFlags[i] || !current[i].same(lastSynced[i])) {
                changes.Add((i, current[i]));
                lastSynced[i] = current[i].copy();
                dirtyFlags[i] = false;
            }
        }
        return changes;
    }

    /** mark inventory as fully synced */
    public void sync(ItemStack[] current) {
        for (int i = 0; i < Math.Min(current.Length, lastSynced.Length); i++) {
            lastSynced[i] = current[i].copy();
        }
        Array.Fill(dirtyFlags, false);
    }

    /** check if slot has changed */
    public bool isDirty(int slot) {
        if (slot < 0 || slot >= dirtyFlags.Length) return false;
        return dirtyFlags[slot];
    }
}