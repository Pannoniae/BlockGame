using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using TrippyGL;

namespace BlockGame;

public class Screen {

    public GUI gui;
    public GraphicsDevice GD;
    public TextureBatch tb;

    public Vector2D<int> size;
    public Vector2D<int> centre;

    public List<GUIElement> elements = new();

    public static Screen MAIN_MENU = null!;
    public static Screen GAME_SCREEN = null!;

    public static void initScreens(GUI gui) {
        MAIN_MENU = new MainMenuScreen(gui, gui.GD, gui.tb);
        GAME_SCREEN = new GameScreen(gui, gui.GD, gui.tb);
    }

    public Screen(GUI gui, GraphicsDevice GD, TextureBatch tb) {
        this.gui = gui;
        this.GD = GD;
        this.tb = tb;
    }

    public void addElement(GUIElement element) {
        elements.Add(element);
    }

    public virtual void draw() {
        tb.Begin();
        foreach (var element in elements) {
            element.draw();
        }
        tb.End();
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