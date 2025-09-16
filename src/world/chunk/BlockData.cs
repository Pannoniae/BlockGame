namespace BlockGame.world.chunk;

public interface BlockData {
    public ushort this[int x, int y, int z] { get; set; }
    
    public byte getMetadata(int x, int y, int z);
    
    public void setMetadata(int x, int y, int z, byte val);
    
    public uint getRaw(int x, int y, int z);

    public byte getLight(int x, int y, int z);

    public byte skylight(int x, int y, int z);

    public byte blocklight(int x, int y, int z);

    public void setSkylight(int x, int y, int z, byte val);

    public void setBlocklight(int x, int y, int z, byte val);

    public bool inited { get; }
}