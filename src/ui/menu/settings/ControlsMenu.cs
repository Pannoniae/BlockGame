using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.ui.screen;
using BlockGame.util;
using Molten;
using Silk.NET.Input;
using Silk.NET.OpenGL.Legacy;
using Button = BlockGame.ui.element.Button;

namespace BlockGame.ui.menu.settings;

public class ControlsMenu : ScrollableMenu {
    private readonly SettingsScreen parentScreen;

    public InputButton? awaitingInput;

    public ControlsMenu(SettingsScreen parentScreen) {
        this.parentScreen = parentScreen;

        var settings = Settings.instance;
        var scrollables = new List<GUIElement>();

        var mouseInv = new ToggleButton(this, "mouseInv", false, settings.mouseInv == 1 ? 0 : 1,
            "Mouse Wheel: Normal", "Mouse Wheel: Inverted");
        mouseInv.centreContents();
        mouseInv.verticalAnchor = VerticalAnchor.TOP;
        mouseInv.clicked += _ => { settings.mouseInv = mouseInv.getIndex() == 1 ? -1 : 1; };
        scrollables.Add(mouseInv);
        addScrollable(mouseInv);

        foreach (var input in InputTracker.all) {
            var button = new InputButton(this, input, input.name, false, input.ToString());
            button.centreContents();
            button.verticalAnchor = VerticalAnchor.TOP;
            scrollables.Add(button);
            addScrollable(button);
        }

        layoutSettings(scrollables, new Vector2I(0, 16));

        // fixed back button (doesn't scroll)
        var back = new Button(this, "back", false, "Back") {
            horizontalAnchor = HorizontalAnchor.LEFT,
            verticalAnchor = VerticalAnchor.BOTTOM
        };
        back.setPosition(new Vector2I(2, -18));
        back.clicked += _ => { parentScreen.returnToPrevScreen(); };
        addElement(back);
    }

    public override void clear(double dt, double interp) {
        Game.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        if (key == Key.Escape) {
            parentScreen.returnToPrevScreen();
        }

        if (awaitingInput != null) {
            var input = awaitingInput.input;
            input.bind(key);
            awaitingInput.text = input != InputTracker.DUMMYINPUT ? input.ToString() : "Unbound";
            awaitingInput = null;
        }
    }

    public override void onMouseDown(IMouse mouse, MouseButton button) {
        if (awaitingInput != null) {
            var input = awaitingInput.input;
            input.bind(button);
            awaitingInput.text = input != InputTracker.DUMMYINPUT ? input.ToString() : "Unbound";
            awaitingInput = null;
        }

        base.onMouseDown(mouse, button);
    }

    public override void draw() {
        Game.gui.drawBG(16);
        base.draw();
    }
}