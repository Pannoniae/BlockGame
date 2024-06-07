using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace BlockGame.util.xNBT;

public class NBTTagEnd : NBTTag {

    public NBTTagEnd() : base("") {
    }

    public override NBTTagType id => NBTTagType.TAG_End;

    public override void readContents(BinaryReader stream) {
    }

    public override void writeContents(BinaryWriter stream) {
    }
}

public class NBTTagByte : NBTTag {
    public byte data;

    public override NBTTagType id => NBTTagType.TAG_Byte;

    public NBTTagByte(string? name) : base(name) {
    }

    public NBTTagByte(string? name, byte data) : base(name) {
        this.data = data;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data);
    }

    public override void readContents(BinaryReader stream) {
        data = stream.ReadByte();
    }

    public override string ToString() {
        return data.ToString();
    }
}

public class NBTTagShort : NBTTag {
    public short data;

    public override NBTTagType id => NBTTagType.TAG_Short;

    public NBTTagShort(string? name) : base(name) {
    }

    public NBTTagShort(string? name, short data) : base(name) {
        this.data = data;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data);
    }

    public override void readContents(BinaryReader stream) {
        data = stream.ReadInt16();
    }

    public override string ToString() {
        return data.ToString();
    }
}

public class NBTTagUShort : NBTTag {
    public ushort data;

    public override NBTTagType id => NBTTagType.TAG_UShort;

    public NBTTagUShort(string? name) : base(name) {
    }

    public NBTTagUShort(string? name, ushort data) : base(name) {
        this.data = data;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data);
    }

    public override void readContents(BinaryReader stream) {
        data = stream.ReadUInt16();
    }

    public override string ToString() {
        return data.ToString();
    }
}

public class NBTTagInt : NBTTag {
    public int data;

    public override NBTTagType id => NBTTagType.TAG_Int;

    public NBTTagInt(string? name) : base(name) {
    }

    public NBTTagInt(string? name, int data) : base(name) {
        this.data = data;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data);
    }

    public override void readContents(BinaryReader stream) {
        data = stream.ReadInt32();
    }

    public override string ToString() {
        return data.ToString();
    }
}

public class NBTTagUInt : NBTTag {
    public uint data;

    public override NBTTagType id => NBTTagType.TAG_UInt;

    public NBTTagUInt(string? name) : base(name) {
    }

    public NBTTagUInt(string? name, uint data) : base(name) {
        this.data = data;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data);
    }

    public override void readContents(BinaryReader stream) {
        data = stream.ReadUInt32();
    }

    public override string ToString() {
        return data.ToString();
    }
}

public class NBTTagLong : NBTTag {
    public long data;

    public override NBTTagType id => NBTTagType.TAG_Long;

    public NBTTagLong(string? name) : base(name) {
    }

    public NBTTagLong(string? name, long data) : base(name) {
        this.data = data;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data);
    }

    public override void readContents(BinaryReader stream) {
        data = stream.ReadInt64();
    }

    public override string ToString() {
        return data.ToString();
    }
}

public class NBTTagULong : NBTTag {
    public ulong data;

    public override NBTTagType id => NBTTagType.TAG_ULong;

    public NBTTagULong(string? name) : base(name) {
    }

    public NBTTagULong(string? name, ulong data) : base(name) {
        this.data = data;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data);
    }

    public override void readContents(BinaryReader stream) {
        data = stream.ReadUInt64();
    }

    public override string ToString() {
        return data.ToString();
    }
}

public class NBTTagFloat : NBTTag {
    public float data;

    public override NBTTagType id => NBTTagType.TAG_Float;

    public NBTTagFloat(string? name) : base(name) {
    }

    public NBTTagFloat(string? name, float data) : base(name) {
        this.data = data;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data);
    }

    public override void readContents(BinaryReader stream) {
        data = stream.ReadSingle();
    }

    public override string ToString() {
        return data.ToString(CultureInfo.InvariantCulture);
    }
}

public class NBTTagDouble : NBTTag {
    public double data;

    public override NBTTagType id => NBTTagType.TAG_Double;

    public NBTTagDouble(string? name) : base(name) {
    }

    public NBTTagDouble(string? name, double data) : base(name) {
        this.data = data;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data);
    }

    public override void readContents(BinaryReader stream) {
        data = stream.ReadDouble();
    }

    public override string ToString() {
        return data.ToString(CultureInfo.InvariantCulture);
    }
}

public class NBTTagString : NBTTag {
    public string data;

    public override NBTTagType id => NBTTagType.TAG_String;

    public NBTTagString(string? name) : base(name) {
    }

