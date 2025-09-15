using System.Numerics;
using BlockGame.ui;

namespace BlockGame.src.ui.element;

/**
 * Not an *actual* button, but it can be used for registering clicks (if you're lazy)
 */
public class HiddenButton : GUIElement {

    public HiddenButton(Menu menu, string name, Vector2 pos, int w, int h) : base(menu, name) {
        guiPosition = new((int)pos.X, (int)pos.Y, w, h);
    }

    public override void draw() {
        // nothing
    }
}