using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Molten.DoublePrecision;

namespace BlockGame.util;

internal sealed partial class Interop {

    private static MethodInfo? _getRandomBytesMethod;

    /*[LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_GetNonCryptographicallySecureRandomBytes")]
    internal static unsafe partial void GetNonCryptographicallySecureRandomBytes(
        byte* buffer,
        int length);*/

    // we reflect this thing
    /*internal static partial class Interop
    {
        internal static partial class Sys
        {
            [LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_GetNonCryptographicallySecureRandomBytes")]
            internal static unsafe partial void GetNonCryptographicallySecureRandomBytes(byte* buffer, int length);

            [LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_GetCryptographicallySecureRandomBytes")]
            internal static unsafe partial int GetCryptographicallySecureRandomBytes(byte* buffer, int length);
        }

        internal static unsafe void GetRandomBytes(byte* buffer, int length)
        {

        }
    */


    static Interop() {
        // time to reflect!
        var a = typeof(object).Assembly;
        var type = a.GetType("Interop");
        var method = type.GetMethod("GetRandomBytes", BindingFlags.Static | BindingFlags.NonPublic);

        // store the method info in a field
        if (method != null) {
            _getRandomBytesMethod = method;
        }
        else {
            throw new InvalidOperationException("Failed to get GetRandomBytes method");
        }
    }

    internal static unsafe void GetRandomBytes(byte* buffer, int length) {
        var bufferP = Pointer.Box(buffer, typeof(byte*));
        var result = _getRandomBytesMethod.Invoke(null, [bufferP, length]);
    }

    internal static unsafe void GetRandomBytes2(byte* buffer, int length) {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        if (length == 0) return;

        // Zero-allocation approach using Span over the unsafe buffer
        // This leverages hardware acceleration when available
        System.Security.Cryptography.RandomNumberGenerator.Fill(
            new Span<byte>(buffer, length));
    }
}

/// <summary>
/// (from stdlib)
/// Provides an implementation of the xoshiro256** algorithm. This implementation is used
/// on 64-bit when no seed is specified and an instance of the base Random class is constructed.
/// As such, we are free to implement however we see fit, without back compat concerns around
/// the sequence of numbers generated or what methods call what other methods.
/// </summary>
public sealed class XRandom {
    // NextUInt64 is based on the algorithm from http://prng.di.unimi.it/xoshiro256starstar.c:
    //
    //     Written in 2018 by David Blackman and Sebastiano Vigna (vigna@acm.org)
    //
    //     To the extent possible under law, the author has dedicated all copyright
    //     and related and neighboring rights to this software to the public domain
    //     worldwide. This software is distributed without any warranty.
    //
    //     See <http://creativecommons.org/publicdomain/zero/1.0/>.

    private ulong _s0, _s1, _s2, _s3;

    public unsafe XRandom() {
        ulong* ptr = stackalloc ulong[4];
        do {
            Interop.GetRandomBytes2((byte*)ptr, 4 * sizeof(ulong));
            _s0 = ptr[0];
            _s1 = ptr[1];
            _s2 = ptr[2];
            _s3 = ptr[3];
        } while ((_s0 | _s1 | _s2 | _s3) == 0); // at least one value must be non-zero
    }

    // Seed with ulong
    public XRandom(ulong seed) {
        SplitMix64Seed(seed);
    }

    // Seed with int (standard Random compatibility)
    public XRandom(int seed) : this((ulong)seed) {
    }
    
    // Convenience array constructor
    public XRandom(ulong[] seeds) : this(seeds.AsSpan()) {
    }

    // Seed with multiple values
    public unsafe XRandom(ReadOnlySpan<ulong> seeds) {
        if (seeds.Length >= 4) {
            // Use provided seeds directly
            _s0 = seeds[0];
            _s1 = seeds[1];
            _s2 = seeds[2];
            _s3 = seeds[3];

            // Sanity check - prevent all zeros
            if ((_s0 | _s1 | _s2 | _s3) == 0) {
                _s0 = 1;
            }
        }
        else if (seeds.Length > 0) {
            // Initialize from first seed
            SplitMix64Seed(seeds[0]);

            // Mix in any additional seeds
            for (int i = 1; i < seeds.Length; i++) {
                _s0 ^= seeds[i];
                // Advance state once per seed
                ulong t = _s1 << 17;
                _s2 ^= _s0;
                _s3 ^= _s1;
                _s1 ^= _s2;
                _s0 ^= _s3;
                _s2 ^= t;
                _s3 = BitOperations.RotateLeft(_s3, 45);
            }
        }
        else {
            // Empty span - fall back to default random initialization
            ulong* ptr = stackalloc ulong[4];
            do {
                Interop.GetRandomBytes2((byte*)ptr, 4 * sizeof(ulong));
                _s0 = ptr[0];
                _s1 = ptr[1];
                _s2 = ptr[2];
                _s3 = ptr[3];
            } while ((_s0 | _s1 | _s2 | _s3) == 0);
        }
    }
    
