using System.Runtime.CompilerServices;

namespace BlockGame.util;

/**
 * Fast deterministic hashing using xxHash3.
 * Use this instead of built-in GetHashCode() when you need consistent results.
 */
public static class XHash {
    
    /**
     * Hash an integer.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int hash(int x) {
        var x_ = (uint)x;
        x_ ^= x_ >> 16;
        x_ *= 0x85ebca6b;
        x_ ^= x_ >> 13;
        return (int)x_;
    }
    
    /**
     * Hash 2D coordinates.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int hash(int x, int y) {
        return x * 374761393 + y * 668265263;
    }

    /**
     * Hash 3D coordinates.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int hash(int x, int y, int z) {
        return x * 374761393 + y * 668265263 + z * 1103515245;
    }

    /**
     * Hash single value with seed.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int hashSeeded(int value, int seed) {
        return (int)(((uint)value ^ seed) * 0x85ebca6b);
    }

    /**
     * Hash 2D coordinates with seed.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int hashSeeded(int x, int y, int seed) {
        return x * 374761393 + y * 668265263 + seed * 1664525;
    }

    /**
     * Hash 3D coordinates with seed.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int hashSeeded(int x, int y, int z, int seed) {
        return x * 374761393 + y * 668265263 + z * 1103515245 + seed * 1664525;
    }
    
    /**
     * Hash to normalized float [0.0, 1.0).
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float hashFloat(int x) {
        return (uint)hash(x) * (1.0f / (1L << 32));
    }

    /**
     * Hash to normalized float [0.0, 1.0).
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float hashFloat(int x, int y) {
        return (uint)hash(x, y) * (1.0f / (1L << 32));
    }

    /**
     * Hash to normalized float [0.0, 1.0) with seed.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float hashFloat(int x, int y, int seed) {
        return (uint)hashSeeded(x, y, seed) * (1.0f / (1L << 32));
    }

    /**
     * Hash to range [0, max).
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int hashRange(int x, int y, int max) {
        return Math.Abs(hash(x, y)) % max;
    }

    /**
     * Hash to range [0, max) with seed.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int hashRange(int x, int y, int max, int seed) {
        return Math.Abs(hashSeeded(x, y, seed)) % max;
    }
}