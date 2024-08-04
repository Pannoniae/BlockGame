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


    public override void onKeyChar(IKeyboard keyboard, char ch) {
        if (ch != 't') {
            message += ch;
        }
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        if (key == Key.Enter) {
            message = "";
            Game.instance.lockMouse();
            Screen.GAME_SCREEN.switchToMenu(Screen.GAME_SCREEN.INGAME_MENU);
        } else {
            if (key == Key.Backspace && message.Length > 0) {
                message = message[..^1];
            }
        }
    }


    public override void draw() {
        base.draw();
        var gui = Game.gui;
        gui.drawUI(gui.colourTexture, RectangleF.FromLTRB(4, gui.uiHeight - 16, gui.uiWidth - 4, gui.uiHeight - 4), color: new Color4b(0, 0, 0, 128));
        gui.drawStringUIThin("> " + message, new Vector2(6, Game.gui.uiHeight - 13));
    }

    public override bool isModal() {
        return false;
    }
}