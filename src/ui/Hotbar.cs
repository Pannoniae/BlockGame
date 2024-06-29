using System.Numerics;
using BlockGame.util;
using Silk.NET.Maths;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.ui;

public class Hotbar : GUIElement {

    public ItemSlot[] slots = new ItemSlot[9];

    public const int SIZE = 20;
    public const int BLOCKSIZE = 16;
    public const int PADDING = 2;

    // todo refactor these to automatically calculate coords
    public Rectangle hotbarTexture = new Rectangle(0, 48, SIZE * 9, SIZE);
    public Rectangle selectedTexture = new Rectangle(180, 48, SIZE, SIZE);

    public  Hotbar(Menu menu, string name, Vector2D<int> pos, string? text = default) : base(menu, name) {
        setPosition(new Rectangle(pos.X, pos.Y, hotbarTexture.Width, hotbarTexture.Height));
        for (int i = 0; i < 9; i++) {
            slots[i] = new ItemSlot(null!, Game.gui.uiCentreX + ((i - 9 / 2) * SIZE - SIZE / 2) + 2,
                GUI.instance.uiHeight - (BLOCKSIZE + 2));
        }
    }

    public override void postDraw() {
        // draw hotbar
        var world = GameScreen.world;
        var items = world.player.hotbar.slots;
        var gui = Game.gui;
        Game.gui.drawUIImmediate(Game.gui.guiTexture, new Vector2(GUIbounds.X, GUIbounds.Y), hotbarTexture);
        for (int i = 0; i < items.Length; i++) {
            var selected = world.player.hotbar.selected == i;
            // if we draw in the middle, then we'll start in the middle of the 5th slot.... need to offset by half a slot
            slots[i].stack = items[i];
            slots[i].itemPos = new Vector2D<int>(Game.gui.uiCentreX + ((i - 9 / 2) * SIZE - SIZE / 2) + 2,
            GUI.instance.uiHeight - (BLOCKSIZE + 2));
            slots[i].drawItemWithoutInv();
            if (selected) {
                // todo make actual fucking gui coord converter so I can lay this out in purely GUI coordinates,
                // not a mix of GUI/screen coords for UI positions and texture drawing like now.....
                Game.gui.drawUIImmediate(Game.gui.guiTexture, new Vector2(gui.uiCentreX + (int)((i - 9 / 2) * SIZE - SIZE / 2), GUIbounds.Y),
                    selectedTexture);
            }
        }
    }
}