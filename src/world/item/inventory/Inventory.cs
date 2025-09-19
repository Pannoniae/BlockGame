using BlockGame.util;

namespace BlockGame.world.item.inventory;

public interface Inventory {

    public const int MAX_STACK_SIZE = 256;
    public const int MAX_STACK_SIZE_FOOD = 8;

    public int size();
    
    /**
     * Returns the stack at index.
     */
    public ItemStack getStack(int index);
    
    /**
     * Overwrites the stack at index.
     */
    public void setStack(int index, ItemStack stack);
    
    /**
     * Removes up to count items from the stack at index. Returns the removed items as a new stack.
     */
    public ItemStack removeStack(int index, int count);
    
    /**
     * Removes the entire stack at index. Returns the removed stack.
     */
    public ItemStack clear(int index);
    
    public void clearAll();
    
    /**
     * Adds up to count items to the stack at index. Returns true if any items were added.
     */
    public bool add(int index, int count);
    
    /*
     * Returns true if the inventory has no items.
     */
    public bool isEmpty();

    /**
     * Returns the name of the inventory (for display purposes).
     */
    public string name();
    
    /*
     * Marks the inventory as dirty (changed). Some inventories may not need this, leave it empty.
     */
    public void setDirty(bool dirty);

}