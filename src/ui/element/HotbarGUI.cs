using System.Numerics;
using BlockGame.main;
using BlockGame.ui.menu;
using BlockGame.util;
using Molten;

namespace BlockGame.ui.element;

public class HotbarGUI : GUIElement {

    public ItemSlot[] slots = new ItemSlot[LENGTH];

    public const int LENGTH = 10;
    public const int SIZE = 20;
    public const int BLOCKSIZE = 16;
    public const int PADDING = 2;

    // todo refactor these to automatically calculate coords
    public Rectangle hotbarTexture = new Rectangle(0, 48, SIZE * 10, SIZE);
    public Rectangle selectedTexture = new Rectangle(200, 48, SIZE, SIZE);

    public HotbarGUI(Menu menu, string name, Vector2I pos, string? text = null) : base(menu, name) {
        setPosition(new Rectangle(pos.X, pos.Y, hotbarTexture.Width, hotbarTexture.Height));
        for (int i = 0; i < 10; i++) {
            slots[i] = new ItemSlot(Game.player.inventory,i, Game.gui.uiCentreX + ((i - 5) * SIZE) + 2,
                GUI.instance.uiHeight - (BLOCKSIZE + 2));
        }
    }

    public override void draw() {
        // draw hotbar
        var world = Game.world;
        var inventory = world.player.inventory;
        var gui = Game.gui;

        Game.gui.drawUIImmediate(Game.gui.guiTexture, new Vector2(GUIbounds.X, GUIbounds.Y), hotbarTexture);
        for (int i = 0; i < LENGTH; i++) {
            var selected = inventory.selected == i;
            // if we draw in the middle, then we'll start in the middle of the 5th slot.... need to offset by half a slot
            slots[i].itemPos = new Vector2I(Game.gui.uiCentreX + ((i - 5) * SIZE) + 2,
                GUI.instance.uiHeight - (BLOCKSIZE + 2));
            Game.gui.drawItem(slots[i], new Vector2(0, 0));
            if (selected) {
                // todo make actual fucking gui coord converter so I can lay this out in purely GUI coordinates,
                // not a mix of GUI/screen coords for UI positions and texture drawing like now.....
                Game.gui.drawUIImmediate(Game.gui.guiTexture, new Vector2(gui.uiCentreX + (i - 5) * SIZE, GUIbounds.Y),
                    selectedTexture);
            }
        }

        // draw hearts in survival
        if (Game.gamemode.gameplay) {
            drawHearts();
        }
    }

    private static void drawHearts() {
        var gui = Game.gui;
        var player = Game.world.player;
        var hp = player.hp;

        const int MAX_HEARTS = 10;
        const int HP_PER_HEART = 10;

        // hearts positioned above hotbar, aligned to left edge
        int startX = gui.uiCentreX - (5 * SIZE);
        int startY = gui.uiHeight - (BLOCKSIZE + 2) - GUI.heartH - 2;

        for (int i = 0; i < MAX_HEARTS; i++) {
            double heartHP = hp - (i * HP_PER_HEART);
            int x = startX + i * GUI.heartW;

            if (heartHP >= HP_PER_HEART) {
                // full heart
                gui.drawUI(gui.guiTexture, new Vector2(x, startY),
                    new Rectangle(GUI.heartX, GUI.heartY, GUI.heartW, GUI.heartH));
            } else if (heartHP > 0) {
                // partial heart - split horizontally
                double fillRatio = heartHP / HP_PER_HEART;
                int fillWidth = (int)(GUI.heartW * fillRatio);

                // left side (filled)
                if (fillWidth > 0) {
                    gui.drawUI(gui.guiTexture,
                        new RectangleF(x, startY, fillWidth, GUI.heartH),
                        new Rectangle(GUI.heartX, GUI.heartY, fillWidth, GUI.heartH));
                }

                // right side (empty)
                int emptyWidth = GUI.heartW - fillWidth;
                if (emptyWidth > 0) {
                    gui.drawUI(gui.guiTexture,
                        new RectangleF(x + fillWidth, startY, emptyWidth, GUI.heartH),
                        new Rectangle(GUI.heartNoX + fillWidth, GUI.heartNoY, emptyWidth, GUI.heartH));
                }
            } else {
                // empty heart
                gui.drawUI(gui.guiTexture, new Vector2(x, startY),
                    new Rectangle(GUI.heartNoX, GUI.heartNoY, GUI.heartW, GUI.heartH));
            }
        }
    }
}