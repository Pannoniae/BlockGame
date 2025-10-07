using BlockGame.main;
using BlockGame.ui.menu;
using Silk.NET.Input;

namespace BlockGame.ui.element;

public class ToggleButton : Button {

    private List<string> states;
    private List<string>? tooltips;
    private int index;

    public ToggleButton(Menu menu, string name, bool wide, int initialState, params string[] states) : base(menu, name, wide) {
        index = initialState;
        this.states = new List<string>(states);
        text = states[index];
    }

    public void setTooltips(params ReadOnlySpan<string> tooltips) {
        this.tooltips = new List<string>(tooltips.ToArray());
        if (tooltips.Length > 0 && index < tooltips.Length) {
            tooltip = tooltips[index];
        }
    }

    public override void click(MouseButton button) {
        switch (button) {
            case MouseButton.Left:
                _click(this);
                break;
            case MouseButton.Right:
                _clickReverse(this);
                break;
        }
        doClick();
    }

    public void _click(GUIElement e) {
        index++;
        index %= states.Count;
        text = states[index];
        updateTooltip();
    }

    public void _clickReverse(GUIElement e) {
        index--;
        if (index < 0) {
            index = states.Count - 1;
        }
        text = states[index];
        updateTooltip();
    }

    private void updateTooltip() {
        if (tooltips != null && index < tooltips.Count) {
            tooltip = tooltips[index];
        }
    }

    public string getState() {
        return states[index];
    }

    public int getIndex() {
        return index;
    }
}