using System.Drawing;

namespace BlockGame;

public class GUIElement {
    public Screen screen;
    public RectangleF position;
    public HorizontalAnchor horizontalAnchor = HorizontalAnchor.LEFT;
    public VerticalAnchor verticalAnchor = VerticalAnchor.TOP;

    /// <summary>
    /// If true, guiScale does not adjust the size of this element (e.g. text)
    /// </summary>
    public bool unscaledSize = false;

    public bool hovered = false;
    public bool pressed = false;

    /// <summary>
    /// Calculate the absolute bounds of a GUIElement.
    /// </summary>
    public RectangleF bounds {
        get {
            var absolutePos = position;
            // handle guiscale
            absolutePos.X *= Game.gui.guiScale;
            absolutePos.Y *= Game.gui.guiScale;
            // handle guiscale
            if (!unscaledSize) {
                absolutePos.Width *= Game.gui.guiScale;
                absolutePos.Height *= Game.gui.guiScale;
            }
            switch (horizontalAnchor) {
                case HorizontalAnchor.LEFT:
                    //absolutePos.X -= screen.width / 2;
                    break;
                case HorizontalAnchor.RIGHT:
                    //absolutePos.X += screen.width / 2;
                    absolutePos.X += screen.size.X;
                    break;
                case HorizontalAnchor.CENTREDCONTENTS:
                    absolutePos.X += screen.size.X / 2 - absolutePos.Width / 2;
                    break;
                case HorizontalAnchor.CENTRED:
                default:
                    absolutePos.X += screen.size.X / 2;
                    break;
            }

            switch (verticalAnchor) {
                case VerticalAnchor.BOTTOM:
                    //absolutePos.Y -= screen.height / 2;
                    absolutePos.Y += screen.size.Y;
                    break;
                case VerticalAnchor.TOP:
                    //absolutePos.Y += screen.height / 2;
                    break;
                case VerticalAnchor.CENTREDCONTENTS:
                    absolutePos.Y += screen.size.Y / 2 - absolutePos.Height / 2;
                    break;
                case VerticalAnchor.CENTRED:
                default:
                    absolutePos.Y += screen.size.Y / 2;
                    break;
            }
            return absolutePos;
        }
    }

    public event Action? clicked;

    protected GUIElement(Screen screen, RectangleF position) {
        this.screen = screen;
        this.position = position;
    }

    public void setPosition(RectangleF pos) {
        position = pos;
    }

    public void centre() {
        horizontalAnchor = HorizontalAnchor.CENTRED;
        verticalAnchor = VerticalAnchor.CENTRED;
    }

    public void centreContents() {
        horizontalAnchor = HorizontalAnchor.CENTREDCONTENTS;
        verticalAnchor = VerticalAnchor.CENTREDCONTENTS;
    }

    public virtual void draw() {

    }

    public virtual void click() {
        clicked?.Invoke();
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