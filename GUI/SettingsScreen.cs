using System.Drawing;
using Silk.NET.Input;
using TrippyGL;

namespace BlockGame;

public class SettingsScreen : Screen {

    public override void activate() {
        base.activate();
        // load settings (later)
        var settings = Settings.instance;
        var vsync = new ToggleButton(this, new RectangleF(0, 16, 96, 16),
            "VSync: OFF", "VSync: ON");
        vsync.topCentre();
        vsync.clicked += () => {
            settings.vSync = vsync.getIndex() == 1;
            Game.window.VSync = settings.vSync;
        };
        elements.Add(vsync);

        var guiScale = new ToggleButton(this, new RectangleF(0, 40, 96, 16),
            "GUI Scale: Large", "GUI Scale: Small");
        guiScale.topCentre();
        guiScale.clicked += () => {
            settings.guiScale = guiScale.getIndex() == 1 ? 2 : 4;
            GUI.guiScale = settings.guiScale;
        };
        elements.Add(guiScale);

        var AO = new ToggleButton(this, new RectangleF(0, 64, 96, 16),
            "Ambient Occlusion: Disabled", "Ambient Occlusion: Enabled");
        AO.topCentre();
        AO.clicked += () => {
            settings.AO = AO.getIndex() == 1;
        };
        elements.Add(AO);

        var back = new Button(this, new RectangleF(0, -16, 96, 16), "Back") {
            horizontalAnchor = HorizontalAnchor.LEFT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        back.clicked += returnToMainMenu;
        elements.Add(back);
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