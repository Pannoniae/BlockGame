namespace BlockGame;

public class ArrayBlockData : BlockData {
    public ushort[,,] blocks;

    public ArrayBlockData() {
        blocks = new ushort[Chunk.CHUNKSIZE, Chunk.CHUNKSIZE, Chunk.CHUNKSIZE];
    }
    public ushort this[int x, int y, int z] {
        get => blocks[x, y, z];
        set => blocks[x, y, z] = value;
    }
}