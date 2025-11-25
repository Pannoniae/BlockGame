namespace BlockGame.util;

public class FixedArrayPool<T> {

    // how many items the pool can hold before it gets trimmed
    private const int MAX_ITEMS_BEFORE_TRIM = 24;

    /**
     * IDK how to do thread-safety here but fear not, we have an overkill solution!
     * Are we just locking everything? Yes. Do I care? no, because the chunkmuncher might come back again
     */
    private readonly Queue<T[]> _objects;
    
    private readonly Lock _objectLock = new();

    public readonly int arrayLength;

    public FixedArrayPool(int arrayLength) {
        this.arrayLength = arrayLength;
        _objects = new Queue<T[]>();
    }

    public T[] grab() {
        T[] item;
        bool found;
        lock (_objectLock) {
            found = _objects.TryDequeue(out item!);
        }
        return found ? item : GC.AllocateUninitializedArray<T>(arrayLength);
    }

    public void putBack(T[] item) {
        lock (_objectLock) {
            _objects.Enqueue(item);
        }
    }

    public void trim() {
        lock (_objectLock) {
            if (_objects.Count > MAX_ITEMS_BEFORE_TRIM) {
                // nuke all the old objects
                // AND TRIM THE ARRAY
                _objects.Clear();
                _objects.TrimExcess(MAX_ITEMS_BEFORE_TRIM);
            }
        }
    }

    public void clear() {
        lock (_objectLock) {
            // clear the pool
            _objects.Clear();
        }
    }
}
