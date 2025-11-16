namespace BlockGame.util.xNBT;

/**
 * Simple XOR-based obfuscation for NBT files.
 * Hey stop looking at my playerdata with notepad ^^
 * @author Luna
 */
public static class NBTScrambler {
    private const byte KEY_BASE = 0x42;
    private const byte KEY_MULT = 0x73;

    /** scramble NBT data using XOR obfuscation */
    public static byte[] scramble(byte[] data) {
        var result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++) {
            byte key = (byte)((i * KEY_MULT + KEY_BASE) & 0xFF);
            result[i] = (byte)(data[i] ^ key);
        }
        return result;
    }

    /** unscramble NBT data (XOR is symmetric) */
    public static byte[] unscramble(byte[] data) {
        return scramble(data);
    }

    /** save scrambled NBT to .enbt file */
    public static void saveScrambled(string path, NBTCompound nbt) {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        NBTTag.write(nbt, writer);

        var rawData = ms.ToArray();
        var scrambled = scramble(rawData);

        File.WriteAllBytes(path, scrambled);
    }

    /** load scrambled NBT from .enbt file */
    public static NBTCompound loadScrambled(string path) {
        var scrambled = File.ReadAllBytes(path);
        var rawData = unscramble(scrambled);

        using var ms = new MemoryStream(rawData);
        using var reader = new BinaryReader(ms);
        return (NBTCompound)NBTTag.read(reader);
    }
}