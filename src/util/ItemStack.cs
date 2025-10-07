using BlockGame.util.xNBT;
using BlockGame.world.block;
using BlockGame.world.item;

namespace BlockGame.util;

/**
 * We don't have IEquatable&lt;ItemStack&gt; or a hashcode here! This is because it's mutable so you'll fuck it up anyway.
 */
public class ItemStack : Persistent {

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

    public ItemStack(NBTCompound data) {
        read(data);
    }

    public static ItemStack fromTag(NBTCompound data) {
        var stack = new ItemStack(0, 0);
        stack.read(data);
        return (stack.id == 0 || stack.quantity <= 0) ? EMPTY : stack;
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

    public void write(NBTCompound data) {
        data.addInt("id", id);
        data.addInt("qty", quantity);
        data.addInt("meta", metadata);
    }

    public void read(NBTCompound data) {
        if (!data.has("id")) {
            id = 0;
            quantity = 0;
            metadata = 0;
            return;
        }

        id = data.getInt("id");
        quantity = data.has("qty") ? data.getInt("qty") : 0;
        metadata = data.has("meta") ? data.getInt("meta") : 0;
    }
}

public static class ItemStackArrayExtensions {
    public static ItemStack[] fill(this ItemStack[] array) {
        new Span<ItemStack>(array).Fill(ItemStack.EMPTY);
        return array;
    }
}