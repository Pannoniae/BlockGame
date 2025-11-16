using System.Collections;
using System.Runtime.CompilerServices;

namespace BlockGame.util;

/**
 * Specialized hash map for long keys.
 * Better hash function than XMap&lt;long, V&gt; using a MurmurHash3 finaliser.
 * Used for packed chunk coordinates.
 */
public class XLongMap<V> : IEnumerable<V> {

    private struct Entry {
        public long key;
        public int hash;  // 0 = empty slot, -1 = tombstone
        public V value;
    }

    private Entry[] entries;
    private int count;
    private int tombstones;

    private const int DEFAULT_CAPACITY = 16;
    private const float LOAD_FACTOR = 0.75f;

    public XLongMap(int capacity = DEFAULT_CAPACITY) {
        int size = NextPow2(Math.Max(capacity, DEFAULT_CAPACITY));
        entries = new Entry[size];
        count = 0;
    }

    public int Count => count;

    public ref V this[long key] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            int hash = GetHash(key);
            int mask = entries.Length - 1;
            int idx = hash & mask;

            while (entries[idx].hash != 0) {
                if (entries[idx].hash == hash && entries[idx].key == key) {
                    return ref entries[idx].value;
                }
                idx = (idx + 1) & mask;
            }

            InputException.throwNew($"Key not found: {key}");
            return ref entries[0].value; // unreachable
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(long key, out V value) {
        int hash = GetHash(key);
        int mask = entries.Length - 1;
        int idx = hash & mask;

        while (entries[idx].hash != 0) {
            if (entries[idx].hash == hash && entries[idx].key == key) {
                value = entries[idx].value;
                return true;
            }
            idx = (idx + 1) & mask;
        }

        value = default!;
        return false;
    }

    /** Gets an entry if it exists, or adds a new entry and returns it */
    public ref V GetOrAdd(long key, out bool added) {
        return ref GetOrAdd(key, default!, out added);
    }

    /** Gets an entry if it exists, or adds a new entry and returns it */
    public ref V GetOrAdd(long key, V defaultValue, out bool added) {
        added = false;
        if (count + tombstones >= entries.Length * LOAD_FACTOR) {
            Resize();
        }

        int hash = GetHash(key);
        int mask = entries.Length - 1;
        int idx = hash & mask;
        int tombstoneIdx = -1;

        while (entries[idx].hash != 0) {
            if (entries[idx].hash == -1) {
                if (tombstoneIdx == -1) {
                    tombstoneIdx = idx;
                }
            }
            else if (entries[idx].hash == hash && entries[idx].key == key) {
                return ref entries[idx].value;
            }
            idx = (idx + 1) & mask;
        }

        int insertIdx = tombstoneIdx != -1 ? tombstoneIdx : idx;
        if (tombstoneIdx != -1) {
            tombstones--;
        }
        entries[insertIdx].hash = hash;
        entries[insertIdx].key = key;
        entries[insertIdx].value = defaultValue;
        count++;
        added = true;
        return ref entries[insertIdx].value;
    }

    public void Add(long key, V value) {
        Insert(key, value, false);
    }

    public void Set(long key, V value) {
        Insert(key, value, true);
    }

    public bool Remove(long key) {
        int hash = GetHash(key);
        int mask = entries.Length - 1;
        int idx = hash & mask;

        while (entries[idx].hash != 0) {
            if (entries[idx].hash == hash && entries[idx].key == key) {
                entries[idx].hash = -1; // tombstone
                entries[idx].key = 0;
                entries[idx].value = default!;
                count--;
                tombstones++;
                return true;
            }
            idx = (idx + 1) & mask;
        }

        return false;
    }

    public bool ContainsKey(long key) {
        return TryGetValue(key, out _);
    }

    public void Clear() {
        Array.Clear(entries);
        count = 0;
        tombstones = 0;
    }

