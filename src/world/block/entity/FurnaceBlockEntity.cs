using BlockGame.main;
using BlockGame.util;
using BlockGame.util.stuff;
using BlockGame.util.xNBT;
using BlockGame.world.item;
using BlockGame.world.item.inventory;

namespace BlockGame.world.block.entity;

public class FurnaceBlockEntity : BlockEntity, Inventory {

    public readonly ItemStack[] slots = new ItemStack[3].fill(); // input, fuel, output

    // smelting state
    private int smeltProgress = 0;       // current smelting time in ticks
    private int fuelRemaining = 0;       // fuel ticks left
    private int fuelMax = 0;             // total fuel from current fuel item (mostly for UI % lol)
    private SmeltingRecipe? currentRecipe = null;

    public FurnaceBlockEntity() : base() {
    }

    public override void update(World world, int x, int y, int z) {
        bool wasLit = fuelRemaining > 0;

        // try to start new recipe if idle
        if (currentRecipe == null && slots[0] != ItemStack.EMPTY) {
            currentRecipe = SmeltingRecipe.findRecipe(slots[0].getItem());
        }

        // consume fuel if needed and recipe exists
        if (currentRecipe != null && fuelRemaining <= 0) {
            if (slots[1] != ItemStack.EMPTY) {
                int fuelVal = Registry.ITEMS.fuelValue[slots[1].id];
                if (fuelVal > 0) {
                    fuelRemaining = fuelVal;
                    fuelMax = fuelVal;
                    slots[1].quantity--;
                    if (slots[1].quantity <= 0) slots[1] = ItemStack.EMPTY;
                }
            }
        }

        // smelt
        if (currentRecipe != null && fuelRemaining > 0) {
            smeltProgress++;
            fuelRemaining--;

            if (smeltProgress >= currentRecipe.getSmeltTime()) {
                // smelting complete - transfer to output
                if (canMergeOutput(currentRecipe.getOutput())) {
                    mergeOutput(currentRecipe.getOutput());
                    slots[0].quantity--;
                    if (slots[0].quantity <= 0) slots[0] = ItemStack.EMPTY;
                    smeltProgress = 0;
                    currentRecipe = null; // check for new recipe next tick
                }
            }
        } else {
            // no fuel or no recipe - reset progress
            if (fuelRemaining <= 0) {
                smeltProgress = 0;
                currentRecipe = null;
            }
        }

        // update lit state if changed
        bool isLit = fuelRemaining > 0;
        if (wasLit != isLit) {
            setLit(world, x, y, z, isLit);
        }
    }

    private bool canMergeOutput(ItemStack result) {
        var output = slots[2];
        if (output == ItemStack.EMPTY) return true;
        if (output.id != result.id || output.metadata != result.metadata) return false;
        return output.quantity + result.quantity <= output.getItem().getMaxStackSize();
    }

    private void mergeOutput(ItemStack result) {
        if (slots[2] == ItemStack.EMPTY) {
            slots[2] = result.copy();
        } else {
            slots[2].quantity += result.quantity;
        }
    }

    private static void setLit(World world, int x, int y, int z, bool lit) {
        uint block = world.getBlockRaw(x, y, z);
        var metadata = block.getMetadata();
        if (lit) {
            metadata |= 0b100; // set bit 2
        } else {
            metadata &= 0b011; // clear bit 2
        }
        block = block.setMetadata(metadata);
        // we cheat! we save&restore the BE
        var be = world.getBlockEntity(x, y, z);
        world.setBlockMetadataSilent(x, y, z, block);
        if (be != null) {
            world.setBlockEntity(x, y, z, be);
        }
    }

    public float getSmeltProgress() => currentRecipe != null ? (float)smeltProgress / currentRecipe.getSmeltTime() : 0f;
    public float getFuelProgress() => fuelMax > 0 ? (float)fuelRemaining / fuelMax : 0f;
    public bool isLit() => fuelRemaining > 0;

    protected override void readx(NBTCompound data) {
        if (data.has("items")) {
            var items = data.getListTag<NBTCompound>("items");
            for (int i = 0; i < items.count() && i < slots.Length; i++) {
                slots[i] = ItemStack.fromTag(items.get(i));
            }
        }

        if (data.has("smeltProgress")) smeltProgress = data.getInt("smeltProgress");
        if (data.has("fuelRemaining")) fuelRemaining = data.getInt("fuelRemaining");
        if (data.has("fuelMax")) fuelMax = data.getInt("fuelMax");

        // restore recipe reference
        if (data.has("smeltProgress") && smeltProgress > 0 && slots[0] != ItemStack.EMPTY) {
            currentRecipe = SmeltingRecipe.findRecipe(slots[0].getItem());
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

        data.addInt("smeltProgress", smeltProgress);
        data.addInt("fuelRemaining", fuelRemaining);
        data.addInt("fuelMax", fuelMax);
    }

    public void dropContents(World world, int x, int y, int z) {
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
        return "Furnace";
    }

    public void setDirty(bool dirty) {
        // todo: save to disk when dirty
    }
}