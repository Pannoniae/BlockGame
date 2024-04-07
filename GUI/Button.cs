using System.Drawing;
using System.Numerics;

namespace BlockGame;

public class Button : GUIElement {
    public Button(Screen screen, RectangleF bounds) : base(screen, bounds) {

    }

    public override void draw() {
        Game.gui.draw(Game.gui.guiTexture, new Vector2(bounds.X, bounds.Y), Game.gui.buttonRect);
    }
}