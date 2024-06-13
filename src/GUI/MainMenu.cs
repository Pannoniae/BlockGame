using BlockGame.util;
using Silk.NET.Maths;
using TrippyGL;

namespace BlockGame.GUI;

public class MainMenu : Menu {
    public override void activate() {
        base.activate();
        var sp = new Button(this, "singleplayer", new Vector2D<int>(0, -64), true, "Singleplayer");
        sp.centreContents();
        sp.clicked += () => {
            // we are *already* on the main thread; this is just needed so it executes a frame later
            // so we don't destroy the menu which we are clicking right now.
            Game.instance.executeOnMainThread(() => {
                Console.Out.WriteLine("CLICKED");
                Game.instance.switchToScreen(Screen.GAME_SCREEN);
                Game.instance.lockMouse();
            });
        };
        Console.Out.WriteLine("sp:" + sp.bounds);
        var button2 = new Button(this, "multiplayer", new Vector2D<int>(0, -32), true, "Multiplayer (soon)");
        button2.centreContents();
        var settings = new Button(this, "settings", new Vector2D<int>(0, 0), true, "Settings");
        settings.centreContents();
        settings.clicked += () => {
                Game.instance.executeOnMainThread(() => {
                    Game.instance.switchTo(SETTINGS);
                });
            }
            ;
        var button4 = new Button(this, "quit", new Vector2D<int>(0, 32), true, "Quit");
        button4.centreContents();
        button4.clicked += () => Environment.Exit(0);
        addElement(sp);
        addElement(button2);
        addElement(settings);
        addElement(button4);
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