using Silk.NET.Maths;

namespace BlockGame.ui;

public class PauseMenu : Menu {
    public override void activate() {
        base.activate();

        var backToGame = new Button(this, "backToGame", false, "Back to the game");
        backToGame.setPosition(new Vector2D<int>(0, 0));
        backToGame.centreContents();
        backToGame.clicked += () => { Screen.GAME_SCREEN.backToGame(); };
        var mainMenu = new Button(this, "mainMenu", false, "Quit to Main Menu");
        mainMenu.setPosition(new Vector2D<int>(0, 32));
        mainMenu.centreContents();
        mainMenu.clicked += returnToMainMenu;
        addElement(backToGame);
        addElement(mainMenu);
    }

    public static void returnToMainMenu() {
        Game.instance.executeOnMainThread(() => Game.instance.switchToScreen(Screen.MAIN_MENU_SCREEN));
    }
}