using System.Numerics;
using BlockGame.main;
using BlockGame.ui.element;
using Molten;

namespace BlockGame.ui.menu;

public class DeathMenu : Menu {
    public override bool pausesWorld() => false;

    public DeathMenu() {
        var respawn = new Button(this, "respawn", false, "Respawn");
        respawn.setPosition(new Vector2I(0, 0));
        respawn.centreContents();
        respawn.clicked += _ => { Game.player.respawn(); };

        var mainMenu = new Button(this, "mainMenu", false, "Quit to Main Menu");
        mainMenu.setPosition(new Vector2I(0, 24));
        mainMenu.centreContents();
        mainMenu.clicked += PauseMenu.returnToMainMenu;

        addElement(respawn);
        addElement(mainMenu);
    }

    public override void update(double dt) {
        base.update(dt);
        // update ingame menu (world keeps running while dead)
        Screen.GAME_SCREEN.INGAME_MENU.update(dt);
    }

    public override void draw() {
        var gui = Game.gui;

        Screen.GAME_SCREEN.INGAME_MENU.draw();

        // draw background (just a fullscreen grey overlay)
        gui.draw(gui.colourTexture, new RectangleF(0, 0, Game.width, Game.height), null, new Color(0, 0, 0, 150));

        base.draw();

        // draw "YOU DIED!" text

        const string deathText = "YOU DED! skill issue?";
        gui.drawStringCentred(deathText, new Vector2(Game.centreX, Game.centreY - 64 * GUI.guiScale),
            Color.Red);
    }

    public override void postDraw() {
        base.postDraw();
        Screen.GAME_SCREEN.INGAME_MENU.postDraw();
    }
}