using System.Collections;
using System.Runtime.CompilerServices;

namespace BlockGame.util;

/**
 * High-performance list with ref indexer and no backwards compat bullshit.
 * Use this instead of List&lt;T&gt; when you need direct element modification without copying.
 */
public class XList<T> : IList<T> {
    private T[] arr;
    private int cnt;

    public XList() : this(4) { }

    public XList(int capacity) {
        arr = new T[capacity];
        cnt = 0;
    }

    public XList(IEnumerable<T> collection) {
        if (collection is ICollection<T> col) {
            arr = new T[col.Count];
            col.CopyTo(arr, 0);
            cnt = col.Count;
        }
        else {
            arr = new T[4];
            cnt = 0;
            foreach (var item in collection) {
                Add(item);
            }
        }
    }

    public bool IsReadOnly => false;

    public int Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => cnt;
    }

    public int Capacity {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => arr.Length;
    }

    T IList<T>.this[int index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            if ((uint)index >= (uint)cnt) ThrowIndexOutOfRange();
            return arr[index];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            if ((uint)index >= (uint)cnt) ThrowIndexOutOfRange();
            arr[index] = value;
        }
    }

    /**
     * Ref indexer - modify elements in-place without copying.
     */
    public ref T this[int idx] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            if ((uint)idx >= (uint)cnt) ThrowIndexOutOfRange();
            return ref arr[idx];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item) {
        if (cnt == arr.Length) {
            Grow();
        }
        arr[cnt++] = item;
    }

    public void AddRange(IEnumerable<T> collection) {
        if (collection is ICollection<T> col) {
            int newCnt = cnt + col.Count;
            if (newCnt > arr.Length) {
                GrowTo(newCnt);
            }
            col.CopyTo(arr, cnt);
            cnt = newCnt;
        }
        else {
            foreach (var item in collection) {
                Add(item);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Insert(int idx, T item) {
        if ((uint)idx > (uint)cnt) ThrowIndexOutOfRange();
        if (cnt == arr.Length) Grow();
        if (idx < cnt) {
            Array.Copy(arr, idx, arr, idx + 1, cnt - idx);
        }
        arr[idx] = item;
        cnt++;
    }

    public bool Remove(T item) {
        int idx = IndexOf(item);
        if (idx >= 0) {
            RemoveAt(idx);
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveAt(int idx) {
        if ((uint)idx >= (uint)cnt) ThrowIndexOutOfRange();
        cnt--;
        if (idx < cnt) {
            Array.Copy(arr, idx + 1, arr, idx, cnt - idx);
        }
        arr[cnt] = default!;
    }

    public void RemoveRange(int idx, int count) {
        if (idx < 0 || count < 0 || idx + count > cnt) ThrowIndexOutOfRange();
        if (count == 0) return;
        cnt -= count;
        if (idx < cnt) {
            Array.Copy(arr, idx + count, arr, idx, cnt - idx);
        }
        Array.Clear(arr, cnt, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
            Array.Clear(arr, 0, cnt);
        }
        cnt = 0;
    }

    public int IndexOf(T item) {
        return Array.IndexOf(arr, item, 0, cnt);
    }

    public int LastIndexOf(T item) {
        return Array.LastIndexOf(arr, item, cnt - 1, cnt);
    }

    public bool Contains(T item) {
        return IndexOf(item) >= 0;
    }

    public void Reverse() {
        Array.Reverse(arr, 0, cnt);
    }

    public void Sort() {
        Array.Sort(arr, 0, cnt);
    }

    public void Sort(IComparer<T> comparer) {
        Array.Sort(arr, 0, cnt, comparer);
    }

    public void Sort(Comparison<T> comparison) {
        Array.Sort(arr, 0, cnt, Comparer<T>.Create(comparison));
    }

    public T[] ToArray() {
        if (cnt == 0) return [];
        var result = new T[cnt];
        Array.Copy(arr, result, cnt);
        return result;
    }

    public void CopyTo(T[] array, int arrayIndex = 0) {
        Array.Copy(arr, 0, array, arrayIndex, cnt);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnsureCapacity(int capacity) {
        if (arr.Length < capacity) {
            GrowTo(capacity);
        }
    }

    public void TrimExcess() {
        if (cnt < arr.Length * 0.9) {
            Array.Resize(ref arr, cnt);
        }
    }

    /**
     * Get the internal array. USE WITH CAUTION.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] GetInternalArray() => arr;

    /**
     * Get span of valid elements.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan() => new Span<T>(arr, 0, cnt);

    /**
     * Get span slice of valid elements.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan(int start, int length) => new Span<T>(arr, start, length);

    private void Grow() {
        int newSize = arr.Length == 0 ? 4 : arr.Length * 2;
        Array.Resize(ref arr, newSize);
    }

    private void GrowTo(int minCapacity) {
        int newSize = arr.Length == 0 ? 4 : arr.Length * 2;
        if (newSize < minCapacity) newSize = minCapacity;
        Array.Resize(ref arr, newSize);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowIndexOutOfRange() {
        throw new IndexOutOfRangeException();
    }

    /**
     * Struct enumerator - foreach with no allocations.
     */
    public Enumerator GetEnumerator() => new Enumerator(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() {
        for (int i = 0; i < cnt; i++) {
            yield return arr[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

    public struct Enumerator {
        private readonly XList<T> list;
        private int idx;

        internal Enumerator(XList<T> list) {
            this.list = list;
            idx = -1;
        }

        public bool MoveNext() {
            return ++idx < list.cnt;
        }

        public ref T Current {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref list.arr[idx];
        }

        public void Reset() {
            idx = -1;
        }
    }
}