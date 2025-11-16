using System.Collections;
using System.Runtime.CompilerServices;

namespace BlockGame.util;

/**
 * Tiny map using linear search - no hashing overhead.
 * Faster than hash maps for ~0-20 entries.
 * Use for sparse collections like chunk block entities.
 */
public class XLinearMap<K, V> : IEnumerable<V> where K : notnull, IEquatable<K> {

    private struct Entry {
        public K key;
        public V value;
    }

    private Entry[] entries;
    private int count;

    public XLinearMap(int capacity = 4) {
        entries = new Entry[capacity];
        count = 0;
    }

    public int Count => count;

    public ref V this[K key] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            for (int i = 0; i < count; i++) {
                if (entries[i].key.Equals(key)) {
                    return ref entries[i].value;
                }
            }

            InputException.throwNew($"Key not found: {key}");
            return ref entries[0].value; // unreachable
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(K key, out V value) {
        for (int i = 0; i < count; i++) {
            if (entries[i].key.Equals(key)) {
                value = entries[i].value;
                return true;
            }
        }

        value = default!;
        return false;
    }

    /** Gets an entry if it exists, or adds a new entry and returns it */
    public ref V GetOrAdd(K key, V defaultValue, out bool added) {
        for (int i = 0; i < count; i++) {
            if (entries[i].key.Equals(key)) {
                added = false;
                return ref entries[i].value;
            }
        }

        Add(key, defaultValue);
        added = true;
        return ref entries[count - 1].value;
    }

    public void Set(K key, V value) {
        for (int i = 0; i < count; i++) {
            if (entries[i].key.Equals(key)) {
                entries[i].value = value;
                return;
            }
        }

        Add(key, value);
    }

    public void Add(K key, V value) {
        // check for duplicate
        for (int i = 0; i < count; i++) {
            if (entries[i].key.Equals(key)) {
                InputException.throwNew($"Key already exists: {key}");
            }
        }

        if (count == entries.Length) {
            Array.Resize(ref entries, entries.Length * 2);
        }

        entries[count].key = key;
        entries[count].value = value;
        count++;
    }

    public bool Remove(K key) {
        for (int i = 0; i < count; i++) {
            if (entries[i].key.Equals(key)) {
                // swap with last element and shrink
                count--;
                if (i < count) {
                    entries[i] = entries[count];
                }
                entries[count] = default; // clear reference
                return true;
            }
        }

        return false;
    }

    public bool ContainsKey(K key) {
        return TryGetValue(key, out _);
    }

    public void Clear() {
        Array.Clear(entries, 0, count);
        count = 0;
    }

    public Enumerator GetEnumerator() => new Enumerator(this);

    IEnumerator<V> IEnumerable<V>.GetEnumerator() {
        for (int i = 0; i < count; i++) {
            yield return entries[i].value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<V>)this).GetEnumerator();

    public struct Enumerator {
        private readonly XLinearMap<K, V> map;
        private int idx;

        internal Enumerator(XLinearMap<K, V> map) {
            this.map = map;
            idx = -1;
        }

        public bool MoveNext() {
            return ++idx < map.count;
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
}