    public NBTTagString(string? name, string data) : base(name) {
        this.data = data;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data);
    }

    public override void readContents(BinaryReader stream) {
        data = stream.ReadString();
    }

    public override string ToString() {
        return data.ToString(CultureInfo.InvariantCulture);
    }
}

public class NBTTagList<T> : NBTTag where T : NBTTag {
    public List<T> list;
    public NBTTagType listType;

    public override NBTTagType id => NBTTagType.TAG_List;

    public NBTTagList(string? name) : base(name) {
        list = new List<T>();
    }

    public override void writeContents(BinaryWriter stream) {
        // empty list so type END
        if (list.Count == 0) {
            listType = NBTTagType.TAG_End;
        }
        else {
            listType = list[0].id;
        }
        // write type and length
        stream.Write((byte)listType);
        stream.Write(list.Count);
        foreach (var t in list) {
            t.writeContents(stream);
        }

    }

    public override void readContents(BinaryReader stream) {
        // read type and length
        listType = (NBTTagType)stream.ReadByte();
        var length = stream.ReadInt32();
        for (int i = 0; i < length; ++i) {
            var tag = createTag(listType, null);
            tag.readContents(stream);
            list[i] = (T)tag;
        }
    }

    public void add(T value) {
        list.Add(value);
    }

    public void remove(T value) {
        list.Remove(value);
    }

    public void removeAt(int index) {
        list.RemoveAt(index);
    }

    public T get(int index) {
        return list[index];
    }

    public int count() {
        return list.Count;
    }

    public override String ToString() {
        return list.Count + " entries of type " + getTypeName(listType);
    }
}

public class NBTTagCompound : NBTTag {
    public Dictionary<string, NBTTag> dict;

    public override NBTTagType id => NBTTagType.TAG_Compound;

    public NBTTagCompound(string? name) : base(name) {
        dict = new Dictionary<string, NBTTag>();
    }

    public override void writeContents(BinaryWriter stream) {
        // write contents
        foreach (var item in dict.Values) {
            write(item, stream);
        }
        // write Tag_END
        stream.Write((byte)0);

    }

    public override void readContents(BinaryReader stream) {
        dict.Clear();
        // how the fuck do you do this without an endless loop?
        while (true) {
            NBTTag tag = read(stream);
            if (tag.id == 0) {
                return;
            }

            dict.Add(tag.name, tag);
        }
    }

    public ICollection<NBTTag> getTags() {
        return dict.Values;
    }

    public int count() {
        return dict.Count;
    }

    // Add functions

    public void add(NBTTag value) {
        dict.Add(value.name, value);
    }

    public void addByte(String name, byte value) {
        dict.Add(name, new NBTTagByte(name, value));
    }

    public void addShort(String name, short value) {
        dict.Add(name, new NBTTagShort(name, value));
    }

    public void addUShort(String name, ushort value) {
        dict.Add(name, new NBTTagUShort(name, value));
    }

    public void addInt(String name, int value) {
        dict.Add(name, new NBTTagInt(name, value));
    }

    public void addUInt(String name, uint value) {
        dict.Add(name, new NBTTagUInt(name, value));
    }

    public void addLong(String name, long value) {
        dict.Add(name, new NBTTagLong(name, value));
    }

    public void addULong(String name, ulong value) {
        dict.Add(name, new NBTTagULong(name, value));
    }

    public void addFloat(String name, float value) {
        dict.Add(name, new NBTTagFloat(name, value));
    }

    public void addDouble(String name, double value) {
        dict.Add(name, new NBTTagDouble(name, value));
    }

    public void addString(String name, String value) {
        dict.Add(name, new NBTTagString(name, value));
    }

    public void addByteArray(String name, byte[] value) {
        dict.Add(name, new NBTTagByteArray(name, value));
    }

    public void addShortArray(String name, short[] value) {
        dict.Add(name, new NBTTagShortArray(name, value));
    }

    public void addUShortArray(String name, ushort[] value) {
        dict.Add(name, new NBTTagUShortArray(name, value));
    }

    public void addIntArray(String name, int[] value) {
        dict.Add(name, new NBTTagIntArray(name, value));
    }

    public void addUIntArray(String name, uint[] value) {
        dict.Add(name, new NBTTagUIntArray(name, value));
    }

    public void addLongArray(String name, long[] value) {
        dict.Add(name, new NBTTagLongArray(name, value));
    }

    public void addULongArray(String name, ulong[] value) {
        dict.Add(name, new NBTTagULongArray(name, value));
    }

    public void addCompoundTag(String name, NBTTagCompound value) {
        dict.Add(name, value);
    }

    public void addListTag(String name, NBTTagList<NBTTag> value) {
        dict.Add(name, value);
    }

