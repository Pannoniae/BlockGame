using System.Drawing;

namespace BlockGame;

public class ToggleButton : Button {

    private List<string> states;
    private int index;

    public ToggleButton(Screen screen, RectangleF guiPosition, int initialState, params string[] states) : base(screen, guiPosition) {
        index = initialState;
        this.states = new List<string>(states);
        text = states[index];
        clicked += doClick;
    }

    public void doClick() {
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