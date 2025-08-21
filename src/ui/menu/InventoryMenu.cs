using System.Numerics;
using BlockGame.GL;
using BlockGame.GL.vertexformats;
using BlockGame.util;
using Molten;
using Silk.NET.Input;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.ui;

public class InventoryMenu : Menu {

    public const int rows = 10;
    public const int cols = 5;

    public const int invOffsetY = 20;
    public const int textOffsetY = 2;
    public const int invOffsetX = 4;

    public ItemSlot[] slots = new ItemSlot[rows * cols];

    public Vector2I guiPos;
    public Rectangle guiBounds;

    public readonly BTexture2D invTex;

    public override bool isModal() {
        return false;
    }

    public InventoryMenu(Vector2I guiPos) {
        this.guiPos = guiPos;
        
        invTex?.Dispose();
        invTex = new BTexture2D("textures/creative_inventory.png");
        invTex.reload();
        
        resize(guiPos);
    }

    public void setup() {
        int i = 1;
        for (int y = 0; y < cols; y++) {
            for (int x = 0; x < rows; x++) {
                while (Block.isBlacklisted(i)) {
                    i++;
                }

                int item = i >= Block.currentID ? 0 : i;

                int slotX = invOffsetX + x * ItemSlot.SLOTSIZE;
                int slotY = invOffsetY + y * ItemSlot.SLOTSIZE;
                slots[y * rows + x] = new ItemSlot(slotX, slotY) {
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
            Game.gui.drawItem(slot, slot.stack, this);
        }
    }

    public override void onMouseUp(Vector2 pos, MouseButton button) {
        var guiPos = GUI.s2u(pos);
        foreach (var slot in slots) {
            var absoluteRect = new Rectangle(guiBounds.X + slot.rect.X, guiBounds.Y + slot.rect.Y, slot.rect.Width, slot.rect.Height);
            if (absoluteRect.Contains((int)guiPos.X, (int)guiPos.Y) && slot.stack.block != Block.AIR.id) {
                Console.Out.WriteLine("clicked!");
                // swap it to the hotbar for now
                var player = Game.world.player;
                player.hotbar.slots[player.hotbar.selected] = slot.stack.copy();
            }
        }
    }

    public sealed override void resize(Vector2I newSize) {
        base.resize(newSize);
        guiBounds = GUIElement.resolveAnchors(new Rectangle(guiPos.X, guiPos.Y, (int)invTex.width, (int)invTex.height),
            HorizontalAnchor.CENTREDCONTENTS, VerticalAnchor.TOP, this);
    }
}