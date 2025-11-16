using System.Numerics;
using BlockGame.util;
using BlockGame.util.xNBT;
using BlockGame.world.chunk;
using Molten;
using Molten.DoublePrecision;

namespace BlockGame.net.packet;

public class PacketBuffer {
    private readonly BinaryWriter? writer;
    private readonly BinaryReader? reader;

    public PacketBuffer(BinaryWriter writer) {
        this.writer = writer;
    }

    public PacketBuffer(BinaryReader reader) {
        this.reader = reader;
    }

    // primitives - signed
    public void writeSByte(sbyte value) => writer!.Write(value);
    public sbyte readSByte() => reader!.ReadSByte();

    public void writeShort(short value) => writer!.Write(value);
    public short readShort() => reader!.ReadInt16();

    public void writeInt(int value) => writer!.Write(value);
    public int readInt() => reader!.ReadInt32();

    public void writeLong(long value) => writer!.Write(value);
    public long readLong() => reader!.ReadInt64();

    // primitives - unsigned
    public void writeByte(byte value) => writer!.Write(value);
    public byte readByte() => reader!.ReadByte();

    public void writeUShort(ushort value) => writer!.Write(value);
    public ushort readUShort() => reader!.ReadUInt16();

    public void writeUInt(uint value) => writer!.Write(value);
    public uint readUInt() => reader!.ReadUInt32();

    public void writeULong(ulong value) => writer!.Write(value);
    public ulong readULong() => reader!.ReadUInt64();

    // floating point
    public void writeFloat(float value) => writer!.Write(value);
    public float readFloat() => reader!.ReadSingle();

    public void writeDouble(double value) => writer!.Write(value);
    public double readDouble() => reader!.ReadDouble();

    // other
    public void writeBool(bool value) => writer!.Write(value);
    public bool readBool() => reader!.ReadBoolean();

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
        var bytes = reader!.ReadBytes(len);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    // byte arrays (length-prefixed)
    public void writeBytes(byte[] data) {
        writeInt(data.Length);
        writer!.Write(data);
    }

    public byte[] readBytes() {
        int len = readInt();
        return reader!.ReadBytes(len);
    }

    // read specified number of bytes (no length prefix!)
    public byte[] readBytes(int len) {
        return reader!.ReadBytes(len);
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