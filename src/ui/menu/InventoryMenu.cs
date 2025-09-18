using System.Numerics;
using BlockGame.GL;
using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.util;
using BlockGame.world;
using BlockGame.world.block;
using BlockGame.world.item;
using BlockGame.world.item.inventory;
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

    private readonly CreativeInventoryContext creativeContext;

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

        creativeContext = new CreativeInventoryContext(ITEMS_PER_PAGE);

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
        // setup slots using the creative context
        creativeContext.setupSlots(rows, cols, invOffsetX, invOffsetY);
        slots = creativeContext.getSlots();
    }


    public override void draw() {
        base.draw();
        Game.gui.drawUIImmediate(invTex, new Vector2(guiBounds.X, guiBounds.Y));
        // draw inventory text with page info
        var currentPage = creativeContext.getCurrentPage();
        var totalPages = creativeContext.totalPages;
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
        // convert MouseButton to ClickType and delegate to the creative context
        var clickType = button == MouseButton.Left ? ClickType.LEFT : ClickType.RIGHT;
        creativeContext.handleSlotClick(slot, clickType);
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
        if (creativeContext.totalPages <= 1) {
            return;
        }

        if (scroll.Y > 0) {
            previousPage();
        } else if (scroll.Y < 0) {
            nextPage();
        }
    }

    private void nextPage() {
        var currentPage = creativeContext.getCurrentPage();
        if (currentPage < creativeContext.totalPages - 1) {
            creativeContext.setPage(currentPage + 1);
        }
    }

    private void previousPage() {
        var currentPage = creativeContext.getCurrentPage();
        if (currentPage > 0) {
            creativeContext.setPage(currentPage - 1);
        }
    }

    public sealed override void resize(Vector2I newSize) {
        base.resize(newSize);
        guiBounds = GUIElement.resolveAnchors(new Rectangle(guiPos.X, guiPos.Y, (int)invTex.width, (int)invTex.height),
            HorizontalAnchor.CENTREDCONTENTS, VerticalAnchor.TOP, this);
    }
}