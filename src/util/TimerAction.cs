namespace BlockGame.util;

public sealed record TimerAction(Action action, double lastCalled, bool repeating, long interval) {
    public readonly Action action = action;
    public double lastCalled = lastCalled;
    public bool repeating = repeating;
    /// <summary>
    /// Interval (in milliseconds)
    /// </summary>
    public long interval = interval;

    /// <summary>
    /// Should this action happen or is it disabled? (disabled timers are scheduled for deletion in the queue)
    /// </summary>
    public bool enabled = true;

    public bool Equals(TimerAction? other) {
        return ReferenceEquals(this, other);
    }

    public override int GetHashCode() {
        return action.GetHashCode();
    }
}