    public void addListTag<T>(String name, NBTTagList<T> value) where T : NBTTag {
        dict.Add(name, value);
    }

    // Get functions

    public byte getByte(String name) {
        return ((NBTTagByte)dict[name]).data;
    }

    public short getShort(String name) {
        return ((NBTTagShort)dict[name]).data;
    }

    public ushort getUShort(String name) {
        return ((NBTTagUShort)dict[name]).data;
    }

    public int getInt(String name) {
        return ((NBTTagInt)dict[name]).data;
    }

    public uint getUInt(String name) {
        return ((NBTTagUInt)dict[name]).data;
    }

    public long getLong(String name) {
        return ((NBTTagLong)dict[name]).data;
    }

    public ulong getULong(String name) {
        return ((NBTTagULong)dict[name]).data;
    }

    public float getFloat(String name) {
        return ((NBTTagFloat)dict[name]).data;
    }

    public double getDouble(String name) {
        return ((NBTTagDouble)dict[name]).data;
    }

    public string getString(String name) {
        return ((NBTTagString)dict[name]).data;
    }

    public byte[] getByteArray(String name) {
        return ((NBTTagByteArray)dict[name]).data;
    }

    public short[] getShortArray(String name) {
        return ((NBTTagShortArray)dict[name]).data;
    }

    public ushort[] getUShortArray(String name) {
        return ((NBTTagUShortArray)dict[name]).data;
    }

    public int[] getIntArray(String name) {
        return ((NBTTagIntArray)dict[name]).data;
    }

    public uint[] getUIntArray(String name) {
        return ((NBTTagUIntArray)dict[name]).data;
    }

    public long[] getLongArray(String name) {
        return ((NBTTagLongArray)dict[name]).data;
    }

    public ulong[] getULongArray(String name) {
        return ((NBTTagULongArray)dict[name]).data;
    }

    public NBTTagList<NBTTag> getListTag(String name) {
        return (NBTTagList<NBTTag>)dict[name];
    }

    public NBTTagList<T> getListTag<T>(String name) where T : NBTTag {
        return (NBTTagList<T>)dict[name];
    }

    public NBTTagCompound getCompoundTag(String name) {
        return (NBTTagCompound)dict[name];
    }

    public void remove(string name) {
        dict.Remove(name);
    }

    public NBTTag get(string name) {
        return dict[name];
    }

    public T get<T>(string name) where T : NBTTag {
        return (T)dict[name];
    }

    public override String ToString() {
        StringBuilder str = new StringBuilder();
        str.Append(name + ":[");

        string var3;
        foreach (string key in dict.Keys) {
            str.Append(key + ":" + dict[key] + ",");
        }

        return str + "]";
    }
}

public class NBTTagByteArray : NBTTag {
    public byte[] data;

    public override NBTTagType id => NBTTagType.TAG_Byte_Array;

    public NBTTagByteArray(string? name) : base(name) {
    }

    public NBTTagByteArray(string? name, byte[] data) : base(name) {
        this.data = data;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data.Length);
        stream.Write(data);
    }

    public override void readContents(BinaryReader stream) {
        int length = stream.ReadInt32();
        data = stream.ReadBytes(length);
    }

    public override string ToString() {
        return "[" + data.Length + " bytes]";
    }
}

public class NBTTagShortArray : NBTTag {
    public short[] data;

    public override NBTTagType id => NBTTagType.TAG_Short_Array;

    public NBTTagShortArray(string? name) : base(name) {
    }

    public NBTTagShortArray(string? name, short[] data) : base(name) {
        this.data = data;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data.Length);
        var values = new Span<short>(data);
        // if we aren't little endian, swap
        if (!BitConverter.IsLittleEndian) {
            BinaryPrimitives.ReverseEndianness(values, values);
        }
        stream.Write(MemoryMarshal.AsBytes(values));
    }

    public override void readContents(BinaryReader stream) {
        int length = stream.ReadInt32();
        data = MemoryMarshal.Cast<byte, short>(stream.ReadBytes(length * sizeof(short))).ToArray();
        if (!BitConverter.IsLittleEndian) {
            BinaryPrimitives.ReverseEndianness(data, data);
        }
    }

    public override string ToString() {
        return "[" + data.Length + " shorts]";
    }
}

public class NBTTagUShortArray : NBTTag {
    public ushort[] data;

    public override NBTTagType id => NBTTagType.TAG_UShort_Array;

    public NBTTagUShortArray(string? name) : base(name) {
    }

