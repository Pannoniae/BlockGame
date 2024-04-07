using System.Drawing;
using Silk.NET.Maths;

namespace BlockGame;

public class MainMenuScreen : Screen {
    public override void activate() {
        base.activate();
        var button = new Button(this, new RectangleF(0, 0, 64, 16));
        button.centreContents();
        button.clicked += () => {
            // we are *already* on the main thread; this is just needed so it executes a frame later
            // so we don't destroy the screen which we are clicking right now.
            Game.instance.executeOnMainThread(() => {
                Console.Out.WriteLine("CLICKED");
                switchTo(GAME_SCREEN);
                Game.instance.resize(new Vector2D<int>(Game.instance.width, Game.instance.height));
                Game.instance.lockMouse();
            });
        };
        elements.Add(button);
    }
}