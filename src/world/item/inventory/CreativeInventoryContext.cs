using BlockGame.main;
using BlockGame.ui.menu;
using BlockGame.util;
using BlockGame.util.stuff;
using BlockGame.world.block;
using BlockGame.world.entity;

namespace BlockGame.world.item.inventory;

/**
 * Creative inventory context that provides infinite items without backing storage.
 * Uses CreativeSlots for the item grid and regular slots for the player hotbar.
 */
public class CreativeInventoryContext : InventoryContext {
    private readonly PlayerInventory playerInv;

    private readonly List<ItemStack> allItems;
    private int currentPage = 0;
    private readonly int itemsPerPage;

    public int totalPages;

    public CreativeInventoryContext(PlayerInventory playerInv, int itemsPerPage) {
        this.playerInv = playerInv;
        this.itemsPerPage = itemsPerPage;
        this.allItems = [];

        collectAllItems();
        calculatePages();


        setupSlots(CreativeInventoryMenu.rows, CreativeInventoryMenu.cols, CreativeInventoryMenu.invOffsetX, CreativeInventoryMenu.invOffsetY);
    }

    private void collectAllItems() {
        allItems.Clear();

        // add all blocks
        for (int i = 1; i < Block.currentID; i++) {
            if (Block.get(i) == null || Block.get(i) == Block.AIR || Registry.ITEMS.blackList[Block.get(i)!.item.id]) {
                continue;
            }

            // special handling for candy block - add all variants
            // todo this is a gross hack. can we move this to a better place? maybe a virtual method on Block or something to get all valid variants
            if (i ==  Block.CANDY.id) {
                for (byte metadata = 0; metadata < Block.CANDY.maxValidMetadata() + 1; metadata++) {
                    allItems.Add(new ItemStack(Block.CANDY.item, 1, metadata));
                }
            }
            // special handling for candy slab - add all colour variants
            else if (i == Block.CANDY_SLAB.id) {
                for (byte color = 0; color < 24; color++) {
                    var metadata = CandySlab.setColour(0, color);
                    allItems.Add(new ItemStack(Block.CANDY_SLAB.item, 1, metadata));
                }
            }
            // special handling for candy stairs - add all colour variants
            else if (i == Block.CANDY_STAIRS.id) {
                for (byte color = 0; color < 24; color++) {
                    var metadata = CandyStairs.setColor(0, color);
                    allItems.Add(new ItemStack(Block.CANDY_STAIRS.item, 1, metadata));
                }
            }
            /*else if (i == Block.CINNABAR_ORE.id) {
                allItems.Add(new ItemStack(Block.CINNABAR_ORE.item, 1, 0));
                allItems.Add(new ItemStack(Block.CINNABAR_ORE.item, 1, 1));
            }*/
            else {
                allItems.Add(new ItemStack(Block.get(i)!.item, 1));
            }
        }

        // add all items
        for (int i = 0; i < Registry.ITEMS.count(); i++) {
            var item = Item.get(i);
            if (item == null || item.isBlock() || item == Item.AIR || item.id == Block.AIR.getItem().id || Registry.ITEMS.blackList[i]) {
                continue;
            }

            // special handling for dye - add all 16 colour variants
            if (item == Item.DYE) {
                for (byte metadata = 0; metadata < Block.CANDY.maxValidMetadata() + 1; metadata++) {
                    allItems.Add(new ItemStack(item, 1, metadata));
                }
            }
            else {
                allItems.Add(new ItemStack(item, 1));
            }
        }
    }

    private void calculatePages() {
        totalPages = (allItems.Count + itemsPerPage - 1) / itemsPerPage;
    }

    public void setupSlots(int rows, int cols, int invOffsetX, int invOffsetY) {
        slots.Clear();

        // create creative slots for the current page
        for (int i = 0; i < itemsPerPage; i++) {
            int x = i % cols;
            int y = i / cols;

            int slotX = invOffsetX + x * ItemSlot.SLOTSIZE;
            int slotY = invOffsetY + y * ItemSlot.SLOTSIZE;

            var itemIndex = currentPage * itemsPerPage + i;
            if (itemIndex < allItems.Count) {
                slots.Add(new CreativeSlot(allItems[itemIndex], slotX, slotY));
            } else {
                // empty slot for pages that don't fill completely
                slots.Add(new CreativeSlot(ItemStack.EMPTY, slotX, slotY));
            }
        }

        // add player hotbar slots (first 10 slots only!)
        for (int i = 0; i < 10; i++) {
            var hotbarSlot = new ItemSlot(playerInv, i,
                invOffsetX + i * ItemSlot.SLOTSIZE,
                invOffsetY + rows * ItemSlot.SLOTSIZE + 2); // 2 pixel padding
            slots.Add(hotbarSlot);
        }
    }

    public void setPage(int page) {
        if (page >= 0 && page < totalPages) {
            currentPage = page;
            updateCreativeSlots();
        }
    }

    private void updateCreativeSlots() {
        // only update the creative slots (first itemsPerPage slots), keep hotbar slots
        for (int i = 0; i < itemsPerPage && i < slots.Count; i++) {
            var itemIndex = currentPage * itemsPerPage + i;
            if (itemIndex < allItems.Count) {
                // replace the slot with new item
                var x = i % 10; // cols is hardcoded as 10 for now
                var y = i / 10;
                int slotX = 5 + x * ItemSlot.SLOTSIZE; // invOffsetX
                int slotY = 20 + y * ItemSlot.SLOTSIZE; // invOffsetY

                slots[i] = new CreativeSlot(allItems[itemIndex], slotX, slotY);
            } else {
                // empty slot for pages that don't fill completely
                var x = i % 10;
                var y = i / 10;
                int slotX = 5 + x * ItemSlot.SLOTSIZE;
                int slotY = 20 + y * ItemSlot.SLOTSIZE;

                slots[i] = new CreativeSlot(ItemStack.EMPTY, slotX, slotY);
            }
        }
    }

    public int getCurrentPage() => currentPage;

    public override void handleSlotClick(ItemSlot slot, ClickType click) {
        handleSlotClick(slot, click, Game.player);
    }

    public override void handleSlotClick(ItemSlot slot, ClickType click, Player player) {
        var cursor = player.inventory.cursor;

        // left-click merge-take: allow combining items from creative slots
        if (click == ClickType.LEFT && cursor != ItemStack.EMPTY && slot is CreativeSlot) {
            var slotStack = slot.getStack();
            if (slotStack != ItemStack.EMPTY && slotStack.same(cursor)) {
                // merge-take: take full stack from creative slot and merge with cursor
                var taken = slot.take(slotStack.quantity);
                if (taken != ItemStack.EMPTY) {
                    cursor.quantity += taken.quantity;
                }
                return;
            }
        }

        // default behaviour for everything else
        base.handleSlotClick(slot, click, player);
    }
}