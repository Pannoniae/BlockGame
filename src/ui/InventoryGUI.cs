using System.Numerics;
using BlockGame.util;
using Silk.NET.Maths;
using TrippyGL;
using TrippyGL.ImageSharp;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.ui;

public class InventoryGUI : Menu {

    public const int rows = 10;
    public const int cols = 4;

    public const int invOffsetY = 20;
    public const int textOffsetY = 4;
    public const int invOffsetX = 4;

    public ItemSlot[] slots = new ItemSlot[rows * cols];

    public Vector2D<int> guiPos;
    public Rectangle guiBounds;

    public Texture2D invTex = Texture2DExtensions.FromFile(Game.GD, "textures/creative_inventory.png");

    public InventoryGUI(Vector2D<int> guiPos) {
        this.guiPos = guiPos;
        resize(guiPos);
    }

    public void setup() {
        int i = 1;
        for (int y = 0; y < cols; y++) {
            for (int x = 0; x < rows; x++) {
                while (Blocks.isBlacklisted(i)) {
                    i++;
                }

                int item = i > Blocks.maxBlock ? 0 : i;

                int slotX = invOffsetX + x * ItemSlot.SLOTSIZE;
                int slotY = invOffsetY + y * ItemSlot.SLOTSIZE;
                slots[y * rows + x] = new ItemSlot(this, slotX, slotY) {
                    stack = new ItemStack((ushort)item, 1),
                };
                i++;
            }
        }
    }

    public override void draw() {
        Game.gui.drawUIImmediate(invTex, new Vector2(guiBounds.X, guiBounds.Y));
        // draw inventory text
        Game.gui.drawStringUI("Inventory", new Vector2(guiBounds.X + invOffsetX, guiBounds.Y + textOffsetY), Color4b.White, new Vector2(2, 2));
        foreach (var slot in slots) {
            slot.drawItem();
        }
    }

    public override void onMouseUp(Vector2 pos) {
        var guiPos = GUI.s2u(pos);
        foreach (var slot in slots) {
            var absoluteRect = new Rectangle(guiBounds.X + slot.rect.X, guiBounds.Y + slot.rect.Y, slot.rect.Width, slot.rect.Height);
            if (absoluteRect.Contains((int)guiPos.X, (int)guiPos.Y) && slot.stack.block != Blocks.AIR.id) {
                Console.Out.WriteLine("clicked!");
                // swap it to the hotbar for now
                var player = GameScreen.world.player;
                player.hotbar.slots[player.hotbar.selected] = slot.stack.copy();
            }
        }
    }

    public sealed override void resize(Vector2D<int> newSize) {
        base.resize(newSize);
        guiBounds = GUIElement.resolveAnchors(new Rectangle(guiPos.X, guiPos.Y, (int)invTex.Width, (int)invTex.Height),
            HorizontalAnchor.CENTREDCONTENTS, VerticalAnchor.TOP, this);
    }
}