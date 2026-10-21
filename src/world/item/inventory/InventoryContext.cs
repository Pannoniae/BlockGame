using BlockGame.main;
using BlockGame.net.packet;
using BlockGame.net.srv;
using BlockGame.util;
using BlockGame.world.block;
using BlockGame.world.entity;
using LiteNetLib;

namespace BlockGame.world.item.inventory;

/**
 * Groups related slots that work together as a unit.
 * Handles complex interactions and coordinates between different inventory areas.
 *
 * This is needed because the player can interact with multiple inventories at once, and we can't just yeet this onto the menu itself (there will be server code!)
 */
public abstract class InventoryContext {

    protected Player player;

    protected readonly List<ItemSlot> slots = [];

    // server-side: track viewers for broadcasting changes
    private List<ServerConnection>? viewers;

    private int invID;

    public List<ItemSlot> getSlots() => slots;

    /** server-side: add a viewer to this context */
    public void addViewer(ServerConnection conn, int inventoryID) {
        viewers ??= [];
        if (!viewers.Contains(conn)) {
            viewers.Add(conn);
        }
        invID = inventoryID;
    }

    /** server-side: remove a viewer from this context */
    public void removeViewer(ServerConnection conn) {
        viewers?.Remove(conn);
    }

    // note this is a mess, the net / inventory index should probably be separated better...
    /** does any slot of this context point into <paramref name="inv"/>? */
    public bool uses(Inventory inv) {
        foreach (var slot in slots) {
            if (slot.inventory == inv) {
                return true;
            }
        }

        return false;
    }

    /** context slot index for (inventory, index), or -1 */
    public int find(Inventory inv, int index) {
        for (int i = 0; i < slots.Count; i++) {
            if (slots[i].inventory == inv && slots[i].index == index) {
                return i;
            }
        }

        return -1;
    }

    /**
     * Wire index for INV_ID_PLAYER packets: PlayerInventory layout, 2x2 grid appended at TOTAL_SIZE.
     * -1 for slots that don't exist on the wire
     */
    private static int playerWireIdx(ItemSlot slot) {
        if (slot.index < 0) {
            return -1;
        }

        return slot.inventory switch {
            PlayerInventory => slot.index,
            CraftingGridInventory => PlayerInventory.TOTAL_SIZE + slot.index,
            _ => -1
        };
    }

    /**
     * server-side: broadcast slot change to all viewers. <paramref name="slotIndex"/> is a CONTEXT index.
     * Player inventory packets get translated to the player layout, window packets stay in context order.
     */
    public void notifySlotChanged(int slotIndex, ItemStack newStack) {
        if (viewers == null) {
            return;
        }

        var wire = slotIndex;
        if (invID == Constants.INV_ID_PLAYER) {
            wire = playerWireIdx(slots[slotIndex]);
            if (wire < 0) {
                return;
            }
        }
        else if (slots[slotIndex].index < 0) {
            return;
        }

        foreach (var viewer in viewers) {
            viewer.send(new SetSlotPacket {
                invID = invID,
                slotIndex = (ushort)wire,
                stack = newStack
            }, DeliveryMethod.ReliableOrdered);
        }
    }

    /** server-side: broadcast all slots belonging to a specific inventory */
    public void notifyInventorySlotsChanged(Inventory inv) {
        if (viewers == null) {
            return;
        }

        for (int i = 0; i < slots.Count; i++) {
            if (slots[i].inventory == inv) {
                notifySlotChanged(i, slots[i].getStack());
            }
        }
    }

    /** server-side: full resync */
    public void notifyAllSlotsChanged() {
        if (viewers == null) {
            return;
        }

        if (invID == Constants.INV_ID_PLAYER) {
            foreach (var viewer in viewers) {
                viewer.player?.syncInventory();
            }

            return;
        }

        var all = snapshot();
        foreach (var viewer in viewers) {
            viewer.send(new InventorySyncPacket {
                invID = invID,
                items = all
            }, DeliveryMethod.ReliableOrdered);
        }
    }

