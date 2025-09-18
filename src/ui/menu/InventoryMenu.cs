using System.Numerics;
using BlockGame.GL;
using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.item;
using Molten;
using Silk.NET.Input;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.ui.menu;

public class InventoryMenu : Menu {

    public const int rows = 4;
    public const int cols = 10;
    public const int ITEMS_PER_PAGE = rows * cols;

    public const int invOffsetY = 20;
    public const int textOffsetX = 6;
    public const int textOffsetY = 8;
    public const int invOffsetX = 5;

    public const int PADDING = 2;
    public const int BUTTONPADDING = 5;
    public const int BUTTONW = 8;
    public const int BUTTONH = 6;

    public List<ItemSlot> slots = [];
    public List<ItemStack> allItems = new();

    public int currentPage = 0;
    public int totalPages = 0;

    private readonly CreativeInventory creativeInventory = new();

    public Vector2I guiPos;
    public Rectangle guiBounds;

    public readonly BTexture2D invTex;
    
    public Rectangle upArrow = new Rectangle(205, 0, BUTTONW, BUTTONH);
    public Rectangle downArrow = new Rectangle(205, 6, BUTTONW, BUTTONH);

    public override bool isModal() {
        return false;
    }

    public InventoryMenu(Vector2I guiPos) {
        this.guiPos = guiPos;
        
        invTex?.Dispose();
        invTex = new BTexture2D("textures/creative_inventory.png");
        invTex.reload();
        
        resize(guiPos);
    }

    public override void activate() {
        base.activate();
        var upButton = new HiddenButton(this, "upArrow", new Vector2(guiBounds.X + guiBounds.Width - BUTTONW - BUTTONPADDING,
            guiBounds.Y + invOffsetY), BUTTONW, BUTTONH);
        upButton.clicked += _ => {
            previousPage();
        };
        addElement(upButton);
        
        var downButton = new HiddenButton(this, "downArrow", new Vector2(guiBounds.X + guiBounds.Width - BUTTONW - BUTTONPADDING,
            guiBounds.Y + invOffsetY + rows * ItemSlot.SLOTSIZE - BUTTONH), BUTTONW, BUTTONH);
        downButton.clicked += _ => {
            nextPage();
        };
        addElement(downButton);
    }

    public void setup() {
        // collect all available items
        allItems.Clear();

        for (int i = 1; i <= Block.currentID; i++) {
            if (Block.blocks[i] == null || Block.isBlacklisted(i)) {
                continue;
            }

            // special handling for candy block - add all 16 variants
            if (i == Blocks.CANDY) {
                for (byte metadata = 0; metadata < 16; metadata++) {
                    allItems.Add(new ItemStack(Item.blockID(i), 1, metadata));
                }
            }
            else {
                allItems.Add(new ItemStack(Item.blockID(i), 1));
            }
        }

        // calculate total pages
        totalPages = (allItems.Count + ITEMS_PER_PAGE - 1) / ITEMS_PER_PAGE;

        // initialize slots layout
        for (int i = 0; i < ITEMS_PER_PAGE; i++) {
            int x = i % cols;
            int y = i / cols;

            int slotX = invOffsetX + x * ItemSlot.SLOTSIZE;
            int slotY = invOffsetY + y * ItemSlot.SLOTSIZE;

            slots.Add(new ItemSlot(creativeInventory, i, slotX, slotY));
        }

        updateCurrentPage();
        
        // add the slots for the hotbar
        
        var player = Game.world.player;
        for (int i = 0; i < player.survivalInventory.slots.Length; i++) {
            var hotbarSlot = new ItemSlot(player.survivalInventory, i, invOffsetX + i * ItemSlot.SLOTSIZE,
                invOffsetY + rows * ItemSlot.SLOTSIZE + PADDING);
            slots.Add(hotbarSlot);
        }

    }

    private void updateCurrentPage() {
        // clear all slots in backing inventory
        creativeInventory.clearAll();

        // fill backing inventory with current page items
        int startIdx = currentPage * ITEMS_PER_PAGE;
        int endIdx = Math.Min(startIdx + ITEMS_PER_PAGE, allItems.Count);

        for (int i = startIdx; i < endIdx; i++) {
            creativeInventory.setStack(i - startIdx, allItems[i].copy());
        }
    }

    public override void draw() {
        base.draw();
        Game.gui.drawUIImmediate(invTex, new Vector2(guiBounds.X, guiBounds.Y));
        // draw inventory text with page info
        string title = totalPages > 1 ? $"Inventory ({currentPage + 1}/{totalPages})" : "Inventory";
        Game.gui.drawStringUI(title, new Vector2(guiBounds.X + textOffsetX, guiBounds.Y + textOffsetY), Color4b.White);

        foreach (var slot in slots) {
            Game.gui.drawItem(slot, new Vector2(guiBounds.X, guiBounds.Y));
        }
        
        // draw the two arrows
        if (totalPages > 1) {
            var upPos = new Vector2(guiBounds.X + guiBounds.Width - BUTTONW - BUTTONPADDING, guiBounds.Y + invOffsetY);
            var downPos = new Vector2(guiBounds.X + guiBounds.Width - BUTTONW - BUTTONPADDING,
                guiBounds.Y + invOffsetY + rows * ItemSlot.SLOTSIZE - BUTTONH);
            Game.gui.drawUIImmediate(Game.gui.guiTexture, upPos, upArrow);
            Game.gui.drawUIImmediate(Game.gui.guiTexture, downPos, downArrow);
        }

        // draw cursor item
        var player = Game.world.player;
        if (player?.survivalInventory?.cursor != null) {
            var mousePos = Game.mousePos;
            Game.gui.drawCursorItem(player.survivalInventory.cursor, mousePos);
        }
    }

