using BlockGame.util;
using BlockGame.util.xNBT;
using BlockGame.world.item.inventory;

namespace BlockGame.world.block.entity;

public class ChestBlockEntity : BlockEntity, Inventory {

    public readonly ItemStack[] slots = new ItemStack[PlayerInventory.MAIN_SIZE].fill();

    public ChestBlockEntity() : base() {
    }

    public override void update(World world, int x, int y, int z) {

    }

    protected override void readx(NBTCompound data) {
        if (data.has("items")) {
            var items = data.getListTag<NBTCompound>("items");
            for (int i = 0; i < items.count() && i < slots.Length; i++) {
                slots[i] = ItemStack.fromTag(items.get(i));
            }
        }
    }

    protected override void writex(NBTCompound data) {
        var items = new NBTList(NBTType.TAG_Compound, "items");
        foreach (var slot in slots) {
            var slotData = new NBTCompound("");
            slot.write(slotData);
            items.add(slotData);
        }
        data.add(items);
    }

    public void dropContents(World world, int x, int y, int z) {
        // drop all items when chest is broken
        for (int i = 0; i < slots.Length; i++) {
            var stack = slots[i];
            if (stack != ItemStack.EMPTY && stack.quantity > 0) {
                world.spawnBlockDrop(x, y, z, stack.getItem(), stack.quantity, stack.metadata);
                slots[i] = ItemStack.EMPTY;
            }
        }
    }

    public int size() {
        return slots.Length;
    }

    public ItemStack getStack(int index) {
        if (index < 0 || index >= slots.Length) return ItemStack.EMPTY;
        return slots[index];
    }

    public void setStack(int index, ItemStack stack) {
        if (index < 0 || index >= slots.Length) return;
        slots[index] = stack;
    }

    public ItemStack removeStack(int index, int count) {
        if (index < 0 || index >= slots.Length) return ItemStack.EMPTY;
        var stack = slots[index];
        if (stack == ItemStack.EMPTY || count <= 0) return ItemStack.EMPTY;

        var removeAmount = Math.Min(count, stack.quantity);
        var removed = new ItemStack(stack.getItem(), removeAmount, stack.metadata);

        stack.quantity -= removeAmount;
        if (stack.quantity <= 0) {
            slots[index] = ItemStack.EMPTY;
        }

        return removed;
    }

    public ItemStack clear(int index) {
        if (index < 0 || index >= slots.Length) return ItemStack.EMPTY;
        var stack = slots[index];
        slots[index] = ItemStack.EMPTY;
        return stack;
    }

    public void clearAll() {
        for (int i = 0; i < slots.Length; i++) {
            slots[i] = ItemStack.EMPTY;
        }
    }

    public bool add(int index, int count) {
        if (index < 0 || index >= slots.Length || count <= 0) return false;
        var stack = slots[index];
        if (stack == ItemStack.EMPTY) return false;

        stack.quantity += count;
        return true;
    }

    public bool isEmpty() {
        foreach (var slot in slots) {
            if (slot != ItemStack.EMPTY && slot.quantity != 0) {
                return false;
            }
        }
        return true;
    }

    public string name() {
        return "Chest";
    }

    public void setDirty(bool dirty) {
        // todo: save to disk when dirty
    }
}