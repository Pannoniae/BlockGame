namespace BlockGame.util;

public class TimerAction {
    public Action action;
    public double lastCalled;
    public bool repeating;
    /// <summary>
    /// Interval (in milliseconds)
    /// </summary>
    public long interval;

    /// <summary>
    /// Should this action happen or is it disabled? (disabled timers are scheduled for deletion in the queue)
    /// </summary>
    public bool enabled;

    public TimerAction(Action action, double lastCalled, bool repeating, long interval) {
        this.action = action;
        this.lastCalled = lastCalled;
        this.repeating = repeating;
        this.interval = interval;
        enabled = true;
    }
}