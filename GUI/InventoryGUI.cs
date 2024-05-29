using System.Numerics;
using Silk.NET.Maths;
using TrippyGL;
using TrippyGL.ImageSharp;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame;

public class InventoryGUI : GUIElement {

    public const int rows = 9;
    public const int cols = 4;

    public const int invOffset = 20;

    public ItemSlot[] slots = new ItemSlot[rows * cols];

    public Texture2D invTex = Texture2DExtensions.FromFile(Game.GD, "textures/inventory.png");

    public InventoryGUI(Screen screen, string name, Vector2D<int> pos) : base(screen, name) {
        setPosition(new Rectangle(pos.X, pos.Y, (int)invTex.Width, (int)invTex.Height));
    }

    public void setup() {
        for (int x = 0; x < rows; x++) {
            for (int y = 0; y < cols; y++) {
                int slotX = GUIbounds.X + x * ItemSlot.SLOTSIZE;
                int slotY = GUIbounds.Y + invOffset + y * ItemSlot.SLOTSIZE;
                slots[y * rows + x] = new ItemSlot(slotX, slotY) {
                    stack = new ItemStack(3, 1)
                };
            }
        }
    }

    public override void draw() {
        Game.gui.draw(invTex, new Vector2(bounds.X, bounds.Y));
        foreach (var slot in slots) {
            slot.drawItem();
        }
    }
}