using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.ui.screen;
using Molten;
using Silk.NET.Input;
using Silk.NET.OpenGL.Legacy;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu.settings;

public class SettingsMenu : Menu {
    private readonly SettingsScreen parentScreen;

    /** Enough of this mess. Time for proper menu management. */
    public readonly Stack<Menu> menuStack = [];

    public SettingsMenu(SettingsScreen parentScreen) {
        this.parentScreen = parentScreen;
        initializeButtons();
    }

    private void initializeButtons() {
        var elements = new List<GUIElement>();
        var settings = Settings.instance;

        var videoSettings = new Button(this, "videoSettings", false, "Video Settings...");
        videoSettings.clicked += _ => { push(SettingsScreen.VIDEO_SETTINGS_MENU); };
        videoSettings.centreContents();
        elements.Add(videoSettings);
        addElement(videoSettings);

        var audioSettings = new Button(this, "audioSettings", false, "Audio Settings...");
        audioSettings.clicked += _ => { push(SettingsScreen.AUDIO_SETTINGS_MENU); };
        audioSettings.centreContents();
        elements.Add(audioSettings);
        addElement(audioSettings);

        var controls = new Button(this, "controls", false, "Controls...");
        controls.clicked += _ => { push(SettingsScreen.CONTROLS_MENU); };
        controls.centreContents();
        elements.Add(controls);
        addElement(controls);

        // texture packs button
        var texturePacksButton = new Button(this, "texturePacks", false, "Texture Packs...");
        texturePacksButton.clicked += _ => {
            var packMenu = new TexturePackMenu(parentScreen);
            push(packMenu);
        };
        texturePacksButton.centreContents();
        elements.Add(texturePacksButton);
        addElement(texturePacksButton);

        var back = new Button(this, "back", false, "Back") {
            horizontalAnchor = HorizontalAnchor.LEFT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        back.setPosition(new Vector2I(2, -18));
        back.clicked += _ => {
            deactivate();
            parentScreen.returnToPrevScreen();
        };
        addElement(back);

        layoutSettingsTwoCols(elements, new Vector2I(0, 16), videoSettings.GUIbounds.Width);
    }

    public void push(Menu menu) {
        menuStack.Push(parentScreen.currentMenu);
        parentScreen.switchToMenu(menu);
    }

    public void pop() {
        if (menuStack.Count > 0) {
            var prevMenu = menuStack.Pop();
            parentScreen.switchToMenu(prevMenu);
        }
    }

    public override void deactivate() {
        base.deactivate();
    }
    
    
    public override void clear(double dt, double interp) {
        Game.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        base.onKeyDown(keyboard, key, scancode);
        if (key == Key.Escape) {
            parentScreen.returnToPrevScreen();
        }
    }

    public override void draw() {
        Game.gui.drawBG(16);
        base.draw();
    }
}