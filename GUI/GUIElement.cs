using System.Drawing;

namespace BlockGame;

public class GUIElement {
    public Screen screen;
    public Rectangle position;
    public HorizontalAnchor horizontalAnchor = HorizontalAnchor.CENTRED;
    public VerticalAnchor verticalAnchor = VerticalAnchor.CENTRED;

    public bool hovered = false;
    public bool pressed = false;

    /// <summary>
    /// Calculate the absolute bounds of a GUIElement.
    /// </summary>
    public Rectangle bounds {
        get {
            var absolutePos = position;
            switch (horizontalAnchor) {
                case HorizontalAnchor.LEFT:
                    //absolutePos.X -= screen.width / 2;
                    break;
                case HorizontalAnchor.RIGHT:
                    //absolutePos.X += screen.width / 2;
                    absolutePos.X += screen.size.X;
                    break;
                case HorizontalAnchor.CENTRED:
                default:
                    absolutePos.X += screen.size.X / 2;
                    break;
            }

            switch (verticalAnchor) {
                case VerticalAnchor.BOTTOM:
                    //absolutePos.Y -= screen.height / 2;
                    break;
                case VerticalAnchor.TOP:
                    //absolutePos.Y += screen.height / 2;
                    absolutePos.Y += screen.size.Y;
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

    protected GUIElement(Screen screen, Rectangle position) {
        this.screen = screen;
        this.position = position;
    }

    public void setPosition(Rectangle pos) {
        position = pos;
    }

    public virtual void draw() {

    }

    public virtual void click() {
        clicked?.Invoke();
    }
}

public enum HorizontalAnchor : byte {
    CENTRED,
    LEFT,
    RIGHT
}

public enum VerticalAnchor : byte {
    CENTRED,
    BOTTOM,
    TOP
}