namespace BlockGame.world;

public readonly record struct LightNode(int x, int y, int z) {
    public readonly int x = x;
    public readonly int y = y;
    public readonly int z = z;
}

public readonly record struct LightRemovalNode(int x, int y, int z, byte value) {
    public readonly int x = x;
    public readonly int y = y;
    public readonly int z = z;
    public readonly byte value = value;
}
