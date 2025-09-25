using System.Numerics;
using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.ui.screen;
using BlockGame.util;
using Molten;
using Silk.NET.Input;
using Silk.NET.OpenGL.Legacy;
using Rectangle = System.Drawing.Rectangle;

namespace BlockGame.ui.menu.settings;

public class ControlsMenu : Menu {
    private readonly SettingsScreen parentScreen;


    // gui coords!
    private float scrollY = 0f;
    private float minScrollY = 0f;
    private float maxScrollY = 0f;
    private const float SCROLL_SPEED = 20f;

    public InputButton? awaitingInput;

    public ControlsMenu(SettingsScreen parentScreen) {
        this.parentScreen = parentScreen;
        
        var elements = new List<GUIElement>();
        
        var settings = Settings.instance;
        var mouseInv = new ToggleButton(this, "mouseInv", false, settings.mouseInv == 1 ? 0 : 1,
            "Mouse Wheel: Normal", "Mouse Wheel: Inverted");
        mouseInv.centreContents();
        mouseInv.verticalAnchor = VerticalAnchor.TOP;
        mouseInv.clicked += _ => {
            settings.mouseInv = mouseInv.getIndex() == 1 ? -1 : 1;
        };
        elements.Add(mouseInv);
        addElement(mouseInv);

        for (int i = 0; i < 10; i++) {
            foreach (var input in InputTracker.all) {
                var button = new InputButton(this, input, input.name + i, false, input.ToString());
                button.centreContents();
                button.verticalAnchor = VerticalAnchor.TOP;
                elements.Add(button);
                addElement(button);
            }
        }

        layoutSettings(elements, new Vector2I(0, 16));
    }

    public override void resize(Vector2I newSize) {
        base.resize(newSize);
        // calculate scroll bounds
        calculateScrollBounds(newSize);
    }

    private void calculateScrollBounds(Vector2I newSize) {
        if (elements.Count == 0) {
            return;
        }

        var maxY = elements.Values.Max(e => e.GUIbounds.Bottom);
        var minY = elements.Values.Min(e => e.GUIbounds.Top);
        var viewportHeight = (newSize.Y / GUI.guiScale) - 32; // convert to GUI pixels and account for margins

        //Console.Out.WriteLine($"Viewport height: {viewportHeight}, maxY: {maxY}, minY: {minY}");
        var totalContentHeight = maxY - minY;
        maxScrollY = Math.Max(0, totalContentHeight - viewportHeight);

        //Console.WriteLine($"calculateScrollBounds: maxY={maxY}, viewportHeight={viewportHeight}, maxScrollY={maxScrollY}, elementCount={elements.Count}");
    }

    public override void scroll(IMouse mouse, ScrollWheel scroll) {
        var oldScrollY = scrollY;
        scrollY -= scroll.Y * SCROLL_SPEED; // scroll down = positive scrollY
        scrollY = Math.Max(minScrollY, Math.Min(maxScrollY, scrollY));
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
        
        // adjust mouse position for scrolling
        var originalPos = Game.mousePos;
        Game.mousePos += new Vector2(0, scrollY * GUI.guiScale);
        base.onMouseDown(mouse, button);
        Game.mousePos = originalPos;
    }

    public override void onMouseUp(Vector2 pos, MouseButton button) {
        // adjust mouse position for scrolling
        var adjustedPos = pos + new Vector2(0, scrollY * GUI.guiScale);
        base.onMouseUp(adjustedPos, button);
    }

    public override void onMouseMove(IMouse mouse, Vector2 pos) {
        // adjust mouse position for scrolling
        var adjustedPos = pos + new Vector2(0, scrollY * GUI.guiScale);
        base.onMouseMove(mouse, adjustedPos);
    }

    public override void draw() {
        Game.gui.drawBG(16);

        Game.graphics.mainBatch.End();
        Game.graphics.mainBatch.Begin();

        // apply scissoring to clip content outside viewport
        const int viewportY = 16; // start position accounting for margins
        var viewportH = size.Y / GUI.guiScale - 32; // account for top/bottom margins
        Game.graphics.scissorUI(0, viewportY, size.X / GUI.guiScale, viewportH);

        //Console.Out.WriteLine("SizeX: " + size.X + ", SizeY: " + size.Y);

        // manually offset element positions for scrolling
        var bounds = new Dictionary<string, Rectangle>();
        foreach (var element in elements.Values) {
            bounds[element.name] = element.guiPosition;
            element.guiPosition = new Rectangle(element.guiPosition.X, element.guiPosition.Y - (int)scrollY,
                                         element.guiPosition.Width, element.guiPosition.Height);
        }

        base.draw();

        // very important part! flush the draw calls before disabling scissor because it will get executed way later otherwise!
        Game.graphics.mainBatch.End();
        Game.graphics.mainBatch.Begin();

        // restore
        foreach (var element in elements.Values) {
            element.guiPosition = bounds[element.name];
        }

        Game.graphics.noScissor();

        // draw scrollbar if needed
        if (maxScrollY > 0) {
            var scrollbarX = size.X / GUI.guiScale - 16; // right side with padding
            var scrollbarY = viewportY;
            var scrollbarH = viewportH;
            var trackH = scrollbarH;

            // calculate thumb position and size
            var thumbHeight = Math.Max(10, (int)(trackH * (viewportH / (viewportH + maxScrollY))));
            var thumbY = scrollbarY + (int)((trackH - thumbHeight) * (scrollY / maxScrollY));

            // draw scrollbar track using 3-patch
            // top cap
            Game.gui.drawUI(Game.gui.guiTexture,
                new Rectangle(scrollbarX, scrollbarY, 6, 3),
                new Rectangle(Game.gui.scrollbarRect.X, Game.gui.scrollbarRect.Y, 6, 3), Color4b.DimGray);

            // middle (stretched)
            if (scrollbarH > 6) {
                Game.gui.drawUI(Game.gui.guiTexture,
                    new Rectangle(scrollbarX, scrollbarY + 3, 6, scrollbarH - 6),
                    new Rectangle(Game.gui.scrollbarRect.X, Game.gui.scrollbarRect.Y + 3, 6, 14), Color4b.DimGray);
            }

            // bottom cap
            Game.gui.drawUI(Game.gui.guiTexture,
                new Rectangle(scrollbarX, scrollbarY + scrollbarH - 3, 6, 3),
                new Rectangle(Game.gui.scrollbarRect.X, Game.gui.scrollbarRect.Y + 17, 6, 3), Color4b.DimGray);

            // draw scrollbar thumb using 3-patch (top 3px, stretched middle, bottom 3px)
            // top cap (first 3 pixels)
            Game.gui.drawUI(Game.gui.guiTexture,
                new Rectangle(scrollbarX, thumbY, 6, 3),
                new Rectangle(Game.gui.scrollbarRect.X, Game.gui.scrollbarRect.Y, 6, 3));

            // middle section (stretched)
            Game.gui.drawUI(Game.gui.guiTexture,
                new Rectangle(scrollbarX, thumbY + 3, 6, thumbHeight - 6),
                new Rectangle(Game.gui.scrollbarRect.X, Game.gui.scrollbarRect.Y + 3, 6, 14));

            // bottom cap (last 3 pixels)
            Game.gui.drawUI(Game.gui.guiTexture,
                new Rectangle(scrollbarX, thumbY + thumbHeight - 3, 6, 3),
                new Rectangle(Game.gui.scrollbarRect.X, Game.gui.scrollbarRect.Y + 17, 6, 3));
        }
    }
}