using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BlockGame.world.entity;

/** delta-encoded entity state synchronization */
public class EntityState {
    private readonly Dictionary<byte, StateField> fields = new();

    private struct StateField {
        public byte fieldID;
        public FieldType type;
        public ulong value;  // union - 8 bytes fits all primitive types
        public bool dirty;
    }

    public enum FieldType : byte {
        BOOL_FALSE = 0x01, // no payload
        BOOL_TRUE = 0x02, // no payload
        BYTE = 0x03, // 1 byte payload
        SHORT = 0x04, // 2 bytes
        USHORT = 0x05, // 4 bytes
        INT = 0x06, // 4 bytes
        UINT = 0x07, // 4 bytes
        LONG = 0x08, // 8 bytes
        FLOAT = 0x09, // 4 bytes
        DOUBLE = 0x10 // 8 bytes
    }

    // standard field IDs (0-31, shared across all entities)
    public const byte SNEAKING = 0;
    public const byte ON_FIRE = 1;
    public const byte FLYING = 2;
    public const byte RIDING = 3; // int: entity ID of mount, or -1
    public const byte ON_GROUND = 4;

    // player-specific (32-63)
    public const byte PLAYER_GAMEMODE = 32; // byte: 0=survival, 1=creative

    // future entity types (64-95, 96-127, etc.)

