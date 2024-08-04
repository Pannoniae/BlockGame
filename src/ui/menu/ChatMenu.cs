using System.Drawing;
using System.Numerics;
using Silk.NET.Input;
using TrippyGL;

namespace BlockGame.ui;

public class ChatMenu : Menu {
    /// <summary>
    /// Current message to be typed
    /// </summary>
    public string message = "Test";

    public override bool isModal() {
        return false;
    }

    public override bool isBlockingInput() {
        return true;
    }

    public override void onKeyChar(IKeyboard keyboard, char ch) {
        if ((char.IsLetterOrDigit(ch) || char.IsPunctuation(ch) || char.IsWhiteSpace(ch)) &&
            !char.IsControl(ch)) {
            message += ch;
        }
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        switch (key) {
            case Key.Enter:
                // if T is pressed but there's a message, don't return
                // wait a frame so the key doesn't immediately get pressed again
                Game.instance.executeOnMainThread(closeChat);
                break;
            case Key.Backspace when message.Length > 0:
                message = message[..^1];
                break;
        }
    }

    public void closeChat() {
        message = "";
        Game.instance.lockMouse();
        screen.switchToMenu(((GameScreen)screen).INGAME_MENU);
    }

    public override void draw() {
        base.draw();
        var gui = Game.gui;
        var cursor = Game.permanentStopwatch.ElapsedMilliseconds % 1000 < 500 ? "|" : "";
        gui.drawUI(gui.colourTexture, RectangleF.FromLTRB(4, gui.uiHeight - 16, gui.uiWidth - 4, gui.uiHeight - 4), color: new Color4b(0, 0, 0, 128));
        gui.drawStringUIThin("> " + message + cursor, new Vector2(6, Game.gui.uiHeight - 13));
    }
}