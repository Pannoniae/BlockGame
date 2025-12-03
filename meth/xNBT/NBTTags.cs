using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace BlockGame.util.xNBT;

public class NBTEnd : NBTTag {
    public NBTEnd() : base("") {
    }

    public override NBTType id => NBTType.TAG_End;

    public override void readContents(BinaryReader stream) {
    }

    public override void writeContents(BinaryWriter stream) {
    }
}

public class NBTByte : NBTTag {
    public byte data;

    public override NBTType id => NBTType.TAG_Byte;

    public NBTByte(string? name) : base(name) {
    }

    public NBTByte(string? name, byte data) : base(name) {
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

public class NBTSByte : NBTTag {
    public sbyte data;

    public override NBTType id => NBTType.TAG_SByte;

    public NBTSByte(string? name) : base(name) {
    }

    public NBTSByte(string? name, sbyte data) : base(name) {
        this.data = data;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data);
    }

    public override void readContents(BinaryReader stream) {
        data = stream.ReadSByte();
    }

    public override string ToString() {
        return data.ToString();
    }
}

public class NBTShort : NBTTag {
    public short data;

    public override NBTType id => NBTType.TAG_Short;

    public NBTShort(string? name) : base(name) {
    }

    public NBTShort(string? name, short data) : base(name) {
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

public class NBTUShort : NBTTag {
    public ushort data;

    public override NBTType id => NBTType.TAG_UShort;

    public NBTUShort(string? name) : base(name) {
    }

    public NBTUShort(string? name, ushort data) : base(name) {
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

public class NBTInt : NBTTag {
    public int data;

    public override NBTType id => NBTType.TAG_Int;

    public NBTInt(string? name) : base(name) {
    }

    public NBTInt(string? name, int data) : base(name) {
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

public class NBTUInt : NBTTag {
    public uint data;

    public override NBTType id => NBTType.TAG_UInt;

    public NBTUInt(string? name) : base(name) {
    }

    public NBTUInt(string? name, uint data) : base(name) {
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

public class NBTLong : NBTTag {
    public long data;

    public override NBTType id => NBTType.TAG_Long;

    public NBTLong(string? name) : base(name) {
    }

    public NBTLong(string? name, long data) : base(name) {
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

public class NBTULong : NBTTag {
    public ulong data;

    public override NBTType id => NBTType.TAG_ULong;

    public NBTULong(string? name) : base(name) {
    }

    public NBTULong(string? name, ulong data) : base(name) {
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

public class NBTFloat : NBTTag {
    public float data;

    public override NBTType id => NBTType.TAG_Float;

    public NBTFloat(string? name) : base(name) {
    }

    public NBTFloat(string? name, float data) : base(name) {
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

public class NBTDouble : NBTTag {
    public double data;

    public override NBTType id => NBTType.TAG_Double;

    public NBTDouble(string? name) : base(name) {
    }

    public NBTDouble(string? name, double data) : base(name) {
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

public class NBTString : NBTTag {
    public string data;

    public override NBTType id => NBTType.TAG_String;

    public NBTString(string? name) : base(name) {
    }

    public NBTString(string? name, string data) : base(name) {
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

// has a typename
public interface INBTList {
    public NBTType listType { get; }

    public int count();
}

public class NBTList : NBTTag, INBTList {
    public readonly XList<NBTTag> list;

    public NBTType listType { get; set; }

    public override NBTType id => NBTType.TAG_List;

    public NBTList(NBTType listType, string? name) : base(name) {
        list = [];
        this.listType = listType;
    }

    public override void writeContents(BinaryWriter stream) {
        // empty list so type END
        listType = list.Count == 0 ? NBTType.TAG_End : list[0].id;
        // write type and length
        stream.Write(list.Count);
        foreach (var t in list) {
            t.writeContents(stream);
        }
    }

    public override void readContents(BinaryReader stream) {
        // read type and length
        var length = stream.ReadInt32();

        // resize the list to be `length` long
        list.Clear();

        for (int i = 0; i < length; ++i) {
            var tag = createTag(listType, null);
            tag.readContents(stream);
            list.Add(tag);
        }
    }

    public void add(NBTTag value) {
        list.Add(value);
    }

    public void remove(NBTTag value) {
        list.Remove(value);
    }

    public void removeAt(int index) {
        list.RemoveAt(index);
    }

    public NBTTag get(int index) {
        return list[index];
    }

    public int count() {
        return list.Count;
    }

    public override String ToString() {
        return list.Count + " entries of type " + getTypeName(listType);
    }
}

public class NBTList<T> : NBTTag, INBTList where T : NBTTag {
    public readonly XList<T> list;

    public NBTType listType { get; set; }

    public override NBTType id => NBTType.TAG_List;

    public NBTList(NBTType listType, string? name) : base(name) {
        list = [];
        this.listType = listType;

        // if this thing is EVER a generic NBTTag, throw an exception. This should never happen and it's a result of skill-issue programming.
        // We "specialise" the lists by a huge fucking switch statement when constructing them. This is not *good* but better than reflection shit.
        if (typeof(T) == typeof(NBTTag)) {
            SkillIssueException.throwNew($"Overly generic NBTList was constructed with name {name} and type {listType} ({list.Count} values)");
        }
    }

    public override void writeContents(BinaryWriter stream) {
        // empty list so type END
        //listType = list.Count == 0 ? NBTType.TAG_End : list[0].id;
        // write type and length
        stream.Write(list.Count);
        foreach (var t in list) {
            t.writeContents(stream);
        }
    }

    public override void readContents(BinaryReader stream) {
        // read type and length
        var length = stream.ReadInt32();

        // resize the list to be `length` long
        list.Clear();

        for (int i = 0; i < length; ++i) {
            var tag = createTag(listType, null);
            tag.readContents(stream);
            list.Add((T)tag);
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

    public override string ToString() {
        return list.Count + " entries of type " + getTypeName(listType);
    }
}

public class NBTCompound : NBTTag {
    public readonly XMap<string, NBTTag> dict;

    public override NBTType id => NBTType.TAG_Compound;

    public NBTCompound(string? name) : base(name) {
        dict = [];
    }

    public NBTCompound() {
        dict = [];
    }

    public override void writeContents(BinaryWriter stream) {
        // write contents
        foreach (var item in dict) {
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

    public XMap<string, NBTTag>.ValueEnumerable getTags() {
        return dict.Values;
    }

    public int count() {
        return dict.Count;
    }

    // Add functions

    public void add(NBTTag value) {
        dict.Add(value.name, value);
    }

    public void addByte(string name, byte value) {
        dict.Add(name, new NBTByte(name, value));
    }

    public void addShort(string name, short value) {
        dict.Add(name, new NBTShort(name, value));
    }

    public void addUShort(string name, ushort value) {
        dict.Add(name, new NBTUShort(name, value));
    }

    public void addInt(string name, int value) {
        dict.Add(name, new NBTInt(name, value));
    }

    public void addUInt(string name, uint value) {
        dict.Add(name, new NBTUInt(name, value));
    }

    public void addLong(string name, long value) {
        dict.Add(name, new NBTLong(name, value));
    }

    public void addULong(string name, ulong value) {
        dict.Add(name, new NBTULong(name, value));
    }

    public void addFloat(string name, float value) {
        dict.Add(name, new NBTFloat(name, value));
    }

    public void addDouble(string name, double value) {
        dict.Add(name, new NBTDouble(name, value));
    }

    public void addString(string name, string value) {
        dict.Add(name, new NBTString(name, value));
    }

    public void addByteArray(string name, byte[] value) {
        dict.Add(name, new NBTByteArray(name, value));
    }

    public void addSByteArray(string name, sbyte[] value) {
        dict.Add(name, new NBTSByteArray(name, value));
    }

    public void addShortArray(string name, short[] value) {
        dict.Add(name, new NBTShortArray(name, value));
    }

    public void addUShortArray(string name, ushort[] value) {
        dict.Add(name, new NBTUShortArray(name, value));
    }

    public void addIntArray(string name, int[] value) {
        dict.Add(name, new NBTIntArray(name, value));
    }

    public void addUIntArray(string name, uint[] value) {
        dict.Add(name, new NBTUIntArray(name, value));
    }

    public void addLongArray(string name, long[] value) {
        dict.Add(name, new NBTLongArray(name, value));
    }

    public void addULongArray(string name, ulong[] value) {
        dict.Add(name, new NBTULongArray(name, value));
    }

    public void addCompoundTag(string name, NBTCompound value) {
        dict.Add(name, value);
    }

    public void addListTag(string name, NBTList<NBTTag> value) {
        dict.Add(name, value);
    }

    public void addListTag<T>(string name, NBTList<T> value) where T : NBTTag {
        dict.Add(name, value);
    }

    // Direct list writing (fastpath for chunk serialization)
    public void addStringListUnsafe(string name, XUList<string> values) {
        dict.Add(name, new NBTDStringList(name, values.ToArray()));
    }

    public void addByteListUnsafe(string name, XUList<byte> values) {
        dict.Add(name, new NBTDByteList(name, values.ToArray()));
    }

    public void addUIntListUnsafe(string name, XUList<uint> values) {
        dict.Add(name, new NBTDUIntList(name, values.ToArray()));
    }

    // Get functions

    public byte getByte(string name) {
        return ((NBTByte)dict[name]).data;
    }

    public short getShort(string name) {
        return ((NBTShort)dict[name]).data;
    }

    public ushort getUShort(string name) {
        return ((NBTUShort)dict[name]).data;
    }

    public int getInt(string name) {
        return ((NBTInt)dict[name]).data;
    }

    public uint getUInt(string name) {
        return ((NBTUInt)dict[name]).data;
    }

    public long getLong(string name) {
        return ((NBTLong)dict[name]).data;
    }

    public ulong getULong(string name) {
        return ((NBTULong)dict[name]).data;
    }

    public float getFloat(string name) {
        return ((NBTFloat)dict[name]).data;
    }

    public double getDouble(string name) {
        return ((NBTDouble)dict[name]).data;
    }

    public string getString(string name) {
        return ((NBTString)dict[name]).data;
    }

    public byte[] getByteArray(string name) {
        return ((NBTByteArray)dict[name]).data;
    }

    public sbyte[] getSByteArray(string name) {
        return ((NBTSByteArray)dict[name]).data;
    }

    public short[] getShortArray(string name) {
        return ((NBTShortArray)dict[name]).data;
    }

    public ushort[] getUShortArray(string name) {
        return ((NBTUShortArray)dict[name]).data;
    }

    public int[] getIntArray(string name) {
        return ((NBTIntArray)dict[name]).data;
    }

    public uint[] getUIntArray(string name) {
        return ((NBTUIntArray)dict[name]).data;
    }

    public long[] getLongArray(string name) {
        return ((NBTLongArray)dict[name]).data;
    }

    public ulong[] getULongArray(string name) {
        return ((NBTULongArray)dict[name]).data;
    }

    public NBTList<NBTTag> getListTag(string name) {
        return (NBTList<NBTTag>)dict[name];
    }

    public NBTList<T> getListTag<T>(string name) where T : NBTTag {
        return (NBTList<T>)dict[name];
    }

    public NBTCompound getCompoundTag(string name) {
        return (NBTCompound)dict[name];
    }

    public void addStruct<T>(string name, T value) where T : unmanaged {
        dict.Add(name, NBTStruct.create(name, value));
    }

    public T getStruct<T>(string name) where T : unmanaged {
        return ((NBTStruct)dict[name]).get<T>();
    }

    public void remove(string name) {
        dict.Remove(name);
    }

    public NBTTag get(string name) {
        return dict[name];
    }

    public bool has(string name) {
        return dict.ContainsKey(name);
    }

    public T get<T>(string name) where T : NBTTag {
        return (T)dict[name];
    }

    /** Default functions **/
    public byte getByte(string name, byte d) {
        return dict.TryGetValue(name, out NBTTag? value) ? ((NBTByte)value).data : d;
    }

    public short getShort(string name, short d) {
        return dict.TryGetValue(name, out NBTTag? value) ? ((NBTShort)value).data : d;
    }

    public ushort getUShort(string name, ushort d) {
        return dict.TryGetValue(name, out NBTTag? value) ? ((NBTUShort)value).data : d;
    }

    public int getInt(string name, int d) {
        return dict.TryGetValue(name, out NBTTag? value) ? ((NBTInt)value).data : d;
    }

    public uint getUInt(string name, uint d) {
        return dict.TryGetValue(name, out NBTTag? value) ? ((NBTUInt)value).data : d;
    }

    public long getLong(string name, long d) {
        return dict.TryGetValue(name, out NBTTag? value) ? ((NBTLong)value).data : d;
    }

    public ulong getULong(string name, ulong d) {
        return dict.TryGetValue(name, out NBTTag? value) ? ((NBTULong)value).data : d;
    }

    public float getFloat(string name, float d) {
        return dict.TryGetValue(name, out NBTTag? value) ? ((NBTFloat)value).data : d;
    }

    public double getDouble(string name, double d) {
        return dict.TryGetValue(name, out NBTTag? value) ? ((NBTDouble)value).data : d;
    }

    public string getString(string name, string d) {
        return dict.TryGetValue(name, out NBTTag? value) ? ((NBTString)value).data : d;
    }

    public byte[] getByteArray(string name, byte[] d) {
        return dict.TryGetValue(name, out NBTTag? value) ? ((NBTByteArray)value).data : d;
    }

    public short[] getShortArray(string name, short[] d) {
        return dict.TryGetValue(name, out NBTTag? value) ? ((NBTShortArray)value).data : d;
    }

    public ushort[] getUShortArray(string name, ushort[] d) {
        return dict.TryGetValue(name, out NBTTag? value) ? ((NBTUShortArray)value).data : d;
    }

    public int[] getIntArray(string name, int[] d) {
        return dict.TryGetValue(name, out NBTTag? value) ? ((NBTIntArray)value).data : d;
    }

    public uint[] getUIntArray(string name, uint[] d) {
        return dict.TryGetValue(name, out NBTTag? value) ? ((NBTUIntArray)value).data : d;
    }

    public long[] getLongArray(string name, long[] d) {
        return dict.TryGetValue(name, out NBTTag? value) ? ((NBTLongArray)value).data : d;
    }

    public ulong[] getULongArray(string name, ulong[] d) {
        return dict.TryGetValue(name, out NBTTag? value) ? ((NBTULongArray)value).data : d;
    }

    public NBTList<NBTTag> getListTag(string name, NBTList<NBTTag> d) {
        return dict.TryGetValue(name, out NBTTag? value) ? (NBTList<NBTTag>)value : d;
    }

    public NBTCompound getCompoundTag(string name, NBTCompound d) {
        return dict.TryGetValue(name, out NBTTag? value) ? (NBTCompound)value : d;
    }


    public override string ToString() {
        StringBuilder str = new StringBuilder();
        str.Append(name + ":{");

        foreach (string key in dict.Keys) {
            str.Append(key + ":" + dict[key] + ",");
        }

        return str + "}";
    }
}

public class NBTByteArray : NBTTag {
    public byte[] data;

    public override NBTType id => NBTType.TAG_Byte_Array;

    public NBTByteArray(string? name) : base(name) {
    }

    public NBTByteArray(string? name, byte[] data) : base(name) {
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

public class NBTSByteArray : NBTTag {
    public sbyte[] data;

    public override NBTType id => NBTType.TAG_SByte_Array;

    public NBTSByteArray(string? name) : base(name) {
    }

    public NBTSByteArray(string? name, sbyte[] data) : base(name) {
        this.data = data;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data.Length);
        stream.Write(MemoryMarshal.AsBytes(data.AsSpan()));
    }

    public override void readContents(BinaryReader stream) {
        int length = stream.ReadInt32();
        data = new sbyte[length];
        var span = MemoryMarshal.AsBytes(data.AsSpan());
        stream.ReadExactly(span);
    }

    public override string ToString() {
        return "[" + data.Length + " sbytes]";
    }
}

public class NBTShortArray : NBTTag {
    public short[] data;

    public override NBTType id => NBTType.TAG_Short_Array;

    public NBTShortArray(string? name) : base(name) {
    }

    public NBTShortArray(string? name, short[] data) : base(name) {
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
        data = new short[length];
        var span = MemoryMarshal.AsBytes(data.AsSpan());
        stream.ReadExactly(span);
        if (!BitConverter.IsLittleEndian) {
            BinaryPrimitives.ReverseEndianness(data, data);
        }
    }

    public override string ToString() {
        return "[" + data.Length + " shorts]";
    }
}

public class NBTUShortArray : NBTTag {
    public ushort[] data;

    public override NBTType id => NBTType.TAG_UShort_Array;

    public NBTUShortArray(string? name) : base(name) {
    }

    public NBTUShortArray(string? name, ushort[] data) : base(name) {
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
        data = new ushort[length];
        var span = MemoryMarshal.AsBytes(data.AsSpan());
        stream.ReadExactly(span);
        if (!BitConverter.IsLittleEndian) {
            BinaryPrimitives.ReverseEndianness(data, data);
        }
    }

    public override string ToString() {
        return "[" + data.Length + " ushorts]";
    }
}

public class NBTIntArray : NBTTag {
    public int[] data;

    public override NBTType id => NBTType.TAG_Int_Array;

    public NBTIntArray(string? name) : base(name) {
    }

    public NBTIntArray(string? name, int[] data) : base(name) {
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
        data = new int[length];
        var span = MemoryMarshal.AsBytes(data.AsSpan());
        stream.ReadExactly(span);
        if (!BitConverter.IsLittleEndian) {
            BinaryPrimitives.ReverseEndianness(data, data);
        }
    }

    public override string ToString() {
        return "[" + data.Length + " ints]";
    }
}

public class NBTUIntArray : NBTTag {
    public uint[] data;

    public override NBTType id => NBTType.TAG_UInt_Array;

    public NBTUIntArray(string? name) : base(name) {
    }

    public NBTUIntArray(string? name, uint[] data) : base(name) {
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
        data = new uint[length];
        var span = MemoryMarshal.AsBytes(data.AsSpan());
        stream.ReadExactly(span);
        if (!BitConverter.IsLittleEndian) {
            BinaryPrimitives.ReverseEndianness(data, data);
        }
    }

    public override string ToString() {
        return "[" + data.Length + " uints]";
    }
}

public class NBTLongArray : NBTTag {
    public long[] data;

    public override NBTType id => NBTType.TAG_Long_Array;

    public NBTLongArray(string? name) : base(name) {
    }

    public NBTLongArray(string? name, long[] data) : base(name) {
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
        data = new long[length];
        var span = MemoryMarshal.AsBytes(data.AsSpan());
        stream.ReadExactly(span);
        if (!BitConverter.IsLittleEndian) {
            BinaryPrimitives.ReverseEndianness(data, data);
        }
    }

    public override string ToString() {
        return "[" + data.Length + " longs]";
    }
}

public class NBTULongArray : NBTTag {
    public ulong[] data;

    public override NBTType id => NBTType.TAG_ULong_Array;

    public NBTULongArray(string? name) : base(name) {
    }

    public NBTULongArray(string? name, ulong[] data) : base(name) {
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
        data = new ulong[length];
        var span = MemoryMarshal.AsBytes(data.AsSpan());
        stream.ReadExactly(span);
        if (!BitConverter.IsLittleEndian) {
            BinaryPrimitives.ReverseEndianness(data, data);
        }
    }

    public override string ToString() {
        return "[" + data.Length + " ulongs]";
    }
}

public class NBTDStringList : NBTTag, INBTList {
    private readonly string[] data;

    public NBTType listType => NBTType.TAG_String;
    public override NBTType id => NBTType.TAG_List;

    public int count() => data.Length;

    public NBTDStringList(string? name, string[] source) : base(name) {
        data = source;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data.Length);
        foreach (var s in data) {
            if (s == null) {
                throw new InvalidOperationException("Palette contains null string");
            }

            stream.Write(s);
        }
    }

    public override void readContents(BinaryReader stream) {
        throw new NotSupportedException("NBTDStringList is write-only");
    }

    public override string ToString() {
        return "[" + data.Length + " strings]";
    }
}

public class NBTDByteList : NBTTag, INBTList {
    private readonly byte[] data;

    public NBTType listType => NBTType.TAG_Byte;
    public override NBTType id => NBTType.TAG_List;

    public int count() => data.Length;

    public NBTDByteList(string? name, byte[] source) : base(name) {
        data = source;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data.Length);
        stream.Write(data);
    }

    public override void readContents(BinaryReader stream) {
        throw new NotSupportedException("NBTDByteList is write-only");
    }

    public override string ToString() {
        return "[" + data.Length + " bytes]";
    }
}

public class NBTDUIntList : NBTTag, INBTList {
    private readonly uint[] data;

    public NBTType listType => NBTType.TAG_UInt;
    public override NBTType id => NBTType.TAG_List;

    public int count() => data.Length;

    public NBTDUIntList(string? name, uint[] source) : base(name) {
        data = source;
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data.Length);
        var values = new Span<uint>(data);
        if (!BitConverter.IsLittleEndian) {
            BinaryPrimitives.ReverseEndianness(values, values);
        }

        stream.Write(MemoryMarshal.AsBytes(values));
    }

    public override void readContents(BinaryReader stream) {
        throw new NotSupportedException("NBTDUIntList is write-only");
    }

    public override string ToString() {
        return "[" + data.Length + " uints]";
    }
}

/**
 * Generic struct storage for efficient serialization of unmanaged types.
 * Stores raw bytes with length prefix. Caller specifies type on read/write.
 */
public class NBTStruct : NBTTag {
    public byte[] data;

    public override NBTType id => NBTType.TAG_Struct;

    public NBTStruct(string? name) : base(name) {
        data = [];
    }

    public NBTStruct(string? name, byte[] rawData) : base(name) {
        data = rawData;
    }

    /** Create NBTStruct from any unmanaged type */
    public static unsafe NBTStruct create<T>(string? name, T value) where T : unmanaged {
        var tag = new NBTStruct(name);
        Span<byte> bytes = stackalloc byte[sizeof(T)];
        MemoryMarshal.Write(bytes, in value);
        tag.data = bytes.ToArray();
        return tag;
    }

    /** Read value as specified type with size validation */
    public unsafe T get<T>() where T : unmanaged {
        if (data.Length != sizeof(T)) {
            throw new InvalidOperationException(
                $"Size mismatch: expected {sizeof(T)} bytes for {typeof(T).Name}, got {data.Length} bytes");
        }

        return MemoryMarshal.Read<T>(data);
    }

    public override void writeContents(BinaryWriter stream) {
        stream.Write(data.Length); // length prefix so we can read without knowing T
        stream.Write(data);
    }

    public override void readContents(BinaryReader stream) {
        int length = stream.ReadInt32();
        if (length < 0 || length > 10_000) {
            // sanity check - structs shouldn't be huge
            throw new IOException($"Invalid struct size: {length}");
        }

        data = stream.ReadBytes(length);
    }

    public override string ToString() {
        return $"[{data.Length} bytes]";
    }
}