namespace BlockGame;

public class World {
    // later an array/mapping of chunks
    public Chunk chunk;

    public World() {
        chunk = new Chunk(this);
    }

    public bool isBlock(int x, int y, int z) {
        if (!inWorld(x, y, z)) {
            return false;
        }

        return getChunk(x, y, z)!.block[x, y, z] != 0;
    }

    private bool inWorld(int x, int y, int z) {
        return x is >= 0 and < Chunk.CHUNKSIZE &&
               y is >= 0 and < Chunk.CHUNKSIZE &&
               z is >= 0 and < Chunk.CHUNKSIZE;
    }

    private Chunk? getChunk(int x, int y, int z) {
        return chunk;
    }

    public void mesh() {
        chunk.meshWorld();
    }

    public void draw() {
        chunk.drawWorld();
    }
}