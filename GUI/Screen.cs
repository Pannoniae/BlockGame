using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using TrippyGL;

namespace BlockGame;

public class Screen {

    public Vector2D<int> size;
    public Vector2D<int> centre;


    /// <summary>
    /// If true, the screen lets screens under it update.
    /// </summary>
    public bool transparentUpdate = false;

    /// <summary>
    /// If true, the screen lets screens under it render. (e.g. the pause menu)
    /// </summary>
    public bool transparentRender = false;

    public Dictionary<string, GUIElement> elements = new();

    public GUIElement? activeElement;

    public static LoadingScreen LOADING = new();
    public static MainMenuScreen MAIN_MENU = new();
    public static GameScreen GAME_SCREEN = new();
    public static SettingsScreen SETTINGS_SCREEN = new();

    public Screen() {
    }

    public virtual void activate() {

    }

    public virtual void deactivate() {
        elements.Clear();
    }

    /// <summary>
    /// Clears the entire screenstack and pushes the screen.
    /// </summary>
    public static void switchTo(Screen screen) {
        foreach (var sc in Game.instance.screenStack) {
            sc.deactivate();
        }
        Game.instance.screenStack.push(screen);
        screen.size = new Vector2D<int>(Game.width, Game.height);
        screen.centre = screen.size / 2;
        screen.activate();
        screen.resize(new Vector2D<int>(Game.width, Game.height));
    }

    /// <summary>
    /// It's like switchTo but the screens already on the stack don't get deactivated.
    /// </summary>
    public static void addToStack(Screen screen) {
        Game.instance.screenStack.push(screen);
        screen.size = new Vector2D<int>(Game.width, Game.height);
        screen.centre = screen.size / 2;
        screen.activate();
        screen.resize(new Vector2D<int>(Game.width, Game.height));

    }

    public void addElement(GUIElement element) {
        elements.Add(element.name, element);
    }

    public GUIElement getElement(string name) {
        return elements[name];
    }

    public virtual void draw() {
        foreach (var element in elements.Values) {
            if (element.active) {
                element.draw();
            }
        }
        // draw tooltip for active element
        var tooltip = activeElement?.tooltip;
        var mousePos = Game.mouse.Position;
        if (!string.IsNullOrEmpty(tooltip)) {
            Game.gui.drawString(tooltip, mousePos + new Vector2(MOUSEPOSPADDING));
        }
    }
    public static int MOUSEPOSPADDING => 4 * GUI.guiScale;

    public virtual void postDraw() {
        foreach (var element in elements.Values) {
            if (element.active) {
                element.postDraw();
            }
        }
    }

    public virtual void imGuiDraw() {
    }

    public virtual void click(Vector2 pos) {
        foreach (var element in elements.Values) {
            //Console.Out.WriteLine(element);
            //Console.Out.WriteLine(element.bounds);
            //Console.Out.WriteLine(pos);
            if (element.active && element.bounds.Contains((int)pos.X, (int)pos.Y)) {
                element.click();
            }
        }
    }

    public virtual void update(double dt) {
        // update hover status
        foreach (var element in elements.Values) {
            element.hovered = element.bounds.Contains((int)Game.mouse.Position.X, (int)Game.mouse.Position.Y);
            element.pressed = element.bounds.Contains((int)Game.mouse.Position.X, (int)Game.mouse.Position.Y) && Game.mouse.IsButtonPressed(MouseButton.Left);
        }
    }

    public virtual void clear(GraphicsDevice GD, double dt, double interp) {
        GD.ClearColor = WorldRenderer.defaultClearColour;
        GD.ClearDepth = 1f;
        GD.Clear(ClearBuffers.Color | ClearBuffers.Depth);
    }

    public virtual void render(double dt, double interp) {

    }

    public virtual void onMouseDown(IMouse mouse, MouseButton button) {

    }

    public virtual void onMouseMove(IMouse mouse, Vector2 pos) {
        bool found = false;
        foreach (var element in elements.Values) {
            //Console.Out.WriteLine(element);
            //Console.Out.WriteLine(element.bounds);
            //Console.Out.WriteLine(pos);
            if (element.bounds.Contains((int)pos.X, (int)pos.Y)) {
                activeElement = element;
                found = true;
            }
        }
        if (!found) {
            activeElement = null;
        }
    }

    public virtual void onKeyDown(IKeyboard keyboard, Key key, int scancode) {

    }

    public virtual void onKeyUp(IKeyboard keyboard, Key key, int scancode) {

    }

    public virtual void scroll(IMouse mouse, ScrollWheel scrollWheel) {

    }

    public virtual void resize(Vector2D<int> newSize) {
        size = newSize;
        centre = size / 2;
    }


}