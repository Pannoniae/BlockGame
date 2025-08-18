namespace BlockGame.util;

public class ItemStack {
    public ushort block;
    public int quantity;

    public ItemStack(ushort block, int quantity) {
        this.block = block;
        this.quantity = quantity;
    }

    public ItemStack copy() {
        return new ItemStack(block, quantity);
    }
}