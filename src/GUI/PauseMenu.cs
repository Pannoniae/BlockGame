using Silk.NET.Maths;

namespace BlockGame.GUI;

public class PauseMenu : Menu {
    public override void activate() {
        base.activate();

        var backToGame = new Button(this, "backToGame", new Vector2D<int>(0, 0), true, "Back to the game");
        backToGame.centreContents();
        backToGame.clicked += () => { Screen.GAME_SCREEN.backToGame(); };
        var mainMenu = new Button(this, "mainMenu", new Vector2D<int>(0, 32), true, "Quit to Main Menu");
        mainMenu.centreContents();
        mainMenu.clicked += returnToMainMenu;
        addElement(backToGame);
        addElement(mainMenu);
    }

    public static void returnToMainMenu() {
        Game.instance.executeOnMainThread(() => Game.instance.switchToScreen(Screen.MAIN_MENU_SCREEN));
    }
}