using System.Drawing;
using System.Numerics;

namespace BlockGame;

public class Text : GUIElement {
    public string text { get; set; }

    public Text(Screen screen, Rectangle position, string text) : base(screen, position) {
        this.text = text;
    }


    public override void draw() {
        Game.gui.drawString(text, new Vector2(bounds.X, bounds.Y));
    }
}