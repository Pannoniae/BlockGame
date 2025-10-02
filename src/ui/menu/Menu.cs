using System.Numerics;
using BlockGame.main;
using BlockGame.render;
using BlockGame.ui.element;
using BlockGame.util;
using Molten;
using Silk.NET.Input;
using Silk.NET.OpenGL.Legacy;
using Rectangle = System.Drawing.Rectangle;
using RectangleF = System.Drawing.RectangleF;

namespace BlockGame.ui.menu;

public class Menu {

    public Vector2I size;
    public Vector2I centre;

    public Dictionary<string, GUIElement> elements = new();

    public GUIElement? hoveredElement;

    /// <summary>
    /// The element which is currently being pressed (has mouse capture).
    /// Receives all mouse events until release, regardless of cursor position.
    /// </summary>
    public GUIElement? pressedElement;

    /// <summary>
    /// The current screen this menu is opened in.
    /// </summary>
    public Screen screen;

    public static LoadingMenu LOADING;
    public static StartupLoadingMenu STARTUP_LOADING;
    public static MainMenu MAIN_MENU;
    public static LevelSelectMenu LEVEL_SELECT;


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

    public static void init() {
        LOADING = new LoadingMenu();
        // NOT HERE! initialised manually
        //STARTUP_LOADING = new StartupLoadingMenu();
        MAIN_MENU = new MainMenu();
        LEVEL_SELECT = new LevelSelectMenu();
    }

    // there is no activate/deactivate here to manage elements here because multiple menus can exist (there no generic system for that; it's on a case-by-case basis)
    // for example, the pause menu calls the ingame menu to still draw and update (in MP) while it's open

    public virtual void activate() {
        // clear mouse capture state when menu activates?
        pressedElement = null;
        hoveredElement = null;
    }

    public virtual void deactivate() {

    }

    public void addElement(GUIElement element) {
        elements.Add(element.name, element);
    }

    public GUIElement getElement(string name) {
        return elements[name];
    }
    
    public bool hasElement(string name) {
        return elements.ContainsKey(name);
    }
    
    public void removeElement(string name) {
        elements.Remove(name);
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
            
            // clamp tooltip to screen bounds
            var screenWidth = Game.window.Size.X;
            var screenHeight = Game.window.Size.Y;
            
            var borderSize = GUI.guiScale;
            
            
            if (pos.X + posExt.X + borderSize > screenWidth) {
                var overflow = pos.X + posExt.X + borderSize - screenWidth;
                pos.X -= overflow;
                textPos.X -= overflow;
            }
            if (pos.Y + posExt.Y + borderSize > screenHeight) {
                var overflow = pos.Y + posExt.Y + borderSize - screenHeight;
                pos.Y -= overflow;
                textPos.Y -= overflow;
            }
            
            var borderRect = new RectangleF(pos.X - borderSize, pos.Y - borderSize, posExt.X + borderSize * 2, posExt.Y + borderSize * 2);
            var bgRect = new RectangleF(pos.X, pos.Y, posExt.X, posExt.Y);
            
            // draw border with gradient (vibrant purple to deep blue)
            var borderColorTop = new Color4b(147, 51, 234, 255);    // bright purple
            var borderColorBottom = new Color4b(59, 130, 246, 255); // bright blue
            Game.gui.drawGradientVertical(Game.gui.colourTexture, borderRect, borderColorTop, borderColorBottom);
            
            // draw background with gradient (dark purple to dark blue)  
            var bgColorTop = new Color4b(30, 15, 45, 240);    // dark purple
            var bgColorBottom = new Color4b(15, 25, 45, 240); // dark blue
            Game.gui.drawGradientVertical(Game.gui.colourTexture, bgRect, bgColorTop, bgColorBottom);
            
            Game.gui.drawStringThin(tooltip, textPos);
        }
    }

    public virtual void update(double dt) {
        // update hover status
        foreach (var element in elements.Values) {
            element.pressed = element.bounds.Contains((int)Game.mousePos.X, (int)Game.mousePos.Y) && Game.inputs.left.down();
            element.update();
        }
    }

    public virtual void clear(double dt, double interp) {
        Game.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public virtual void render(double dt, double interp) {

    }

    public virtual void postRender(double dt, double interp) {

    }

    public virtual void onMouseDown(IMouse mouse, MouseButton button) {
        // find element under cursor
        GUIElement? target = null;
        foreach (var element in elements.Values) {
            if (element.active && element.bounds.Contains((int)Game.mousePos.X, (int)Game.mousePos.Y)) {
                target = element;
                break;
            }
        }

        // only the target element receives the event
        if (target != null) {
            target.onMouseDown(button);
            if (button == MouseButton.Left) {
                pressedElement = target;
            }
        }
    }

    public virtual void onMouseUp(Vector2 pos, MouseButton button) {
        if (pressedElement != null) {
            // captured element gets the release
            pressedElement.onMouseUp(button);
            if (pressedElement.active) {
                pressedElement.click(button);
            }
            if (button == MouseButton.Left) {
                pressedElement = null;
            }
        } else {
            // no capture - check for element under cursor
            foreach (var element in elements.Values) {
                if (element.active && element.bounds.Contains((int)pos.X, (int)pos.Y)) {
                    element.onMouseUp(button);
                    element.click(button);
                    break;
                }
            }
        }
    }

    public virtual void onMouseMove(IMouse mouse, Vector2 pos) {
        if (pressedElement != null) {
            // captured element gets all moves
            pressedElement.onMouseMove();
        } else {
            // normal hover behaviour
            bool found = false;
            foreach (var element in elements.Values) {
                element.hovered = element.bounds.Contains((int)pos.X, (int)pos.Y);
                if (element.bounds.Contains((int)pos.X, (int)pos.Y)) {
                    element.onMouseMove();
                    hoveredElement = element;
                    found = true;
                }
            }
            if (!found) {
                hoveredElement = null;
            }
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

    public virtual void scroll(IMouse mouse, ScrollWheel scroll) {

    }

    public virtual void resize(Vector2I newSize) {
        size = newSize;
        centre = size / 2;
    }

    public static void layoutSettingsTwoCols(List<GUIElement> elements, Vector2I startPos, int buttonWidth) {
        // to the left/right
        var offset = buttonWidth / 2 + 8;
        var pos = startPos;
        for (int i = 0; i < elements.Count; i++) {
            var element = elements[i];
            int o;
            if (i % 2 == 0) {
                o = -offset;
            }
            else {
                o = offset;
            }

            element.setPosition(new Rectangle(pos.X + o, pos.Y, element.GUIbounds.Width, element.GUIbounds.Height));
            if (i % 2 == 1) {
                pos.Y += 18;
            }
        }
    }

    public static void layoutSettings(List<GUIElement> elements, Vector2I startPos) {
        var pos = startPos;
        foreach (var element in elements) {
            element.setPosition(new Rectangle(pos.X, pos.Y, element.GUIbounds.Width, element.GUIbounds.Height));
            pos.Y += 18;
        }
    }
}