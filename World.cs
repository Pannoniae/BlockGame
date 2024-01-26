using Silk.NET.Maths;

namespace BlockGame;

public class World {
    private const int WORLDSIZE = 6;
    private const int WORLDHEIGHT = 3;

    public Chunk[,,] chunks;

    public World() {
        chunks = new Chunk[WORLDSIZE, WORLDHEIGHT, WORLDSIZE];
        for (int x = 0; x < WORLDSIZE; x++) {
            for (int y = 0; y < WORLDHEIGHT; y++) {
                for (int z = 0; z < WORLDSIZE; z++) {
                    chunks[x, y, z] = new Chunk(this, x, y, z);
                }
            }
        }

        Console.Out.WriteLine(isBlock(0, 3 * Chunk.CHUNKSIZE - 1, 0));
        Console.Out.WriteLine(isBlock(0, 3 * Chunk.CHUNKSIZE, 0));
        // separate loop so all data is there
        for (int x = 0; x < WORLDSIZE; x++) {
            for (int y = 0; y < WORLDHEIGHT; y++) {
                for (int z = 0; z < WORLDSIZE; z++) {
                    chunks[x, y, z].meshChunk();
                }
            }
        }
    }

    public bool isBlock(int x, int y, int z) {
        if (!inWorld(x, y, z)) {
            return false;
        }

        var blockPos = getBlockPos(x, y, z);
        var chunkPos = getChunkPos(x, y, z);
        return getChunk(chunkPos.X, chunkPos.Y, chunkPos.Z)!.block[blockPos.X, blockPos.Y, blockPos.Z] != 0;
    }

    private bool inWorld(int x, int y, int z) {
        var chunkpos = getChunkPos(x, y, z);
        return chunkpos.X is >= 0 and < WORLDSIZE &&
               chunkpos.Y is >= 0 and < WORLDHEIGHT &&
               chunkpos.Z is >= 0 and < WORLDSIZE;
    }

    private Vector3D<int> getChunkPos(int x, int y, int z) {
        return new Vector3D<int>(
            (int)MathF.Floor(x / (float)Chunk.CHUNKSIZE),
            (int)MathF.Floor(y / (float)Chunk.CHUNKSIZE),
            (int)MathF.Floor(z / (float)Chunk.CHUNKSIZE));
    }
    
    private Vector3D<int> getBlockPos(int x, int y, int z) {
        return new Vector3D<int>(
            x % Chunk.CHUNKSIZE,
            y % Chunk.CHUNKSIZE,
            z % Chunk.CHUNKSIZE);
    }

    private Chunk getChunk(int x, int y, int z) {
        var pos = getChunkPos(x, y, z);
        return chunks[pos.X, pos.Y, pos.Z];
    }

    public void mesh() {
        foreach (var chunk in chunks) {
            chunk.meshChunk();
        }
    }

    public void draw() {
        foreach (var chunk in chunks) {
            chunk.drawChunk();
        }
    }

    public Vector3D<int> toWorldPos(int chunkX, int chunkY, int chunkZ, int x, int y, int z) {
        return new Vector3D<int>(chunkX * Chunk.CHUNKSIZE + x,
            chunkY * Chunk.CHUNKSIZE + y,
            chunkZ * Chunk.CHUNKSIZE + z);
    }
}