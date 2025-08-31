namespace BlockGame.util.xNBT;

/**
 * Anything which can be saved to and loaded from NBT data.
 */
public interface Persistent {
    /**
     * Loads the data from the given NBT compound.
     */
    public void read(NBTCompound data);
    
    /**
     * Writes the data to the given NBT compound.
     */
    public void write(NBTCompound data);
}