    public void Insert(long key, V value, bool overwrite) {
        if (count + tombstones >= entries.Length * LOAD_FACTOR) {
            Resize();
        }

        int hash = GetHash(key);
        int mask = entries.Length - 1;
        int idx = hash & mask;
        int tombstoneIdx = -1;

        while (entries[idx].hash != 0) {
            if (entries[idx].hash == -1) {
                if (tombstoneIdx == -1) {
                    tombstoneIdx = idx;
                }
            }
            else if (entries[idx].hash == hash && entries[idx].key == key) {
                if (!overwrite) {
                    InputException.throwNew($"Key already exists: {key}");
                }
                entries[idx].value = value;
                return;
            }
            idx = (idx + 1) & mask;
        }

        int insertIdx = tombstoneIdx != -1 ? tombstoneIdx : idx;
        if (tombstoneIdx != -1) {
            tombstones--;
        }
        entries[insertIdx].hash = hash;
        entries[insertIdx].key = key;
        entries[insertIdx].value = value;
        count++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetHash(long key) {
        // MurmurHash3 finaliser for 64-bit keys
        ulong h = (ulong)key;
        h ^= h >> 33;
        h *= 0xff51afd7ed558ccdUL;
        h ^= h >> 33;
        h *= 0xc4ceb9fe1a85ec53UL;
        h ^= h >> 33;

        int hash = (int)h;
        return hash is 0 or -1 ? 2 : hash;
    }

    private void Resize() {
        var oldEntries = entries;
        entries = new Entry[oldEntries.Length * 2];
        int oldCount = count;
        count = 0;
        tombstones = 0;

        foreach (var e in oldEntries) {
            if (e.hash != 0 && e.hash != -1) {
                Insert(e.key, e.value, false);
            }
        }

        if (count != oldCount) {
            SkillIssueException.throwNew("Count mismatch during resize");
        }
    }

    public Enumerator GetEnumerator() => new Enumerator(this);
    public PairEnumerable Pairs => new PairEnumerable(this);

    IEnumerator<V> IEnumerable<V>.GetEnumerator() {
        for (int i = 0; i < entries.Length; i++) {
            if (entries[i].hash != 0 && entries[i].hash != -1) {
                yield return entries[i].value;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<V>)this).GetEnumerator();

    public struct Enumerator {
        private readonly XLongMap<V> map;
        private int idx;

        internal Enumerator(XLongMap<V> map) {
            this.map = map;
            idx = -1;
        }

        public bool MoveNext() {
            idx++;
            while (idx < map.entries.Length) {
                var hash = map.entries[idx].hash;
                if (hash != 0 && hash != -1) {
                    return true;
                }
                idx++;
            }
            return false;
        }

        /** Returns ref to value for in-place mutation */
        public ref V Current {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref map.entries[idx].value;
        }

        public void Reset() {
            idx = -1;
        }
    }

    public readonly struct PairEnumerable {
        private readonly XLongMap<V> map;

        internal PairEnumerable(XLongMap<V> map) {
            this.map = map;
        }

        public PairEnumerator GetEnumerator() => new PairEnumerator(map);
    }

    public struct PairEnumerator {
        private readonly XLongMap<V> map;
        private int idx;

        internal PairEnumerator(XLongMap<V> map) {
            this.map = map;
            idx = -1;
        }

        public bool MoveNext() {
            idx++;
            while (idx < map.entries.Length) {
                var hash = map.entries[idx].hash;
                if (hash != 0 && hash != -1) {
                    return true;
                }
                idx++;
            }
            return false;
        }

        public Pair Current {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Pair(map.entries[idx].key, ref map.entries[idx].value);
        }

        public void Reset() {
            idx = -1;
        }
    }

    public readonly ref struct Pair {
        public readonly long Key;
        private readonly ref V val;

        internal Pair(long key, ref V value) {
            this.Key = key;
            this.val = ref value;
        }

        /** Returns ref to value for in-place mutation */
        public ref V Value {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref val;
        }
    }

    private static int NextPow2(int n) {
        n--;
        n |= n >> 1;
        n |= n >> 2;
        n |= n >> 4;
        n |= n >> 8;
        n |= n >> 16;
        return n + 1;
    }
}