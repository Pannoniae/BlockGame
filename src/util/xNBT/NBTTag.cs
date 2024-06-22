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

    public abstract NBTTagType id { get; }

    protected NBTTag() {
        name = "";
    }

    protected NBTTag(string? name) {
        this.name = name ?? "";
    }

    public static NBTTag read(BinaryReader stream) {
        NBTTagType type = (NBTTagType)stream.ReadByte();
        if (type == NBTTagType.TAG_End) {
            return new NBTTagEnd();
        }
        string name = stream.ReadString();
        NBTTag tag = createTag(type, name);
        tag.readContents(stream);
        return tag;
    }

    public static void write(NBTTag tag, BinaryWriter stream) {
        stream.Write((byte)tag.id);
        if (tag.id != NBTTagType.TAG_End) {
            stream.Write(tag.name);
            tag.writeContents(stream);
        }
    }

    public static NBTTag createTag(NBTTagType tag, string? name) {
        // todo
        switch (tag) {
            case NBTTagType.TAG_End:
                return new NBTTagEnd();
            case NBTTagType.TAG_Byte:
                return new NBTTagByte(name);
            case NBTTagType.TAG_Short:
                return new NBTTagShort(name);
            case NBTTagType.TAG_UShort:
                return new NBTTagUShort(name);
            case NBTTagType.TAG_Int:
                return new NBTTagInt(name);
            case NBTTagType.TAG_UInt:
                return new NBTTagUInt(name);
            case NBTTagType.TAG_Long:
                return new NBTTagLong(name);
            case NBTTagType.TAG_ULong:
                return new NBTTagULong(name);
            case NBTTagType.TAG_Float:
                return new NBTTagFloat(name);
            case NBTTagType.TAG_Double:
                return new NBTTagDouble(name);
            case NBTTagType.TAG_String:
                return new NBTTagString(name);
            case NBTTagType.TAG_List:
                return new NBTTagList<NBTTag>(name);
            case NBTTagType.TAG_Compound:
                return new NBTTagCompound(name);
            case NBTTagType.TAG_Byte_Array:
                return new NBTTagByteArray(name);
            case NBTTagType.TAG_Short_Array:
                return new NBTTagShortArray(name);
            case NBTTagType.TAG_UShort_Array:
                return new NBTTagUShortArray(name);
            case NBTTagType.TAG_Int_Array:
                return new NBTTagIntArray(name);
            case NBTTagType.TAG_UInt_Array:
                return new NBTTagUIntArray(name);
            case NBTTagType.TAG_Long_Array:
                return new NBTTagLongArray(name);
            case NBTTagType.TAG_ULong_Array:
                return new NBTTagULongArray(name);
            default:
                throw new ArgumentOutOfRangeException(nameof(tag), tag, null);
        }
    }

    public static string getTypeName(NBTTagType id) {
        return id switch {
            NBTTagType.TAG_End => "TAG_End",
            NBTTagType.TAG_Byte => "TAG_Byte",
            NBTTagType.TAG_Short => "TAG_Short",
            NBTTagType.TAG_UShort => "TAG_UShort",
            NBTTagType.TAG_Int => "TAG_Int",
            NBTTagType.TAG_UInt => "TAG_UInt",
            NBTTagType.TAG_Long => "TAG_Long",
            NBTTagType.TAG_ULong => "TAG_ULong",
            NBTTagType.TAG_Float => "TAG_Float",
            NBTTagType.TAG_Double => "TAG_Double",
            NBTTagType.TAG_String => "TAG_String",
            NBTTagType.TAG_List => "TAG_List",
            NBTTagType.TAG_Compound => "TAG_Compound",
            NBTTagType.TAG_Byte_Array => "TAG_Byte_Array",
            NBTTagType.TAG_Short_Array => "TAG_Short_Array",
            NBTTagType.TAG_UShort_Array => "TAG_UShort_Array",
            NBTTagType.TAG_Int_Array => "TAG_Int_Array",
            NBTTagType.TAG_UInt_Array => "TAG_UInt_Array",
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

public enum NBTTagType : byte {
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