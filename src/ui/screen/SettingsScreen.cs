namespace BlockGame.ui;

public class SettingsScreen : Screen {

    public Screen? prevScreen;
    private readonly SettingsMenu settingsMenu;

    public SettingsScreen() {
        settingsMenu = new SettingsMenu(this);
    }

    public override void activate() {
        switchToMenu(settingsMenu);
    }

    public void returnToPrevScreen() {
        if (prevScreen != null) {
            Game.instance.switchToScreen(prevScreen);
        } else {
            // fallback to main menu screen if no previous screen
            Game.instance.switchToScreen(Screen.MAIN_MENU_SCREEN);
        }
    }
}