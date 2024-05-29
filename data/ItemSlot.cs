using Silk.NET.Maths;
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

    public Rectangle rect;
    public Vector2D<int> itemPos;

    public ItemSlot(int x, int y) {
        rect = new Rectangle(x, y, SLOTSIZE, SLOTSIZE);
        itemPos = new Vector2D<int>(x + PADDING, y + PADDING);
    }


    public void drawItem() {
        var world = GameScreen.world;
        //Game.gui.drawBlock(world, Blocks.get(stack.block), itemPos.X, itemPos.Y, ITEMSIZE);
    }
}