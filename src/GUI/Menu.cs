using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using TrippyGL;

namespace BlockGame.GUI;

public class Menu {

    public Vector2D<int> size;
    public Vector2D<int> centre;


    /// <summary>
    /// If true, the menu lets screens under it update.
    /// </summary>
    public bool transparentUpdate = false;

    /// <summary>
    /// If true, the menu lets screens under it render. (e.g. the pause menu)
    /// </summary>
    public bool transparentRender = false;

    public Dictionary<string, GUIElement> elements = new();

    public GUIElement? activeElement;

    public static LoadingMenu LOADING = new();
    public static MainMenu MAIN_MENU = new();
    public static SettingsMenu SETTINGS = new();

    public Menu() {
    }

    public virtual void activate() {

    }

    public virtual void deactivate() {
        elements.Clear();
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
        if (!string.IsNullOrEmpty(tooltip)) {
            Game.gui.drawString(tooltip, Game.mousePos + new Vector2(MOUSEPOSPADDING));
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
            element.hovered = element.bounds.Contains((int)Game.mousePos.X, (int)Game.mousePos.Y);
            element.pressed = element.bounds.Contains((int)Game.mousePos.X, (int)Game.mousePos.Y) && Game.mouse.IsButtonPressed(MouseButton.Left);
        }
    }

    public virtual void clear(GraphicsDevice GD, double dt, double interp) {
        GD.ClearColor = WorldRenderer.defaultClearColour;
        GD.ClearDepth = 1f;
        GD.Clear(ClearBuffers.Color | ClearBuffers.Depth);
    }

    public virtual void render(double dt, double interp) {

    }

    public virtual void postRender(double dt, double interp) {

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