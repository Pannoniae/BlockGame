using System.Numerics;
using Silk.NET.Maths;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame;

public class Button : GUIElement {
    public string? text { get; set; }

    public bool wide;

    // todo refactor these to automatically calculate coords
    public static Rectangle button = new(0, WIDE_OFFSET + 0, 128, 16);
    public static Rectangle hoveredButton = new(0, WIDE_OFFSET + 16, 128, 16);
    public static Rectangle pressedButton = new(0, WIDE_OFFSET + 16 * 2, 128, 16);

    public const int WIDE_OFFSET = 80;

    public static Rectangle buttonWide = new(0, 0, 192, 16);
    public static Rectangle hoveredButtonWide = new(0,  16, 192, 16);
    public static Rectangle pressedButtonWide = new(0, 16 * 2, 192, 16);

    public bool shadowed = false;

    public Button(Menu menu, string name, Vector2D<int> pos, bool wide, string? text = default) : base(menu, name) {
        this.text = text;
        setPosition(new Rectangle(pos.X, pos.Y, wide ? 192 : 128, 16));
        this.wide = wide;
    }

    public override void draw() {
        Rectangle tex;
        if (wide) {
            tex = hovered ? hoveredButtonWide : buttonWide;
            tex = pressed ? pressedButtonWide : tex;
        }
        else {
            tex = hovered ? hoveredButton : button;
            tex = pressed ? pressedButton : tex;
        }
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