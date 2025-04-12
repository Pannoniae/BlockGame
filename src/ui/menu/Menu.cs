using System.Numerics;
using Molten;
using Silk.NET.Input;
using TrippyGL;

using RectangleF = System.Drawing.RectangleF;

namespace BlockGame.ui;

public class Menu {

    public Vector2I size;
    public Vector2I centre;

    public Dictionary<string, GUIElement> elements = new();

    public GUIElement? hoveredElement;

    /// <summary>
    /// The element which is currently being pressed.
    /// </summary>
    public GUIElement? pressedElement;

    /// <summary>
    /// The current screen this menu is opened in.
    /// </summary>
    public Screen screen;

    public static LoadingMenu LOADING = new();
    public static MainMenu MAIN_MENU = new();
    public static LevelSelectMenu LEVEL_SELECT = new();
    public static SettingsMenu SETTINGS = new();


    /// <summary>
    /// Does this menu cover the whole screen?
    /// </summary>
    /// <returns></returns>
    public virtual bool isModal() {
        return true;
    }

    /// <summary>
    /// Does this menu block input from the screen?
    /// </summary>
    /// <returns></returns>
    public virtual bool isBlockingInput() {
        return isModal();
    }

    public Menu() {
    }

    // there is no activate/deactivate here to manage elements here because multiple menus can exist (there no generic system for that; it's on a case-by-case basis)
    // for example, the pause menu calls the ingame menu to still draw and update (in MP) while it's open

    public virtual void activate() {

    }

    public virtual void deactivate() {

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
    }
    public static int MOUSEPOSPADDING => 4 * GUI.guiScale;

    public virtual void postDraw() {
        foreach (var element in elements.Values) {
            if (element.active) {
                element.postDraw();
            }
        }
        // draw tooltip for active element
        var tooltip = hoveredElement?.tooltip;
        if (!string.IsNullOrEmpty(tooltip)) {
            var pos = Game.mousePos + new Vector2(MOUSEPOSPADDING) - new Vector2(2);
            var posExt = Game.gui.measureStringThin(tooltip) + new Vector2(4);
            var textPos = Game.mousePos + new Vector2(MOUSEPOSPADDING);
            Game.gui.draw(Game.gui.colourTexture, new RectangleF((int)pos.X, (int)pos.Y, (int)posExt.X, (int)posExt.Y), null, new Color4b(28, 28, 28, 255));
            Game.gui.drawStringThin(tooltip, textPos);
        }
    }

    public virtual void update(double dt) {
        // update hover status
        foreach (var element in elements.Values) {
            element.pressed = element.bounds.Contains((int)Game.mousePos.X, (int)Game.mousePos.Y) && Game.mouse.IsButtonPressed(MouseButton.Left);
            element.update();
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
        foreach (var element in elements.Values) {
            if (element.active && element.bounds.Contains((int)Game.mousePos.X, (int)Game.mousePos.Y)) {
                pressedElement = element;
            }
        }
    }

    public virtual void onMouseUp(Vector2 pos) {
        foreach (var element in elements.Values) {
            //Console.Out.WriteLine(element);
            //Console.Out.WriteLine(element.bounds);
            //Console.Out.WriteLine(pos);
            element.onMouseUp();
            if (element.active && element.bounds.Contains((int)pos.X, (int)pos.Y)) {
                element.click();
            }
        }

        // clear pressed element
        pressedElement = null;
    }

    public virtual void onMouseMove(IMouse mouse, Vector2 pos) {
        bool found = false;
        foreach (var element in elements.Values) {
            //Console.Out.WriteLine(element);
            //Console.Out.WriteLine(element.bounds);
            //Console.Out.WriteLine(pos);
            element.onMouseMove();
            element.hovered = element.bounds.Contains((int)pos.X, (int)pos.Y);
            if (element.bounds.Contains((int)pos.X, (int)pos.Y)) {
                hoveredElement = element;
                found = true;
            }
        }
        if (!found) {
            hoveredElement = null;
        }
    }

    public virtual void onKeyDown(IKeyboard keyboard, Key key, int scancode) {

    }

    public virtual void onKeyRepeat(IKeyboard keyboard, Key key, int scancode) {

    }

    public virtual void onKeyUp(IKeyboard keyboard, Key key, int scancode) {

    }

    public virtual void onKeyChar(IKeyboard keyboard, char c) {

    }

    public virtual void scroll(IMouse mouse, ScrollWheel scrollWheel) {

    }

    public virtual void resize(Vector2I newSize) {
        size = newSize;
        centre = size / 2;
    }
}