using System.Numerics;
using BlockGame.main;
using BlockGame.ui.menu;
using Molten;
using Silk.NET.Input;

namespace BlockGame.ui.element;

public class TextBox : GUIElement {

    public string input = "";
    public int maxLength = 32;
    public bool centred;
    private bool focused => menu.focusedElement == this;

    public static readonly Vector2 padding = new(5, 4);

    public TextBox(Menu menu, string name) : base(menu, name) {
        guiPosition.Height = 16;
        guiPosition.Width = 128;
    }

    public override void click(MouseButton button) {
        if (button == MouseButton.Left) {
            menu.focusedElement = this;
        }
        base.click(button);
    }

    public override void onKeyChar(char c) {
        if (!focused) {
            return;
        }

        if (!char.IsControl(c)) {
            if (input.Length < maxLength) {
                input += c;
            }
        }

        // allow all printable unicode
    }

    public override void onKeyDown(Key key, int scancode) {
        if (!focused) return;

        if (key == Key.Backspace && input.Length > 0) {
            input = input[..^1];
        } else if (key == Key.Delete) {
            input = "";
        }
    }

    public override void onKeyRepeat(Key key, int scancode) {
        if (!focused) {
            return;
        }

        if (key == Key.Backspace && input.Length > 0) {
            input = input[..^1];
        }
    }

    public override void draw() {
        // draw background (different colour when focused)
        //var bgColour = focused ? new Color(255, 255, 255, 60) : new Color(128, 128, 128, 128);
        Game.gui.draw(Game.gui.guiTexture, new Vector2(bounds.X, bounds.Y), source: Button.button);

        // draw border if focused
        if (focused) {
            Game.gui.drawBorderUI(GUIbounds.X, GUIbounds.Y, GUIbounds.Width, GUIbounds.Height, 1, new Color(100, 150, 255, 255));
        }

        // draw text
        if (centred) {
            var textSize = Game.gui.measureString(input);
            var textX = bounds.X + (bounds.Width - textSize.X) / 2f;
            Game.gui.drawString(input, new Vector2(textX, bounds.Y + padding.Y * GUI.guiScale));
        }
        else {
            Game.gui.drawString(input, new Vector2(bounds.X, bounds.Y) + padding * GUI.guiScale);
        }
        // draw cursor if focused (blink every 500ms)
        if (focused && (Game.permanentStopwatch.ElapsedMilliseconds / 500) % 2 == 0) {

            if (centred) {
                var textSize = Game.gui.measureString(input);
                var textX = bounds.X + (bounds.Width - textSize.X) / 2f;
                var cursorX = textX + textSize.X;
                var cursorY = bounds.Y + padding.Y * GUI.guiScale;
                Game.gui.draw(Game.gui.colourTexture, new RectangleF(cursorX, cursorY, 1 * GUI.guiScale, 8 * GUI.guiScale), default, new Color(255, 255, 255, 255));
            }
            else {
                var cursorX = bounds.X + padding.X * GUI.guiScale + Game.gui.measureString(input).X;
                var cursorY = bounds.Y + padding.Y * GUI.guiScale;
                Game.gui.draw(Game.gui.colourTexture, new RectangleF(cursorX, cursorY, 1 * GUI.guiScale, 8 * GUI.guiScale), default,
                    new Color(255, 255, 255, 255));
            }
        }
    }
}