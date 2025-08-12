using BlockGame.GL.vertexformats;
using BlockGame.util;
using Molten;
using Silk.NET.OpenGL;

namespace BlockGame.ui;

public class MainMenu : Menu {
    public MainMenu() {
        var title = new Image(this, "title", "textures/title.png");
        title.setPosition(new Vector2I(0, -70));
        title.centreContents();
        title.setScale(2);


        var sp = new Button(this, "singleplayer", true, "Singleplayer");
        sp.setPosition(new Vector2I(0, -34));
        sp.centreContents();
        sp.clicked += _ => {
            // we are *already* on the main thread; this is just needed so it executes a frame later
            // so we don't destroy the menu which we are clicking right now.
            Game.instance.executeOnMainThread(() => { Game.instance.switchTo(LEVEL_SELECT); });
        };
        Console.Out.WriteLine("sp:" + sp.bounds);
        var button2 = new Button(this, "multiplayer", true, "Multiplayer (soon)");
        button2.setPosition(new Vector2I(0, -8));
        button2.centreContents();
        var settings = new Button(this, "settings", true, "Settings");
        settings.setPosition(new Vector2I(0, 18));
        settings.centreContents();
        settings.clicked += _ => {
            Game.instance.executeOnMainThread(() => {
                SETTINGS.prevMenu = MAIN_MENU;
                Game.instance.switchTo(SETTINGS);
            });
        };
        var button4 = new Button(this, "quit", true, "Quit");
        button4.setPosition(new Vector2I(0, 44));
        button4.centreContents();
        button4.clicked += _ => Environment.Exit(0);
        addElement(title);
        addElement(sp);
        addElement(button2);
        addElement(settings);
        addElement(button4);
    }

    public override void clear(double dt, double interp) {
        Game.graphics.clearColor(Color4b.SlateGray);
        Game.GL.ClearDepth(1f);
        Game.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public override void draw() {
        Game.gui.drawScrollingBG(16);
        base.draw();
    }
}