    public override void onMouseUp(Vector2 pos, MouseButton button) {
        base.onMouseUp(pos, button);
        var guiPos = GUI.s2u(pos);
        var player = Game.world.player;

        foreach (var slot in slots) {
            var absoluteRect = new Rectangle(guiBounds.X + slot.rect.X, guiBounds.Y + slot.rect.Y, slot.rect.Width, slot.rect.Height);
            if (absoluteRect.Contains((int)guiPos.X, (int)guiPos.Y)) {
                handleSlotClick(slot, button, player);
                return;
            }
        }
    }

    private void handleSlotClick(ItemSlot slot, MouseButton button, Player player) {
        var slotStack = slot.getStack();
        var cursor = player.survivalInventory.cursor;

        if (button == MouseButton.Left) {
            // left click behavior
            if (cursor == null) {
                // cursor empty, pick up entire stack
                if (slotStack != null && slotStack.id != Items.AIR) {
                    player.survivalInventory.cursor = slotStack.copy();
                    slot.inv.setStack(slot.index, null);
                }
            } else {
                // cursor has items
                if (slotStack == null || slotStack.id == Items.AIR) {
                    // slot empty, place entire cursor stack
                    slot.inv.setStack(slot.index, cursor.copy());
                    player.survivalInventory.cursor = null;
                } else if (slotStack.id == cursor.id && slotStack.metadata == cursor.metadata) {
                    // same item type, try to merge
                    var totalQuantity = slotStack.quantity + cursor.quantity;
                    if (totalQuantity <= 99) { // assuming max stack size is 99
                        slotStack.quantity = totalQuantity;
                        player.survivalInventory.cursor = null;
                    } else {
                        slotStack.quantity = 99;
                        cursor.quantity = totalQuantity - 99;
                    }
                } else {
                    // different items, swap them
                    var temp = slotStack.copy();
                    slot.inv.setStack(slot.index, cursor.copy());
                    player.survivalInventory.cursor = temp;
                }
            }
        } else if (button == MouseButton.Right) {
            // right click behavior
            if (cursor == null) {
                // cursor empty, pick up half of stack (rounded up)
                if (slotStack != null && slotStack.id != Items.AIR) {
                    var halfQuantity = (slotStack.quantity + 1) / 2;
                    player.survivalInventory.cursor = new ItemStack(slotStack.id, halfQuantity, slotStack.metadata);

                    slotStack.quantity -= halfQuantity;
                    if (slotStack.quantity <= 0) {
                        slot.inv.setStack(slot.index, null);
                    }
                }
            } else {
                // cursor has items
                if (slotStack == null || slotStack.id == Items.AIR) {
                    // slot empty, place one item from cursor
                    slot.inv.setStack(slot.index, new ItemStack(cursor.id, 1, cursor.metadata));
                    cursor.quantity--;
                    if (cursor.quantity <= 0) {
                        player.survivalInventory.cursor = null;
                    }
                } else if (slotStack.id == cursor.id && slotStack.metadata == cursor.metadata) {
                    // same item, place one item from cursor
                    if (slotStack.quantity < 99) {
                        slotStack.quantity++;
                        cursor.quantity--;
                        if (cursor.quantity <= 0) {
                            player.survivalInventory.cursor = null;
                        }
                    }
                }
                // if different items, do nothing on right click
            }
        }
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        base.onKeyDown(keyboard, key, scancode);
        switch (key) {
            case Key.PageUp:
                previousPage();
                break;
            case Key.PageDown:
                nextPage();
                break;
        }
    }

    public override void scroll(IMouse mouse, ScrollWheel scroll) {
        base.scroll(mouse, scroll);
        if (totalPages <= 1) {
            return;
        }

        if (scroll.Y > 0) {
            previousPage();
        } else if (scroll.Y < 0) {
            nextPage();
        }
    }

    private void nextPage() {
        if (currentPage < totalPages - 1) {
            currentPage++;
            updateCurrentPage();
        }
    }

    private void previousPage() {
        if (currentPage > 0) {
            currentPage--;
            updateCurrentPage();
        }
    }

    public sealed override void resize(Vector2I newSize) {
        base.resize(newSize);
        guiBounds = GUIElement.resolveAnchors(new Rectangle(guiPos.X, guiPos.Y, (int)invTex.width, (int)invTex.height),
            HorizontalAnchor.CENTREDCONTENTS, VerticalAnchor.TOP, this);
    }
}