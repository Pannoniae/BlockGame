using System.Drawing;

namespace BlockGame;

public class GUIElement {
    public Screen screen;
    public Rectangle bounds;

    public event Action clicked;

    protected GUIElement(Screen screen, Rectangle bounds) {
        this.screen = screen;
        this.bounds = bounds;
    }

    public virtual void draw() {

    }

    public virtual void click() {
        clicked?.Invoke();
    }
}