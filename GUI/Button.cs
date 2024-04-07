using System.Drawing;
using System.Numerics;

namespace BlockGame;

public class Button : GUIElement {
    public string? text { get; set; }

    public bool shadowed = false;

    public Button(Screen screen, RectangleF guiPosition, string? text = default) : base(screen, guiPosition) {
        this.text = text;
    }

    public override void draw() {
        Game.gui.draw(Game.gui.guiTexture, new Vector2(bounds.X, bounds.Y), Game.gui.grayButtonRect);
        var centre = new Vector2(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        if (text != null) {
            if (shadowed) {
                Game.gui.drawStringCentredShadowed(text, centre);
            }
            else {
                Game.gui.drawStringCentred(text, centre);
            }
        }
    }
}