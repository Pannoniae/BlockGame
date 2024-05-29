using System.Drawing;
using System.Numerics;

namespace BlockGame;

public class Text : GUIElement {
    public string text { get; set; }

    public bool shadowed = false;

    public Text(Screen screen, string text) : base(screen) {
        this.text = text;
        unscaledSize = true;
    }

    public static Text createText(Screen screen, Vector2 position, string text) {
        var bounds = Game.gui.guiFont.Measure(text);
        var guitext = new Text(screen, text);
        guitext.setPosition(new RectangleF(position.X, position.Y, position.X + bounds.X, position.Y + bounds.Y));
        return guitext;
    }


    public override void draw() {
        if (shadowed) {
            Game.gui.drawStringShadowed(text, new Vector2(bounds.X, bounds.Y));
        }
        else {
            Game.gui.drawString(text, new Vector2(bounds.X, bounds.Y));
        }
    }
}