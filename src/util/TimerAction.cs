namespace BlockGame.util;

public sealed record TimerAction(int id, Action action, double lastCalled, bool repeating, long interval) {

    /**
     * I'm not *entirely* sure why equality is weird, but I'll give them an ID just in case
     */
    public readonly int id = id;
    
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
        return id == other?.id;
    }

    public override int GetHashCode() {
        return id.GetHashCode();
    }
}