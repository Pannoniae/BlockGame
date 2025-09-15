using BlockGame.item.inventory;
using Molten;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.util;

/// <summary>
/// Handles an ItemStack + item movement + gui-related code.
///
/// No longer *stores* an ItemStack, just references one by index in the inventory.
/// </summary>
public class ItemSlot {

    public const int SLOTSIZE = 20;
    public const int PADDING = 2;
    public const int ITEMSIZE = 16;
    public const int ITEMSIZEHALF = 8;
    
    private readonly Inventory inv;
    
    public int index = -1; // index in inventory, -1 = none
    
    public Rectangle rect;
    public Vector2I itemPos;
    

    public ItemSlot(Inventory inv, int index, int x, int y) {
        this.inv = inv;
        this.index = index;
        rect = new Rectangle(x, y, SLOTSIZE, SLOTSIZE);
        itemPos = new Vector2I(rect.X + PADDING, rect.Y + PADDING);
    }
    
    public ItemStack? getStack() {
        if (index == -1) return null;
        return inv.getStack(index);
    }
}