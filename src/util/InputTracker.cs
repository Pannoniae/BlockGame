using BlockGame.main;
using Silk.NET.Input;

namespace BlockGame.util;

/**
 * This exists so we don't just call random game methods in the input handler. That's processed in DoEvents() before the game loop!
 * Calling stuff outside the game loop can cause fuckups.
 *
 * We use the same negative mapping trick, where mouse buttons are negative numbers, to distinguish from keyboard keys. I'm lazy, OK?
 *
 * This works for both mouse and kb!
 * Will do controller stuff later.
 */
public class InputTracker {
    public Input w;
    public Input a;
    public Input s;
    public Input d;
    public Input space;
    public Input shift;
    public Input ctrl;

    public Input left;
    public Input right;
    public Input middle;
    
    public static Input DUMMYINPUT;

    public static List<Input> all = [];

    public static int DUMMY = -999;

    private static Dictionary<int, bool> pressedKeys = new();
    private static Dictionary<int, bool> releasedKeys = new();
    private static Dictionary<int, bool> previousFrameKeys = new();

    public InputTracker() {
        
        w = new Input("Forward", (int)Key.W);
        a = new Input("Left", (int)Key.A);
        s = new Input("Backward", (int)Key.S);
        d = new Input("Right", (int)Key.D);
        space = new Input("Jump", (int)Key.Space);
        shift = new Input("Sneak", (int)Key.ShiftLeft);
        ctrl = new Input("Sprint", (int)Key.ControlLeft);
        
        left = new Input("Attack", -(int)MouseButton.Left);
        right = new Input("Use", -(int)MouseButton.Right);
        middle = new Input("Pick Block", -(int)MouseButton.Middle);
        
        DUMMYINPUT = new Input("Unbound", DUMMY);
    }

    public static bool pressed(int key) {
        return pressedKeys.GetValueOrDefault(key, false);
    }

    public static bool down(int key) {
        if (key <= 0) {
            var mouseButton = (MouseButton)(-key);
            return Game.mouse.IsButtonPressed(mouseButton);
        }
        
        return Game.keyboard.IsKeyPressed((Key)key);
    }

    public static bool released(int key) {
        return releasedKeys.GetValueOrDefault(key, false);
    }

    public static string getKeyName(int key) {
        if (key == DUMMY) return "unbound";

        if (key <= 0) {
            var mouseButton = (MouseButton)(-key);
            return mouseButton.ToString();
        }

        return ((Key)key).ToString();
    }

    public void reset() {
        pressedKeys.Clear();
        releasedKeys.Clear();

        // update pressed/released states for keyboard keys
        foreach (var key in Enum.GetValues<Key>()) {
            
            if (key == Key.Unknown) {
                continue;
            }

            var keyInt = (int)key;
            var currentlyDown = Game.keyboard.IsKeyPressed(key);
            var wasDown = previousFrameKeys.GetValueOrDefault(keyInt, false);

            if (currentlyDown && !wasDown) {
                pressedKeys[keyInt] = true;
            }
            else if (!currentlyDown && wasDown) {
                releasedKeys[keyInt] = true;
            }

            previousFrameKeys[keyInt] = currentlyDown;
        }

        // update pressed/released states for mouse buttons
        foreach (var button in Enum.GetValues<MouseButton>()) {
            
            if (button == MouseButton.Unknown) {
                continue;
            }
            
            var buttonInt = -(int)button;
            var currentlyDown = Game.mouse.IsButtonPressed(button);
            var wasDown = previousFrameKeys.GetValueOrDefault(buttonInt, false);

            if (currentlyDown && !wasDown) {
                pressedKeys[buttonInt] = true;
            }
            else if (!currentlyDown && wasDown) {
                releasedKeys[buttonInt] = true;
            }

            previousFrameKeys[buttonInt] = currentlyDown;
        }
    }

    public static Input get(Key key) {
        foreach (var input in all) {
            if (input.key == (int)key) {
                return input;
            }
        }

        return DUMMYINPUT;
    }
}