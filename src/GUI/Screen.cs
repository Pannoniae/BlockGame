using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using TrippyGL;

namespace BlockGame.GUI;

// It's like a menu but fullscreen!
public class Screen : Menu {

    public static MainMenuScreen MAIN_MENU_SCREEN = new();
    public static GameScreen GAME_SCREEN = new();

    /// <summary>
    /// The current game menu which is shown.
    /// </summary>
    public Menu currentMenu;

    // passthrough methods
    public override void draw() {
        base.draw();
        currentMenu.draw();
    }

    public override void postDraw() {
        base.postDraw();
        currentMenu.postDraw();
    }

    public override void imGuiDraw() {
        base.imGuiDraw();
        currentMenu.imGuiDraw();
    }

    public override void click(Vector2 pos) {
        base.click(pos);
        currentMenu.click(pos);
    }

    public override void update(double dt) {
        base.update(dt);
        currentMenu.update(dt);
    }

    public override void clear(GraphicsDevice GD, double dt, double interp) {
        base.clear(GD, dt, interp);
        currentMenu.clear(GD, dt, interp);
    }

    public override void render(double dt, double interp) {
        base.render(dt, interp);
        currentMenu.render(dt, interp);
    }

    public override void onMouseDown(IMouse mouse, MouseButton button) {
        base.onMouseDown(mouse, button);
        currentMenu.onMouseDown(mouse, button);
    }

    public override void onMouseMove(IMouse mouse, Vector2 pos) {
        base.onMouseMove(mouse, pos);
        currentMenu.onMouseMove(mouse, pos);
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        base.onKeyDown(keyboard, key, scancode);
        currentMenu.onKeyDown(keyboard, key, scancode);
    }

    public override void onKeyUp(IKeyboard keyboard, Key key, int scancode) {
        base.onKeyUp(keyboard, key, scancode);
        currentMenu.onKeyUp(keyboard, key, scancode);
    }

    public override void scroll(IMouse mouse, ScrollWheel scrollWheel) {
        base.scroll(mouse, scrollWheel);
        currentMenu.scroll(mouse, scrollWheel);
    }

    public override void resize(Vector2D<int> newSize) {
        base.resize(newSize);
        currentMenu.resize(newSize);
    }
}