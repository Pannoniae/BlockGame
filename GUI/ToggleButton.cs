using System.Drawing;

namespace BlockGame;

public class ToggleButton : Button {

    private List<string> states = [];
    private int index = 0;

    public ToggleButton(Screen screen, RectangleF guiPosition, params string[] states) : base(screen, guiPosition) {
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