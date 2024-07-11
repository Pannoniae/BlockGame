namespace BlockGame;

public readonly record struct LightNode(int x, int y, int z, Chunk chunk, bool noRemesh = false) {
    public readonly int x = x;
    public readonly int y = y;
    public readonly int z = z;
    public readonly Chunk chunk = chunk;
    public readonly bool noRemesh = noRemesh;
}

// noRemesh = true means that the chunk will not be remeshed after the light is added
public readonly record struct LightRemovalNode(int x, int y, int z, byte value, Chunk chunk, bool noRemesh = false) {
    public readonly int x = x;
    public readonly int y = y;
    public readonly int z = z;
    public readonly byte value = value;
    public readonly Chunk chunk = chunk;
    public readonly bool noRemesh = noRemesh;
}