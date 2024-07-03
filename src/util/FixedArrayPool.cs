using System.Collections.Concurrent;

namespace BlockGame.util;

public class FixedArrayPool<T> {
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
        return _objects.TryTake(out var item) ? item : new T[arrayLength];
    }

    public void putBack(T[] item) {
        putBackCtr++;
        _objects.Add(item);
    }
}