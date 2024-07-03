using BlockGame.util;

namespace BlockGame;

public class HeightMap : IDisposable {

    private static FixedArrayPool<byte> heightPool = new(Chunk.CHUNKSIZE * Chunk.CHUNKSIZE);

    public byte[] height;
    public Chunk chunk;

    public HeightMap(Chunk chunk) {
        this.chunk = chunk;
        height = heightPool.grab();
        Array.Clear(height);
    }

    public byte get(int x, int z) {
        return height[x * Chunk.CHUNKSIZE + z];
    }

    public void set(int x, int z, byte val) {
        // pack it back inside
        height[x * Chunk.CHUNKSIZE + z] = val;
    }
    private void ReleaseUnmanagedResources() {
        heightPool.putBack(height);
    }
    public void Dispose() {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
    ~HeightMap() {
        ReleaseUnmanagedResources();
    }
}