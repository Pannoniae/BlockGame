using System.Drawing;
using Silk.NET.Maths;
using TrippyGL;

namespace BlockGame;

public class MainMenuScreen : Screen {
    public override void activate() {
        base.activate();
        var sp = new Button(this, new RectangleF(0, -64, 96, 16), "Singleplayer");
        sp.centreContents();
        sp.clicked += () => {
            // we are *already* on the main thread; this is just needed so it executes a frame later
            // so we don't destroy the screen which we are clicking right now.
            Game.instance.executeOnMainThread(() => {
                Console.Out.WriteLine("CLICKED");
                switchTo(GAME_SCREEN);
                Game.instance.resize(new Vector2D<int>(Game.width, Game.height));
                Game.instance.lockMouse();
            });
        };
        Console.Out.WriteLine("sp:" + sp.bounds);
        var button2 = new Button(this, new RectangleF(0, -32, 96, 16), "Multiplayer (soon)");
        button2.centreContents();
        var button3 = new Button(this, new RectangleF(0, 0, 96, 16), "Settings (soon)");
        button3.centreContents();
        var button4 = new Button(this, new RectangleF(0, 32, 96, 16), "Quit");
        button4.centreContents();
        button4.clicked += () => Environment.Exit(0);
        elements.Add(sp);
        elements.Add(button2);
        elements.Add(button3);
        elements.Add(button4);
    }

    public override void clear(GraphicsDevice GD, double dt, double interp) {
        GD.ClearColor = Color4b.SlateGray;
        GD.ClearDepth = 1f;
        GD.Clear(ClearBuffers.Color | ClearBuffers.Depth);
    }

    public override void render(double dt, double interp) {

    }
}