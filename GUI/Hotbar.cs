using System.Drawing;
using System.Numerics;
using TrippyGL;

namespace BlockGame;

public class Hotbar : GUIElement {

    public Inventory inv;

    public const int SIZE = 20;
    public const int BLOCKSIZE = 16;
    public const int PADDING = 2;

    // todo refactor these to automatically calculate coords
    public static Rectangle hotbarTexture = new Rectangle(0, 48, SIZE * 9, SIZE);
    public static Rectangle selectedTexture = new Rectangle(180, 48, SIZE, SIZE);

    public Hotbar(Screen screen, RectangleF guiPosition, string? text = default) : base(screen, guiPosition) {
        inv = GameScreen.world.player.hotbar;
    }

    public override void postDraw() {
        // draw hotbar
        var world = GameScreen.world;
        var slots = world.player.hotbar.slots;
        var gui = Game.gui;
        gui.tb.Begin(BatcherBeginMode.Immediate);
        Game.gui.draw(Game.gui.guiTexture, new Vector2(bounds.X, bounds.Y), hotbarTexture);
        for (int i = 0; i < slots.Length; i++) {
            var block = slots[i];
            var selected = world.player.hotbar.selected == i;
            // if we draw in the middle, then we'll start in the middle of the 5th slot.... need to offset by half a slot
            Game.gui.drawBlock(world, Blocks.get(block), Game.centreX + ((i - 9 / 2) * SIZE - SIZE / 2 + PADDING) * GUI.guiScale
                , Game.height - (BLOCKSIZE + 2) * GUI.guiScale, BLOCKSIZE);
            if (selected) {
                // todo make actual fucking gui coord converter so I can lay this out in purely GUI coordinates,
                // not a mix of GUI/screen coords for UI positions and texture drawing like now.....
                Game.gui.draw(Game.gui.guiTexture, new Vector2(Game.centreX + (int)((i - 9 / 2) * SIZE - SIZE / 2) * GUI.guiScale, bounds.Y),
                    selectedTexture);
            }
        }
        gui.tb.End();
    }
}