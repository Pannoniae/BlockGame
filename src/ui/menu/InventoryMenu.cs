using System.Numerics;
using BlockGame.GL;
using BlockGame.item;
using BlockGame.src.ui.element;
using BlockGame.util;
using Molten;
using Silk.NET.Input;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.ui;

public class InventoryMenu : Menu {

    public const int rows = 4;
    public const int cols = 10;
    public const int ITEMS_PER_PAGE = rows * cols;

    public const int invOffsetY = 20;
    public const int textOffsetX = 6;
    public const int textOffsetY = 8;
    public const int invOffsetX = 5;

    public const int PADDING = 2;
    public const int BUTTONPADDING = 6;
    public const int BUTTONSIZE = 6;

    public ItemSlot[] slots = new ItemSlot[ITEMS_PER_PAGE];
    public List<ItemStack> allItems = new();

    public int currentPage = 0;
    public int totalPages = 0;

    public Vector2I guiPos;
    public Rectangle guiBounds;

    public readonly BTexture2D invTex;
    
    public Rectangle upArrow = new Rectangle(199, 0, BUTTONSIZE, BUTTONSIZE);
    public Rectangle downArrow = new Rectangle(199, 14, BUTTONSIZE, BUTTONSIZE);

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
        var upButton = new HiddenButton(this, "upArrow", new Vector2(guiBounds.X + guiBounds.Width - BUTTONSIZE - BUTTONPADDING,
            guiBounds.Y + invOffsetY), BUTTONSIZE, BUTTONSIZE);
        upButton.clicked += _ => {
            previousPage();
        };
        addElement(upButton);
        
        var downButton = new HiddenButton(this, "downArrow", new Vector2(guiBounds.X + guiBounds.Width - BUTTONSIZE - BUTTONPADDING,
            guiBounds.Y + invOffsetY + rows * ItemSlot.SLOTSIZE - BUTTONSIZE), BUTTONSIZE, BUTTONSIZE);
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

            slots[i] = new ItemSlot(slotX, slotY);
        }

        updateCurrentPage();
    }

    private void updateCurrentPage() {
        // clear all slots
        for (int i = 0; i < ITEMS_PER_PAGE; i++) {
            slots[i].stack = new ItemStack(Blocks.AIR, 1);
        }

        // fill slots with current page items
        int startIdx = currentPage * ITEMS_PER_PAGE;
        int endIdx = Math.Min(startIdx + ITEMS_PER_PAGE, allItems.Count);

        for (int i = startIdx; i < endIdx; i++) {
            slots[i - startIdx].stack = allItems[i].copy();
        }
    }

    public override void draw() {
        base.draw();
        Game.gui.drawUIImmediate(invTex, new Vector2(guiBounds.X, guiBounds.Y));
        // draw inventory text with page info
        string title = totalPages > 1 ? $"Inventory ({currentPage + 1}/{totalPages})" : "Inventory";
        Game.gui.drawStringUI(title, new Vector2(guiBounds.X + textOffsetX, guiBounds.Y + textOffsetY), Color4b.White);

        foreach (var slot in slots) {
            Game.gui.drawItem(slot, slot.stack, this);
        }
        
        // draw the two arrows
        if (totalPages > 1) {
            var upPos = new Vector2(guiBounds.X + guiBounds.Width - BUTTONSIZE - BUTTONPADDING, guiBounds.Y + invOffsetY);
            var downPos = new Vector2(guiBounds.X + guiBounds.Width - BUTTONSIZE - BUTTONPADDING,
                guiBounds.Y + invOffsetY + rows * ItemSlot.SLOTSIZE - BUTTONSIZE);
            Game.gui.drawUIImmediate(Game.gui.guiTexture, upPos, upArrow);
            Game.gui.drawUIImmediate(Game.gui.guiTexture, downPos, downArrow);
        }
    }

    public override void onMouseUp(Vector2 pos, MouseButton button) {
        base.onMouseUp(pos, button);
        var guiPos = GUI.s2u(pos);
        foreach (var slot in slots) {
            var absoluteRect = new Rectangle(guiBounds.X + slot.rect.X, guiBounds.Y + slot.rect.Y, slot.rect.Width, slot.rect.Height);
            if (absoluteRect.Contains((int)guiPos.X, (int)guiPos.Y) && slot.stack.id != Blocks.AIR) {
                Log.debug("clicked!");
                // swap it to the hotbar for now
                var player = Game.world.player;
                player.hotbar.slots[player.hotbar.selected] = slot.stack.copy();
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