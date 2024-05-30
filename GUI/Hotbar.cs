using System.Numerics;
using Silk.NET.Maths;
using TrippyGL;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame;

public class Hotbar : GUIElement {

    public ItemSlot[] slots = new ItemSlot[9];

    public const int SIZE = 20;
    public const int BLOCKSIZE = 16;
    public const int PADDING = 2;

    // todo refactor these to automatically calculate coords
    public Rectangle hotbarTexture = new Rectangle(0, 48, SIZE * 9, SIZE);
    public Rectangle selectedTexture = new Rectangle(180, 48, SIZE, SIZE);

    public Hotbar(Screen screen, string name, Vector2D<int> pos, string? text = default) : base(screen, name) {
        setPosition(new Rectangle(pos.X, pos.Y, hotbarTexture.Width, hotbarTexture.Height));
        for (int i = 0; i < 9; i++) {
            slots[i] = new ItemSlot(null!, Game.gui.uiCentreX + ((i - 9 / 2) * SIZE - SIZE / 2),
                (i - 9 / 2) * SIZE - SIZE / 2);
        }
    }

    public override void postDraw() {
        // draw hotbar
        var world = GameScreen.world;
        var slots = world.player.hotbar.slots;
        var gui = Game.gui;
        gui.tb.Begin(BatcherBeginMode.Immediate);
        Game.gui.draw(Game.gui.guiTexture, new Vector2(bounds.X, bounds.Y), hotbarTexture);
        for (int i = 0; i < slots.Length; i++) {
            var stack = slots[i];
            var selected = world.player.hotbar.selected == i;
            // if we draw in the middle, then we'll start in the middle of the 5th slot.... need to offset by half a slot
            Game.gui.drawBlock(Blocks.get(stack.block), Game.centreX + ((i - 9 / 2) * SIZE - SIZE / 2 + PADDING) * GUI.guiScale
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