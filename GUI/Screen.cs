using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using TrippyGL;

namespace BlockGame;

public class Screen {

    public Vector2D<int> size;
    public Vector2D<int> centre;

    public List<GUIElement> elements = new();

    public static LoadingScreen LOADING = new();
    public static MainMenuScreen MAIN_MENU = new();
    public static GameScreen GAME_SCREEN = new();

    public Screen() {
    }

    public virtual void activate() {

    }

    public virtual void deactivate() {
        elements.Clear();
    }

    public static void switchTo(Screen screen) {
        Game.instance.screen?.deactivate();
        Game.instance.screen = screen;
        screen.activate();
        screen.resize(new Vector2D<int>(Game.instance.width, Game.instance.height));
    }

    public void addElement(GUIElement element) {
        elements.Add(element);
    }

    public virtual void draw() {
        foreach (var element in elements) {
            element.draw();
        }
    }

    public virtual void imGuiDraw() {
    }

    public virtual void click(Vector2 pos) {
        foreach (var element in elements) {
            Console.Out.WriteLine(element);
            Console.Out.WriteLine(element.bounds);
            Console.Out.WriteLine(pos);
            if (element.bounds.Contains((int)pos.X, (int)pos.Y)) {
                element.click();
            }
        }
    }

    public virtual void update(double dt) {

    }

    public virtual void render(double dt, double interp) {

    }

    public virtual void onMouseDown(IMouse mouse, MouseButton button) {

    }

    public virtual void onMouseMove(IMouse mouse, Vector2 position) {

    }

    public virtual void onKeyDown(IKeyboard keyboard, Key key, int scancode) {

    }

    public virtual void resize(Vector2D<int> newSize) {
        size = newSize;
        centre = size / 2;
    }
}