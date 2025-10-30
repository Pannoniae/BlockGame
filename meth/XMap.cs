using System.Collections;
using System.Runtime.CompilerServices;

namespace BlockGame.util;

/**
 * Open addressing hash map with linear probing.
 * Single flat array = better cache locality than BCL Dictionary in theory:tm:
 * Read-optimised for hot paths.
 */
public class XMap<K, V> : IEnumerable<KeyValuePair<K, V>> where K : notnull, IEquatable<K> {

    private struct Entry {
        public int hash;  // 0 = empty slot, -1 = tombstone
        public K key;
        public V value;
    }

    private Entry[] entries;
    private int count;
    private int tombstones;

    private const int DEFAULT_CAPACITY = 16;
    private const float LOAD_FACTOR = 0.75f;

    public XMap(int capacity = DEFAULT_CAPACITY) {
        int size = NextPow2(Math.Max(capacity, DEFAULT_CAPACITY));
        entries = new Entry[size];
        count = 0;
    }

    public int Count => count;

    public ref V this[K key] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            int hash = GetHash(key);
            int mask = entries.Length - 1;
            int idx = hash & mask;

            ref var entry = ref entries[idx];
            while (entry.hash != 0) {
                if (entry.hash == hash && entry.key.Equals(key)) {
                    return ref entry.value;
                }
                idx = (idx + 1) & mask;
            }

            InputException.throwNew($"Key not found: {key}");
            return ref entries[0].value; // unreachable, but required by compiler :(
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(K key, out V value) {
        int hash = GetHash(key);
        int mask = entries.Length - 1;
        int idx = hash & mask;

        ref var entry = ref entries[idx];
        while (entry.hash != 0) {
            if (entry.hash == hash && entry.key.Equals(key)) {
                value = entry.value;
                return true;
            }
            idx = (idx + 1) & mask;
        }

        value = default!;
        return false;
    }

    public void Add(K key, V value) {
        Insert(key, value, false);
    }

    public bool Remove(K key) {
        int hash = GetHash(key);
        int mask = entries.Length - 1;
        int idx = hash & mask;


        ref var entry = ref entries[idx];
        while (entry.hash != 0) {
            if (entry.hash == hash && entry.key.Equals(key)) {
                entry.hash = -1; // tombstone
                entry.key = default!;
                entry.value = default!;
                count--;
                tombstones++;
                return true;
            }
            idx = (idx + 1) & mask;
        }

        return false;
    }

    public bool ContainsKey(K key) {
        return TryGetValue(key, out _);
    }

    public void Clear() {
        Array.Clear(entries);
        count = 0;
        tombstones = 0;
    }

    private void Insert(K key, V value, bool overwrite) {
        if (count + tombstones >= entries.Length * LOAD_FACTOR) {
            Resize();
        }

        int hash = GetHash(key);
        int mask = entries.Length - 1;
        int idx = hash & mask;
        int tombstoneIdx = -1;

        ref var entry = ref entries[idx];
        while (entry.hash != 0) {
            if (entry.hash == -1) {
                if (tombstoneIdx == -1) {
                    tombstoneIdx = idx;
                }
            }
            else if (entry.hash == hash && entry.key.Equals(key)) {
                if (!overwrite) {
                    InputException.throwNew($"Key already exists: {key}");
                }
                entry.value = value;
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
    private int GetHash(K key) {
        int h = key.GetHashCode();
        // ensure non-zero (0 = empty, -1 = tombstone)
        return h is 0 or -1 ? 2 : h;
    }

    private void Resize() {
        var oldEntries = entries;
        entries = new Entry[oldEntries.Length * 2];
        int oldCount = count;
        count = 0;
        tombstones = 0;

        foreach (var e in oldEntries) {
            if (e.hash != 0 && e.hash != -1) { // skip empty (0) and tombstone (-1)
                Insert(e.key, e.value, false);
            }
        }

        if (count != oldCount) {
            SkillIssueException.throwNew("Count mismatch during resize");
        }
    }

    public Enumerator GetEnumerator() => new Enumerator(this);
    public PairEnumerable Pairs => new PairEnumerable(this);

    IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator() {
        for (int i = 0; i < entries.Length; i++) {
            if (entries[i].hash != 0 && entries[i].hash != -1) {
                yield return new KeyValuePair<K, V>(entries[i].key, entries[i].value);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<K, V>>)this).GetEnumerator();

    public struct Enumerator {
        private readonly XMap<K, V> map;
        private int idx;

        internal Enumerator(XMap<K, V> map) {
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
        private readonly XMap<K, V> map;

        internal PairEnumerable(XMap<K, V> map) {
            this.map = map;
        }

        public PairEnumerator GetEnumerator() => new PairEnumerator(map);
    }

    public struct PairEnumerator {
        private readonly XMap<K, V> map;
        private int idx;

        internal PairEnumerator(XMap<K, V> map) {
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
            get => new(map.entries[idx].key, ref map.entries[idx].value);
        }

        public void Reset() {
            idx = -1;
        }
    }

    public readonly ref struct Pair {
        public readonly K Key;
        private readonly ref V val;

        internal Pair(K key, ref V value) {
            Key = key;
            val = ref value;
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