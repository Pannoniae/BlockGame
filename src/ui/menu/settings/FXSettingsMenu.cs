using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.ui.screen;
using Molten;
using Silk.NET.Input;
using Silk.NET.OpenGL.Legacy;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu.settings;

public class FXSettingsMenu : Menu {
    private readonly SettingsScreen parentScreen;

    public FXSettingsMenu(SettingsScreen parentScreen) {
        this.parentScreen = parentScreen;
        initializeSettings();
    }

    private void initializeSettings() {
        var settings = Settings.instance;
        var settingElements = new List<GUIElement>();

        var crtEffect = new ToggleButton(this, "crtEffect", false, settings.crtEffect ? 1 : 0,
            "CRT Effect: OFF", "CRT Effect: ON");
        crtEffect.centreContents();
        crtEffect.clicked += _ => {
            settings.crtEffect = crtEffect.getIndex() == 1;
            Game.instance.updateFramebuffers();
        };
        crtEffect.tooltip =
            "CRT Effect adds a retro CRT monitor effect with scanlines.\nProvides an authentic vintage computing experience or something.";
        settingElements.Add(crtEffect);
        addElement(crtEffect);

        var affineMapping = new ToggleButton(this, "affineMapping", false, settings.affineMapping ? 1 : 0,
            "Affine Mapping: OFF", "Affine Mapping: ON");
        affineMapping.centreContents();
        affineMapping.clicked += _ => { settings.affineMapping = affineMapping.getIndex() == 1; };
        affineMapping.tooltip =
            "PS1-style affine texture mapping (no perspective correction).\nVERY LIMINAL.";
        settingElements.Add(affineMapping);
        addElement(affineMapping);

        var vertexJitter = new ToggleButton(this, "vertexJitter", false, settings.vertexJitter ? 1 : 0,
            "Vertex Jitter: OFF", "Vertex Jitter: ON");
        vertexJitter.centreContents();
        vertexJitter.clicked += _ => { settings.vertexJitter = vertexJitter.getIndex() == 1; };
        vertexJitter.tooltip =
            "PS1-style vertex snapping/wobble effect.\nMUCH NOSTALGIA.";
        settingElements.Add(vertexJitter);
        addElement(vertexJitter);

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

        layoutSettingsTwoCols(settingElements, new Vector2I(0, 16), crtEffect.GUIbounds.Width);
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
