namespace BlockGame;

public readonly record struct TickAction(Action action, int tick);