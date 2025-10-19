using BlockGame.main;
using BlockGame.ui.menu;
using BlockGame.util;
using Silk.NET.Input;

namespace BlockGame.ui.element;

public class ToggleButton : Button {

    private List<string> ids; // internal IDs
    private int index;

    public ToggleButton(Menu menu, string name, bool wide, int initialState, params string[] ids) : base(menu, name, wide) {
        index = initialState;

        // cap index!! we might have corrupted settings
        index = int.Clamp(index, 0, ids.Length - 1);

        this.ids = new List<string>(ids);
        updateDisplay();
    }

    private void updateDisplay() {
        var id = ids[index];
        text = Loc.get(id);

        // try to find tooltip with .tooltip suffix
        var tooltipKey = id + ".tooltip";
        if (Loc.has(tooltipKey)) {
            tooltip = Loc.get(tooltipKey);
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
        index %= ids.Count;
        updateDisplay();
    }

    public void _clickReverse(GUIElement e) {
        index--;
        if (index < 0) {
            index = ids.Count - 1;
        }
        updateDisplay();
    }

    public string getState() {
        return ids[index];
    }

    public int getIndex() {
        return index;
    }
}