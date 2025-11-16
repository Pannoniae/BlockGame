using System.Collections;
using System.Runtime.CompilerServices;

namespace BlockGame.util;

/**
 * Bidirectional map with O(1) lookups in both directions.
 * Maintains two internal hash maps: forward (K→V) and reverse (V→K).
 * Keys and values must both be unique.
 *
 * The copypasta is getting a bit grating but oh well. /shrug
 */
public class XBiMap<K, V> : IEnumerable<XBiMap<K, V>.Pair> where K : notnull where V : notnull {

    private struct FwdEntry {
        public int hash;  // 0 = empty slot, -1 = tombstone
        public K key;
        public V value;
    }

    private struct RevEntry {
        public int hash;  // 0 = empty slot, -1 = tombstone
        public V key;
        public K value;
    }

    private FwdEntry[] forward;   // K → V
    private RevEntry[] reverse;   // V → K
    private int count;
    private int fwdTombstones;
    private int revTombstones;

    private const int DEFAULT_CAPACITY = 16;
    private const float LOAD_FACTOR = 0.75f;

    public XBiMap(int capacity = DEFAULT_CAPACITY) {
        int size = NextPow2(Math.Max(capacity, DEFAULT_CAPACITY));
        forward = new FwdEntry[size];
        reverse = new RevEntry[size];
        count = 0;
    }

    public int Count => count;

    /** forward lookup: K → V */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public V getValue(K key) {
        if (TryGetValue(key, out var value)) {
            return value;
        }
        InputException.throwNew($"Key not found: {key}");
        return default!;
    }

    /** reverse lookup: V → K */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public K getKey(V value) {
        if (TryGetKey(value, out var key)) {
            return key;
        }
        InputException.throwNew($"Value not found: {value}");
        return default!;
    }

