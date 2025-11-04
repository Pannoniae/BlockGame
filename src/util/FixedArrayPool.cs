namespace BlockGame.util;

public class FixedArrayPool<T> {

    // how many items the pool can hold before it gets trimmed
    private const int MAX_ITEMS_BEFORE_TRIM = 24;

    /**
     * IDK how to do thread-safety here but fear not, we have an overkill solution!
     * Are we just locking everything? Yes. Do I care? no, because the chunkmuncher might come back again and I don't stand for that
     */
    private readonly Queue<T[]> _objects;
    
    private readonly Lock _objectLock = new();

    public readonly int arrayLength;

    public int grabCtr;
    public int putBackCtr;

    public FixedArrayPool(int arrayLength) {
        this.arrayLength = arrayLength;
        _objects = new Queue<T[]>();
    }

    public T[] grab() {
        lock (_objectLock) {
            grabCtr++;
            //Console.Out.WriteLine("diff: " + (grabCtr - putBackCtr));
            return _objects.TryDequeue(out var item) ? item : GC.AllocateUninitializedArray<T>(arrayLength);
        }
    }

    public void putBack(T[] item) {
        lock (_objectLock) {
            putBackCtr++;
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
            grabCtr = 0;
            putBackCtr = 0;
        }
    }
}
