using System.Drawing;
using System.Numerics;

namespace BlockGame.GUI;

public class Slider : GUIElement {

    public float value;
    public int min;
    public int max;

    public float step;

    public const int knobWidth = 7;
    public const int knobHeight = 18;

    public static Rectangle slider = new(0, 80, 128, 16);
    public static Rectangle pressedSlider = new(0, 80 + 32, 128, 16);
    public static Rectangle knob = new(192, 0, knobWidth, knobHeight);

    public Slider(Menu menu, string name, int min, int max, float step, int defaultValue) : base(menu, name) {
        this.min = min;
        this.max = max;
        this.step = step;
        value = defaultValue;
    }


    public override void update() {
        if (menu.pressedElement == this) {
            // in UI coords
            float mouseX = GUI.s2u(Game.mousePos).X;
            float ratio = (mouseX - GUIbounds.X) / GUIbounds.Width;
            value = min + ratio * (max - min);
            value = Math.Clamp(value, min, max);
            value = roundTo(value, step);
            apply();
        }
    }

    /// <summary>
    /// Returns the number rounded to a value multiple of step.
    /// </summary>
    public float roundTo(float number, float step) {
        return (float)Math.Round(number / step) * step;
    }

    public override void draw() {
        var tex = pressed ? pressedSlider : slider;
        Game.gui.draw(Game.gui.guiTexture, new Vector2(bounds.X, bounds.Y), tex);

        // draw the thing on it
        float mouseX = GUI.s2u(Game.mousePos).X;
        float ratio = (value - min) / (max - min);
        float knobX = GUIbounds.X + ratio * GUIbounds.Width - knobWidth / 2f;
        float knobY = GUIbounds.Y - 1f;
        Game.gui.drawUI(Game.gui.guiTexture, new Vector2(knobX, knobY), knob);
        var centre = new Vector2(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f);
        Game.gui.drawStringCentred(getText(), centre);
    }

    public string getText() {
        return "Render Distance: " + value;
    }

    public event Action? applied;

    protected virtual void apply() {
        applied?.Invoke();
    }
}