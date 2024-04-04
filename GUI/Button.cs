using System.Drawing;
using System.Numerics;

namespace BlockGame;

public class Button : GUIElement {
    public Button(Screen screen, Rectangle bounds) : base(screen, bounds) {

    }

    public override void draw() {
        screen.gui.draw(screen.gui.guiTexture, new Vector2(bounds.X, bounds.Y), screen.gui.buttonRect);
    }
}