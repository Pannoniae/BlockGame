using BlockGame.main;
using BlockGame.ui.menu;
using BlockGame.ui.screen;
using BlockGame.util;
using Silk.NET.Input;

namespace BlockGame.ui.element;

public class InputButton : Button {
    public Input input;
    
    public InputButton(Menu menu, Input input, string name, bool wide, string? text = default) : base(menu, name, wide, text) {
        this.input = input;
        this.text = input.ToString();
    }
    
    public override void onMouseDown(MouseButton button) {
        if (button == MouseButton.Right && SettingsScreen.CONTROLS_MENU.awaitingInput == null) {
            text = "Unbound";
            SettingsScreen.CONTROLS_MENU.awaitingInput = null;
            return;
        }

        // don't click if awaiting input
        if (SettingsScreen.CONTROLS_MENU.awaitingInput != null) {
            return;
        }

        if (button == MouseButton.Left) {
            text = "Press a key...";
            SettingsScreen.CONTROLS_MENU.awaitingInput = this;
            return;
        }
    }

    public override void draw() {
        base.draw();
    }
}