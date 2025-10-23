using System.Numerics;
using BlockGame.main;
using BlockGame.ui.element;
using BlockGame.util;
using Molten;
using Silk.NET.Input;

namespace BlockGame.ui.menu;

public abstract class ScrollableMenu : Menu {
    private readonly HashSet<GUIElement> scrollables = [];

    private float scrollY = 0f;
    private float minScrollY = 0f;
    private float maxScrollY = 0f;
    private const float SCROLL_SPEED = 20f;

    // scrollbar drag state
    private bool draggingScrollbar = false;
    private float scrollbarDragOffset = 0f; // offset from thumb top to click position

    // viewport bounds in GUI coords
    protected virtual int viewportY => 16;
    protected virtual int viewportMargin => 32; // top + bottom

    protected void addScrollable(GUIElement element) {
        scrollables.Add(element);
        addElement(element);
    }

    protected void removeScrollable(GUIElement element) {
        scrollables.Remove(element);
        removeElement(element.name);
    }

    public override void resize(Vector2I newSize) {
        base.resize(newSize);
        calculateScrollBounds();
    }

    private void calculateScrollBounds() {
        if (scrollables.Count == 0) return;

        var maxY = scrollables.Max(e => e.GUIbounds.Bottom);
        var minY = scrollables.Min(e => e.GUIbounds.Top);
        var viewportH = (size.Y / GUI.guiScale) - viewportMargin;

        var totalContentHeight = maxY - minY;
        maxScrollY = Math.Max(0, totalContentHeight - viewportH);
    }

    public override void scroll(IMouse mouse, ScrollWheel scroll) {
        scrollY -= scroll.Y * SCROLL_SPEED;
        scrollY = Math.Max(minScrollY, Math.Min(maxScrollY, scrollY));
    }

    // get scrollbar thumb bounds in screen coords
    private Rectangle? getScrollbarThumbBounds() {
        if (maxScrollY <= 0) return null;

        var viewportH = size.Y / GUI.guiScale - viewportMargin;
        var scrollbarX = size.X / GUI.guiScale - 16;
        var viewportRatio = viewportH / (viewportH + maxScrollY);
        var scrollProgress = scrollY / maxScrollY;

        const int WIDTH = 6;
        var thumbHeight = Math.Max(10, (int)(viewportH * viewportRatio));
        var thumbY = viewportY + (int)((viewportH - thumbHeight) * scrollProgress);

        // convert to screen coords
        return new Rectangle(
            scrollbarX * GUI.guiScale,
            thumbY * GUI.guiScale,
            WIDTH * GUI.guiScale,
            thumbHeight * GUI.guiScale
        );
    }

    // get screen-space bounds with scroll applied
    private Rectangle getEffectiveBounds(GUIElement e) {
        if (scrollables.Contains(e)) {
            // apply scroll offset in screen space
            return e.bounds with { Y = e.bounds.Y - (int)(scrollY * GUI.guiScale) };
        }
        // fixed elements use normal bounds
        return e.bounds;
    }

    public override void onMouseDown(IMouse mouse, MouseButton button) {
        
        // check if clicking on scrollbar thumb
        var thumbBounds = getScrollbarThumbBounds();
        if (thumbBounds.HasValue && button == MouseButton.Left && thumbBounds.Value.Contains((int)Game.mousePos.X, (int)Game.mousePos.Y)) {
            draggingScrollbar = true;
            scrollbarDragOffset = Game.mousePos.Y - thumbBounds.Value.Top;
            return;
        }

        // find element under cursor using effective bounds
        GUIElement? target = null;
        foreach (var element in elements.Values) {
            if (element.active && getEffectiveBounds(element).Contains((int)Game.mousePos.X, (int)Game.mousePos.Y)) {
                target = element;
                break;
            }
        }

        // only the target element receives the event
        if (target != null) {
            target.onMouseDown(button);
            if (button is MouseButton.Left or MouseButton.Right) {
                playClick();
                if (button is MouseButton.Left) {
                    pressedElement = target;
                }
            }
        }
    }

