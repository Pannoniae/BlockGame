using System.Collections.Concurrent;

namespace BlockGame.util;

public class FixedArrayPool<T> {

    // how many items the pool can hold before it gets trimmed
    private const int MAX_ITEMS_BEFORE_TRIM = 1024;

    private readonly ConcurrentBag<T[]> _objects;

    public readonly int arrayLength;

    private int grabCtr;
    private int putBackCtr;

    public FixedArrayPool(int arrayLength) {
        this.arrayLength = arrayLength;
        _objects = new ConcurrentBag<T[]>();
    }

    public T[] grab() {
        grabCtr++;
        //Console.Out.WriteLine("diff: " + (grabCtr - putBackCtr));
        return _objects.TryTake(out var item) ? item : GC.AllocateUninitializedArray<T>(arrayLength);
    }

    public void putBack(T[] item) {
        putBackCtr++;
        _objects.Add(item);
    }

    public void trim() {
        if (_objects.Count > MAX_ITEMS_BEFORE_TRIM) {
            // nuke all the old objects
            _objects.Clear();
        }
    }

    public void clear() {
        // clear the pool
        _objects.Clear();
        grabCtr = 0;
        putBackCtr = 0;
    }
}