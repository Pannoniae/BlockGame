using System.Numerics;
using BlockGame.GL;
using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.util;
using BlockGame.world.item.inventory;
using Molten;
using Silk.NET.Input;

namespace BlockGame.ui.menu;

public class CreativeInventoryMenu : InventoryMenu {

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

    private readonly CreativeInventoryContext creativeCtx;

    public Rectangle upArrow = new Rectangle(205, 0, BUTTONW, BUTTONH);
    public Rectangle downArrow = new Rectangle(205, 6, BUTTONW, BUTTONH);

    public CreativeInventoryMenu(Vector2I guiPos) {
        this.guiPos = guiPos;

        invTex?.Dispose();
        invTex = new BTexture2D("textures/creative_inventory.png");
        invTex.reload();

        creativeCtx = (CreativeInventoryContext)Game.player.inventoryCtx;

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

    public override void deactivate() {
        base.deactivate();
        // reset to default player inventory ID
        if (Game.player != null) {
            Game.player.currentInventoryID = -1;
        }
    }

    public void setup() {
        creativeCtx.setupSlots(rows, cols, invOffsetX, invOffsetY);
        slots = creativeCtx.getSlots();
    }

    protected override string getTitle() {
        var currentPage = creativeCtx.getCurrentPage();
        var totalPages = creativeCtx.totalPages;
        return totalPages > 1 ? $"Inventory ({currentPage + 1}/{totalPages})" : "Inventory";
    }

    protected override BTexture2D getTexture() => invTex;
    protected override int getTextOffsetX() => textOffsetX;
    protected override int getTextOffsetY() => textOffsetY;

    protected override void drawSlots(Vector2 guiBoundsPos) {
        foreach (var slot in slots) {
            Game.gui.drawItem(slot, guiBoundsPos);
        }

        // draw the two arrows
        if (creativeCtx.totalPages > 1) {
            var upPos = new Vector2(guiBounds.X + guiBounds.Width - BUTTONW - BUTTONPADDING, guiBounds.Y + invOffsetY);
            var downPos = new Vector2(guiBounds.X + guiBounds.Width - BUTTONW - BUTTONPADDING,
                guiBounds.Y + invOffsetY + rows * ItemSlot.SLOTSIZE - BUTTONH);
            Game.gui.drawUIImmediate(Game.gui.guiTexture, upPos, upArrow);
            Game.gui.drawUIImmediate(Game.gui.guiTexture, downPos, downArrow);
        }
    }

    protected override void handleSlotClick(ItemSlot slot, MouseButton button) {
        var clickType = button == MouseButton.Left ? ClickType.LEFT : ClickType.RIGHT;
        creativeCtx.handleSlotClick(slot, clickType);
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
        if (creativeCtx.totalPages <= 1) {
            return;
        }

        if (scroll.Y > 0) {
            previousPage();
        } else if (scroll.Y < 0) {
            nextPage();
        }
    }

    private void nextPage() {
        var currentPage = creativeCtx.getCurrentPage();
        if (currentPage < creativeCtx.totalPages - 1) {
            creativeCtx.setPage(currentPage + 1);
        }
    }

    private void previousPage() {
        var currentPage = creativeCtx.getCurrentPage();
        if (currentPage > 0) {
            creativeCtx.setPage(currentPage - 1);
        }
    }
}