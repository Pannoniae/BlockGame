using System.IO.Hashing;
using System.Runtime.InteropServices;

namespace BlockGame.util;

/**
 * Fast deterministic hashing using xxHash3.
 * Use this instead of built-in GetHashCode() when you need consistent results.
 */
public static class XHash {
    
    /**
     * Hash 2D coordinates.
     */
    public static int hash(int x, int y) {
        Span<byte> data = stackalloc byte[8];
        MemoryMarshal.Write(data[..4], in x);
        MemoryMarshal.Write(data[4..], in y);
        return (int)XxHash3.HashToUInt64(data);
    }

    /**
     * Hash 3D coordinates.
     */
    public static int hash(int x, int y, int z) {
        Span<byte> data = stackalloc byte[12];
        MemoryMarshal.Write(data[..4], in x);
        MemoryMarshal.Write(data[4..8], in y);
        MemoryMarshal.Write(data[8..], in z);
        return (int)XxHash3.HashToUInt64(data);
    }

    /**
     * Hash single value with seed.
     */
    public static int hashSeeded(int value, int seed) {
        Span<byte> data = stackalloc byte[4];
        MemoryMarshal.Write(data, in value);
        return (int)XxHash3.HashToUInt64(data, seed);
    }

    /**
     * Hash 2D coordinates with seed.
     */
    public static int hashSeeded(int x, int y, int seed) {
        Span<byte> data = stackalloc byte[8];
        MemoryMarshal.Write(data[..4], in x);
        MemoryMarshal.Write(data[4..], in y);
        return (int)XxHash3.HashToUInt64(data, seed);
    }

    /**
     * Hash 3D coordinates with seed.
     */
    public static int hashSeeded(int x, int y, int z, int seed) {
        Span<byte> data = stackalloc byte[12];
        MemoryMarshal.Write(data[..4], in x);
        MemoryMarshal.Write(data[4..8], in y);
        MemoryMarshal.Write(data[8..], in z);
        return (int)XxHash3.HashToUInt64(data, seed);
    }

    /**
     * Hash to normalized float [0.0, 1.0).
     */
    public static float hashFloat(int x, int y) {
        var h = (uint)hash(x, y);
        return h * (1.0f / (1L << 32));
    }

    /**
     * Hash to normalized float [0.0, 1.0) with seed.
     */
    public static float hashFloat(int x, int y, int seed) {
        var h = (uint)hashSeeded(x, y, seed);
        return h * (1.0f / (1L << 32));
    }

    /**
     * Hash to range [0, max).
     */
    public static int hashRange(int x, int y, int max) {
        return Math.Abs(hash(x, y)) % max;
    }

    /**
     * Hash to range [0, max) with seed.
     */
    public static int hashRange(int x, int y, int max, int seed) {
        return Math.Abs(hashSeeded(x, y, seed)) % max;
    }
}