namespace BlockGame;

public class Inventory {
    public ItemStack[] slots = new ItemStack[9];
    /// <summary>
    /// Selected index
    /// </summary>
    public int selected;

    public Inventory() {
        for (int i = 0; i < slots.Length; i++) {
            slots[i] = new ItemStack((ushort)(i + 1), 1);
        }
        // replace water with something useful
        slots[Blocks.WATER.id - 1] = new ItemStack(Blocks.LEAVES.id, 1);
    }

    public ItemStack getSelected() {
        return slots[selected];
    }
}