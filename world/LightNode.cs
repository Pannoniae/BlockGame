namespace BlockGame;

public readonly record struct LightNode(int x, int y, int z, Chunk chunk) {
    public readonly int x = x;
    public readonly int y = y;
    public readonly int z = z;
    public readonly Chunk chunk = chunk;

}