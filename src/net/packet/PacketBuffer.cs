using System.Buffers.Binary;
using System.Numerics;
using BlockGame.util;
using BlockGame.util.xNBT;
using BlockGame.world.chunk;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.net.packet;

/**
 * Some day we'll have a clean version this without if-else hell. Today is not that day though.
 */
public ref struct PacketBuffer {
    private readonly BinaryWriter? writer;
    private readonly BinaryReader? reader;

    // span-based reading
    private readonly ReadOnlySpan<byte> span;
    private int pos;

    public PacketBuffer(BinaryWriter writer) {
        this.writer = writer;
        this.reader = null;
        this.span = default;
        this.pos = 0;
    }

    public PacketBuffer(BinaryReader reader) {
        this.writer = null;
        this.reader = reader;
        this.span = default;
        this.pos = 0;
    }

    public PacketBuffer(ReadOnlySpan<byte> span) {
        this.writer = null;
        this.reader = null;
        this.span = span;
        this.pos = 0;
    }

    // primitives - signed
    public void writeSByte(sbyte value) => writer!.Write(value);
    public sbyte readSByte() {
        if (reader != null) return reader.ReadSByte();
        var val = (sbyte)span[pos];
        pos += 1;
        return val;
    }

    public void writeShort(short value) => writer!.Write(value);
    public short readShort() {
        if (reader != null) return reader.ReadInt16();
        var val = BinaryPrimitives.ReadInt16LittleEndian(span[pos..]);
        pos += 2;
        return val;
    }

    public void writeInt(int value) => writer!.Write(value);
    public int readInt() {
        if (reader != null) return reader.ReadInt32();
        var val = BinaryPrimitives.ReadInt32LittleEndian(span[pos..]);
        pos += 4;
        return val;
    }

    public void writeLong(long value) => writer!.Write(value);
    public long readLong() {
        if (reader != null) return reader.ReadInt64();
        var val = BinaryPrimitives.ReadInt64LittleEndian(span[pos..]);
        pos += 8;
        return val;
    }

    // primitives - unsigned
    public void writeByte(byte value) => writer!.Write(value);
    public byte readByte() {
        if (reader != null) return reader.ReadByte();
        var val = span[pos];
        pos += 1;
        return val;
    }

    public void writeUShort(ushort value) => writer!.Write(value);
    public ushort readUShort() {
        if (reader != null) return reader.ReadUInt16();
        var val = BinaryPrimitives.ReadUInt16LittleEndian(span[pos..]);
        pos += 2;
        return val;
    }

    public void writeUInt(uint value) => writer!.Write(value);
    public uint readUInt() {
        if (reader != null) return reader.ReadUInt32();
        var val = BinaryPrimitives.ReadUInt32LittleEndian(span[pos..]);
        pos += 4;
        return val;
    }

    public void writeULong(ulong value) => writer!.Write(value);
    public ulong readULong() {
        if (reader != null) return reader.ReadUInt64();
        var val = BinaryPrimitives.ReadUInt64LittleEndian(span[pos..]);
        pos += 8;
        return val;
    }

    // floating point
    public void writeFloat(float value) => writer!.Write(value);
    public float readFloat() {
        if (reader != null) return reader.ReadSingle();
        var val = BinaryPrimitives.ReadSingleLittleEndian(span[pos..]);
        pos += 4;
        return val;
    }

    public void writeDouble(double value) => writer!.Write(value);
    public double readDouble() {
        if (reader != null) return reader.ReadDouble();
        var val = BinaryPrimitives.ReadDoubleLittleEndian(span[pos..]);
        pos += 8;
        return val;
    }

    // other
    public void writeBool(bool value) => writer!.Write(value);
    public bool readBool() {
        if (reader != null) return reader.ReadBoolean();
        var val = span[pos] != 0;
        pos += 1;
        return val;
    }

    // bitfields (compact flag storage)
    public void writeBits8(Bits8 bits) => writeByte(bits);
    public Bits8 readBits8() => readByte();

    public void writeBits16(Bits16 bits) => writeUShort(bits);
    public Bits16 readBits16() => readUShort();

    public void writeBits32(Bits32 bits) => writeUInt(bits);
    public Bits32 readBits32() => readUInt();

    public void writeBits64(Bits64 bits) => writeULong(bits);
    public Bits64 readBits64() => readULong();

    // strings (length-prefixed, UTF-8)
    public void writeString(string value) {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        writeInt(bytes.Length);
        writer!.Write(bytes);
    }

    public string readString() {
        int len = readInt();
        if (reader != null) {
            var bytes = reader.ReadBytes(len);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        var str = System.Text.Encoding.UTF8.GetString(span.Slice(pos, len));
        pos += len;
        return str;
    }

    // byte arrays (length-prefixed)
    public void writeBytes(byte[] data) {
        writeInt(data.Length);
        writer!.Write(data);
    }

    public byte[] readBytes() {
        int len = readInt();
        if (reader != null) {
            return reader.ReadBytes(len);
        }
        var bytes = span.Slice(pos, len).ToArray();
        pos += len;
        return bytes;
    }

    // read specified number of bytes (no length prefix!)
    public byte[] readBytes(int len) {
        if (reader != null) {
            return reader.ReadBytes(len);
        }
        var bytes = span.Slice(pos, len).ToArray();
        pos += len;
        return bytes;
    }

    // write raw bytes (no length prefix!)
    public void writeRawBytes(byte[] data) {
        writer!.Write(data);
    }

    public void writeRawBytes(byte[] data, int offset, int count) {
        writer!.Write(data, offset, count);
    }

    // game types
    public void writeVec3D(Vector3D v) {
        writeDouble(v.X);
        writeDouble(v.Y);
        writeDouble(v.Z);
    }

    public Vector3D readVec3D() {
        return new Vector3D(readDouble(), readDouble(), readDouble());
    }

    public void writeVec3(Vector3 v) {
        writeFloat(v.X);
        writeFloat(v.Y);
        writeFloat(v.Z);
    }

    public Vector3 readVec3() {
        return new Vector3(readFloat(), readFloat(), readFloat());
    }

    public void writeVec3I(Vector3I v) {
        writeInt(v.X);
        writeInt(v.Y);
        writeInt(v.Z);
    }

    public Vector3I readVec3I() {
        return new Vector3I(readInt(), readInt(), readInt());
    }

    public void writeChunkCoord(ChunkCoord c) {
        writeInt(c.x);
        writeInt(c.z);
    }

    public ChunkCoord readChunkCoord() {
        return new ChunkCoord(readInt(), readInt());
    }

    public void writeSubChunkCoord(SubChunkCoord c) {
        writeInt(c.x);
        writeInt(c.y);
        writeInt(c.z);
    }

    public SubChunkCoord readSubChunkCoord() {
        return new SubChunkCoord(readInt(), readInt(), readInt());
    }


    public void writeItemStack(ItemStack? stack) {
        if (stack == null || stack == ItemStack.EMPTY) {
            writeInt(-1);
            return;
        }
        writeInt(stack.id);
        writeInt(stack.metadata);
        writeInt(stack.quantity);
    }

    public ItemStack readItemStack() {
        int id = readInt();
        if (id == -1) {
            return ItemStack.EMPTY;
        }

        int meta = readInt();
        int qty = readInt();
        return new ItemStack(id, qty, meta);
    }


    public void writeNBT(NBTTag tag) {
        NBTTag.write(tag, writer!);
    }

    public NBTTag readNBT() {
        return NBTTag.read(reader!);
    }
}