using System.Numerics;

namespace BlockGame.ui;

public class ChatMenu : Menu {
    /// <summary>
    /// Current message to be typed
    /// </summary>
    public string message = ">Test";


    public override void draw() {
        base.draw();
        Game.gui.drawStringUIThin(message, new Vector2(4, Game.gui.uiHeight - 24));
    }

    public override bool isModal() {
        return false;
    }
}