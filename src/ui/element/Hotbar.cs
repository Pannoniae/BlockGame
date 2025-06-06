using System.Numerics;
using BlockGame.util;
using Molten;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.ui;

public class Hotbar : GUIElement {

    public ItemSlot[] slots = new ItemSlot[10];

    public const int SIZE = 20;
    public const int BLOCKSIZE = 16;
    public const int PADDING = 2;

    // todo refactor these to automatically calculate coords
    public Rectangle hotbarTexture = new Rectangle(0, 48, SIZE * 10, SIZE);
    public Rectangle selectedTexture = new Rectangle(200, 48, SIZE, SIZE);

    public Hotbar(Menu menu, string name, Vector2I pos, string? text = default) : base(menu, name) {
        setPosition(new Rectangle(pos.X, pos.Y, hotbarTexture.Width, hotbarTexture.Height));
        for (int i = 0; i < 10; i++) {
            slots[i] = new ItemSlot(Game.gui.uiCentreX + ((i - 5) * SIZE) + 2,
                GUI.instance.uiHeight - (BLOCKSIZE + 2));
        }
    }

    public override void postDraw() {
        // draw hotbar
        var world = Game.world;
        var items = world.player.hotbar.slots;
        var gui = Game.gui;
        Game.gui.drawUIImmediate(Game.gui.guiTexture, new Vector2(GUIbounds.X, GUIbounds.Y), hotbarTexture);
        for (int i = 0; i < items.Length; i++) {
            var selected = world.player.hotbar.selected == i;
            // if we draw in the middle, then we'll start in the middle of the 5th slot.... need to offset by half a slot
            slots[i].stack = items[i];
            slots[i].itemPos = new Vector2I((int)(Game.gui.uiCentreX + ((i - 5) * SIZE) + 2),
                GUI.instance.uiHeight - (BLOCKSIZE + 2));
            Game.gui.drawItemWithoutInv(slots[i]);
            if (selected) {
                // todo make actual fucking gui coord converter so I can lay this out in purely GUI coordinates,
                // not a mix of GUI/screen coords for UI positions and texture drawing like now.....
                Game.gui.drawUIImmediate(Game.gui.guiTexture, new Vector2(gui.uiCentreX + (int)((i - 5) * SIZE), GUIbounds.Y),
                    selectedTexture);
            }
        }
    }
}