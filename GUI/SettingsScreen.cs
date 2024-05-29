using System.Drawing;
using System.Numerics;
using Silk.NET.Input;
using TrippyGL;

namespace BlockGame;

public class SettingsScreen : Screen {

    public override void activate() {
        base.activate();
        // load settings (later)
        var settings = Settings.instance;
        var vsync = new ToggleButton(this, "vsync", new Vector2(0, 16), false, settings.vSync ? 1 : 0,
            "VSync: OFF", "VSync: ON");
        vsync.topCentre();
        vsync.clicked += () => {
            settings.vSync = vsync.getIndex() == 1;
            Game.window.VSync = settings.vSync;
        };
        vsync.tooltip = "Turns on vertical synchronisation.";
        addElement(vsync);

        var guiScale = new ToggleButton(this, "guiScale", new Vector2(0, 40), false, settings.guiScale == 4 ? 1 : 0,
            "GUI Scale: Small", "GUI Scale: Large");
        guiScale.topCentre();
        guiScale.clicked += () => {
            settings.guiScale = guiScale.getIndex() == 1 ? 4 : 2;
            GUI.guiScale = settings.guiScale;
        };
        addElement(guiScale);

        var AO = new ToggleButton(this, "ao", new Vector2(0, 64), false, settings.AO ? 1 : 0,
            "Ambient Occlusion: Disabled", "Ambient Occlusion: Enabled");
        AO.topCentre();
        AO.clicked += () => {
            settings.AO = AO.getIndex() == 1;
        };
        AO.tooltip = "Ambient Occlusion makes block corners darker to simulate shadows.";
        addElement(AO);

        var smoothLighting = new ToggleButton(this, "smoothLighting", new Vector2(0, 88), false, settings.smoothLighting ? 1 : 0,
            "Smooth Lighting: Disabled", "Smooth Lighting: Enabled");
        smoothLighting.topCentre();
        smoothLighting.clicked += () => {
            settings.smoothLighting = smoothLighting.getIndex() == 1;
        };
        smoothLighting.tooltip = "Smooth Lighting improves the game's look by smoothing the lighting between blocks.";
        addElement(smoothLighting);

        var back = new Button(this, "back", new Vector2(2, -18), false, "Back") {
            horizontalAnchor = HorizontalAnchor.LEFT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        back.clicked += returnToMainMenu;
        addElement(back);
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

    private void returnToMainMenu() {
        Game.instance.executeOnMainThread(() => switchTo(MAIN_MENU));
    }

    public override void draw() {
        Game.gui.drawBG(Blocks.DIRT, 16);
        base.draw();
    }
}