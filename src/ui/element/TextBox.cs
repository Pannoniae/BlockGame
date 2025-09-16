using System.Numerics;
using BlockGame.ui.menu;

namespace BlockGame.ui.element;

public class TextBox : GUIElement {

    public string input;

    public static int padding => 2 * GUI.guiScale;

    public TextBox(Menu menu, string name) : base(menu, name) {
    }


    public override void draw() {
        main.Game.gui.draw(main.Game.gui.guiTexture, new Vector2(bounds.X, bounds.Y), source: main.Game.gui.grayButtonRect);
        main.Game.gui.drawString(input, new Vector2(bounds.X + padding, bounds.Y + padding));
    }
}