using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using TrippyGL;

namespace BlockGame.ui;

// It's like a menu but fullscreen!
public class Screen {

    public Vector2D<int> size;
    public Vector2D<int> centre;

    public static MainMenuScreen MAIN_MENU_SCREEN = new();
    public static GameScreen GAME_SCREEN = new();

    /// <summary>
    /// The current game menu which is shown.
    /// </summary>
    public Menu currentMenu;

    public void switchToMenu(Menu menu) {
        //currentMenu?.deactivate();
        currentMenu = menu;
        menu.size = new Vector2D<int>(Game.width, Game.height);
        menu.centre = menu.size / 2;
        menu.screen = this;
        menu.activate();
        menu.resize(new Vector2D<int>(Game.width, Game.height));
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

    public virtual void onMouseUp(Vector2 pos) {
        currentMenu?.onMouseUp(pos);
    }

    public virtual void update(double dt) {
        currentMenu?.update(dt);
    }

    public virtual void clear(GraphicsDevice GD, double dt, double interp) {
        currentMenu?.clear(GD, dt, interp);
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

    public virtual void onKeyUp(IKeyboard keyboard, Key key, int scancode) {
        currentMenu?.onKeyUp(keyboard, key, scancode);
    }

    public virtual void onKeyChar(IKeyboard keyboard, char c) {
        currentMenu?.onKeyChar(keyboard, c);
    }

    public virtual void scroll(IMouse mouse, ScrollWheel scrollWheel) {
        currentMenu?.scroll(mouse, scrollWheel);
    }

    public virtual void resize(Vector2D<int> newSize) {
        currentMenu?.resize(newSize);
    }
}