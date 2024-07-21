namespace BlockGame.ui;

public class ToggleButton : Button {

    private List<string> states;
    private int index;

    public ToggleButton(Menu menu, string name, bool wide, int initialState, params string[] states) : base(menu, name, wide) {
        index = initialState;
        this.states = new List<string>(states);
        text = states[index];
        clicked += doClick;
    }

    public void doClick(GUIElement guiElement) {
        index++;
        index %= states.Count;
        text = states[index];
    }

    public string getState() {
        return states[index];
    }

    public int getIndex() {
        return index;
    }
}