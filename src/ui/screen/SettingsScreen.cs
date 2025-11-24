using BlockGame.main;
using BlockGame.ui.menu.settings;

namespace BlockGame.ui.screen;

public class SettingsScreen : Screen {

    public Screen? prevScreen;
    public static VideoSettingsMenu VIDEO_SETTINGS_MENU = null!;
    public static AudioSettingsMenu AUDIO_SETTINGS_MENU = null!;
    public static SettingsMenu SETTINGS_MENU = null!;
    public static ControlsMenu CONTROLS_MENU = null!;

    public SettingsScreen() {
        VIDEO_SETTINGS_MENU = new VideoSettingsMenu(this);
        AUDIO_SETTINGS_MENU = new AudioSettingsMenu(this);
        SETTINGS_MENU = new SettingsMenu(this);
        CONTROLS_MENU = new ControlsMenu(this);
    }

    public override void activate() {
        switchToMenu(SETTINGS_MENU);
    }

    public void returnToPrevScreen() {
        // fallback to main menu screen if no previous screen
        Game.instance.switchToScreen(prevScreen ?? MAIN_MENU_SCREEN);
    }
}