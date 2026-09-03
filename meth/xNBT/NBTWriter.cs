using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace BlockGame.util.xNBT;

/**
 * A streaming NBT writer which emits the same binary format as the tag tree without building one.
 * Use this for bulk serialization (chunks); use the tree when you want random access.
 *
 * TODO please somebody tell future me, do we even need the "tree" writer anymore or is that legacy crap?
 * or the advantage is the safety?
 *
 * Rules (unvalidated, don't fuck it up):
 * - inside a compound, every value needs a name; inside a list, nothing has a name
 * - beginList needs the element count upfront (the format has a length prefix).
 * - the root must be a single beginCompound(null) ... endCompound().
 */
public sealed class NBTWriter {
    private readonly BinaryWriter w;

    // one bit per depth level: 1 = inside a list
    private ulong listBits;
    private int depth;

    public NBTWriter(Stream stream) {
        w = new BinaryWriter(stream);
    }

    private bool inList => (listBits & (1UL << (depth - 1))) != 0;

    private void push(bool list) {
        depth++;
        if (list) {
            listBits |= 1UL << (depth - 1);
        }
        else {
            listBits &= ~(1UL << (depth - 1));
        }
    }

    private void pop() {
        depth--;
    }
    
    private void header(NBTType id, string? name) {
        if (depth > 0 && inList) {
            return;
        }

        w.Write((byte)id);
        w.Write(name ?? "");
    }

    public void beginCompound(string? name) {
        header(NBTType.TAG_Compound, name);
        push(false);
    }

    public void endCompound() {
        // TAG_End terminator
        w.Write((byte)0);
        pop();
    }

    public void beginList(string? name, NBTType elemType, int count) {
        header(NBTType.TAG_List, name);
        w.Write((byte)elemType);
        w.Write(count);
        push(true);
    }

    public void endList() {
        pop();
    }

    /** Splice a prebuilt tag tree into the stream, for anything that writes through Persistent. */
    public void writeTag(NBTTag tag) {
        if (depth > 0 && inList) {
            tag.writeContents(w);
        }
        else {
            NBTTag.write(tag, w);
        }
    }

    public void writeByte(string? name, byte v) {
        header(NBTType.TAG_Byte, name);
        w.Write(v);
    }

    public void writeSByte(string? name, sbyte v) {
        header(NBTType.TAG_SByte, name);
        w.Write(v);
    }

    public void writeShort(string? name, short v) {
        header(NBTType.TAG_Short, name);
        w.Write(v);
    }

    public void writeUShort(string? name, ushort v) {
        header(NBTType.TAG_UShort, name);
        w.Write(v);
    }

    public void writeInt(string? name, int v) {
        header(NBTType.TAG_Int, name);
        w.Write(v);
    }

    public void writeUInt(string? name, uint v) {
        header(NBTType.TAG_UInt, name);
        w.Write(v);
    }

    public void writeLong(string? name, long v) {
        header(NBTType.TAG_Long, name);
        w.Write(v);
    }

    public void writeULong(string? name, ulong v) {
        header(NBTType.TAG_ULong, name);
        w.Write(v);
    }

    public void writeFloat(string? name, float v) {
        header(NBTType.TAG_Float, name);
        w.Write(v);
    }

    public void writeDouble(string? name, double v) {
        header(NBTType.TAG_Double, name);
        w.Write(v);
    }

    public void writeString(string? name, string v) {
        header(NBTType.TAG_String, name);
        w.Write(v);
    }

    public void writeByteArray(string? name, ReadOnlySpan<byte> v) {
        header(NBTType.TAG_Byte_Array, name);
        w.Write(v.Length);
        w.Write(v);
    }

    public void writeSByteArray(string? name, ReadOnlySpan<sbyte> v) {
        header(NBTType.TAG_SByte_Array, name);
        w.Write(v.Length);
        w.Write(MemoryMarshal.AsBytes(v));
    }

    public void writeUShortArray(string? name, ReadOnlySpan<ushort> v) {
        header(NBTType.TAG_UShort_Array, name);
        writeSpanLE(v);
    }

    public void writeUIntArray(string? name, ReadOnlySpan<uint> v) {
        header(NBTType.TAG_UInt_Array, name);
        writeSpanLE(v);
    }

    public void writeIntArray(string? name, ReadOnlySpan<int> v) {
        header(NBTType.TAG_Int_Array, name);
        writeSpanLE(v);
    }
    
    // I'm not 100% sure the big endian support works but we don't use it so who cares
    private void writeSpanLE<T>(ReadOnlySpan<T> v) where T : unmanaged {
        w.Write(v.Length);
        if (BitConverter.IsLittleEndian) {
            w.Write(MemoryMarshal.AsBytes(v));
        }
        else {
            foreach (var e in v) {
                switch (e) {
                    case ushort us: w.Write(us); break;
                    case uint ui: w.Write(ui); break;
                    case int i: w.Write(i); break;
                    default: throw new NotSupportedException(typeof(T).Name);
                }
            }
        }
    }

    public void flush() {
        w.Flush();
    }
}
