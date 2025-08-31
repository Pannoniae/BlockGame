namespace BlockGame.util;

/**
 * Slightly inspired by Terraria's BitsByte, but simpler and more general. Or something.
 */
public struct Bits(byte value) {
    private byte _value = value;

    public bool this[int index] {
        get => (_value & (1 << index)) != 0;
        set => _value = (byte)(value ? _value | (1 << index) : _value & ~(1 << index));
    }

    public byte get(int offset, int count) {
        var mask = (1 << count) - 1;
        return (byte)((_value >> offset) & mask);
    }

    public void set(int offset, int count, byte value) {
        var mask = (1 << count) - 1;
        _value = (byte)((_value & ~(mask << offset)) | ((value & mask) << offset));
    }

    public static implicit operator byte(Bits bits) => bits._value;
    public static implicit operator Bits(byte value) => new(value);

    public override string ToString() => Convert.ToString(_value, 2).PadLeft(8, '0');
}