    private ItemStack[] snap = [];

    public ItemStack[] snapshot() {
        if (snap.Length != slots.Count) {
            snap = new ItemStack[slots.Count];
        }

        for (int i = 0; i < slots.Count; i++) {
            snap[i] = slots[i].getStack();
        }

        return snap;
    }

    private (int id, int qty, int meta)[] prevItems = [];

    public void updatePrevItems() {
        if (prevItems.Length != slots.Count) {
            prevItems = new (int, int, int)[slots.Count];
        }

        for (int i = 0; i < slots.Count; i++) {
            var s = slots[i].getStack();
            prevItems[i] = (s.id, s.quantity, s.metadata);
        }
    }

    /**
     * server-side: broadcast every slot that changed since mark(), to our viewers and to anyone else looking at
     * the same inventory. One click can touch any number of slots (shift-click, crafting eating the grid).
     */
    public void broadcastSlotUpdates(ServerConnection src) {
        for (int i = 0; i < slots.Count; i++) {
            var slot = slots[i];
            var s = slot.getStack();
            if (prevItems[i] == (s.id, s.quantity, s.metadata)) {
                continue;
            }

            notifySlotChanged(i, s);
            if (slot.inventory != null && slot.index >= 0) {
                GameServer.instance.notifyOtherViewers(src, slot.inventory, slot.index, s);
            }
        }
    }

    public int viewerCount() => viewers?.Count ?? 0;

    public virtual void handleSlotClick(ItemSlot slot, ClickType click) {
        handleSlotClick(slot, click, Game.player);
    }

    public virtual void handleSlotClick(ItemSlot slot, ClickType click, Player player) {
        var cursor = player.inventory.cursor;

        if (click is ClickType.SHIFT_LEFT or ClickType.SHIFT_RIGHT) {
            var idx = slots.IndexOf(slot);
            if (idx >= 0) {
                quickMove(idx);
            }
        }
        else if (click == ClickType.LEFT) {
            if (cursor == ItemStack.EMPTY) {
                // take from slot
                var currentStack = slot.getStack();
                var taken = slot.take(currentStack == ItemStack.EMPTY ? 1 : currentStack.quantity);
                if (taken != ItemStack.EMPTY && taken.id != Item.AIR.id && taken.id != Block.AIR.getItem().id) {
                    player.inventory.cursor = taken;
                }
            }
            else {
                // try to place cursor into slot
                if (slot.canPlace()) {
                    // normal slot - handles merging, swapping, etc.
                    var remaining = slot.place(cursor);
                    player.inventory.cursor = remaining;
                }
                else {
                    // output-only slot - try take+merge
                    var result = slot.getStack();
                    if (result != ItemStack.EMPTY && result.same(cursor)) {
                        var maxStack = cursor.getItem().getMaxStackSize();
                        var canMerge = maxStack - cursor.quantity;
                        if (canMerge >= result.quantity) {
                            var taken = slot.take(result.quantity);
                            player.inventory.cursor = new ItemStack(cursor.getItem(), cursor.quantity + taken.quantity, cursor.metadata);
                        }
                        // else cursor stack is full, do nothing :(
                    }
                }
            }
        }
        else if (click == ClickType.RIGHT) {
            if (cursor == ItemStack.EMPTY) {
                // take half
                var currentStack = slot.getStack();
                if (currentStack != ItemStack.EMPTY && currentStack.quantity > 0) {
                    var halfQuantity = (currentStack.quantity + 1) / 2;
                    var taken = slot.take(halfQuantity);
                    if (taken != ItemStack.EMPTY) {
                        player.inventory.cursor = taken;
                    }
                }
            }
            else {
                // right-click only places 1 item if slot is empty or can merge (no swapping!)
                if (!slot.canPlace()) {
                    // can't place into output-only slots
                    return;
                }

                var slotStack = slot.getStack();

                if (!slot.accept(cursor)) {
                    // slot doesn't accept this item type - do nothing
                    return;
                }

                if (slotStack == ItemStack.EMPTY) {
                    // empty slot - place 1 item
                    var singleItem = new ItemStack(cursor.getItem(), 1, cursor.metadata);
                    slot.place(singleItem);
                    player.inventory.cursor = new ItemStack(cursor.getItem(), cursor.quantity - 1, cursor.metadata);
                    if (cursor.quantity <= 1) {
                        player.inventory.cursor = ItemStack.EMPTY;
                    }
                }
                else if (slotStack.same(cursor)) {
                    // same item - try to add 1 if there's room
                    var maxStack = cursor.getItem().getMaxStackSize();
                    if (slotStack.quantity < maxStack) {
                        var singleItem = new ItemStack(cursor.getItem(), 1, cursor.metadata);
                        var remaining = slot.place(singleItem);
                        if (remaining == ItemStack.EMPTY) {
                            // successfully placed
                            player.inventory.cursor = new ItemStack(cursor.getItem(), cursor.quantity - 1, cursor.metadata);
                            if (cursor.quantity <= 1) {
                                player.inventory.cursor = ItemStack.EMPTY;
                            }
                        }
                    }
                    // else slot is full, do nothing
                }
                // else different item - do nothing (no swap on right-click)
            }
        }
    }

