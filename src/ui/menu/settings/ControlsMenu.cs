using BlockGame.util;
using Silk.NET.Input;
using Silk.NET.OpenGL.Legacy;

namespace BlockGame.ui;

public class ControlsMenu : Menu {
    private readonly SettingsScreen parentScreen;

    public ControlsMenu(SettingsScreen parentScreen) {
        this.parentScreen = parentScreen;
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
    }

    public override void draw() {
        Game.gui.drawBG(16);
        base.draw();
    }
}