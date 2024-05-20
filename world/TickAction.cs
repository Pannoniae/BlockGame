namespace BlockGame;

public record TickAction(Action action, int tick) {
    /// <summary>
    /// Should this action happen or is it disabled? (disabled timers are scheduled for deletion in the queue)
    /// </summary>
    public bool enabled = true;
}