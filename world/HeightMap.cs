namespace BlockGame;

public class HeightMap {
    /// <summary>
    /// Skylight is on the lower 4 bits, blocklight is on the upper 4 bits.
    /// Stored in YZX order.
    /// </summary>
    public byte[] height;
    public Chunk chunk;

    public HeightMap(Chunk chunk) {
        this.chunk = chunk;
        height = new byte[Chunk.CHUNKSIZE * Chunk.CHUNKSIZE];
    }

    public byte get(int x, int z) {
        var value = height[
                          x * Chunk.CHUNKSIZE +
                          z];
        return (byte)(value);
    }

    public void set(int x, int z, byte val) {
        // pack it back inside
        height[x * Chunk.CHUNKSIZE + z] = val;
    }
}