using BlockGame.src.ui.element;
using BlockGame.util;
using Molten;
using Silk.NET.Input;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.ui;

public class ControlsMenu : Menu {
    private readonly SettingsScreen parentScreen;
    
    
    public InputButton? awaitingInput;

    public ControlsMenu(SettingsScreen parentScreen) {
        this.parentScreen = parentScreen;
        
        var elements = new List<GUIElement>();
        
        foreach (var input in InputTracker.all) {
            var button = new InputButton(this, input, input.name, false, input.ToString());
            button.centreContents();
            button.verticalAnchor = VerticalAnchor.TOP;
            elements.Add(button);
            addElement(button);
        }
        
        layoutSettings(elements, new Vector2I(0, 16));
    }
    
    public override void clear(double dt, double interp) {
        Game.graphics.clearColor(Color4b.SlateGray);
        Game.graphics.clearDepth();
        Game.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public override void onKeyDown(IKeyboard keyboard, Key key, int scancode) {
        if (key == Key.Escape) {
            parentScreen.returnToPrevScreen();
        }
        
        if (awaitingInput != null) {
            var input = awaitingInput.input;
            // assign to the input
            input.bind(key);
            
            awaitingInput.text = input != InputTracker.DUMMYINPUT ? input.ToString() : "Unbound";
            
            awaitingInput = null;
        }
    }


    public override void onMouseDown(IMouse mouse, MouseButton button) {
        if (awaitingInput != null) {
            var input = awaitingInput.input;
            // assign to the input
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