using System.Collections;
using System.Runtime.CompilerServices;

namespace BlockGame.util;

/**
 * Fixed-capacity ring buffer with efficient push/pop from both ends.
 * When full, new elements overwrite oldest. Use for chat history, frame buffers, etc.
 */
public class XRingBuffer<T> : IEnumerable<T> {
    private readonly T[] buf;
    private int head; // index of first element
    private int tail; // index after last element (next write position)
    private int cnt;

    public XRingBuffer(int capacity) {
        if (capacity < 1) throw new ArgumentException("Capacity must be positive", nameof(capacity));
        buf = new T[capacity];
        head = 0;
        tail = 0;
        cnt = 0;
    }

    public int Capacity {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => buf.Length;
    }

    public int Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => cnt;
    }

    public bool IsFull {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => cnt == buf.Length;
    }

    public bool IsEmpty {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => cnt == 0;
    }

    /**
     * Logical indexer - [0] is oldest element, [Count-1] is newest.
     */
    public ref T this[int idx] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            if ((uint)idx >= (uint)cnt) ThrowIndexOutOfRange();
            return ref buf[WrapIndex(head + idx)];
        }
    }

    /**
     * Add to back (newest). Overwrites oldest if full.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushBack(T item) {
        buf[tail] = item;
        tail = WrapIndex(tail + 1);
        if (cnt == buf.Length) {
            head = tail; // overwrite oldest
        }
        else {
            cnt++;
        }
    }

    /**
     * Add to front (oldest). Overwrites newest if full.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushFront(T item) {
        head = WrapIndex(head - 1);
        buf[head] = item;
        if (cnt == buf.Length) {
            tail = head; // overwrite newest
        }
        else {
            cnt++;
        }
    }

    /**
     * Remove from back (newest). Throws if empty.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T PopBack() {
        if (cnt == 0) ThrowEmpty();
        tail = WrapIndex(tail - 1);
        T item = buf[tail];
        buf[tail] = default!;
        cnt--;
        return item;
    }

    /**
     * Remove from front (oldest). Throws if empty.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T PopFront() {
        if (cnt == 0) ThrowEmpty();
        T item = buf[head];
        buf[head] = default!;
        head = WrapIndex(head + 1);
        cnt--;
        return item;
    }

    /**
     * Peek oldest element without removing.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Front() {
        if (cnt == 0) ThrowEmpty();
        return ref buf[head];
    }

    /**
     * Peek newest element without removing.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Back() {
        if (cnt == 0) ThrowEmpty();
        return ref buf[WrapIndex(tail - 1)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
            Array.Clear(buf, 0, buf.Length);
        }
        head = 0;
        tail = 0;
        cnt = 0;
    }

    /**
     * Copy to array in logical order (oldest to newest).
     */
    public T[] ToArray() {
        if (cnt == 0) return [];
        var result = new T[cnt];
        if (head < tail) {
            // contiguous
            Array.Copy(buf, head, result, 0, cnt);
        }
        else {
            // wrapped
            int firstLen = buf.Length - head;
            Array.Copy(buf, head, result, 0, firstLen);
            Array.Copy(buf, 0, result, firstLen, tail);
        }
        return result;
    }

    /**
     * Get two spans representing the ring buffer contents.
     * First span is the older segment, second is the newer segment.
     * One or both may be empty.
     */
    public SpanPair AsSpans() {
        if (cnt == 0) return new SpanPair();
        if (head < tail) {
            return new SpanPair(new Span<T>(buf, head, cnt), Span<T>.Empty);
        }
        else {
            int firstLen = buf.Length - head;
            return new SpanPair(new Span<T>(buf, head, firstLen), new Span<T>(buf, 0, tail));
        }
    }

    public ref struct SpanPair(Span<T> first, Span<T> second) {
        public Span<T> First = first;
        public Span<T> Second = second;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int WrapIndex(int idx) {
        int cap = buf.Length;
        if (idx >= cap) return idx - cap;
        if (idx < 0) return idx + cap;
        return idx;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowIndexOutOfRange() {
        throw new IndexOutOfRangeException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowEmpty() {
        throw new InvalidOperationException("Buffer is empty");
    }

    /**
     * Struct enumerator - foreach with no allocations.
     * Iterates from oldest to newest.
     */
    public Enumerator GetEnumerator() => new Enumerator(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() {
        for (int i = 0; i < cnt; i++) {
            yield return buf[WrapIndex(head + i)];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

    public struct Enumerator {
        private readonly XRingBuffer<T> ring;
        private int idx;

        internal Enumerator(XRingBuffer<T> ring) {
            this.ring = ring;
            idx = -1;
        }

        public bool MoveNext() {
            return ++idx < ring.cnt;
        }

        public ref T Current {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref ring.buf[ring.WrapIndex(ring.head + idx)];
        }

        public void Reset() {
            idx = -1;
        }
    }
}