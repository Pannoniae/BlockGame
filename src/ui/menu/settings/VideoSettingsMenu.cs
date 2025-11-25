using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.ui.screen;
using Molten;
using Silk.NET.Input;
using Silk.NET.OpenGL.Legacy;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu.settings;

public class VideoSettingsMenu : Menu {
    private readonly SettingsScreen parentScreen;

    public VideoSettingsMenu(SettingsScreen parentScreen) {
        this.parentScreen = parentScreen;
        initializeCategoryButtons();
    }

    private void initializeCategoryButtons() {
        var settings = Settings.instance;
        var elements = new List<GUIElement>();

        Func<float, string> getText = value => "Render Distance: " + value;
        var renderDistance = new Slider(this, "renderDistance", 2, 96, 1, settings.renderDistance, getText);
        renderDistance.setPosition(new Rectangle(0, 112, 128, 16));
        renderDistance.centreContents();
        renderDistance.tooltip =
            "The maximum distance at which blocks are rendered.\nHigher values may reduce performance.";
        renderDistance.applied += () => {
            var old = settings.renderDistance;
            settings.renderDistance = (int)renderDistance.value;
            remeshIfRequired(old);
        };
        elements.Add(renderDistance);
        addElement(renderDistance);

        var AO = new ToggleButton(this, "ao", false, settings.AO ? 1 : 0,
            "Ambient Occlusion: Disabled", "Ambient Occlusion: Enabled");
        AO.centreContents();
        AO.clicked += _ => {
            settings.AO = AO.getIndex() == 1;
            remeshIfRequired(settings.renderDistance);
        };
        AO.tooltip = "Ambient Occlusion makes block corners darker to simulate shadows.";
        elements.Add(AO);
        addElement(AO);

        var smoothLighting = new ToggleButton(this, "smoothLighting", false, settings.smoothLighting ? 1 : 0,
            "Smooth Lighting: Disabled", "Smooth Lighting: Enabled");
        smoothLighting.centreContents();
        smoothLighting.clicked += _ => {
            settings.smoothLighting = smoothLighting.getIndex() == 1;
            remeshIfRequired(settings.renderDistance);
        };
        smoothLighting.tooltip = "Smooth Lighting improves the game's look by smoothing the lighting between blocks.";
        elements.Add(smoothLighting);
        addElement(smoothLighting);

        var guiScale = new ToggleButton(this, "guiScale", false, settings.guiScale == 4 ? 1 : 0,
            "GUI Scale: Small", "GUI Scale: Large");
        guiScale.centreContents();
        guiScale.clicked += _ => {
            settings.guiScale = guiScale.getIndex() == 1 ? 4 : 2;
            GUI.guiScale = settings.guiScale;
            Game.instance.resize();
        };
        elements.Add(guiScale);
        addElement(guiScale);

        var display = new Button(this, "display", false, "Display...");
        display.centreContents();
        display.clicked += _ => {
            var menu = new DisplaySettingsMenu(parentScreen);
            SettingsScreen.SETTINGS_MENU.push(menu);
        };
        display.tooltip = "Window, resolution, and display settings";
        elements.Add(display);
        addElement(display);

        var graphics = new Button(this, "graphics", false, "Graphics...");
        graphics.centreContents();
        graphics.clicked += _ => {
            var menu = new GraphicsSettingsMenu(parentScreen);
            SettingsScreen.SETTINGS_MENU.push(menu);
        };
        graphics.tooltip = "Settings which make the game look nicer!";
        elements.Add(graphics);
        addElement(graphics);

        var performance = new Button(this, "performance", false, "Performance...");
        performance.centreContents();
        performance.clicked += _ => {
            var menu = new PerformanceSettingsMenu(parentScreen);
            SettingsScreen.SETTINGS_MENU.push(menu);
        };
        performance.tooltip = "Settings which make the game run faster!";
        elements.Add(performance);
        addElement(performance);

        /*var antiAliasing = new Button(this, "antiAliasing", false, "Anti-Aliasing...");
        antiAliasing.centreContents();
        antiAliasing.clicked += _ => {
            var menu = new AntiAliasingSettingsMenu(parentScreen);
            SettingsScreen.SETTINGS_MENU.push(menu);
        };
        antiAliasing.tooltip = "Anti-aliasing and edge smoothing";
        elements.Add(antiAliasing);
        addElement(antiAliasing);*/

        var camera = new Button(this, "camera", false, "Camera...");
        camera.centreContents();
        camera.clicked += _ => {
            var menu = new CameraSettingsMenu(parentScreen);
            SettingsScreen.SETTINGS_MENU.push(menu);
        };
        camera.tooltip = "FOV, mouse sensitivity, and camera options";
        elements.Add(camera);
        addElement(camera);

        var fx = new Button(this, "fx", false, "FX...");
        fx.centreContents();
        fx.clicked += _ => {
            var menu = new FXSettingsMenu(parentScreen);
            SettingsScreen.SETTINGS_MENU.push(menu);
        };
        fx.tooltip = "The special effects department presents: FX settings!";
        elements.Add(fx);
        addElement(fx);

        /*var advanced = new Button(this, "advanced", false, "Advanced Settings...");
        advanced.centreContents();
        advanced.clicked += _ => {
            var menu = new AdvancedSettingsMenu(parentScreen);
            SettingsScreen.SETTINGS_MENU.push(menu);
        };
        advanced.tooltip = "Advanced technical settings";
        elements.Add(advanced);
        addElement(advanced);*/

        // Windows Defender exclusion (Windows only)
        if (OperatingSystem.IsWindows()) {
            var defenderExclusion = new Button(this, "defenderExclusion", false, "Add Defender Exclusion");
            defenderExclusion.centreContents();
            defenderExclusion.clicked += _ => {
                Game.addDefenderExclusion();
                // refresh button text after adding
                defenderExclusion.text = "Defender Exclusion: Added";
            };
            defenderExclusion.tooltip =
                "Adds this folder to Windows Defender exclusions to improve file I/O performance.\nRequires administrator privileges (UAC prompt will appear).";
            elements.Add(defenderExclusion);
            addElement(defenderExclusion);
        }

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

        layoutSettingsTwoCols(elements, new Vector2I(0, 64), display.GUIbounds.Width);
    }

    private void remeshIfRequired(int oldRenderDist) {
        // only remesh if we're in the game, NOT on the main menu
        if (Game.world != null && Game.renderer != null) {
            Screen.GAME_SCREEN.remeshWorld(oldRenderDist);
        }
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
