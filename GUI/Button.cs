using System.Drawing;
using System.Numerics;

namespace BlockGame;

public class Button : GUIElement {
    public string? text { get; set; }

    // todo refactor these to automatically calculate coords
    public Rectangle button = new(96, 0, 96, 16);
    public Rectangle hoveredButton = new(0, 16, 96, 16);
    public Rectangle pressedButton = new(0, 16 * 2, 96, 16);

    public bool shadowed = false;

    public Button(Screen screen, RectangleF guiPosition, string? text = default) : base(screen, guiPosition) {
        this.text = text;
    }

    public override void draw() {
        var tex = hovered ? hoveredButton : button;
        tex = pressed ? pressedButton : tex;
        Game.gui.draw(Game.gui.guiTexture, new Vector2(bounds.X, bounds.Y), tex);
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