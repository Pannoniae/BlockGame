namespace BlockGame;

public class ItemStack {
    public ushort block;
    public int quantity;

    public ItemStack(ushort block, int quantity) {
        this.block = block;
        this.quantity = quantity;
    }
}