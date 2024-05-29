using System.Numerics;
using Silk.NET.Maths;

namespace BlockGame;

public class ToggleButton : Button {

    private List<string> states;
    private int index;

    public ToggleButton(Screen screen, string name, Vector2D<int> pos, bool wide, int initialState, params string[] states) : base(screen, name, pos, wide) {
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