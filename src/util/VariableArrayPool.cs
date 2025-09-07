namespace BlockGame.util;

public class VariableArrayPool<T> {

    private readonly Dictionary<int, FixedArrayPool<T>> _pools;
    private readonly Lock _poolsLock = new();

    public VariableArrayPool() {
        _pools = new Dictionary<int, FixedArrayPool<T>>();
    }

    public T[] grab(int size) {
        if (size <= 0) SkillIssueException.throwNew($"Size must be positive ({nameof(size)}");
        
        FixedArrayPool<T> pool;
        lock (_poolsLock) {
            if (!_pools.TryGetValue(size, out pool)) {
                pool = new FixedArrayPool<T>(size);
                _pools[size] = pool;
            }
        }
        
        return pool.grab();
    }

    public void putBack(T[] array) {
        if (array == null) return;
        
        int size = array.Length;
        FixedArrayPool<T> pool;
        
        lock (_poolsLock) {
            if (!_pools.TryGetValue(size, out pool)) {
                // pool doesn't exist yet, create it
                pool = new FixedArrayPool<T>(size);
                _pools[size] = pool;
            }
        }
        
        pool.putBack(array);
    }

    public void trim() {
        lock (_poolsLock) {
            foreach (var pool in _pools.Values) {
                pool.trim();
            }
        }
    }

    public void clear() {
        lock (_poolsLock) {
            foreach (var pool in _pools.Values) {
                pool.clear();
            }
            _pools.Clear();
        }
    }

    /**
     * gets stats about the pool usage for debugging
     */
    public Dictionary<int, (int grabbed, int putBack)> getStats() {
        lock (_poolsLock) {
            var stats = new Dictionary<int, (int, int)>();
            foreach (var kvp in _pools) {
                var pool = kvp.Value;
                stats[kvp.Key] = (pool.grabCtr, pool.putBackCtr);
            }
            return stats;
        }
    }
}