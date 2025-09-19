using BlockGame.world.block;
using BlockGame.world.item;

namespace BlockGame.util;

/**
 * We don't have IEquatable&lt;ItemStack&gt; or a hashcode here! This is because it's mutable so you'll fuck it up anyway.
 */
public class ItemStack {

    public static readonly ItemStack EMPTY = new ItemStack(0, 0);

    public int id;
    /**
     * Item metadata is a signed integer! So you can use it for durability or other fancy stuff. Is this confusing? Probably. Do I have a better idea atm? No.
     */
    public int metadata;
    public int quantity;

    public ItemStack(Item item, int quantity, int metadata = 0) {
        this.id = item.id;
        this.quantity = quantity;
        this.metadata = metadata;
    }
    
    public ItemStack(Block block, int quantity, int metadata = 0) {
        this.id = -block.id;
        this.quantity = quantity;
        this.metadata = metadata;
    }
    
    public ItemStack(int id, int quantity, int metadata = 0) {
        this.id = id;
        this.quantity = quantity;
        this.metadata = metadata;
    }

    public ItemStack copy() {
        return new ItemStack(id, quantity, metadata);
    }
    
    public Item getItem() {
        return Item.get(id);
    }

    public bool same(ItemStack stack) {
        return stack != EMPTY && stack.id == id && stack.metadata == metadata;
    }
}

public static class ItemStackArrayExtensions {
    public static ItemStack[] fill(this ItemStack[] array) {
        for (int i = 0; i < array.Length; i++) {
            array[i] = ItemStack.EMPTY;
        }
        return array;
    }
}