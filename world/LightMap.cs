namespace BlockGame;

public class LightMap {
    /// <summary>
    /// Skylight is on the lower 4 bits, blocklight is on the upper 4 bits.
    /// </summary>
    public byte[,,] light;
    public Chunk chunk;

    public LightMap(Chunk chunk) {
        this.chunk = chunk;
    }

    public byte skylight(int x, int y, int z) {
        var value = light[x, y, z];
        return (byte)(value & 0xF);
    }

    public byte blocklight(int x, int y, int z) {
        var value = light[x, y, z];
        return (byte)((value & 0xF) >> 4);
    }

    public void setSkylight(int x, int y, int z, byte val) {
        var value = light[x, y, z];
        var blocklight = (byte)((value & 0xF) >> 4);
        // pack it back inside
        light[x, y, z] = (byte)(blocklight | (val & 0xF) << 4);
    }

    public void setBlocklight(int x, int y, int z, byte val) {
        var value = light[x, y, z];
        var skylight = (byte)(value & 0xF);
        // pack it back inside
        light[x, y, z] = (byte)(val | (skylight & 0xF) << 4);
    }
}