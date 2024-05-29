using System.Drawing;
using System.Numerics;
using TrippyGL;
using TrippyGL.ImageSharp;

namespace BlockGame;

public class InventoryGUI : GUIElement {

    public const int rows = 9;
    public const int cols = 4;

    public ItemSlot[] slots = new ItemSlot[rows * cols];

    public Texture2D invTex = Texture2DExtensions.FromFile(Game.GD, "textures/inventory.png");

    public InventoryGUI(Screen screen, string name, Vector2 pos) : base(screen, name) {
        setPosition(new RectangleF(pos.X, pos.Y, invTex.Width, invTex.Height));
        for (int x = 0; x < rows; x++) {
            for (int y = 0; y < cols; y++) {
                slots[y * rows + x] = new ItemSlot(x, y);
            }
        }
    }

    public override void draw() {
        Game.gui.draw(invTex, new Vector2(bounds.X, bounds.Y));
    }
}