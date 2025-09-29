using System.Numerics;
using BlockGame.main;
using BlockGame.ui.menu;
using BlockGame.util;
using Molten;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.ui.element;

public class HotbarGUI : GUIElement {

    public ItemSlot[] slots = new ItemSlot[LENGTH];

    public const int LENGTH = 10;
    public const int TEXTURE_SIZE = 20;
    public const int BLOCK_SIZE = 16;
    public const int PADDING = 2;

    // todo refactor these to automatically calculate coords
    public Rectangle hotbarTexture = new Rectangle(0, 48, TEXTURE_SIZE * 10, TEXTURE_SIZE);
    public Rectangle selectedTexture = new Rectangle(200, 48, TEXTURE_SIZE, TEXTURE_SIZE);

    public HotbarGUI(Menu menu, string name, Vector2I pos, string? text = null) : base(menu, name) {
        setPosition(new Rectangle(pos.X, pos.Y, hotbarTexture.Width, hotbarTexture.Height));
        for (int i = 0; i < LENGTH; i++) {
            slots[i] = new ItemSlot(Game.player.survivalInventory,i, Game.gui.uiCentreX + ((i - 5) * TEXTURE_SIZE) + 2,
                GUI.instance.uiHeight - (BLOCK_SIZE + 2));
        }
    }

    public override void postDraw() {
        // draw hotbar
        var world = Game.world;
        var inventory = world.player.survivalInventory;
        var gui = Game.gui;

        Game.gui.drawUIImmediate(Game.gui.guiTexture, new Vector2(GUIbounds.X, GUIbounds.Y), hotbarTexture);
        for (int i = 0; i < LENGTH; i++) {
            var selected = inventory.selected == i;
            // if we draw in the middle, then we'll start in the middle of the 5th slot.... need to offset by half a slot
            slots[i].itemPos = new Vector2I(Game.gui.uiCentreX + ((i - 5) * TEXTURE_SIZE) + 2,
                GUI.instance.uiHeight - (BLOCK_SIZE + 2));
            Game.gui.drawItem(slots[i], new Vector2(0, 0));
            if (selected) {
                // todo make actual fucking gui coord converter so I can lay this out in purely GUI coordinates,
                // not a mix of GUI/screen coords for UI positions and texture drawing like now.....
                Game.gui.drawUIImmediate(Game.gui.guiTexture, new Vector2(gui.uiCentreX + (i - 5) * TEXTURE_SIZE, GUIbounds.Y),
                    selectedTexture);
            }
        }
    }
}
