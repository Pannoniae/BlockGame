using System.Numerics;
using Silk.NET.Maths;
using TrippyGL;
using TrippyGL.ImageSharp;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame;

public class InventoryGUI : Menu {

    public const int rows = 9;
    public const int cols = 4;

    public const int invOffsetY = 22;
    public const int invOffsetX = 4;

    public ItemSlot[] slots = new ItemSlot[rows * cols];

    public Rectangle pos;

    public Texture2D invTex = Texture2DExtensions.FromFile(Game.GD, "textures/inventory.png");

    public InventoryGUI(Vector2D<int> pos) {
        this.pos = GUIElement.resolveAnchors(new Rectangle(pos.X, pos.Y, (int)invTex.Width, (int)invTex.Height),
            HorizontalAnchor.CENTREDCONTENTS, VerticalAnchor.TOP, this);
    }

    public void setup() {
        int i = 1;
        for (int y = 0; y < cols; y++) {
            for (int x = 0; x < rows; x++) {
                var item = i > Blocks.maxBlock ? 0 : i;
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
        Game.gui.drawUIImmediate(invTex, new Vector2(pos.X, pos.Y));
        foreach (var slot in slots) {
            slot.drawItem();
        }
    }

    public override void click(Vector2 pos) {
        var guiPos = GUI.s2u(pos);
        foreach (var slot in slots) {
            var absoluteRect = new Rectangle(this.pos.X + slot.rect.X, this.pos.Y + slot.rect.Y, slot.rect.Width, slot.rect.Height);
            if (absoluteRect.Contains((int)guiPos.X, (int)guiPos.Y)) {
                Console.Out.WriteLine("clicked!");
                // swap it to the hotbar for now
                var player = GameScreen.world.player;
                player.hotbar.slots[player.hotbar.selected] = slot.stack.copy();
            }
        }
    }
}