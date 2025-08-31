namespace BlockGame.util;

/**
 * This exists so we don't just call random game methods in the input handler. That's processed in DoEvents() before the game loop!
 * Calling stuff outside the game loop can cause fuckups.
 */
public class InputTracker {
    public bool w;
    public bool a;
    public bool s;
    public bool d;

    public bool left;
    public bool right;
    public bool middle;


    public void reset() {
        w = false;
        a = false;
        s = false;
        d = false;
        left = false;
        right = false;
        middle = false;
    }
}