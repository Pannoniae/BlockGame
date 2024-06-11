using System.Drawing;

namespace BlockGame.GUI;

public class Slider : GUIElement {

    public static Rectangle slider = new(0, 80, 128, 16);

    protected Slider(Menu menu, string name) : base(menu, name) {
    }
}