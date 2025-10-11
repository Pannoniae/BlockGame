using BlockGame.main;
using BlockGame.util;
using BlockGame.world.block;

namespace BlockGame.world.item.inventory;

/**
 * Creative inventory context that provides infinite items without backing storage.
 * Uses CreativeSlots for the item grid and regular slots for the player hotbar.
 */
public class CreativeInventoryContext : InventoryContext {
    private readonly List<ItemStack> allItems;
    private int currentPage = 0;
    private readonly int itemsPerPage;

    public int totalPages;

    public CreativeInventoryContext(int itemsPerPage) {
        this.itemsPerPage = itemsPerPage;
        this.allItems = [];

        collectAllItems();
        calculatePages();
    }

    private void collectAllItems() {
        allItems.Clear();

        // add all blocks
        for (int i = 1; i < Block.currentID; i++) {
            if (Block.blocks[i] == null || Block.isBlacklisted(i)) {
                continue;
            }

            // special handling for candy block - add all 16 variants
            if (i == Blocks.CANDY) {
                for (byte metadata = 0; metadata < Block.CANDY.maxValidMetadata() + 1; metadata++) {
                    allItems.Add(new ItemStack(Item.blockID(i), 1, metadata));
                }
            }
            else {
                allItems.Add(new ItemStack(Item.blockID(i), 1));
            }
        }

        // add all items
        for (int i = 1; i < Item.currentID; i++) {
            var item = Item.get(i);
            if (item != null && item.isItem()) {
                allItems.Add(new ItemStack(i, 1));
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
        var player = Game.world.player;
        for (int i = 0; i < 10; i++) {
            var hotbarSlot = new ItemSlot(player.inventory, i,
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
}