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
        NBTTag tag = createTag(type, name);
        tag.readContents(stream);
        return tag;
    }

    public static void write(NBTTag tag, BinaryWriter stream) {
        stream.Write((byte)tag.id);
        if (tag.id != NBTType.TAG_End) {
            stream.Write(tag.name);
            tag.writeContents(stream);
        }
    }

    public static NBTTag createTag(NBTType tag, string? name) {
        // todo
        switch (tag) {
            case NBTType.TAG_End:
                return new NBTEnd();
            case NBTType.TAG_Byte:
                return new NBTByte(name);
            case NBTType.TAG_Short:
                return new NBTShort(name);
            case NBTType.TAG_UShort:
                return new NBTUShort(name);
            case NBTType.TAG_Int:
                return new NBTInt(name);
            case NBTType.TAG_UInt:
                return new NBTUInt(name);
            case NBTType.TAG_Long:
                return new NBTLong(name);
            case NBTType.TAG_ULong:
                return new NBTULong(name);
            case NBTType.TAG_Float:
                return new NBTFloat(name);
            case NBTType.TAG_Double:
                return new NBTDouble(name);
            case NBTType.TAG_String:
                return new NBTString(name);
            case NBTType.TAG_List:
                return new NBTList<NBTTag>(name);
            case NBTType.TAG_Compound:
                return new NBTCompound(name);
            case NBTType.TAG_Byte_Array:
                return new NBTByteArray(name);
            case NBTType.TAG_Short_Array:
                return new NBTShortArray(name);
            case NBTType.TAG_UShort_Array:
                return new NBTUShortArray(name);
            case NBTType.TAG_Int_Array:
                return new NBTIntArray(name);
            case NBTType.TAG_UInt_Array:
                return new NBTUIntArray(name);
            case NBTType.TAG_Long_Array:
                return new NBTLongArray(name);
            case NBTType.TAG_ULong_Array:
                return new NBTULongArray(name);
            default:
                throw new ArgumentOutOfRangeException(nameof(tag), tag, null);
        }
    }

    public static string getTypeName(NBTType id) {
        return id switch {
            NBTType.TAG_End => "TAG_End",
            NBTType.TAG_Byte => "TAG_Byte",
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
            NBTType.TAG_Short_Array => "TAG_Short_Array",
            NBTType.TAG_UShort_Array => "TAG_UShort_Array",
            NBTType.TAG_Int_Array => "TAG_Int_Array",
            NBTType.TAG_UInt_Array => "TAG_UInt_Array",
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
    TAG_ULong_Array
}