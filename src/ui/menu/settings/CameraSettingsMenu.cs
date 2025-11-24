using System.Diagnostics.CodeAnalysis;
using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.ui.screen;
using Molten;
using Silk.NET.Input;
using Silk.NET.OpenGL.Legacy;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu.settings;

public class CameraSettingsMenu : Menu {
    private readonly SettingsScreen parentScreen;

    public CameraSettingsMenu(SettingsScreen parentScreen) {
        this.parentScreen = parentScreen;
        initializeSettings();
    }

    private void initializeSettings() {
        var settings = Settings.instance;
        var settingElements = new List<GUIElement>();

        Func<float, string> getText = [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")](value) => {
            return value switch {
                75 => "Field of View: Normal",
                50 => "Field of View: Fish Eye",
                120 => "Field of View: Quake Pro",
                150 => "Field of View: Tunnel Vision",
                _ => "Field of View: " + value
            };
        };
        var FOV = new FOVSlider(this, "FOV", 50, 150, 1, (int)settings.FOV, getText);
        FOV.setPosition(new Rectangle(0, 112, 128, 16));
        FOV.centreContents();
        FOV.applied += () => { settings.FOV = (int)FOV.value; };
        settingElements.Add(FOV);
        addElement(FOV);

        getText = value => "Mouse Sensitivity: " + (value / 10f).ToString("0.0");
        var mouseSens = new Slider(this, "mouseSens", 1, 30, 1, (int)(settings.mouseSensitivity * 10), getText);
        mouseSens.setPosition(new Rectangle(0, 112, 128, 16));
        mouseSens.centreContents();
        mouseSens.applied += () => { settings.mouseSensitivity = mouseSens.value / 10f; };
        mouseSens.tooltip = "Controls how fast the camera rotates when moving the mouse.";
        settingElements.Add(mouseSens);
        addElement(mouseSens);

        var mouseInv = new ToggleButton(this, "mouseInv", false, settings.mouseInv == 1 ? 0 : 1,
            "Mouse Y-Axis: Normal", "Mouse Y-Axis: Inverted");
        mouseInv.centreContents();
        mouseInv.clicked += _ => { settings.mouseInv = mouseInv.getIndex() == 1 ? -1 : 1; };
        mouseInv.tooltip = "Inverts the vertical camera movement.";
        settingElements.Add(mouseInv);
        addElement(mouseInv);

        var viewBobbing = new ToggleButton(this, "viewBobbing", false, settings.viewBobbing ? 1 : 0,
            "View Bobbing: OFF", "View Bobbing: ON");
        viewBobbing.centreContents();
        viewBobbing.clicked += _ => { settings.viewBobbing = viewBobbing.getIndex() == 1; };
        viewBobbing.tooltip = "Camera bobbing when moving.";
        settingElements.Add(viewBobbing);
        addElement(viewBobbing);

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

        layoutSettingsTwoCols(settingElements, new Vector2I(0, 16), FOV.GUIbounds.Width);
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
