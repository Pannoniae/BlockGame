using System.Numerics;
using BlockGame.main;
using BlockGame.ui.menu;
using Molten;
using Silk.NET.Input;

namespace BlockGame.ui.element;

public class TextBox : GUIElement {

    public string input = "";
    public int maxLength = 32;
    private bool focused => menu.focusedElement == this;

    public static readonly Vector2 padding = new(5, 4);

    public TextBox(Menu menu, string name) : base(menu, name) {
    }

    public override void click(MouseButton button) {
        if (button == MouseButton.Left) {
            menu.focusedElement = this;
        }
        base.click(button);
    }

    public override void onKeyChar(char c) {
        if (!focused) return;

        // only allow printable characters
        if (c >= 32 && c < 127) {
            if (input.Length < maxLength) {
                input += c;
            }
        }
    }

    public override void onKeyDown(Key key, int scancode) {
        if (!focused) return;

        if (key == Key.Backspace && input.Length > 0) {
            input = input[..^1];
        } else if (key == Key.Delete) {
            input = "";
        }
    }

    public override void draw() {
        // draw background (different color when focused)
        var bgColor = focused ? new Color(255, 255, 255, 60) : new Color(128, 128, 128, 128);
        Game.gui.draw(Game.gui.guiTexture, new Vector2(bounds.X, bounds.Y), source: Button.button);

        // draw border if focused
        if (focused) {
            Game.gui.drawBorderUI(GUIbounds.X, GUIbounds.Y, GUIbounds.Width, GUIbounds.Height, 1, new Color(100, 150, 255, 255));
        }

        // draw text
        Game.gui.drawString(input, new Vector2(bounds.X, bounds.Y) + padding * GUI.guiScale);

        // draw cursor if focused (blink every 500ms)
        if (focused && (Game.permanentStopwatch.ElapsedMilliseconds / 500) % 2 == 0) {
            var cursorX = bounds.X + padding.X * GUI.guiScale + Game.gui.measureString(input).X;
            var cursorY = bounds.Y + padding.Y * GUI.guiScale;
            Game.gui.draw(Game.gui.colourTexture, new RectangleF(cursorX, cursorY, 1 * GUI.guiScale, 8 * GUI.guiScale), default, new Color(255, 255, 255, 255));
        }
    }
}