    public NBTTagUShortArray(string? name, ushort[] data) : base(name) {
        this.data = data;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data.Length);
        var values = new Span<ushort>(data);
        // if we aren't little endian, swap
        if (!BitConverter.IsLittleEndian) {
            BinaryPrimitives.ReverseEndianness(values, values);
        }
        stream.Write(MemoryMarshal.AsBytes(values));
    }

    public override void readContents(BinaryReader stream) {
        int length = stream.ReadInt32();
        data = MemoryMarshal.Cast<byte, ushort>(stream.ReadBytes(length * sizeof(ushort))).ToArray();
        if (!BitConverter.IsLittleEndian) {
            BinaryPrimitives.ReverseEndianness(data, data);
        }
    }

    public override string ToString() {
        return "[" + data.Length + " ushorts]";
    }
}

public class NBTTagIntArray : NBTTag {
    public int[] data;

    public override NBTTagType id => NBTTagType.TAG_Int_Array;

    public NBTTagIntArray(string? name) : base(name) {
    }

    public NBTTagIntArray(string? name, int[] data) : base(name) {
        this.data = data;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data.Length);
        var values = new Span<int>(data);
        // if we aren't little endian, swap
        if (!BitConverter.IsLittleEndian) {
            BinaryPrimitives.ReverseEndianness(values, values);
        }
        stream.Write(MemoryMarshal.AsBytes(values));
    }

    public override void readContents(BinaryReader stream) {
        int length = stream.ReadInt32();
        data = MemoryMarshal.Cast<byte, int>(stream.ReadBytes(length * sizeof(int))).ToArray();
        if (!BitConverter.IsLittleEndian) {
            BinaryPrimitives.ReverseEndianness(data, data);
        }
    }

    public override string ToString() {
        return "[" + data.Length + " ints]";
    }
}

public class NBTTagUIntArray : NBTTag {
    public uint[] data;

    public override NBTTagType id => NBTTagType.TAG_UInt_Array;

    public NBTTagUIntArray(string? name) : base(name) {
    }

    public NBTTagUIntArray(string? name, uint[] data) : base(name) {
        this.data = data;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data.Length);
        var values = new Span<uint>(data);
        // if we aren't little endian, swap
        if (!BitConverter.IsLittleEndian) {
            BinaryPrimitives.ReverseEndianness(values, values);
        }
        stream.Write(MemoryMarshal.AsBytes(values));
    }

    public override void readContents(BinaryReader stream) {
        int length = stream.ReadInt32();
        data = MemoryMarshal.Cast<byte, uint>(stream.ReadBytes(length * sizeof(uint))).ToArray();
        if (!BitConverter.IsLittleEndian) {
            BinaryPrimitives.ReverseEndianness(data, data);
        }
    }

    public override string ToString() {
        return "[" + data.Length + " uints]";
    }
}

public class NBTTagLongArray : NBTTag {
    public long[] data;

    public override NBTTagType id => NBTTagType.TAG_Long_Array;

    public NBTTagLongArray(string? name) : base(name) {
    }

    public NBTTagLongArray(string? name, long[] data) : base(name) {
        this.data = data;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data.Length);
        var values = new Span<long>(data);
        // if we aren't little endian, swap
        if (!BitConverter.IsLittleEndian) {
            BinaryPrimitives.ReverseEndianness(values, values);
        }
        stream.Write(MemoryMarshal.AsBytes(values));
    }

    public override void readContents(BinaryReader stream) {
        int length = stream.ReadInt32();
        data = MemoryMarshal.Cast<byte, long>(stream.ReadBytes(length * sizeof(long))).ToArray();
        if (!BitConverter.IsLittleEndian) {
            BinaryPrimitives.ReverseEndianness(data, data);
        }
    }

    public override string ToString() {
        return "[" + data.Length + " longs]";
    }
}

public class NBTTagULongArray : NBTTag {
    public ulong[] data;

    public override NBTTagType id => NBTTagType.TAG_ULong_Array;

    public NBTTagULongArray(string? name) : base(name) {
    }

    public NBTTagULongArray(string? name, ulong[] data) : base(name) {
        this.data = data;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data.Length);
        var values = new Span<ulong>(data);
        // if we aren't little endian, swap
        if (!BitConverter.IsLittleEndian) {
            BinaryPrimitives.ReverseEndianness(values, values);
        }
        stream.Write(MemoryMarshal.AsBytes(values));
    }

    public override void readContents(BinaryReader stream) {
        int length = stream.ReadInt32();
        data = MemoryMarshal.Cast<byte, ulong>(stream.ReadBytes(length * sizeof(ulong))).ToArray();
        if (!BitConverter.IsLittleEndian) {
            BinaryPrimitives.ReverseEndianness(data, data);
        }
    }

    public override string ToString() {
        return "[" + data.Length + " ulongs]";
    }
}