using System.Numerics;
using BlockGame.ui;
using Molten;
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

    public Rectangle rect;
    public Vector2I itemPos;

    public ItemSlot(int x, int y) {
        rect = new Rectangle(x, y, SLOTSIZE, SLOTSIZE);
        itemPos = new Vector2I(x + PADDING, y + PADDING);
    }
}