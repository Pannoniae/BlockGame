using System.Numerics;
using BlockGame.ui;
using Silk.NET.Maths;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.util;

/// <summary>
/// Handles an ItemStack + item movement + gui-related code.
/// </summary>
public class ItemSlot {

    public const int SLOTSIZE = 20;
    public const int PADDING = 2;
    public const int ITEMSIZE = 16;
    public const int ITEMSIZEHALF = 8;

    public ItemStack stack;

    public InventoryMenu inventory;

    public Rectangle rect;
    public Vector2D<int> itemPos;

    public ItemSlot(InventoryMenu inventory, int x, int y) {
        this.inventory = inventory;
        rect = new Rectangle(x, y, SLOTSIZE, SLOTSIZE);
        itemPos = new Vector2D<int>(x + PADDING, y + PADDING);
    }


    public void drawItem() {
        Game.gui.drawBlockUI(Blocks.get(stack.block), inventory.guiBounds.X + itemPos.X, inventory.guiBounds.Y + itemPos.Y, ITEMSIZE);
        // draw amount text
        if (stack.quantity > 1) {
            var s = stack.quantity.ToString();
            Game.gui.drawStringUIThin(s, new Vector2(inventory.guiBounds.X + itemPos.X + ITEMSIZE - PADDING - s.Length * 6f / ui.GUI.guiScale,
                inventory.guiBounds.Y + itemPos.Y + ITEMSIZE - 13f / GUI.guiScale - PADDING));
        }
    }

    public void drawItemWithoutInv() {
        Game.gui.drawBlockUI(Blocks.get(stack.block), itemPos.X, itemPos.Y, ITEMSIZE);
        if (stack.quantity > 1) {
            var s = stack.quantity.ToString();
            Game.gui.drawStringUIThin(s, new Vector2(itemPos.X + ITEMSIZE - PADDING - s.Length * 6f / ui.GUI.guiScale,
                itemPos.Y + ITEMSIZE - 13f / GUI.guiScale - PADDING));
        }
    }
}