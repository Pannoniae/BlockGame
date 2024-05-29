using System.Numerics;
using TrippyGL;

namespace BlockGame;

public class MainMenuScreen : Screen {
    public override void activate() {
        base.activate();
        var sp = new Button(this, new Vector2(0, -64), true, "Singleplayer");
        sp.centreContents();
        sp.clicked += () => {
            // we are *already* on the main thread; this is just needed so it executes a frame later
            // so we don't destroy the screen which we are clicking right now.
            Game.instance.executeOnMainThread(() => {
                Console.Out.WriteLine("CLICKED");
                switchTo(GAME_SCREEN);
                Game.instance.lockMouse();
            });
        };
        Console.Out.WriteLine("sp:" + sp.bounds);
        var button2 = new Button(this, new Vector2(0, -32), true, "Multiplayer (soon)");
        button2.centreContents();
        var settings = new Button(this, new Vector2(0, 0), true, "Settings (soon)");
        settings.centreContents();
        settings.clicked += () => {
                Game.instance.executeOnMainThread(() => {
                    switchTo(SETTINGS_SCREEN);
                });
            }
            ;
        var button4 = new Button(this, new Vector2(0, 32), true, "Quit");
        button4.centreContents();
        button4.clicked += () => Environment.Exit(0);
        elements.Add(sp);
        elements.Add(button2);
        elements.Add(settings);
        elements.Add(button4);
    }

    public override void clear(GraphicsDevice GD, double dt, double interp) {
        GD.ClearColor = Color4b.SlateGray;
        GD.ClearDepth = 1f;
        GD.Clear(ClearBuffers.Color | ClearBuffers.Depth);
    }

    public override void draw() {
        Game.gui.drawBG(Blocks.DIRT, 16);
        base.draw();
    }
}