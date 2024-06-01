using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame;


/// <summary>
/// Handles an ItemStack + item movement + gui-related code.
/// </summary>
public class ItemSlot {

    public const int SLOTSIZE = 20;
    public const int PADDING = 2;
    public const int ITEMSIZE = 16;

    public ItemStack stack;

    public InventoryGUI inventory;

    public Rectangle rect;
    public Vector2D<int> itemPos;

    public ItemSlot(InventoryGUI inventory, int x, int y) {
        this.inventory = inventory;
        rect = new Rectangle(x, y, SLOTSIZE, SLOTSIZE);
        itemPos = new Vector2D<int>(x + PADDING, y + PADDING);
    }


    public void drawItem() {
        //Game.gui.tb.End();
        Game.gui.drawBlockUI(Blocks.get(stack.block), inventory.bounds.X + itemPos.X, inventory.bounds.Y + itemPos.Y, ITEMSIZE);
        //Game.gui.tb.Begin();
    }
}