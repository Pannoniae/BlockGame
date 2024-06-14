using BlockGame.util;
using Silk.NET.Input;
using Silk.NET.Maths;
using TrippyGL;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.GUI;

public class SettingsMenu : Menu {

    public override void activate() {
        base.activate();
        // load settings (later)
        var settings = Settings.instance;

        var settingElements = new List<GUIElement>();

        var vsync = new ToggleButton(this, "vsync",false, settings.vSync ? 1 : 0,
            "VSync: OFF", "VSync: ON");
        vsync.topCentre();
        vsync.clicked += () => {
            settings.vSync = vsync.getIndex() == 1;
            Game.window.VSync = settings.vSync;
        };
        vsync.tooltip = "Turns on vertical synchronisation.";
        settingElements.Add(vsync);
        addElement(vsync);

        var guiScale = new ToggleButton(this, "guiScale",false, settings.guiScale == 4 ? 1 : 0,
            "GUI Scale: Small", "GUI Scale: Large");
        guiScale.topCentre();
        guiScale.clicked += () => {
            settings.guiScale = guiScale.getIndex() == 1 ? 4 : 2;
            GUI.guiScale = settings.guiScale;
        };
        settingElements.Add(guiScale);
        addElement(guiScale);

        var AO = new ToggleButton(this, "ao",false, settings.AO ? 1 : 0,
            "Ambient Occlusion: Disabled", "Ambient Occlusion: Enabled");
        AO.topCentre();
        AO.clicked += () => {
            settings.AO = AO.getIndex() == 1;
        };
        AO.tooltip = "Ambient Occlusion makes block corners darker to simulate shadows.";
        settingElements.Add(AO);
        addElement(AO);

        var smoothLighting = new ToggleButton(this, "smoothLighting", false, settings.smoothLighting ? 1 : 0,
            "Smooth Lighting: Disabled", "Smooth Lighting: Enabled");
        smoothLighting.topCentre();
        smoothLighting.clicked += () => {
            settings.smoothLighting = smoothLighting.getIndex() == 1;
        };
        smoothLighting.tooltip = "Smooth Lighting improves the game's look by smoothing the lighting between blocks.";
        settingElements.Add(smoothLighting);
        addElement(smoothLighting);

        var renderDistance = new Slider(this, "renderDistance", 2, 32, 1, settings.renderDistance);
        renderDistance.setPosition(new Rectangle(0, 112, 128, 16));
        renderDistance.topCentre();
        renderDistance.tooltip = "The maximum distance at which blocks are rendered.\nHigher values may reduce performance.";
        renderDistance.applied += () => {
            settings.renderDistance = (int)renderDistance.value;
        };
        renderDistance.getText = value => "Render Distance: " + value;
        settingElements.Add(renderDistance);
        addElement(renderDistance);

        var FOV = new FOVSlider(this, "FOV", 60, 120, 1, (int)settings.FOV);
        FOV.setPosition(new Rectangle(0, 112, 128, 16));
        FOV.topCentre();
        FOV.applied += () => {
            settings.FOV = (int)FOV.value;
        };
        FOV.getText = value => {
            if (value == 75)
                return "Field of View: Normal";
            if (value == 110)
                return "Field of View: Quake Pro";
            if (value == 60)
                return "Field of View: Fish Eye";
            if (value == 120)
                return "Field of View: Tunnel Vision";
            return "Field of View: " + value;
        };
        settingElements.Add(FOV);
        addElement(FOV);

        var back = new Button(this, "back",false, "Back") {
            horizontalAnchor = HorizontalAnchor.LEFT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        back.setPosition(new Vector2D<int>(2, -18));
        back.clicked += returnToMainMenu;
        addElement(back);

        layoutSettings(settingElements, new Vector2D<int>(0, 16));
    }

    public void layoutSettings(List<GUIElement> elements, Vector2D<int> startPos) {
        var pos = startPos;
        foreach (var element in elements) {
            element.setPosition(new Rectangle(pos.X, pos.Y, element.GUIbounds.Width, element.GUIbounds.Height));
            pos.Y += 18;
        }
    }

    public override void deactivate() {
        base.deactivate();
        // save settings too
    }

    public override void clear(GraphicsDevice GD, double dt, double interp) {
        GD.ClearColor = Color4b.SlateGray;
        GD.ClearDepth = 1f;
        GD.Clear(ClearBuffers.Color | ClearBuffers.Depth);
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        if (key == Key.Escape) {
            returnToMainMenu();
        }
    }

    public static void returnToMainMenu() {
        Game.instance.executeOnMainThread(() => Game.instance.switchTo(MAIN_MENU));
    }

    public override void draw() {
        Game.gui.drawBG(Blocks.DIRT, 16);
        base.draw();
    }
}