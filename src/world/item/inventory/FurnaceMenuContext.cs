using BlockGame.net.packet;
using BlockGame.net.srv;
using BlockGame.util;
using BlockGame.util.stuff;
using BlockGame.world.block.entity;
using LiteNetLib;

namespace BlockGame.world.item.inventory;

public class FurnaceMenuContext : InventoryContext {
    private readonly PlayerInventory playerInv;
    private readonly Inventory furnaceInv;

    public Inventory getFurnaceInventory() => furnaceInv;

    public const int hotbarX = 5;
    public const int hotbarY = 166;
    public const int mainY = 144;
    public const int inputX = 71;
    public const int inputY = 14;
    public const int fuelX = 71;
    public const int fuelY = 54;
    public const int outputX = 120;
    public const int outputY = 32;

    public FurnaceMenuContext(PlayerInventory playerInv, Inventory furnaceInv) {
        this.playerInv = playerInv;
        this.furnaceInv = furnaceInv;
        setupSlots();
    }

    public void setupSlots() {
        slots.Clear();

        const int cols = 10;
        const int rows = 5;

        // hotbar (first 10 slots)
        for (int i = 0; i < cols; i++) {
            slots.Add(new ItemSlot(playerInv, i,
                hotbarX + i * ItemSlot.SLOTSIZE,
                hotbarY));
        }

        // main inventory (4 rows)
        for (int row = 0; row < rows - 1; row++) {
            for (int col = 0; col < cols; col++) {
                int slotIndex = cols + row * cols + col;
                slots.Add(new ItemSlot(playerInv, slotIndex,
                    hotbarX + col * ItemSlot.SLOTSIZE,
                    mainY - row * ItemSlot.SLOTSIZE));
            }
        }

        // furnace slots: input (0), fuel (1), output (2)
        slots.Add(new ItemSlot(furnaceInv, 0, inputX, inputY));
        slots.Add(new FuelSlot(furnaceInv, 1, fuelX, fuelY));
        slots.Add(new OutputSlot(furnaceInv, 2, outputX, outputY));
    }

    // todo tuples are a stupid hack, this should be fixed up....
    private readonly (int id, int qty, int meta)[] sentSlots = [(-1, 0, 0), (-1, 0, 0), (-1, 0, 0)];
    private (int progress, int time, int fuel, int fuelMax, bool lit) sentBars = (-1, 0, 0, 0, false);

    public void sync(ServerConnection conn, FurnaceBlockEntity f) {
        for (int i = 0; i < 3; i++) {
            var s = slots[PLAYER_END + i].getStack();
            var v = (s.id, s.quantity, s.metadata);
            if (sentSlots[i] != v) {
                sentSlots[i] = v;
                notifySlotChanged(PLAYER_END + i, s);
            }
        }

        var bars = (f.smeltProgress, f.smeltTime, f.fuelRemaining, f.fuelMax, f.isLit());
        if (bars == sentBars) {
            return;
        }

        sentBars = bars;
        conn.send(new FurnaceSyncPacket {
            position = f.pos,
            smeltProgress = bars.smeltProgress,
            smeltTime = bars.smeltTime,
            fuelRemaining = bars.fuelRemaining,
            fuelMax = bars.fuelMax,
            lit = bars.Item5
        }, DeliveryMethod.ReliableOrdered);
    }

    protected override ItemStack getDestination(int from, ItemStack stack) {
        if (from >= PLAYER_END) {
            return base.getDestination(from, stack);
        }

        if (SmeltingRecipe.findRecipe(stack.getItem()) != null) {
            return getDestination(stack, PLAYER_END, PLAYER_END + 1);
        }

        if (Registry.ITEMS.fuelValue[stack.id] > 0) {
            return getDestination(stack, PLAYER_END + 1, PLAYER_END + 2);
        }

        return base.getDestination(from, stack);
    }
}
