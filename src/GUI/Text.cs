using System.Numerics;
using Silk.NET.Maths;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.GUI;

public class Text : GUIElement {

    public bool shadowed = false;

    public Text(Menu menu, string name, string text) : base(menu, name) {
        this.text = text;
        unscaledSize = true;
    }

    public static Text createText(Menu menu, string name, Vector2D<int> pos, string text) {
        var bounds = Game.gui.measureString(text);
        var guitext = new Text(menu, name, text);
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