    public K this[V val] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => getKey(val);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => set(value, val);
    }

    public V this[K key] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => getValue(key);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => set(key, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(K key, out V value) {
        int hash = hashK(key);
        int mask = forward.Length - 1;
        int idx = hash & mask;

        ref var entry = ref forward[idx];
        while (entry.hash != 0) {
            if (entry.hash == hash && EqualityComparer<K>.Default.Equals(entry.key, key)) {
                value = entry.value;
                return true;
            }
            idx = (idx + 1) & mask;
            entry = ref forward[idx];
        }

        value = default!;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetKey(V value, out K key) {
        int hash = hashV(value);
        int mask = reverse.Length - 1;
        int idx = hash & mask;

        ref var entry = ref reverse[idx];
        while (entry.hash != 0) {
            // in reverse map: entry.key = V, entry.value = K
            if (entry.hash == hash && EqualityComparer<V>.Default.Equals(entry.key, value)) {
                key = entry.value;
                return true;
            }
            idx = (idx + 1) & mask;
            entry = ref reverse[idx];
        }

        key = default!;
        return false;
    }

    public void add(K key, V value) {
        insert(key, value, false);
    }

    public void set(K key, V value) {
        // early out if exact mapping already exists
        if (TryGetValue(key, out var existingValue) &&
            EqualityComparer<V>.Default.Equals(existingValue, value)) {
            return;
        }
        
        bool removedKey = false;
        bool removedValue = false;

        if (TryGetValue(key, out var oldValue)) {
            removeFromForward(key);
            removeFromReverse(oldValue);
            removedKey = true;
        }

        if (TryGetKey(value, out var oldKey) &&
            !EqualityComparer<K>.Default.Equals(oldKey, key)) {
            removeFromForward(oldKey);
            removeFromReverse(value);
            removedValue = true;
        }

        // adjust count for fully removed entries
        if (removedKey) count--;
        if (removedValue) count--;

        // insert new mapping (will increment count)
        insert(key, value, true);
    }

    public bool remove(K key) {
        if (!TryGetValue(key, out var value)) {
            return false;
        }
        removeFromForward(key);
        removeFromReverse(value);
        count--;
        return true;
    }

    public bool removeValue(V value) {
        if (!TryGetKey(value, out var key)) {
            return false;
        }
        removeFromForward(key);
        removeFromReverse(value);
        count--;
        return true;
    }

    public bool containsKey(K key) => TryGetValue(key, out _);
    public bool containsValue(V value) => TryGetKey(value, out _);

    public void clear() {
        Array.Clear(forward);
        Array.Clear(reverse);
        count = 0;
        fwdTombstones = 0;
        revTombstones = 0;
    }

    private void insert(K key, V value, bool overwrite) {
        // check if resize needed
        if (count + fwdTombstones >= forward.Length * LOAD_FACTOR ||
            count + revTombstones >= reverse.Length * LOAD_FACTOR) {
            resize();
        }

        // check for duplicate key or value
        if (!overwrite) {
            if (containsKey(key)) {
                InputException.throwNew($"Key already exists: {key}");
            }
            if (containsValue(value)) {
                InputException.throwNew($"Value already exists: {value}");
            }
        }

        insertfwd(key, value);
        insertrev(value, key);
        count++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void insertfwd(K key, V value) {
        int hash = hashK(key);
        int mask = forward.Length - 1;
        int idx = hash & mask;
        int tombstoneIdx = -1;

        ref var entry = ref forward[idx];
        while (entry.hash != 0) {
            if (entry.hash == -1) {
                if (tombstoneIdx == -1) {
                    tombstoneIdx = idx;
                }
            }
            idx = (idx + 1) & mask;
            entry = ref forward[idx];
        }

        int insertIdx = tombstoneIdx != -1 ? tombstoneIdx : idx;
        if (tombstoneIdx != -1) {
            fwdTombstones--;
        }
        forward[insertIdx].hash = hash;
        forward[insertIdx].key = key;
        forward[insertIdx].value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void insertrev(V value, K key) {
        int hash = hashV(value);
        int mask = reverse.Length - 1;
        int idx = hash & mask;
        int tombstoneIdx = -1;

        ref var entry = ref reverse[idx];
        while (entry.hash != 0) {
            if (entry.hash == -1) {
                if (tombstoneIdx == -1) {
                    tombstoneIdx = idx;
                }
            }
            idx = (idx + 1) & mask;
            entry = ref reverse[idx];
        }

        int insertIdx = tombstoneIdx != -1 ? tombstoneIdx : idx;
        if (tombstoneIdx != -1) {
            revTombstones--;
        }
        reverse[insertIdx].hash = hash;
        reverse[insertIdx].key = value;    // V stored as key
        reverse[insertIdx].value = key;    // K stored as value
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void removeFromForward(K key) {
        int hash = hashK(key);
        int mask = forward.Length - 1;
        int idx = hash & mask;

        ref var entry = ref forward[idx];
        while (entry.hash != 0) {
            if (entry.hash == hash && EqualityComparer<K>.Default.Equals(entry.key, key)) {
                entry.hash = -1; // tombstone
                entry.key = default!;
                entry.value = default!;
                fwdTombstones++;
                return;
            }
            idx = (idx + 1) & mask;
            entry = ref forward[idx];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void removeFromReverse(V value) {
        int hash = hashV(value);
        int mask = reverse.Length - 1;
        int idx = hash & mask;

        ref var entry = ref reverse[idx];
        while (entry.hash != 0) {
            if (entry.hash == hash && EqualityComparer<V>.Default.Equals(entry.key, value)) {
                entry.hash = -1; // tombstone
                entry.key = default!;
                entry.value = default!;
                revTombstones++;
                return;
            }
            idx = (idx + 1) & mask;
            entry = ref reverse[idx];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int hashK(K key) {
        int h = key.GetHashCode();
        return h is 0 or -1 ? 2 : h;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int hashV(V value) {
        int h = value.GetHashCode();
        return h is 0 or -1 ? 2 : h;
    }

    private void resize() {
        var oldForward = forward;
        int newSize = oldForward.Length * 2;
        forward = new FwdEntry[newSize];
        reverse = new RevEntry[newSize];
        int oldCount = count;
        count = 0;
        fwdTombstones = 0;
        revTombstones = 0;

        foreach (var e in oldForward) {
            if (e.hash != 0 && e.hash != -1) {
                insertfwd(e.key, e.value);
                insertrev(e.value, e.key);
                count++;
            }
        }

        if (count != oldCount) {
            SkillIssueException.throwNew("Count mismatch during resize");
        }
    }

    public Enumerator GetEnumerator() => new Enumerator(this);

    IEnumerator<Pair> IEnumerable<Pair>.GetEnumerator() {
        for (int i = 0; i < forward.Length; i++) {
            if (forward[i].hash != 0 && forward[i].hash != -1) {
                yield return new Pair(forward[i].key, forward[i].value);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<Pair>)this).GetEnumerator();

    public struct Enumerator {
        private readonly XBiMap<K, V> map;
        private int idx;

        internal Enumerator(XBiMap<K, V> map) {
            this.map = map;
            idx = -1;
        }

        public bool MoveNext() {
            idx++;
            while (idx < map.forward.Length) {
                var hash = map.forward[idx].hash;
                if (hash != 0 && hash != -1) {
                    return true;
                }
                idx++;
            }
            return false;
        }

        public Pair Current {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Pair(map.forward[idx].key, map.forward[idx].value);
        }

        public void Reset() {
            idx = -1;
        }
    }

    public readonly struct Pair {
        public readonly K Key;
        public readonly V Value;

        internal Pair(K key, V value) {
            Key = key;
            Value = value;
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