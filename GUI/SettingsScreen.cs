using System.Drawing;
using TrippyGL;

namespace BlockGame;

public class SettingsScreen : Screen {

    public static Settings settings;

    public override void activate() {
        base.activate();
        // load settings (later)
        settings = new Settings();
        var vsync = new ToggleButton(this, new RectangleF(0, 16, 96, 16),
            "VSync: OFF", "VSync: ON");
        vsync.topCentre();
        vsync.clicked += () => {
            settings.vSync = vsync.getIndex() == 1;
            Game.window.VSync = settings.vSync;
        };
        elements.Add(vsync);
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
}