using System.Drawing;
using System.Numerics;

namespace BlockGame;


/// <summary>
/// Handles an ItemStack + item movement + gui-related code.
/// </summary>
public class ItemSlot {

    public const int SLOTSIZE = 20;
    public const int PADDING = 2;
    public const int ITEMSIZE = 16;

    public ItemStack stack;

    public RectangleF rect;
    public Vector2 itemPos;

    public ItemSlot(int x, int y) {
        rect = new RectangleF(x, y, SLOTSIZE, SLOTSIZE);
        itemPos = new Vector2(x + PADDING, y + PADDING);
    }
}