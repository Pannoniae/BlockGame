using BlockGame.world.chunk;

namespace BlockGame.world;

public readonly record struct LightNode(int x, int y, int z, Chunk chunk) {
    public readonly int x = x;
    public readonly int y = y;
    public readonly int z = z;
    public readonly Chunk chunk = chunk;
}

public readonly record struct LightRemovalNode(int x, int y, int z, byte value, Chunk chunk) {
    public readonly int x = x;
    public readonly int y = y;
    public readonly int z = z;
    public readonly byte value = value;
    public readonly Chunk chunk = chunk;
}