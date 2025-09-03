using BlockGame.item;
using BlockGame.util;

namespace BlockGame;

public class Inventory {
    public ItemStack[] slots = new ItemStack[10];
    /// <summary>
    /// Selected index
    /// </summary>
    public int selected;

    public Inventory() {
        for (int i = 0; i < slots.Length; i++) {
            slots[i] = new ItemStack(Item.blockID(i + 1), Random.Shared.Next(15));
        }
        // replace water with something useful
        //slots[Block.WATER.id - 1] = new ItemStack(Block.LEAVES.id, 1);
    }

    public ItemStack getSelected() {
        return slots[selected];
    }
}