    // ============ SHIFT-CLICK ============

    protected const int HOTBAR = PlayerInventory.HOTBAR_SIZE;
    protected const int PLAYER_END = PlayerInventory.HOTBAR_SIZE + PlayerInventory.MAIN_SIZE;

    private void quickMove(int from) {
        var slot = slots[from];

        if (slot is CraftingResultSlot) {
            for (var r = slot.getStack(); r != ItemStack.EMPTY && fits(r, 0, PLAYER_END); r = slot.getStack()) {
                var taken = slot.take(r.quantity);
                if (taken == ItemStack.EMPTY) {
                    break;
                }

                getDestination(taken, 0, PLAYER_END);
            }

            return;
        }

        var stack = slot.getStack();
        if (stack == ItemStack.EMPTY) {
            return;
        }

        var moved = stack.quantity - getDestination(from, stack.copy()).quantity;
        if (moved > 0) {
            slot.take(moved);
        }
    }

    protected virtual ItemStack getDestination(int from, ItemStack stack) {
        if (from < HOTBAR) {
            return getDestination(stack, HOTBAR, PLAYER_END);
        }

        if (from < PLAYER_END) {
            return getDestination(stack, 0, HOTBAR);
        }

        return getDestination(stack, 0, PLAYER_END);
    }

    protected ItemStack getDestination(ItemStack stack, int start, int end) {
        for (int pass = 0; pass < 2; pass++) {
            for (int i = start; i < end; i++) {
                if (stack == ItemStack.EMPTY) {
                    return stack;
                }

                var s = slots[i];
                if (!s.canPlace() || !s.accept(stack)) {
                    continue;
                }

                var cur = s.getStack();
                if (pass == 0 ? cur != ItemStack.EMPTY && cur.same(stack) : cur == ItemStack.EMPTY) {
                    stack = s.place(stack);
                }
            }
        }

        return stack;
    }

    private bool fits(ItemStack stack, int start, int end) {
        var max = stack.getItem().getMaxStackSize();
        var room = 0;
        for (int i = start; i < end; i++) {
            var s = slots[i];
            var cur = s.getStack();
            if (cur == ItemStack.EMPTY) {
                if (s.canPlace() && s.accept(stack)) {
                    room += max;
                }
            }
            else if (cur.same(stack)) {
                room += max - cur.quantity;
            }

            if (room >= stack.quantity) {
                return true;
            }
        }

        return false;
    }

    public virtual void close() {

    }
}

public enum ClickType {
    LEFT,
    RIGHT,
    SHIFT_LEFT,
    SHIFT_RIGHT
}