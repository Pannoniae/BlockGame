namespace BlockGame.net.packet;

/**
 * pooled packet writer to avoid MemoryStream/BinaryWriter allocations
 * todo if we'll have multiple threads, we'll probably need threadlocal storage here
 */
public static class PacketWriter {
    private static MemoryStream? stream;
    private static BinaryWriter? writer;

    /** get a pooled writer, ready to use (stream is reset to position 0) */
    public static PacketBuffer get() {
        if (stream == null) {
            stream = new MemoryStream(1024); // start with 1KB, it will grow as needed
            writer = new BinaryWriter(stream);
        }

        // reset stream position for reuse
        stream.Position = 0;
        stream.SetLength(0);

        return new PacketBuffer(writer);
    }

    /** get the written bytes (after writing to the buffer) */
    public static byte[] getBytes() {
        // return copy of the data (caller owns it)
        return stream == null ? [] : stream.ToArray();
    }

    /** get bytes without copying (faster but caller must not modify or store the array) */
    public static Span<byte> getBytesUnsafe() {
        // return segment of internal buffer (no allocation)
        return stream == null ? [] : stream.GetBuffer().AsSpan(0, (int)stream.Length);
    }
}