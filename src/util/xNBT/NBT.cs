using K4os.Compression.LZ4.Streams;

namespace BlockGame.util.xNBT;

public class NBT {
    public static NBTTagCompound readCompressed(Stream stream) {
        using var decompress = LZ4Stream.Decode(stream);
        using var reader = new BinaryReader(decompress);
        var nbt = NBTTag.read(reader);
        if (nbt is NBTTagCompound compound) {
            return compound;
        }
        throw new IOException("Root tag must be a compound!");
    }

    public static void writeCompressed(NBTTagCompound nbt, Stream stream) {
        using var compress = LZ4Stream.Encode(stream);
        using var writer = new BinaryWriter(compress);
        NBTTag.write(nbt, writer);
    }

    public static void writeFile(NBTTagCompound nbt, string name) {
        using var stream = new FileStream(name, FileMode.Create, FileAccess.Write);
        writeCompressed(nbt, stream);
    }

    public static NBTTagCompound readFile(string name) {
        using var stream = new FileStream(name, FileMode.Open, FileAccess.Read);
        return readCompressed(stream);
    }
}