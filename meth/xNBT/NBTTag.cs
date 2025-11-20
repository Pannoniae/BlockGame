using System.Text;

namespace BlockGame.util.xNBT;

public abstract class NBTTag : IEquatable<NBTTag> {
    public readonly string name;

    /// <summary>
    /// Read the contents of this tag from a stream.
    /// </summary>
    public abstract void readContents(BinaryReader stream);

    /// <summary>
    /// Write the contents of this tag into a stream.
    /// </summary>
    public abstract void writeContents(BinaryWriter stream);

    public abstract NBTType id { get; }

    protected NBTTag() {
        name = "";
    }

    protected NBTTag(string? name) {
        this.name = name ?? "";
    }

    public static NBTTag read(BinaryReader stream) {
        NBTType type = (NBTType)stream.ReadByte();
        if (type == NBTType.TAG_End) {
            return new NBTEnd();
        }
        string name = stream.ReadString();
        // lists have a type
        NBTTag tag;
        if (type == NBTType.TAG_List) {
            NBTType listType = (NBTType)stream.ReadByte();
            tag = createListTag(listType, name);
        }
        else {
            tag = createTag(type, name);
        }
        tag.readContents(stream);
        return tag;
    }

    public static void write(NBTTag tag, BinaryWriter stream) {
        stream.Write((byte)tag.id);
        if (tag.id == NBTType.TAG_End) {
            return;
        }
        stream.Write(tag.name);
        if (tag.id == NBTType.TAG_List) {
            // we can cast to anything, doesn't matter
            NBTType listType = ((INBTList)tag).listType;
            stream.Write((byte)listType);
        }
        tag.writeContents(stream);
    }

