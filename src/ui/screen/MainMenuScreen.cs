using BlockGame.ui.menu;

namespace BlockGame.ui.screen;

public class MainMenuScreen : Screen {

    public override void activate() {
        currentMenu = Menu.MAIN_MENU;
    }
}