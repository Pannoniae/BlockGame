using BlockGame.main;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.entity;
using BlockGame.world.item;
using Molten.DoublePrecision;

namespace BlockGame.util;

/**
 * We don't have IEquatable&lt;ItemStack&gt; or a hashcode here! This is because it's mutable so you'll fuck it up anyway.
 */
public class ItemStack {

    public static readonly ItemStack EMPTY = new(0, 0);

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

    // TODO: doesn't work properly since most comparisons to the empty item stack are done by value
    public void setToEmpty() {
        id = 0;
        metadata = 0;
        quantity = 0;
    }

    public void drop(World world, Vector3D position, int amount = 1) {
        var droppedStack = this.copy();
        droppedStack.quantity = amount;
        quantity -= amount;

        var droppedItem = ItemEntity.create(world, position, droppedStack);
        world.addEntity(droppedItem);

        // Dropped enough to be empty
        if (quantity <= 0)
            setToEmpty();
    }

    public void dropAll(World world, Vector3D position) {
        drop(world, position, quantity);
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
