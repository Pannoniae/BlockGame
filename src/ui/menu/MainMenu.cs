using System.Diagnostics;
using System.Numerics;
using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.util.log;
using Molten;
using Silk.NET.Input;
using Silk.NET.OpenGL.Legacy;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu;

public class MainMenu : Menu {
    public MainMenu() {
        var title = new Image(this, "title", "textures/title.png");
        title.setPosition(new Vector2I(0, -75));
        title.centreContents();
        title.setScale(1.5f);

        var subtitle = new Subtitle(this, "subtitle");
        subtitle.setPosition(new Vector2I(0, -55));
        subtitle.centreContents();

        var sp = new Button(this, "singleplayer", true, "Singleplayer");
        sp.setPosition(new Vector2I(0, -34));
        sp.centreContents();
        sp.clicked += _ => {
            // we are *already* on the main thread; this is just needed so it executes a frame later
            // so we don't destroy the menu which we are clicking right now.
            Game.instance.executeOnMainThread(() => { Game.instance.switchTo(LEVEL_SELECT); });
        };
        Log.debug("sp:" + sp.bounds);
        var button2 = new Button(this, "multiplayer", true, "Multiplayer (soon)");
        button2.setPosition(new Vector2I(0, -8));
        button2.centreContents();
        var settings = new Button(this, "settings", true, "Settings");
        settings.setPosition(new Vector2I(0, 18));
        settings.centreContents();
        settings.clicked += _ => {
            Game.instance.executeOnMainThread(() => {
                Screen.SETTINGS_SCREEN.prevScreen = Screen.MAIN_MENU_SCREEN;
                Game.instance.switchToScreen(Screen.SETTINGS_SCREEN);
            });
        };

        var discord = new Button(this, "discord", true, "Discord (click!)");
        discord.setPosition(new Vector2I(0, 44));
        discord.centreContents();
        discord.clicked += _ => {
            try {
                Process.Start(new ProcessStartInfo {
                    FileName = "https://discord.gg/tdbsvWpADe",
                    UseShellExecute = true
                });
            }
            catch (Exception e) {
                Log.error("failed to open discord link: ");
                Log.error(e);
            }
        };

        var button4 = new Button(this, "quit", true, "Quit");
        button4.setPosition(new Vector2I(0, 70));
        button4.centreContents();
        button4.clicked += _ => Environment.Exit(0);
        addElement(title);
        addElement(subtitle);
        addElement(sp);
        addElement(button2);
        addElement(settings);
        addElement(discord);
        addElement(button4);
    }

    public override void clear(double dt, double interp) {
        Game.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public override void update(double dt) {
        Game.gui.updateDrag(Game.mousePos, dt);
        base.update(dt);
    }

    public override void onMouseDown(IMouse mouse, MouseButton button) {
        // check if clicking on anything of value first
        bool c = false;
        foreach (var element in elements.Values) {
            if (element.active && element.bounds.Contains((int)Game.mousePos.X, (int)Game.mousePos.Y)) {
                c = true;
                break;
            }
        }

        if (!c && button == MouseButton.Left) {
            Game.gui.startDrag(Game.mousePos);
        }

        base.onMouseDown(mouse, button);
    }

    public override void onMouseUp(Vector2 pos, MouseButton button) {
        if (button == MouseButton.Left && Game.gui.draggingBG) {
            Game.gui.endDrag();
        }
        base.onMouseUp(pos, button);
    }

    public override void draw() {
        Game.gui.drawScrollingBG(16);
        base.draw();
    }
}