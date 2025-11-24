using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.ui.screen;
using Molten;
using Silk.NET.Input;
using Silk.NET.OpenGL.Legacy;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu.settings;

public class DisplaySettingsMenu : Menu {
    private readonly SettingsScreen parentScreen;

    public DisplaySettingsMenu(SettingsScreen parentScreen) {
        this.parentScreen = parentScreen;
        initializeSettings();
    }

    private void initializeSettings() {
        var settings = Settings.instance;
        var settingElements = new List<GUIElement>();

        var fullscreen = new ToggleButton(this, "fullscreen", false, (int)settings.fullscreen,
            "Fullscreen: Windowed", "Fullscreen: Fullscreen", "Fullscreen: Borderless");
        fullscreen.centreContents();
        fullscreen.clicked += _ => {
            settings.fullscreen = (FullscreenState)fullscreen.getIndex();
            Game.instance.setFullscreen(settings.fullscreen);
        };
        fullscreen.tooltip = "Toggles fullscreen mode.";
        settingElements.Add(fullscreen);
        addElement(fullscreen);

        var vsync = new ToggleButton(this, "vsync", false, settings.vSync ? 1 : 0,
            "VSync: OFF", "VSync: ON");
        vsync.centreContents();
        vsync.clicked += _ => {
            settings.vSync = vsync.getIndex() == 1;
            Game.window.VSync = settings.vSync;
        };
        vsync.tooltip = "VSync locks your framerate to your monitor's refresh rate to prevent screen tearing.";
        settingElements.Add(vsync);
        addElement(vsync);

        var resScale = new ToggleButton(this, "resScale", false,
            settings.resolutionScale switch {
                0.25f => 0, 0.5f => 1, 0.75f => 2, _ => 3
            },
            "Resolution: 25%", "Resolution: 50%", "Resolution: 75%", "Resolution: 100%");
        resScale.centreContents();
        resScale.clicked += _ => {
            settings.resolutionScale = resScale.getIndex() switch {
                0 => 0.25f, 1 => 0.5f, 2 => 0.75f, _ => 1.0f
            };
            Game.instance.updateFramebuffers();
        };
        resScale.tooltip = "Renders the game at a lower internal resolution then upscales to window size.\nReduces GPU load for better performance on weaker PCs.";
        settingElements.Add(resScale);
        addElement(resScale);

        var resScaleFilter = new ToggleButton(this, "resScaleFilter", false, settings.resolutionScaleLinear ? 1 : 0,
            "Upscale Filter: Nearest", "Upscale Filter: Linear");
        resScaleFilter.centreContents();
        resScaleFilter.clicked += _ => {
            settings.resolutionScaleLinear = resScaleFilter.getIndex() == 1;
            Game.instance.updateFramebuffers();
        };
        resScaleFilter.tooltip = "Texture filtering for resolution scaling.\nNearest: pixelated/sharp upscaling\nLinear: smooth upscaling";
        settingElements.Add(resScaleFilter);
        addElement(resScaleFilter);

        var back = new Button(this, "back", false, "Back") {
            horizontalAnchor = HorizontalAnchor.LEFT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        back.setPosition(new Vector2I(2, -18));
        back.clicked += _ => {
            deactivate();
            SettingsScreen.SETTINGS_MENU.pop();
        };
        addElement(back);

        layoutSettingsTwoCols(settingElements, new Vector2I(0, 16), fullscreen.GUIbounds.Width);
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
            SettingsScreen.SETTINGS_MENU.pop();
        }
    }

    public override void draw() {
        Game.gui.drawBG(16);
        base.draw();
    }
}
