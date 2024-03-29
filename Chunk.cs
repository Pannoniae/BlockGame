namespace BlockGame;

public class Chunk {
    public ushort[,,] block;
    public int x;
    public int z;
    public ChunkSection[] chunks;
    private World world;


    public Chunk(World world, Shader shader, int x, int z) {
        block = new ushort[ChunkSection.CHUNKSIZE, ChunkSection.CHUNKSIZE * CHUNKHEIGHT, ChunkSection.CHUNKSIZE];
        this.world = world;
        chunks = new ChunkSection[CHUNKHEIGHT];
        this.x = x;
        this.z = z;

        for (int i = 0; i < CHUNKHEIGHT; i++) {
            chunks[i] = new ChunkSection(world, this, shader, x, i, z);
        }
    }

    public void meshChunk() {
        for (int i = 0; i < CHUNKHEIGHT; i++) {
            chunks[i].meshChunk();
        }
    }

    public const int CHUNKHEIGHT = 8;

    public void drawChunk(Camera camera) {
        for (int i = 0; i < CHUNKHEIGHT; i++) {
            chunks[i].drawChunk(camera);
        }
    }

    public void drawOpaque(Camera camera) {
        for (int i = 0; i < CHUNKHEIGHT; i++) {
            chunks[i].drawOpaque(camera);
        }
    }

    public void drawTransparent(Camera camera) {
        for (int i = 0; i < CHUNKHEIGHT; i++) {
            chunks[i].drawTransparent(camera);
        }
    }
}