    public static NBTTag readS(string s) {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(s));
        using var reader = new BinaryReader(stream);
        return read(reader);
    }
    
    public static string writeS(NBTTag tag) {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        write(tag, writer);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static NBTTag createTag(NBTType tag, string? name) {
        return tag switch {
            NBTType.TAG_End => new NBTEnd(),
            NBTType.TAG_Byte => new NBTByte(name),
            NBTType.TAG_SByte => new NBTSByte(name),
            NBTType.TAG_Short => new NBTShort(name),
            NBTType.TAG_UShort => new NBTUShort(name),
            NBTType.TAG_Int => new NBTInt(name),
            NBTType.TAG_UInt => new NBTUInt(name),
            NBTType.TAG_Long => new NBTLong(name),
            NBTType.TAG_ULong => new NBTULong(name),
            NBTType.TAG_Float => new NBTFloat(name),
            NBTType.TAG_Double => new NBTDouble(name),
            NBTType.TAG_String => new NBTString(name),
            NBTType.TAG_List => throw new ArgumentException("Cannot create a TAG_List without a type", nameof(tag)),
            NBTType.TAG_Compound => new NBTCompound(name),
            NBTType.TAG_Byte_Array => new NBTByteArray(name),
            NBTType.TAG_SByte_Array => new NBTSByteArray(name),
            NBTType.TAG_Short_Array => new NBTShortArray(name),
            NBTType.TAG_UShort_Array => new NBTUShortArray(name),
            NBTType.TAG_Int_Array => new NBTIntArray(name),
            NBTType.TAG_UInt_Array => new NBTUIntArray(name),
            NBTType.TAG_Long_Array => new NBTLongArray(name),
            NBTType.TAG_ULong_Array => new NBTULongArray(name),
            _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, null)
        };
    }
    
    public static NBTTag createListTag(NBTType listType, string name) {
        return listType switch {
            NBTType.TAG_End => new NBTList<NBTEnd>(listType, name),
            NBTType.TAG_Byte => new NBTList<NBTByte>(listType, name),
            NBTType.TAG_SByte => new NBTList<NBTSByte>(listType, name),
            NBTType.TAG_Short => new NBTList<NBTShort>(listType, name),
            NBTType.TAG_UShort => new NBTList<NBTUShort>(listType, name),
            NBTType.TAG_Int => new NBTList<NBTInt>(listType, name),
            NBTType.TAG_UInt => new NBTList<NBTUInt>(listType, name),
            NBTType.TAG_Long => new NBTList<NBTLong>(listType, name),
            NBTType.TAG_ULong => new NBTList<NBTULong>(listType, name),
            NBTType.TAG_Float => new NBTList<NBTFloat>(listType, name),
            NBTType.TAG_Double => new NBTList<NBTDouble>(listType, name),
            NBTType.TAG_String => new NBTList<NBTString>(listType, name),
            NBTType.TAG_List => new NBTList<NBTList<NBTTag>>(listType, name),
            NBTType.TAG_Compound => new NBTList<NBTCompound>(listType, name),
            NBTType.TAG_Byte_Array => new NBTList<NBTByteArray>(listType, name),
            NBTType.TAG_SByte_Array => new NBTList<NBTSByteArray>(listType, name),
            NBTType.TAG_Short_Array => new NBTList<NBTShortArray>(listType, name),
            NBTType.TAG_UShort_Array => new NBTList<NBTUShortArray>(listType, name),
            NBTType.TAG_Int_Array => new NBTList<NBTIntArray>(listType, name),
            NBTType.TAG_UInt_Array => new NBTList<NBTUIntArray>(listType, name),
            NBTType.TAG_Long_Array => new NBTList<NBTLongArray>(listType, name),
            NBTType.TAG_ULong_Array => new NBTList<NBTULongArray>(listType, name),
            _ => throw new ArgumentOutOfRangeException(nameof(listType), listType, null)
        };
    }

    public static string getTypeName(NBTType id) {
        return id switch {
            NBTType.TAG_End => "TAG_End",
            NBTType.TAG_Byte => "TAG_Byte",
            NBTType.TAG_SByte => "TAG_SByte",
            NBTType.TAG_Short => "TAG_Short",
            NBTType.TAG_UShort => "TAG_UShort",
            NBTType.TAG_Int => "TAG_Int",
            NBTType.TAG_UInt => "TAG_UInt",
            NBTType.TAG_Long => "TAG_Long",
            NBTType.TAG_ULong => "TAG_ULong",
            NBTType.TAG_Float => "TAG_Float",
            NBTType.TAG_Double => "TAG_Double",
            NBTType.TAG_String => "TAG_String",
            NBTType.TAG_List => "TAG_List",
            NBTType.TAG_Compound => "TAG_Compound",
            NBTType.TAG_Byte_Array => "TAG_Byte_Array",
            NBTType.TAG_SByte_Array => "TAG_SByte_Array",
            NBTType.TAG_Short_Array => "TAG_Short_Array",
            NBTType.TAG_UShort_Array => "TAG_UShort_Array",
            NBTType.TAG_Int_Array => "TAG_Int_Array",
            NBTType.TAG_UInt_Array => "TAG_UInt_Array",
            NBTType.TAG_Long_Array => "TAG_Long_Array",
            NBTType.TAG_ULong_Array => "TAG_ULong_Array",
            _ => "UNKNOWN"
        };
    }

    // BOILERPLATE

    public bool Equals(NBTTag? other) {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return string.Equals(name, other.name, StringComparison.InvariantCulture) && id == other.id;
    }
    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((NBTTag)obj);
    }
    public override int GetHashCode() {
        var hashCode = new HashCode();
        hashCode.Add(name, StringComparer.InvariantCulture);
        hashCode.Add((int)id);
        return hashCode.ToHashCode();
    }
}

public enum NBTType : byte {
    TAG_End,
    TAG_Byte,
    TAG_Short,
    TAG_UShort,
    TAG_Int,
    TAG_UInt,
    TAG_Long,
    TAG_ULong,
    TAG_Float,
    TAG_Double,
    TAG_String,
    TAG_List,
    TAG_Compound,
    TAG_Byte_Array,
    TAG_Short_Array,
    TAG_UShort_Array,
    TAG_Int_Array,
    TAG_UInt_Array,
    TAG_Long_Array,
    TAG_ULong_Array,
    TAG_SByte,
    TAG_SByte_Array
}