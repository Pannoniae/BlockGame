using System.Drawing;
using System.Numerics;

namespace BlockGame;

public class Text : GUIElement {
    private readonly string text;

    public Text(Screen screen, Rectangle position, string text) : base(screen, position) {
        this.text = text;
    }


    public override void draw() {
        Game.gui.drawString(text, new Vector2(bounds.X, bounds.Y));
    }
}