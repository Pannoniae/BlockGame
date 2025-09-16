using System.Numerics;
using BlockGame.main;
using BlockGame.ui.menu;
using BlockGame.ui.screen;
using Molten;
using Silk.NET.Input;

namespace BlockGame.ui;

// It's like a menu but fullscreen!
public class Screen {

    public Vector2I size;
    public Vector2I centre;

    public static MainMenuScreen MAIN_MENU_SCREEN;
    public static GameScreen GAME_SCREEN;
    public static SettingsScreen SETTINGS_SCREEN;

    /// <summary>
    /// The current game menu which is shown.
    /// </summary>
    public Menu currentMenu;


    public static void init() {
        MAIN_MENU_SCREEN = new MainMenuScreen();
        GAME_SCREEN = new GameScreen();
        SETTINGS_SCREEN = new SettingsScreen();
    }

    public void switchToMenu(Menu menu) {
        currentMenu?.deactivate();
        currentMenu = menu;
        menu.size = new Vector2I(Game.width, Game.height);
        menu.centre = menu.size / 2;
        menu.screen = this;
        menu.activate();
        menu.resize(new Vector2I(Game.width, Game.height));
    }

    public virtual void activate() {

    }

    public virtual void deactivate() {

    }

    public void exitMenu() {
        currentMenu?.deactivate();
        currentMenu = null!;
    }

    // passthrough methods
    public virtual void draw() {
        currentMenu?.draw();
    }

    public virtual void postDraw() {
        currentMenu?.postDraw();
    }

    public virtual void onMouseUp(Vector2 pos, MouseButton button) {
        currentMenu?.onMouseUp(pos, button);
    }

    public virtual void update(double dt) {
        currentMenu?.update(dt);
    }

    public virtual void clear(double dt, double interp) {
        currentMenu?.clear(dt, interp);
    }

    public virtual void render(double dt, double interp) {
        currentMenu?.render(dt, interp);
    }

    public virtual void postRender(double dt, double interp) {
        currentMenu?.postRender(dt, interp);
    }

    public virtual void onMouseDown(IMouse mouse, MouseButton button) {
        currentMenu?.onMouseDown(mouse, button);
    }

    public virtual void onMouseMove(IMouse mouse, Vector2 pos) {
        currentMenu?.onMouseMove(mouse, pos);
    }

    public virtual void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        currentMenu?.onKeyDown(keyboard, key, scancode);
    }

    public virtual void onKeyRepeat(IKeyboard keyboard, Key key, int scancode) {
        currentMenu?.onKeyRepeat(keyboard, key, scancode);
    }

    public virtual void onKeyUp(IKeyboard keyboard, Key key, int scancode) {
        currentMenu?.onKeyUp(keyboard, key, scancode);
    }

    public virtual void onKeyChar(IKeyboard keyboard, char c) {
        currentMenu?.onKeyChar(keyboard, c);
    }

    public virtual void scroll(IMouse mouse, ScrollWheel scrollWheel) {
        currentMenu?.scroll(mouse, scrollWheel);
    }

    public virtual void resize(Vector2I newSize) {
        currentMenu?.resize(newSize);
    }
}