    // ============ SETTERS (mark dirty automatically) ============

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void setBool(byte fieldID, bool value) {
        if (fields.TryGetValue(fieldID, out var existing)) {
            bool currentValue = existing.type == FieldType.BOOL_TRUE;
            if (currentValue == value) {
                return; // no change
            }
        }

        fields[fieldID] = new StateField {
            fieldID = fieldID,
            type = value ? FieldType.BOOL_TRUE : FieldType.BOOL_FALSE,
            value = 0,
            dirty = true
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void setByte(byte fieldID, byte value) {
        if (fields.TryGetValue(fieldID, out var existing) &&
            existing.type == FieldType.BYTE &&
            (byte)existing.value == value) {
            return; // no change
        }

        fields[fieldID] = new StateField {
            fieldID = fieldID,
            type = FieldType.BYTE,
            value = value,
            dirty = true
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void setShort(byte fieldID, short value) {
        if (fields.TryGetValue(fieldID, out var existing) &&
            existing.type == FieldType.SHORT &&
            Unsafe.As<ulong, short>(ref Unsafe.AsRef(in existing.value)) == value) {
            return; // no change
        }

        ulong bits = 0;
        Unsafe.As<ulong, short>(ref bits) = value;
        fields[fieldID] = new StateField {
            fieldID = fieldID,
            type = FieldType.SHORT,
            value = bits,
            dirty = true
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void setInt(byte fieldID, int value) {
        if (fields.TryGetValue(fieldID, out var existing) &&
            existing.type == FieldType.INT &&
            Unsafe.As<ulong, int>(ref Unsafe.AsRef(in existing.value)) == value) {
            return; // no change
        }

        ulong bits = 0;
        Unsafe.As<ulong, int>(ref bits) = value;
        fields[fieldID] = new StateField {
            fieldID = fieldID,
            type = FieldType.INT,
            value = bits,
            dirty = true
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void setLong(byte fieldID, long value) {
        if (fields.TryGetValue(fieldID, out var existing) &&
            existing.type == FieldType.LONG &&
            Unsafe.As<ulong, long>(ref Unsafe.AsRef(in existing.value)) == value) {
            return; // no change
        }

        ulong bits = 0;
        Unsafe.As<ulong, long>(ref bits) = value;
        fields[fieldID] = new StateField {
            fieldID = fieldID,
            type = FieldType.LONG,
            value = bits,
            dirty = true
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void setUShort(byte fieldID, ushort value) {
        if (fields.TryGetValue(fieldID, out var existing) &&
            existing.type == FieldType.USHORT &&
            Unsafe.As<ulong, ushort>(ref Unsafe.AsRef(in existing.value)) == value) {
            return; // no change
        }

        ulong bits = 0;
        Unsafe.As<ulong, ushort>(ref bits) = value;
        fields[fieldID] = new StateField {
            fieldID = fieldID,
            type = FieldType.USHORT,
            value = bits,
            dirty = true
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void setUInt(byte fieldID, uint value) {
        if (fields.TryGetValue(fieldID, out var existing) &&
            existing.type == FieldType.UINT &&
            Unsafe.As<ulong, uint>(ref Unsafe.AsRef(in existing.value)) == value) {
            return; // no change
        }

        ulong bits = 0;
        Unsafe.As<ulong, uint>(ref bits) = value;
        fields[fieldID] = new StateField {
            fieldID = fieldID,
            type = FieldType.UINT,
            value = bits,
            dirty = true
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void setFloat(byte fieldID, float value) {
        if (fields.TryGetValue(fieldID, out var existing) &&
            existing.type == FieldType.FLOAT &&
            Unsafe.As<ulong, float>(ref Unsafe.AsRef(in existing.value)) == value) {
            return; // no change
        }

        ulong bits = 0;
        Unsafe.As<ulong, float>(ref bits) = value;
        fields[fieldID] = new StateField {
            fieldID = fieldID,
            type = FieldType.FLOAT,
            value = bits,
            dirty = true
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void setDouble(byte fieldID, double value) {
        if (fields.TryGetValue(fieldID, out var existing) &&
            existing.type == FieldType.DOUBLE &&
            Unsafe.As<ulong, double>(ref Unsafe.AsRef(in existing.value)) == value) {
            return; // no change
        }

        ulong bits = 0;
        Unsafe.As<ulong, double>(ref bits) = value;
        fields[fieldID] = new StateField {
            fieldID = fieldID,
            type = FieldType.DOUBLE,
            value = bits,
            dirty = true
        };
    }

    // ============ GETTERS ============

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool getBool(byte fieldID, bool defaultValue = false) {
        if (!fields.TryGetValue(fieldID, out var field)) return defaultValue;
        return field.type == FieldType.BOOL_TRUE;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte getByte(byte fieldID, byte defaultValue = 0) {
        if (!fields.TryGetValue(fieldID, out var field) || field.type != FieldType.BYTE)
            return defaultValue;
        return (byte)field.value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short getShort(byte fieldID, short defaultValue = 0) {
        if (!fields.TryGetValue(fieldID, out var field) || field.type != FieldType.SHORT)
            return defaultValue;
        return Unsafe.As<ulong, short>(ref Unsafe.AsRef(in field.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int getInt(byte fieldID, int defaultValue = 0) {
        if (!fields.TryGetValue(fieldID, out var field) || field.type != FieldType.INT)
            return defaultValue;
        return Unsafe.As<ulong, int>(ref Unsafe.AsRef(in field.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long getLong(byte fieldID, long defaultValue = 0) {
        if (!fields.TryGetValue(fieldID, out var field) || field.type != FieldType.LONG)
            return defaultValue;
        return Unsafe.As<ulong, long>(ref Unsafe.AsRef(in field.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort getUShort(byte fieldID, ushort defaultValue = 0) {
        if (!fields.TryGetValue(fieldID, out var field) || field.type != FieldType.USHORT)
            return defaultValue;
        return Unsafe.As<ulong, ushort>(ref Unsafe.AsRef(in field.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint getUInt(byte fieldID, uint defaultValue = 0) {
        if (!fields.TryGetValue(fieldID, out var field) || field.type != FieldType.UINT)
            return defaultValue;
        return Unsafe.As<ulong, uint>(ref Unsafe.AsRef(in field.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float getFloat(byte fieldID, float defaultValue = 0f) {
        if (!fields.TryGetValue(fieldID, out var field) || field.type != FieldType.FLOAT)
            return defaultValue;
        return Unsafe.As<ulong, float>(ref Unsafe.AsRef(in field.value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double getDouble(byte fieldID, double defaultValue = 0.0) {
        if (!fields.TryGetValue(fieldID, out var field) || field.type != FieldType.DOUBLE)
            return defaultValue;
        return Unsafe.As<ulong, double>(ref Unsafe.AsRef(in field.value));
    }

    // ============ DIRTY FLAG MANAGEMENT ============

    public bool isDirty() => fields.Values.Any(f => f.dirty);

    public void clearDirty() {
        // use CollectionsMarshal to get ref and modify in-place (zero-copy)
        foreach (var key in fields.Keys) {
            ref var field = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrNullRef(fields, key);
            if (!Unsafe.IsNullRef(ref field)) {
                field.dirty = false;
            }
        }
    }

    /** mark all fields dirty (for initial sync on spawn) */
    public void markAllDirty() {
        foreach (var key in fields.Keys) {
            ref var field = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrNullRef(fields, key);
            if (!Unsafe.IsNullRef(ref field)) {
                field.dirty = true;
            }
        }
    }

    // ============ SERIALIZATION ============

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int getByteCount(FieldType type) => type switch {
        FieldType.BOOL_FALSE or FieldType.BOOL_TRUE => 0,
        FieldType.BYTE => 1,
        FieldType.SHORT or FieldType.USHORT => 2,
        FieldType.INT or FieldType.UINT or FieldType.FLOAT => 4,
        FieldType.LONG or FieldType.DOUBLE => 8,
        _ => 0
    };

    /** serialize only dirty fields (zero-alloc except final array) */
    public unsafe byte[] serialize() {
        Span<byte> buffer = stackalloc byte[256];
        int offset = 0;

        foreach (var key in fields.Keys) {
            ref var field = ref CollectionsMarshal.GetValueRefOrNullRef(fields, key);
            if (Unsafe.IsNullRef(ref field) || !field.dirty) continue;

            buffer[offset++] = field.fieldID;
            buffer[offset++] = (byte)field.type;

            // write value bytes directly from ulong
            int byteCount = getByteCount(field.type);
            if (byteCount > 0) {
                var valueBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in field.value), 1));
                valueBytes[..byteCount].CopyTo(buffer[offset..]);
                offset += byteCount;
            }

            // clear dirty flag in-place
            field.dirty = false;
        }

        buffer[offset++] = 0xFF; // terminator
        return buffer[..offset].ToArray();
    }

    /** serialize all fields (for initial sync) */
    public unsafe byte[] serializeAll() {
        Span<byte> buffer = stackalloc byte[256];
        int offset = 0;

        foreach (var field in fields.Values) {
            buffer[offset++] = field.fieldID;
            buffer[offset++] = (byte)field.type;

            int byteCount = getByteCount(field.type);
            if (byteCount > 0) {
                var valueBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in field.value), 1));
                valueBytes[..byteCount].CopyTo(buffer[offset..]);
                offset += byteCount;
            }
        }

        buffer[offset++] = 0xFF;
        return buffer[..offset].ToArray();
    }

    /** deserialize from byte array */
    public unsafe void deserialize(byte[] data) {
        int i = 0;
        while (i < data.Length) {
            byte fieldID = data[i++];
            if (fieldID == 0xFF) break; // terminator

            FieldType type = (FieldType)data[i++];
            int byteCount = getByteCount(type);

            ulong value = 0;
            if (byteCount > 0) {
                // read bytes directly into ulong
                var valueBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));
                data.AsSpan(i, byteCount).CopyTo(valueBytes);
                i += byteCount;
            }

            fields[fieldID] = new StateField {
                fieldID = fieldID,
                type = type,
                value = value,
                dirty = false
            };
        }
    }

}