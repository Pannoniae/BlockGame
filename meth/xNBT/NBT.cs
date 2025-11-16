using K4os.Compression.LZ4.Streams;

namespace BlockGame.util.xNBT;

public static class NBT {
    public static NBTCompound readCompressed(Stream stream) {
        using var decompress = LZ4Stream.Decode(stream);
        using var reader = new BinaryReader(decompress);
        var nbt = NBTTag.read(reader);
        if (nbt is NBTCompound compound) {
            
            // root tag can't have a name
            if (!string.IsNullOrEmpty(compound.name)) {
                throw new IOException("Root tag must not have a name!");
            }
            
            return compound;
        }
        throw new IOException("Root tag must be a compound!");
    }

    public static void writeCompressed(NBTCompound nbt, Stream stream) {
        
        // root tag can't have a name
        if (!string.IsNullOrEmpty(nbt.name)) {
            throw new IOException("Root tag must not have a name!");
        }
        
        using var compress = LZ4Stream.Encode(stream);
        using var writer = new BinaryWriter(compress);
        NBTTag.write(nbt, writer);
    }
    
    public static void write(NBTTag nbt, Stream stream) {
        
        // root tag can't have a name if compound
        if (nbt is NBTCompound compound && !string.IsNullOrEmpty(compound.name)) {
            throw new IOException("Root tag must not have a name!");
        }
        
        using var writer = new BinaryWriter(stream);
        NBTTag.write(nbt, writer);
    }
    
    public static NBTTag read(Stream stream) {
        using var reader = new BinaryReader(stream);
        var nbt = NBTTag.read(reader);
        
        // root tag can't have a name if compound
        if (nbt is NBTCompound compound && !string.IsNullOrEmpty(compound.name)) {
            throw new IOException("Root tag must not have a name!");
        }
        
        //if (nbt is NBTCompound compound) {
        //    return compound;
        //}
        //throw new IOException("Root tag must be a compound!");
        return nbt;
    }
    
    /** Convenience method, copies the data. */
    public static byte[] write(NBTTag nbt) {
        using var stream = new MemoryStream();
        write(nbt, stream);
        return stream.ToArray();
    }
    
    /** Convenience method, copies the data. */
    public static NBTTag read(ReadOnlySpan<byte> data) {
        using var stream = new MemoryStream(data.ToArray());
        return read(stream);
    }

    public static void writeFile(NBTCompound nbt, string name) {
        // we don't need it to complete immediately anyway....
        // todo disable this until saving is fixed
        //_ = Task.Run(() => saveFileAsync(nbt, name));
        using var stream = new FileStream(name, FileMode.Create, FileAccess.Write, FileShare.Read);
        writeCompressed(nbt, stream);
    }

    private static void saveFileAsync(NBTCompound nbt, string name) {
        using var stream = new FileStream(name, FileMode.Create, FileAccess.Write, FileShare.Read);
        writeCompressed(nbt, stream);
    }

    public static NBTCompound readFile(string name) {
        using var stream = new FileStream(name, FileMode.Open, FileAccess.Read, FileShare.Read);
        return readCompressed(stream);
    }
}