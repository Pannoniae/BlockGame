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
    public int smeltProgress = 0;       // current smelting time in ticks
    public int smeltTime = 200;         // total time for current recipe
    public int fuelRemaining = 0;       // fuel ticks left
    public int fuelMax = 0;             // total fuel from current fuel item (mostly for UI % lol)
    public SmeltingRecipe? currentRecipe = null;

    public FurnaceBlockEntity() : base("furnace") {
    }

    public override void update(World world, int x, int y, int z) {
        // server-only logic - smelting, fuel consumption, item manipulation
        if (Net.mode.isMPC()) {
            return;
        }

        bool wasLit = fuelRemaining > 0;

        // try to start new recipe if idle
        if (currentRecipe == null && slots[0] != ItemStack.EMPTY) {
            currentRecipe = SmeltingRecipe.findRecipe(slots[0].getItem());
            if (currentRecipe != null) {
                smeltTime = currentRecipe.getSmeltTime();
            }
        }

        // consume fuel if we're out and have fuel in the slot (mfs shouldn't overcook)
        if (fuelRemaining <= 0 && slots[1] != ItemStack.EMPTY) {
            int fuelVal = Registry.ITEMS.fuelValue[slots[1].id];
            if (fuelVal > 0) {
                fuelRemaining = fuelVal;
                fuelMax = fuelVal;
                slots[1].quantity--;
                if (slots[1].quantity <= 0) slots[1] = ItemStack.EMPTY;
            }
        }

        // fuel always burns down if lit, regardless of recipe lol
        if (fuelRemaining > 0) {
            fuelRemaining--;
        }

        // smelt (only if recipe exists and fuel is burning)
        if (currentRecipe != null && fuelRemaining > 0) {
            smeltProgress++;

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
            // no recipe - reset progress
            smeltProgress = 0;
            currentRecipe = null;
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
        ushort id = block.getID();
        var metadata = block.getMetadata();

        ushort lv;
        if (id == Block.FURNACE.id || id == Block.FURNACE_LIT.id) {
            lv = lit ? Block.FURNACE_LIT.id : Block.FURNACE.id;
        } else if (id == Block.BRICK_FURNACE.id || id == Block.BRICK_FURNACE_LIT.id) {
            lv = lit ? Block.BRICK_FURNACE_LIT.id : Block.BRICK_FURNACE.id;
        } else {
            // not a furnace?
            return;
        }

        uint ll = ((uint)lv).setMetadata(metadata);

        // we cheat! we save&restore the BE
        var be = world.getBlockEntity(x, y, z);
        // don't trigger hooks!!
        world.setBlockMetadataSilentDumb(x, y, z, ll);

        // since we did it manually, we have to add the light ourselves
        world.setBlockLightRemesh(x, y, z, Block.lightLevel[lv] > 0 ? Block.lightLevel[lv] : (byte)0);

        // todo cook something up to update lighting quickly? like, something better than this GROSS hack below, like a nice method in World or something?
        var unlit = Block.lightLevel[id] > 0 && Block.lightLevel[lv] == 0;
        if (unlit) {
            // remove lightsource
            world.removeBlockLightAndPropagate(x, y, z);
        }

        var llit = Block.lightLevel[lv] > 0 && Block.lightLevel[id] == 0;
        if (llit) {
            // add lightsource
            world.setBlockLight(x, y, z, Block.lightLevel[lv]);
            var chunk = world.getChunk(x, z);
            world.blockLightQueue.Enqueue(new LightNode(x, y, z, chunk));
        }

        if (be != null) {
            world.setBlockEntity(x, y, z, be);
        }
    }

    public float getSmeltProgress() => smeltTime > 0 ? (float)smeltProgress / smeltTime : 0f;
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
            if (currentRecipe != null) {
                smeltTime = currentRecipe.getSmeltTime();
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

        data.addInt("smeltProgress", smeltProgress);
        data.addInt("fuelRemaining", fuelRemaining);
        data.addInt("fuelMax", fuelMax);
    }

    public void dropContents(World world, int x, int y, int z) {
        // server-only - only server spawns item entities
        if (Net.mode.isMPC()) {
            return;
        }

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