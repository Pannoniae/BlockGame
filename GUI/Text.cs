using System.Numerics;
using Silk.NET.Maths;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame;

public class Text : GUIElement {
    public string text { get; set; }

    public bool shadowed = false;

    public Text(Screen screen, string name, string text) : base(screen, name) {
        this.text = text;
        unscaledSize = true;
    }

    public static Text createText(Screen screen, string name, Vector2D<int> pos, string text) {
        var bounds = Game.gui.guiFont.Measure(text);
        var guitext = new Text(screen, name, text);
        guitext.setPosition(new Rectangle(pos.X, pos.Y, (int)(pos.X + bounds.X), (int)(pos.Y + bounds.Y)));
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