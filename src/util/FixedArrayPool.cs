using System.Collections.Concurrent;

namespace BlockGame.util;

public class FixedArrayPool<T> {
    private readonly ConcurrentBag<T[]> _objects;

    public readonly int arrayLength;

    public FixedArrayPool(int arrayLength) {
        this.arrayLength = arrayLength;
        _objects = new ConcurrentBag<T[]>();
    }

    public T[] grab() {
        return _objects.TryTake(out var item) ? item : new T[arrayLength];
    }

    public void putBack(T[] item) => _objects.Add(item);
}