    private static ulong NextSplitMix64(ref ulong x) {
        ulong z = (x += 0x9E3779B97F4A7C15UL);
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    private void SplitMix64Seed(ulong seed) {
        // Initialize all four states
        _s0 = NextSplitMix64(ref seed);
        _s1 = NextSplitMix64(ref seed);
        _s2 = NextSplitMix64(ref seed);
        _s3 = NextSplitMix64(ref seed);
    }

    public void Seed(int seed) {
        SplitMix64Seed((ulong)seed);
    }

    /// <summary>Produces a value in the range [0, uint.MaxValue].</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // small-ish hot path used by very few call sites
    internal uint NextUInt32() => (uint)(NextUInt64() >> 32);

    /// <summary>Produces a value in the range [0, ulong.MaxValue].</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // small-ish hot path used by a handful of "next" methods
    internal ulong NextUInt64() {
        ulong s0 = _s0, s1 = _s1, s2 = _s2, s3 = _s3;

        ulong result = BitOperations.RotateLeft(s1 * 5, 7) * 9;
        ulong t = s1 << 17;

        s2 ^= s0;
        s3 ^= s1;
        s1 ^= s2;
        s0 ^= s3;

        s2 ^= t;
        s3 = BitOperations.RotateLeft(s3, 45);

        _s0 = s0;
        _s1 = s1;
        _s2 = s2;
        _s3 = s3;

        return result;
    }

    public int Next() {
        while (true) {
            // Get top 31 bits to get a value in the range [0, int.MaxValue], but try again
            // if the value is actually int.MaxValue, as the method is defined to return a value
            // in the range [0, int.MaxValue).
            ulong result = NextUInt64() >> 33;
            if (result != int.MaxValue) {
                return (int)result;
            }
        }
    }

    public int Next(int maxValue) {
        System.Diagnostics.Debug.Assert(maxValue >= 0);

        return (int)_NextUInt32((uint)maxValue);
    }

    public int Next(int minValue, int maxValue) {
        System.Diagnostics.Debug.Assert(minValue <= maxValue);

        return (int)_NextUInt32((uint)(maxValue - minValue)) + minValue;
    }

    public long NextInt64() {
        while (true) {
            // Get top 63 bits to get a value in the range [0, long.MaxValue], but try again
            // if the value is actually long.MaxValue, as the method is defined to return a value
            // in the range [0, long.MaxValue).
            ulong result = NextUInt64() >> 1;
            if (result != long.MaxValue) {
                return (long)result;
            }
        }
    }

    public long NextInt64(long maxValue) {
        System.Diagnostics.Debug.Assert(maxValue >= 0);

        return (long)_NextUInt64((ulong)maxValue);
    }

    public long NextInt64(long minValue, long maxValue) {
        System.Diagnostics.Debug.Assert(minValue <= maxValue);

        return (long)_NextUInt64((ulong)(maxValue - minValue)) + minValue;
    }

    // NextUInt32/64 algorithms based on https://arxiv.org/pdf/1805.10941.pdf and https://github.com/lemire/fastrange.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint _NextUInt32(uint maxValue) {
        ulong randomProduct = (ulong)maxValue * NextUInt32();
        uint lowPart = (uint)randomProduct;

        if (lowPart < maxValue) {
            uint remainder = (0u - maxValue) % maxValue;

            while (lowPart < remainder) {
                randomProduct = (ulong)maxValue * NextUInt32();
                lowPart = (uint)randomProduct;
            }
        }

        return (uint)(randomProduct >> 32);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong _NextUInt64(ulong maxValue) {
        ulong randomProduct = Math.BigMul(maxValue, NextUInt64(), out ulong lowPart);

        if (lowPart < maxValue) {
            ulong remainder = (0ul - maxValue) % maxValue;

            while (lowPart < remainder) {
                randomProduct = Math.BigMul(maxValue, NextUInt64(), out lowPart);
            }
        }

        return randomProduct;
    }

    public void NextBytes(byte[] buffer) => NextBytes((Span<byte>)buffer);

    public unsafe void NextBytes(Span<byte> buffer) {
        ulong s0 = _s0, s1 = _s1, s2 = _s2, s3 = _s3;

        while (buffer.Length >= sizeof(ulong)) {
            MemoryMarshal.Write(buffer, BitOperations.RotateLeft(s1 * 5, 7) * 9);

            // Update PRNG state.
            ulong t = s1 << 17;
            s2 ^= s0;
            s3 ^= s1;
            s1 ^= s2;
            s0 ^= s3;
            s2 ^= t;
            s3 = BitOperations.RotateLeft(s3, 45);

            buffer = buffer.Slice(sizeof(ulong));
        }

        if (!buffer.IsEmpty) {
            ulong next = BitOperations.RotateLeft(s1 * 5, 7) * 9;
            byte* remainingBytes = (byte*)&next;
            System.Diagnostics.Debug.Assert(buffer.Length < sizeof(ulong));
            for (int i = 0; i < buffer.Length; i++) {
                buffer[i] = remainingBytes[i];
            }

            // Update PRNG state.
            ulong t = s1 << 17;
            s2 ^= s0;
            s3 ^= s1;
            s1 ^= s2;
            s0 ^= s3;
            s2 ^= t;
            s3 = BitOperations.RotateLeft(s3, 45);
        }

        _s0 = s0;
        _s1 = s1;
        _s2 = s2;
        _s3 = s3;
    }

    public double NextDouble() =>
        // As described in http://prng.di.unimi.it/:
        // "A standard double (64-bit) floating-point number in IEEE floating point format has 52 bits of significand,
        //  plus an implicit bit at the left of the significand. Thus, the representation can actually store numbers with
        //  53 significant binary digits. Because of this fact, in C99 a 64-bit unsigned integer x should be converted to
        //  a 64-bit double using the expression
        //  (x >> 11) * 0x1.0p-53"
        (NextUInt64() >> 11) * (1.0 / (1ul << 53));

    public float NextSingle() =>
        // Same as above, but with 24 bits instead of 53.
        (NextUInt64() >> 40) * (1.0f / (1u << 24));
    
    /// <summary>
    /// Produces a random single-precision floating-point in the specified range.
    /// </summary>
    /// <param name="max">The inclusive maximum value.</param>
    /// <returns>A random value in the range [0.0 &lt;= x &lt;= <paramref name="max"/>].</returns>
    public float NextSingle(float max)
    {
        return max * NextSingle();
    }

    /// <summary>
    /// Produces a random single-precision floating-point in the specified range.
    /// </summary>
    /// <param name="min">The inclusive minimum value.</param>
    /// <param name="max">The inclusive maximum value.</param>
    /// <returns>A random value in the range [<paramref name="min"/> &lt;= x &lt;= <paramref name="max"/>].</returns>
    public float NextSingle(float min, float max)
    {
        return (max - min) * NextSingle() + min;
    }

    public double Sample() {
        System.Diagnostics.Debug.Fail("Not used or called for this implementation.");
        throw new NotSupportedException();
    }

    public double ApproxGaussian() {
        //ported version of dist_normal_approx from https://marc-b-reynolds.github.io/distribution/2021/03/18/CheapGaussianApprox.html
        Span<byte> rnd = stackalloc byte[16];
        NextBytes(rnd);

        long bd = BitOperations.PopCount(BitConverter.ToUInt64(rnd)) - 32;
        return ((bd << 32) + BitConverter.ToUInt32(rnd[8..]) - BitConverter.ToUInt32(rnd[12..])) * 5.76916501E-11;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mu">The mean of the distribution.</param>
    /// <param name="sigma">The standard deviation of the distribution.</param>
    /// <returns></returns>
    public double ApproxGaussian(double mu, double sigma) {
        return mu + sigma * ApproxGaussian();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sigma">The standard deviation of the distribution.</param>
    /// <returns></returns>
    public double ApproxGaussian(double sigma) {
        return sigma * ApproxGaussian();
    }
    
    public float NextAngle()
    {
        return NextSingle(-MathF.PI, MathF.PI);
    }

    public Vector2 NextUnitVector2()
    {
        float angle = NextAngle();
        (float sin, float cos) = MathF.SinCos(angle);
        return new Vector2(cos, sin);
    }

    public Vector3 NextUnitVector3()
    {
        float u = NextSingle();
        float v = NextSingle();

        float t = u * 2 * MathF.PI;
        float z = 2 * v - 1;
        float sf = MathF.Sqrt(1 - z * z);
        (float sin, float cos) = MathF.SinCos(t);
        float x = cos * sf;
        float y = sin * sf;

        return new Vector3(x, y, z);
    }
    
    public Vector3D NextUnitVector3D()
    {
        double u = NextDouble();
        double v = NextDouble();

        double t = u * 2 * Math.PI;
        double z = 2 * v - 1;
        double sf = Math.Sqrt(1 - z * z);
        (double sin, double cos) = Math.SinCos(t);
        double x = cos * sf;
        double y = sin * sf;

        return new Vector3D(x, y, z);
    }
}
