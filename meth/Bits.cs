using System.Numerics;

namespace BlockGame.util;

/**
 * Compact bitfield wrappers for networking and state packing.
 * Slightly inspired by Terraria's BitsByte. Or something.
 */
public struct Bits8(byte value) {
    public byte value = value;

    public bool this[int i] {
        get => (value & (1 << i)) != 0;
        set => this.value = (byte)(value ? this.value | (1 << i) : this.value & ~(1 << i));
    }

    public byte get(int offset, int count) {
        var mask = (1 << count) - 1;
        return (byte)((value >> offset) & mask);
    }

    public void set(int offset, int count, byte val) {
        var mask = (1 << count) - 1;
        value = (byte)((value & ~(mask << offset)) | ((val & mask) << offset));
    }

    public void clear() => value = 0;
    public void setAll() => value = 0xFF;
    public bool hasAny() => value != 0;
    public bool hasAll() => value == 0xFF;
    public int popCount() => BitOperations.PopCount(value);

    public static implicit operator byte(Bits8 b) => b.value;
    public static implicit operator Bits8(byte v) => new(v);

    public override string ToString() => Convert.ToString(value, 2).PadLeft(8, '0');
}

public struct Bits16(ushort value) {
    public ushort value = value;

    public bool this[int i] {
        get => (value & (1 << i)) != 0;
        set => this.value = (ushort)(value ? this.value | (1 << i) : this.value & ~(1 << i));
    }

    public ushort get(int offset, int count) {
        var mask = (1 << count) - 1;
        return (ushort)((value >> offset) & mask);
    }

    public void set(int offset, int count, ushort val) {
        var mask = (1 << count) - 1;
        value = (ushort)((value & ~(mask << offset)) | ((val & mask) << offset));
    }

    public void clear() => value = 0;
    public void setAll() => value = 0xFFFF;
    public bool hasAny() => value != 0;
    public bool hasAll() => value == 0xFFFF;
    public int popCount() => BitOperations.PopCount(value);

    public static implicit operator ushort(Bits16 b) => b.value;
    public static implicit operator Bits16(ushort v) => new(v);

    public override string ToString() => Convert.ToString(value, 2).PadLeft(16, '0');
}

public struct Bits32(uint value) {
    public uint value = value;

    public bool this[int i] {
        get => (value & (1u << i)) != 0;
        set => this.value = value ? this.value | (1u << i) : this.value & ~(1u << i);
    }

    public uint get(int offset, int count) {
        var mask = (1u << count) - 1;
        return (value >> offset) & mask;
    }

    public void set(int offset, int count, uint val) {
        var mask = (1u << count) - 1;
        value = (value & ~(mask << offset)) | ((val & mask) << offset);
    }

    public void clear() => value = 0;
    public void setAll() => value = 0xFFFFFFFF;
    public bool hasAny() => value != 0;
    public bool hasAll() => value == 0xFFFFFFFF;
    public int popCount() => BitOperations.PopCount(value);

    public static implicit operator uint(Bits32 b) => b.value;
    public static implicit operator Bits32(uint v) => new(v);

    public override string ToString() => Convert.ToString(value, 2).PadLeft(32, '0');
}

public struct Bits64(ulong value) {
    public ulong value = value;

    public bool this[int i] {
        get => (value & (1ul << i)) != 0;
        set => this.value = value ? this.value | (1ul << i) : this.value & ~(1ul << i);
    }

    public ulong get(int offset, int count) {
        var mask = (1ul << count) - 1;
        return (value >> offset) & mask;
    }

    public void set(int offset, int count, ulong val) {
        var mask = (1ul << count) - 1;
        value = (value & ~(mask << offset)) | ((val & mask) << offset);
    }

    public void clear() => value = 0;
    public void setAll() => value = 0xFFFFFFFFFFFFFFFF;
    public bool hasAny() => value != 0;
    public bool hasAll() => value == 0xFFFFFFFFFFFFFFFF;
    public int popCount() => BitOperations.PopCount(value);

    public static implicit operator ulong(Bits64 b) => b.value;
    public static implicit operator Bits64(ulong v) => new(v);

    public override string ToString() => Convert.ToString((long)value, 2).PadLeft(64, '0');
}