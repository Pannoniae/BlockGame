using System.Numerics;
using BlockGame.main;
using BlockGame.ui.menu;
using Molten;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.ui.element;

public class Button : GUIElement {

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

    public Vector2I position {
        get => new(GUIbounds.X, GUIbounds.Y);
        set => setPosition(new Rectangle(value.X, value.Y, wide ? 192 : 128, 16));
    }

    public Button(Menu menu, string name, bool wide, string? text = default) : base(menu, name) {
        this.text = text;
        this.wide = wide;
        guiPosition.Width = wide ? 192 : 128;
        guiPosition.Height = 16;
    }

    public void setPosition(Vector2I pos) {
        setPosition(new Rectangle(pos.X, pos.Y, guiPosition.Width, guiPosition.Height));
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
        Game.gui.draw(Game.gui.guiTexture, new Vector2(bounds.X, bounds.Y), source: tex);
        var centre = new Vector2(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
        
        // shift centre down by 1 gui px
        //centre.Y += (int)(GUI.u2s(1) / 2f);
        
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