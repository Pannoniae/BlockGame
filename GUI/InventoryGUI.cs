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

    public InventoryGUI(Screen screen, string name, Vector2D<int> pos) : base(screen, name) {
        setPosition(new Rectangle(pos.X, pos.Y, (int)invTex.Width, (int)invTex.Height));
    }

    public void setup() {
        for (int x = 0; x < rows; x++) {
            for (int y = 0; y < cols; y++) {
                int slotX = invOffsetX + x * ItemSlot.SLOTSIZE;
                int slotY = invOffsetY + y * ItemSlot.SLOTSIZE;
                slots[y * rows + x] = new ItemSlot(this, slotX, slotY) {
                    stack = new ItemStack(3, 1)
                };
            }
        }
    }

    public override void draw() {
        Game.gui.drawUI(invTex, new Vector2(GUIbounds.X, GUIbounds.Y));
        foreach (var slot in slots) {
            slot.drawItem();
        }
    }
}