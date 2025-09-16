using BlockGame.ui.menu.settings;

namespace BlockGame.ui.screen;

public class SettingsScreen : Screen {

    public Screen? prevScreen;
    public static VideoSettingsMenu VIDEO_SETTINGS_MENU;
    public static SettingsMenu SETTINGS_MENU;
    public static ControlsMenu CONTROLS_MENU;

    public SettingsScreen() {
        VIDEO_SETTINGS_MENU = new VideoSettingsMenu(this);
        SETTINGS_MENU = new SettingsMenu(this);
        CONTROLS_MENU = new ControlsMenu(this);
    }

    public override void activate() {
        switchToMenu(SETTINGS_MENU);
    }

    public void returnToPrevScreen() {
        if (prevScreen != null) {
            main.Game.instance.switchToScreen(prevScreen);
        }
        else {
            // fallback to main menu screen if no previous screen
            main.Game.instance.switchToScreen(MAIN_MENU_SCREEN);
        }
    }
}