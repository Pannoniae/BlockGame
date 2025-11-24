using System.Numerics;
using BlockGame.main;
using BlockGame.ui.element;
using Molten;
using Silk.NET.Input;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.ui.menu;

public class Menu {

    protected static long clickTimer = 0;

    protected static void playClick() {
        Game.snd.plays("click");
        clickTimer = Game.permanentStopwatch.ElapsedMilliseconds;
    }

    protected static void playRelease() {
        if (clickTimer + 200 < Game.permanentStopwatch.ElapsedMilliseconds) {
            Game.snd.plays("clickr");
        }
    }

    public Vector2I size;
    public Vector2I centre;

    public readonly Dictionary<string, GUIElement> elements = new();

    public GUIElement? hoveredElement;

    /// <summary>
    /// The element which is currently being pressed (has mouse capture).
    /// Receives all mouse events until release, regardless of cursor position.
    /// </summary>
    public GUIElement? pressedElement;

    /// <summary>
    /// The element which currently has keyboard focus.
    /// Receives all keyboard events.
    /// </summary>
    public GUIElement? focusedElement;

    /// <summary>
    /// The current screen this menu is opened in.
    /// </summary>
    public Screen screen;

    public static LoadingMenu LOADING;
    public static StartupLoadingMenu STARTUP_LOADING;
    public static MainMenu MAIN_MENU;
    public static MultiplayerMenu MULTIPLAYER_MENU;
    public static LevelSelectMenu LEVEL_SELECT;
    public static CreateWorldMenu CREATE_WORLD;
    public static ConfirmDialog CONFIRM_DIALOG;
    public static DisconnectedMenu DISCONNECTED_MENU;
    public static LoginMenu LOGIN_MENU;
    public static RegisterMenu REGISTER_MENU;


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

    /// <summary>
    /// Does this menu pause the world?
    /// </summary>
    /// <returns></returns>
    public virtual bool pausesWorld() {
        return isModal();
    }

    public static void init() {
        LOADING = new LoadingMenu();
        // NOT HERE! initialised manually
        //STARTUP_LOADING = new StartupLoadingMenu();
        MAIN_MENU = new MainMenu();
        MULTIPLAYER_MENU = new MultiplayerMenu();
        LEVEL_SELECT = new LevelSelectMenu();
        CREATE_WORLD = new CreateWorldMenu();
        CONFIRM_DIALOG = new ConfirmDialog();
        DISCONNECTED_MENU = new DisconnectedMenu();
        LOGIN_MENU = new LoginMenu();
        REGISTER_MENU = new RegisterMenu();
    }

    // there is no activate/deactivate here to manage elements here because multiple menus can exist (there no generic system for that; it's on a case-by-case basis)
    // for example, the pause menu calls the ingame menu to still draw and update (in MP) while it's open

    public virtual void activate() {
        // clear mouse capture state when menu activates?
        pressedElement = null;
        hoveredElement = null;
        focusedElement = null;
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
    public static Vector2 MOUSEPOSPADDING => new Vector2(10 * GUI.guiScale, 6 * GUI.guiScale);

    public virtual void postDraw() {
        foreach (var element in elements.Values) {
            if (element.active) {
                element.postDraw();
            }
        }

        var tooltip = getTooltipText();
        if (!string.IsNullOrEmpty(tooltip)) {
            drawTooltip(tooltip);
        }
    }

    /** Override to provide custom tooltip text (e.g. from item slots) */
    protected virtual string? getTooltipText() {
        return hoveredElement?.tooltip;
    }

    /** Draw a tooltip at mouse position with the given text */
    protected static void drawTooltip(string tooltip) {
        var pos = Game.mousePos + MOUSEPOSPADDING;
        var posExt = Game.gui.measureStringThin(tooltip) + new Vector2(4, 2) * GUI.guiScale;
        var textPos = Game.mousePos + MOUSEPOSPADDING + new Vector2(2, 1) * GUI.guiScale;

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

        // draw border with gradient
        var borderColorTop = new Color(0, 115, 226, 255);
        var borderColorBottom = new Color(0, 151, 226, 240);
        Game.gui.drawGradientVertical(Game.gui.colourTexture, borderRect, borderColorTop, borderColorBottom);

        // draw background with gradient (dark purple to dark blue)
        var bgColorTop = new Color(0, 17, 40, 255);
        var bgColorBottom = new Color(0, 25, 40, 240);
        Game.gui.drawGradientVertical(Game.gui.colourTexture, bgRect, bgColorTop, bgColorBottom);

        Game.gui.drawStringThin(tooltip, textPos);
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
            if (button is MouseButton.Left or MouseButton.Right) {
                playClick();
                if (button is MouseButton.Left) {
                    pressedElement = target;
                }
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
            if (button is MouseButton.Left or MouseButton.Right) {
                playRelease();
                if (button is MouseButton.Left) {
                    pressedElement = null;
                }
            }
        } else {
            // no capture - check for element under cursor
            foreach (var element in elements.Values) {
                if (element.active && element.bounds.Contains((int)pos.X, (int)pos.Y)) {
                    element.onMouseUp(button);
                    element.click(button);
                    if (button is MouseButton.Left or MouseButton.Right) {
                        playRelease();
                    }
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
        focusedElement?.onKeyDown(key, scancode);
    }

    public virtual void onKeyRepeat(IKeyboard keyboard, Key key, int scancode) {
        focusedElement?.onKeyRepeat(key, scancode);
    }

    public virtual void onKeyUp(IKeyboard keyboard, Key key, int scancode) {
        focusedElement?.onKeyUp(key, scancode);
    }

    public virtual void onKeyChar(IKeyboard keyboard, char c) {
        focusedElement?.onKeyChar(c);
    }

    public virtual void scroll(IMouse mouse, ScrollWheel scroll) {

    }

    public virtual void resize(Vector2I newSize) {
        size = newSize;
        centre = size / 2;
    }

    public static void layoutSettingsTwoCols(List<GUIElement> elements, Vector2I startPos, int buttonWidth) {
        // calculate vertical centre (elements use CENTREDCONTENTS anchor, so position is relative to centre)
        var rows = (elements.Count + 1) / 2;
        // centre the group: first element offset from centre
        var centredY = -((rows - 1) * 18) / 2;

        // to the left/right
        var offset = buttonWidth / 2 + 8;
        var pos = new Vector2I(startPos.X, centredY);
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