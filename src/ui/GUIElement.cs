using System.Drawing;

namespace BlockGame.ui;

public class GUIElement {
    public Menu menu;

    public string name;
    public bool active = true;

    public Rectangle guiPosition;
    public HorizontalAnchor horizontalAnchor = HorizontalAnchor.LEFT;
    public VerticalAnchor verticalAnchor = VerticalAnchor.TOP;

    public string? text = null;

    public string tooltip = "";

    /// <summary>
    /// If true, guiScale does not adjust the size of this element (e.g. text)
    /// </summary>
    public bool unscaledSize = false;

    public bool hovered = false;
    public bool pressed = false;

    /// <summary>
    /// Calculate the absolute bounds of a GUIElement.
    /// </summary>
    public Rectangle bounds {
        get {
            var absolutePos = guiPosition;
            // handle guiscale
            absolutePos.X *= GUI.guiScale;
            absolutePos.Y *= GUI.guiScale;
            // handle guiscale
            if (!unscaledSize) {
                absolutePos.Width *= GUI.guiScale;
                absolutePos.Height *= GUI.guiScale;
            }
            return resolveAnchors(absolutePos, horizontalAnchor, verticalAnchor, menu, false);
        }
    }

    /// <summary>
    /// Calculate the bounds of a GUIElement in GUI space.
    /// </summary>
    public Rectangle GUIbounds {
        get {
            var absolutePos = guiPosition;
            return resolveAnchors(absolutePos, horizontalAnchor, verticalAnchor, menu, true);
        }
    }

    public static Rectangle resolveAnchors(Rectangle absolutePos, HorizontalAnchor horizontalAnchor, VerticalAnchor verticalAnchor, Menu menu, bool uiSpace = true) {
        var sizeX = uiSpace ? Game.gui.uiWidth : menu.size.X;
        var sizeY = uiSpace ? Game.gui.uiHeight : menu.size.Y;
        switch (horizontalAnchor) {
            case HorizontalAnchor.LEFT:
                //absolutePos.X -= menu.width / 2;
                break;
            case HorizontalAnchor.RIGHT:
                //absolutePos.X += menu.width / 2;
                absolutePos.X += sizeX;
                break;
            case HorizontalAnchor.CENTREDCONTENTS:
                absolutePos.X += sizeX / 2 - absolutePos.Width / 2;
                break;
            case HorizontalAnchor.CENTRED:
            default:
                absolutePos.X += sizeX / 2;
                break;
        }

        switch (verticalAnchor) {
            case VerticalAnchor.BOTTOM:
                //absolutePos.Y -= menu.height / 2;
                absolutePos.Y += sizeY;
                break;
            case VerticalAnchor.TOP:
                //absolutePos.Y += menu.height / 2;
                break;
            case VerticalAnchor.CENTREDCONTENTS:
                absolutePos.Y += sizeY / 2 - absolutePos.Height / 2;
                break;
            case VerticalAnchor.CENTRED:
            default:
                absolutePos.Y += sizeY / 2;
                break;
        }
        return absolutePos;
    }

    public event Action? clicked;

    protected GUIElement(Menu menu, string name) {
        this.menu = menu;
        this.name = name;
    }

    public void setPosition(Rectangle pos) {
        guiPosition = pos;
    }

    public void centre() {
        horizontalAnchor = HorizontalAnchor.CENTRED;
        verticalAnchor = VerticalAnchor.CENTRED;
    }

    public void centreContents() {
        horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        verticalAnchor = VerticalAnchor.CENTREDCONTENTS;
    }

    public void topCentre() {
        horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        verticalAnchor = VerticalAnchor.TOP;
    }

    public virtual void draw() {

    }

    public virtual void postDraw() {

    }

    public virtual void click() {
        clicked?.Invoke();
    }

    public virtual void onMouseMove() {
    }

    public virtual void onMouseUp() {
    }

    public virtual void update() {
    }
}

public enum HorizontalAnchor : byte {
    CENTRED,
    CENTREDCONTENTS,
    LEFT,
    RIGHT
}

public enum VerticalAnchor : byte {
    CENTRED,
    CENTREDCONTENTS,
    BOTTOM,
    TOP
}