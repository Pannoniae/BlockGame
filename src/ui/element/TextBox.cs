using System.Linq;
using System.Numerics;
using BlockGame.main;
using BlockGame.ui.menu;
using Molten;
using Silk.NET.Input;

namespace BlockGame.ui.element;

public class TextBox : GUIElement {

    private string input = "";
    public int cursorPos = 0;
    public int maxLength = 32;
    public bool centred;
    public string? header;
    public bool isPassword;
    private bool focused => menu.focusedElement == this;

    public static readonly Vector2 padding = new(5, 4);

    public TextBox(Menu menu, string name) : base(menu, name) {
        guiPosition.Height = 16;
        guiPosition.Width = 128;
    }

    public string getInput() {
        return input;
    }

    public void setInput(string newInput) {
        input = newInput.Length > maxLength ? newInput[..maxLength] : newInput;
        cursorPos = input.Length;
    }

    public override void click(MouseButton button) {
        if (button == MouseButton.Left) {
            menu.focusedElement = this;
            cursorPos = input.Length; // move cursor to end when clicking
        }
        base.click(button);
    }

    public override void onKeyChar(char c) {
        if (!focused) {
            return;
        }

        if (!char.IsControl(c)) {
            if (input.Length < maxLength) {
                input = input.Insert(cursorPos, c.ToString());
                cursorPos++;
            }
        }

        // allow all printable unicode
    }

    public override void onKeyDown(Key key, int scancode) {
        if (!focused) return;

        switch (key) {
            case Key.Backspace when cursorPos > 0:
                input = input.Remove(cursorPos - 1, 1);
                cursorPos--;
                break;

            case Key.Delete when cursorPos < input.Length:
                input = input.Remove(cursorPos, 1);
                break;

            case Key.Left when cursorPos > 0:
                cursorPos--;
                break;

            case Key.Right when cursorPos < input.Length:
                cursorPos++;
                break;

            case Key.Home:
                cursorPos = 0;
                break;

            case Key.End:
                cursorPos = input.Length;
                break;

            case Key.C when Game.keyboard.IsKeyPressed(Key.ControlLeft) || Game.keyboard.IsKeyPressed(Key.ControlRight):
                if (!string.IsNullOrEmpty(input)) {
                    Game.keyboard.ClipboardText = input;
                }
                break;

            case Key.V when Game.keyboard.IsKeyPressed(Key.ControlLeft) || Game.keyboard.IsKeyPressed(Key.ControlRight):
                var clipboardText = Game.keyboard.ClipboardText;
                if (!string.IsNullOrEmpty(clipboardText)) {
                    // filter to printable characters only
                    var filtered = new string(clipboardText.Where(c => !char.IsControl(c)).ToArray());
                    if (!string.IsNullOrEmpty(filtered)) {
                        // trim to maxLength
                        var remaining = maxLength - input.Length;
                        if (remaining > 0) {
                            var toInsert = filtered.Length > remaining ? filtered[..remaining] : filtered;
                            input = input.Insert(cursorPos, toInsert);
                            cursorPos += toInsert.Length;
                        }
                    }
                }
                break;
        }
    }

    public override void onKeyRepeat(Key key, int scancode) {
        if (!focused) {
            return;
        }

        switch (key) {
            case Key.Backspace when cursorPos > 0:
                input = input.Remove(cursorPos - 1, 1);
                cursorPos--;
                break;

            case Key.Delete when cursorPos < input.Length:
                input = input.Remove(cursorPos, 1);
                break;

            case Key.Left when cursorPos > 0:
                cursorPos--;
                break;

            case Key.Right when cursorPos < input.Length:
                cursorPos++;
                break;
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

        // prepare text
        var displayInput = isPassword ? new string('*', input.Length) : input;
        var headerText = header ?? "";

        // draw text
        if (centred) {
            var textSize = Game.gui.measureString(header + input);
            var textX = bounds.X + (bounds.Width - textSize.X) / 2f;
            Game.gui.drawString(headerText + displayInput, new Vector2(textX, bounds.Y + padding.Y * GUI.guiScale));
        }
        else {
            Game.gui.drawString(headerText + displayInput, new Vector2(bounds.X, bounds.Y) + padding * GUI.guiScale);
        }
        // draw cursor if focused (blink every 500ms)
        if (focused && (Game.permanentStopwatch.ElapsedMilliseconds / 500) % 2 == 0) {
            var beforeCursor = displayInput[..Math.Min(cursorPos, displayInput.Length)];

            if (centred) {
                var textSize = Game.gui.measureString(headerText + displayInput);
                var beforeSize = Game.gui.measureString(headerText + beforeCursor);
                var textX = bounds.X + (bounds.Width - textSize.X) / 2f;
                var cursorX = textX + beforeSize.X;
                var cursorY = bounds.Y + padding.Y * GUI.guiScale;
                Game.gui.draw(Game.gui.colourTexture, new RectangleF(cursorX, cursorY, 1 * GUI.guiScale, 8 * GUI.guiScale), null, new Color(255, 255, 255, 255));
            }
            else {
                var cursorX = bounds.X + padding.X * GUI.guiScale + Game.gui.measureString(headerText + beforeCursor).X;
                var cursorY = bounds.Y + padding.Y * GUI.guiScale;
                Game.gui.draw(Game.gui.colourTexture, new RectangleF(cursorX, cursorY, 1 * GUI.guiScale, 8 * GUI.guiScale), null,
                    new Color(255, 255, 255, 255));
            }
        }
    }
}