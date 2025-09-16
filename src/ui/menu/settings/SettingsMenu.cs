using BlockGame.ui.element;
using BlockGame.ui.screen;
using BlockGame.util;
using Molten;
using Silk.NET.Input;
using Silk.NET.OpenGL.Legacy;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu.settings;

public class SettingsMenu : Menu {
    private readonly SettingsScreen parentScreen;

    public SettingsMenu(SettingsScreen parentScreen) {
        this.parentScreen = parentScreen;
        initializeButtons();
    }

    private void initializeButtons() {
        // Two buttons: Video Settings and Controls
        var elements = new List<GUIElement>();
        
        var videoSettings = new Button(this, "videoSettings", false, "Video Settings");
        videoSettings.clicked += _ => { parentScreen.switchToMenu(SettingsScreen.VIDEO_SETTINGS_MENU); };
        videoSettings.centreContents();
        elements.Add(videoSettings);
        addElement(videoSettings);
        
        var controls = new Button(this, "controls", false, "Controls");
        controls.clicked += _ => { parentScreen.switchToMenu(SettingsScreen.CONTROLS_MENU); };
        controls.centreContents();
        elements.Add(controls);
        addElement(controls);
        
        layoutSettingsTwoCols(elements, new Vector2I(0, 16), videoSettings.GUIbounds.Width);
    }
    
    
    public override void clear(double dt, double interp) {
        main.Game.graphics.clearColor(Color4b.SlateGray);
        main.Game.graphics.clearDepth();
        main.Game.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        if (key == Key.Escape) {
            parentScreen.returnToPrevScreen();
        }
    }

    public override void draw() {
        main.Game.gui.drawBG(16);
        base.draw();
    }
}