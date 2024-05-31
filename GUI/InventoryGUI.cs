using System.Numerics;
using Silk.NET.Maths;
using TrippyGL;
using TrippyGL.ImageSharp;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame;

public class InventoryGUI : GUIElement {

    public const int rows = 9;
    public const int cols = 4;

    public const int invOffsetY = 22;
    public const int invOffsetX = 4;

    public ItemSlot[] slots = new ItemSlot[rows * cols];

    public Texture2D invTex = Texture2DExtensions.FromFile(Game.GD, "textures/inventory.png");

    public InventoryGUI(Menu menu, string name, Vector2D<int> pos) : base(menu, name) {
        setPosition(new Rectangle(pos.X, pos.Y, (int)invTex.Width, (int)invTex.Height));
    }

    public void setup() {
        int i = 1;
        for (int y = 0; y < cols; y++) {
            for (int x = 0; x < rows; x++) {
                var item = i > Blocks.blockCount - 1 ? 0 : i;
                int slotX = invOffsetX + x * ItemSlot.SLOTSIZE;
                int slotY = invOffsetY + y * ItemSlot.SLOTSIZE;
                slots[y * rows + x] = new ItemSlot(this, slotX, slotY) {
                    stack = new ItemStack((ushort)item, 1)
                };
                i++;
            }
        }
    }

    public override void draw() {
        Game.gui.drawUIImmediate(invTex, new Vector2(GUIbounds.X, GUIbounds.Y));
        foreach (var slot in slots) {
            slot.drawItem();
        }
    }
}