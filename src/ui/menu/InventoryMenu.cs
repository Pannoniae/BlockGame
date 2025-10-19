using System.Numerics;
using BlockGame.GL;
using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.util;
using BlockGame.world.item.inventory;
using Molten;
using Silk.NET.Input;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.ui.menu;

/**
 * Base class for inventory-based menus to reduce duplication slop...
 */
public abstract class InventoryMenu : Menu {
    protected List<ItemSlot> slots = [];
    protected Vector2I guiPos;
    protected Rectangle guiBounds;
    public BTexture2D invTex;

    public override bool isModal() {
        return false;
    }

    public override void deactivate() {
        base.deactivate();

        // drop cursor items when closing inventory to prevent voiding
        var player = Game.world?.player;
        if (player?.inventory?.cursor != null && player.inventory.cursor != ItemStack.EMPTY) {
            player.dropItemStack(player.inventory.cursor, withVelocity: false);
            player.inventory.cursor = ItemStack.EMPTY;
        }
    }

    protected abstract string getTitle();
    protected abstract BTexture2D getTexture();

    protected virtual int getWidth() {
        return (int)invTex.width;
    }

    protected virtual int getHeight() {
        return (int)invTex.height;
    }

    protected abstract int getTextOffsetX();
    protected abstract int getTextOffsetY();

    /** Hook for subclasses to draw slots with custom logic (e.g. crafting tints). */
    protected virtual void drawSlots(Vector2 guiBoundsPos) {
        foreach (var slot in slots) {
            Game.gui.drawItem(slot, guiBoundsPos);
        }
    }

    public override void draw() {
        base.draw();
        var guiBoundsPos = new RectangleF(guiBounds.X, guiBounds.Y, getWidth(), getHeight());
        Game.gui.drawUIImmediate(invTex, guiBoundsPos, new Rectangle(0, 0, getWidth(), getHeight()));
        Game.gui.drawStringUI(getTitle(), new Vector2(guiBounds.X + getTextOffsetX(), guiBounds.Y + getTextOffsetY()), Color.White);

        drawSlots(new Vector2(guiBounds.X, guiBounds.Y));

        // draw cursor item
        var player = Game.world.player;
        if (player?.inventory?.cursor != null) {
            Game.gui.drawCursorItem(player.inventory.cursor, Game.mousePos);
        }
    }

    protected override string? getTooltipText() {
        var player = Game.world.player;

        // if holding an item in cursor, show its tooltip
        if (player?.inventory?.cursor != null && player.inventory.cursor != ItemStack.EMPTY) {
            return player.inventory.cursor.getItem().getName(player.inventory.cursor);
        }

        var guiPos = GUI.s2u(Game.mousePos);

        // check slots first
        foreach (var slot in slots) {
            var absoluteRect = new Rectangle(guiBounds.X + slot.rect.X, guiBounds.Y + slot.rect.Y, slot.rect.Width, slot.rect.Height);
            if (absoluteRect.Contains((int)guiPos.X, (int)guiPos.Y)) {
                var stack = slot.getStack();
                if (stack != ItemStack.EMPTY && stack.id != 0) {
                    return stack.getItem().getName(stack);
                }
                break;
            }
        }

        // fallback to base (GUIElement tooltips)
        return base.getTooltipText();
    }

    public override void onMouseUp(Vector2 pos, MouseButton button) {
        base.onMouseUp(pos, button);
        var guiPos = GUI.s2u(pos);

        foreach (var slot in slots) {
            var absoluteRect = new Rectangle(guiBounds.X + slot.rect.X, guiBounds.Y + slot.rect.Y, slot.rect.Width, slot.rect.Height);
            if (absoluteRect.Contains((int)guiPos.X, (int)guiPos.Y)) {
                handleSlotClick(slot, button);
                return;
            }
        }
    }

    protected abstract void handleSlotClick(ItemSlot slot, MouseButton button);

    public sealed override void resize(Vector2I newSize) {
        base.resize(newSize);
        invTex = getTexture();
        guiBounds = GUIElement.resolveAnchors(new Rectangle(guiPos.X, guiPos.Y, getWidth(), getHeight()),
            HorizontalAnchor.CENTREDCONTENTS, VerticalAnchor.TOP, this);
    }
}