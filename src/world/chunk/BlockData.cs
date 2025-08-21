namespace BlockGame;

public interface BlockData {
    public ushort this[int x, int y, int z] { get; set; }
    
    public byte getMetadata(int x, int y, int z);
    
    public void setMetadata(int x, int y, int z, byte val);

    public byte getLight(int x, int y, int z);

    public byte skylight(int x, int y, int z);

    public byte blocklight(int x, int y, int z);

    public void setSkylight(int x, int y, int z, byte val);

    public void setBlocklight(int x, int y, int z, byte val);
}