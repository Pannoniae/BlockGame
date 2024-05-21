namespace BlockGame;

public class Inventory {
    public ushort[] slots = new ushort[9];
    /// <summary>
    /// Selected index
    /// </summary>
    public int selected;

    public Inventory() {
        for (int i = 0; i < slots.Length; i++) {
            slots[i] = (ushort)(i + 1);
        }
        // replace water with something useful
        slots[Blocks.WATER.id - 1] = Blocks.LEAVES.id;
    }

    public ushort getSelected() {
        return slots[selected];
    }
}