    public override void onMouseUp(Vector2 pos, MouseButton button) {
        // end scrollbar drag
        if (draggingScrollbar && button == MouseButton.Left) {
            draggingScrollbar = false;
            return;
        }

        if (pressedElement != null) {
            // captured element gets the release
            pressedElement.onMouseUp(button);
            if (pressedElement.active) {
                pressedElement.click(button);
            }
            if (button is MouseButton.Left or MouseButton.Right) {
                playRelease();
                if (button is MouseButton.Left) {
                    pressedElement = null;
                }
            }
        } else {
            // no capture - check for element under cursor using effective bounds
            foreach (var element in elements.Values) {
                if (element.active && getEffectiveBounds(element).Contains((int)pos.X, (int)pos.Y)) {
                    element.onMouseUp(button);
                    element.click(button);
                    if (button is MouseButton.Left or MouseButton.Right) {
                        playRelease();
                    }
                    break;
                }
            }
        }
    }

    public override void onMouseMove(IMouse mouse, Vector2 pos) {
        // handle scrollbar drag
        if (draggingScrollbar) {
            var viewportH = size.Y / GUI.guiScale - viewportMargin;
            var thumbBounds = getScrollbarThumbBounds();
            if (thumbBounds.HasValue) {
                var thumbHeight = thumbBounds.Value.Height;
                var trackHeight = viewportH * GUI.guiScale - thumbHeight;

                // calculate desired thumb top position
                var desiredThumbY = pos.Y - scrollbarDragOffset;
                var trackTop = viewportY * GUI.guiScale;

                // calculate scroll progress (0-1)
                var scrollProgress = float.Max(0, float.Min(1, (desiredThumbY - trackTop) / trackHeight));
                scrollY = scrollProgress * maxScrollY;
            }
            return;
        }

        if (pressedElement != null) {
            // captured element gets all moves
            pressedElement.onMouseMove();
        } else {
            // normal hover behaviour using effective bounds
            bool found = false;
            foreach (var element in elements.Values) {
                var effectiveBounds = getEffectiveBounds(element);
                element.hovered = effectiveBounds.Contains((int)pos.X, (int)pos.Y);
                if (effectiveBounds.Contains((int)pos.X, (int)pos.Y)) {
                    element.onMouseMove();
                    hoveredElement = element;
                    found = true;
                }
            }
            if (!found) {
                hoveredElement = null;
            }
        }
    }

    public override void update(double dt) {
        foreach (var element in elements.Values) {
            element.pressed = getEffectiveBounds(element).Contains((int)Game.mousePos.X, (int)Game.mousePos.Y) && Game.inputs.left.down();
            element.update();
        }
    }

    public override void draw() {
        // draw fixed elements (not in scrollables set)
        foreach (var element in elements.Values) {
            if (!scrollables.Contains(element) && element.active) {
                element.draw();
            }
        }

        // setup scissor for scrollable region
        var viewportH = size.Y / GUI.guiScale - viewportMargin;
        Game.graphics.mainBatch.End();
        Game.graphics.mainBatch.Begin();
        Game.graphics.scissorUI(0, viewportY, size.X / GUI.guiScale, viewportH);

        // draw scrollable elements with offset (no mutation!)
        var saved = new Dictionary<string, Rectangle>();
        foreach (var element in scrollables) {
            saved[element.name] = element.guiPosition;
            element.guiPosition = new Rectangle(
                element.guiPosition.X,
                element.guiPosition.Y - (int)scrollY,
                element.guiPosition.Width,
                element.guiPosition.Height
            );
        }

        foreach (var element in scrollables) {
            if (element.active) element.draw();
        }

        // restore positions
        foreach (var element in scrollables) {
            element.guiPosition = saved[element.name];
        }

        // cleanup scissor
        Game.graphics.mainBatch.End();
        Game.graphics.mainBatch.Begin();
        Game.graphics.noScissor();

        // draw scrollbar
        if (maxScrollY > 0) {
            var scrollbarX = size.X / GUI.guiScale - 16;
            var viewportRatio = viewportH / (viewportH + maxScrollY);
            var scrollProgress = scrollY / maxScrollY;
            Game.gui.drawScrollbarUI(scrollbarX, viewportY, viewportH, scrollProgress, viewportRatio);
        }
    }
}