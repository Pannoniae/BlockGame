using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.ui.screen;
using BlockGame.util;
using Molten;
using Silk.NET.Input;
using Silk.NET.OpenGL.Legacy;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu.settings;

public class AudioSettingsMenu : Menu {
    private readonly SettingsScreen parentScreen;

    public AudioSettingsMenu(SettingsScreen parentScreen) {
        this.parentScreen = parentScreen;
        initializeSettings();
    }

    private void initializeSettings() {
        var settings = Settings.instance;
        var settingElements = new List<GUIElement>();

        static string sfxText(float value) => $"SFX Volume: {value}%";
        var sfxVolume = new Slider(this, "sfxVolume", 0, 100, 1, (int)(settings.sfxVolume * 100), sfxText);
        sfxVolume.setPosition(new Rectangle(0, 0, 128, 16));
        sfxVolume.topCentre();
        sfxVolume.applied += () => {
            settings.sfxVolume = sfxVolume.value / 100f;
            Game.snd?.updateSfxVolumes();
        };
        sfxVolume.tooltip = "Controls the volume of sound effects.";
        settingElements.Add(sfxVolume);
        addElement(sfxVolume);

        static string musicText(float value) => $"Music Volume: {value}%";
        var musicVolume = new Slider(this, "musicVolume", 0, 100, 1, (int)(settings.musicVolume * 100), musicText);
        musicVolume.setPosition(new Rectangle(0, 0, 128, 16));
        musicVolume.topCentre();
        musicVolume.applied += () => {
            settings.musicVolume = musicVolume.value / 100f;
            Game.snd?.updateMusicVolumes();
        };
        musicVolume.tooltip = "Controls the volume of music.";
        settingElements.Add(musicVolume);
        addElement(musicVolume);

        var back = new Button(this, "back", false, "Back") {
            horizontalAnchor = HorizontalAnchor.LEFT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        back.setPosition(new Vector2I(2, -18));
        back.clicked += _ => {
            deactivate();
            parentScreen.switchToMenu(SettingsScreen.SETTINGS_MENU);
        };
        addElement(back);

        layoutSettingsTwoCols(settingElements, new Vector2I(0, 16), sfxVolume.GUIbounds.Width);
    }

    public override void deactivate() {
        base.deactivate();
        Settings.instance.save();
    }

    public override void clear(double dt, double interp) {
        Game.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        if (key == Key.Escape) {
            parentScreen.switchToMenu(SettingsScreen.SETTINGS_MENU);
        }
    }

    public override void draw() {
        Game.gui.drawBG(16);
        base.draw();
    }
}