using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BlockGame.util;
using BlockGame.world.block;

namespace BlockGame.render;

public partial class BlockRenderer {

    /// <summary>
    /// SIMD average2 - dispatches to AVX2 or SSE based on CPU support
    /// </summary>
    /// <param name="lightNibbles">4 packed light values (each uint has 4 nibbles)</param>
    /// <param name="oFlagsPacked">4 opacity flags packed into uint (4 bytes)</param>
    /// <returns>4 averaged light bytes packed into a uint</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint average2x4_simd(Vector128<uint> lightNibbles, uint oFlagsPacked) {
        return Avx2.IsSupported
            ? average2x4_avx2(lightNibbles, oFlagsPacked)
            : average2x4_sse(lightNibbles, oFlagsPacked);
    }

    /// <summary>
    /// AVX2 version - packs sky+block into 256-bit vectors for parallel processing
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint average2x4_avx2(Vector128<uint> lightNibbles, uint oFlagsPacked) {
        var oFlags = Vector128.CreateScalarUnsafe(oFlagsPacked).AsByte();
        var three = Vector128.Create((byte)3);
        var lt3 = Sse2.CompareLessThan(oFlags.AsSByte(), three.AsSByte()).AsByte();
        var masked = Sse2.And(oFlags, three);
        var clamped = Sse41.BlendVariable(oFlags, masked, lt3);

        // inv = ~oFlags & 0x7
        var inv = Sse2.AndNot(clamped, Vector128.Create((byte)0x7));

        // popcnt + 1
        var popLUT = Vector128.Create((byte)1, 2, 2, 3, 2, 3, 3, 4, 0, 0, 0, 0, 0, 0, 0, 0);
        var popcnts = Ssse3.Shuffle(popLUT, inv);

        var m = Vector128.Create(0xFu);

        var s0 = Sse2.And(lightNibbles, m);
        var b0 = Sse2.And(Sse2.ShiftRightLogical(lightNibbles, 4), m);
        var n0 = Vector256.Create(s0, b0);

        var s1 = Sse2.And(Sse2.ShiftRightLogical(lightNibbles, 8), m);
        var b1 = Sse2.And(Sse2.ShiftRightLogical(lightNibbles, 12), m);
        var n1 = Vector256.Create(s1, b1);

        var s2 = Sse2.And(Sse2.ShiftRightLogical(lightNibbles, 16), m);
        var b2 = Sse2.And(Sse2.ShiftRightLogical(lightNibbles, 20), m);
        var n2 = Vector256.Create(s2, b2);

        var s3 = Sse2.And(Sse2.ShiftRightLogical(lightNibbles, 24), m);
        var b3 = Sse2.ShiftRightLogical(lightNibbles, 28);
        var n3 = Vector256.Create(s3, b3);

        // create masks /broadcast to 256
        var invU32 = Sse41.ConvertToVector128Int32(inv).AsUInt32();
        var mask0 = Sse2.And(invU32, Vector128.Create(1u));
        var mask1 = Sse2.ShiftRightLogical(Sse2.And(invU32, Vector128.Create(2u)), 1);
        var mask2 = Sse2.ShiftRightLogical(Sse2.And(invU32, Vector128.Create(4u)), 2);

        var mask0_256 = Vector256.Create(mask0, mask0);
        var mask1_256 = Vector256.Create(mask1, mask1);
        var mask2_256 = Vector256.Create(mask2, mask2);

        n0 = Avx2.MultiplyLow(n0, mask0_256);
        n1 = Avx2.MultiplyLow(n1, mask1_256);
        n2 = Avx2.MultiplyLow(n2, mask2_256);
        // n3 always included!!

        var sum = Avx2.Add(Avx2.Add(n0, n1), Avx2.Add(n2, n3));


        var popcntsU32 = Sse41.ConvertToVector128Int32(popcnts).AsUInt32();
        var popcnts256 = Vector256.Create(popcntsU32, popcntsU32);
        var r = divpopcnt256(sum, popcnts256);

        // extract sky and block, then pack
        var sky = r.GetLower();
        var block = r.GetUpper();
        block = Sse2.ShiftLeftLogical(block, 4);
        var packed = Sse2.Or(sky, block);

        // pack 4 bytes into uint32
        var mask = Vector128.Create(0, 4, 8, 12, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF);
        var packedBytes = Ssse3.Shuffle(packed.AsByte(), mask);
        return packedBytes.AsUInt32().ToScalar();
    }

    /// <summary>
    /// SSE fallback
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint average2x4_sse(Vector128<uint> lightNibbles, uint oFlagsPacked) {
        // if (oFlags < 3) oFlags &= 3; else unchanged
        var oFlags = Vector128.CreateScalarUnsafe(oFlagsPacked).AsByte();
        var three = Vector128.Create((byte)3);
        var lt3 = Sse2.CompareLessThan(oFlags.AsSByte(), three.AsSByte()).AsByte();
        var masked = Sse2.And(oFlags, three);
        var clamped = Sse41.BlendVariable(oFlags, masked, lt3);

        // inv = ~oFlags & 0x7
        var seven = Vector128.Create((byte)0x7);
        var inv = Sse2.AndNot(clamped, seven);

        // popcnt + 1
        var popLUT = Vector128.Create((byte)1, 2, 2, 3, 2, 3, 3, 4, 0, 0, 0, 0, 0, 0, 0, 0);
        var popcnts = Ssse3.Shuffle(popLUT, inv);

        var m = Vector128.Create(0xFu);

        // skylight
        var s0 = Sse2.And(lightNibbles, m);
        var s1 = Sse2.And(Sse2.ShiftRightLogical(lightNibbles, 8), m);
        var s2 = Sse2.And(Sse2.ShiftRightLogical(lightNibbles, 16), m);
        var s3 = Sse2.And(Sse2.ShiftRightLogical(lightNibbles, 24), m);

        // blocklight
        var b0 = Sse2.And(Sse2.ShiftRightLogical(lightNibbles, 4), m);
        var b1 = Sse2.And(Sse2.ShiftRightLogical(lightNibbles, 12), m);
        var b2 = Sse2.And(Sse2.ShiftRightLogical(lightNibbles, 20), m);
        var b3 = Sse2.ShiftRightLogical(lightNibbles, 28);

        var invU32 = Sse41.ConvertToVector128Int32(inv).AsUInt32();
        var mask0 = Sse2.And(invU32, Vector128.Create(1u));
        var mask1 = Sse2.ShiftRightLogical(Sse2.And(invU32, Vector128.Create(2u)), 1);
        var mask2 = Sse2.ShiftRightLogical(Sse2.And(invU32, Vector128.Create(4u)), 2);

        // Conditional sum (multiply by 0 or 1 masks)
        s0 = Sse41.MultiplyLow(s0, mask0);
        s1 = Sse41.MultiplyLow(s1, mask1);
        s2 = Sse41.MultiplyLow(s2, mask2);
        // s3 (face light) always included, no mask ^^
        b0 = Sse41.MultiplyLow(b0, mask0);
        b1 = Sse41.MultiplyLow(b1, mask1);
        b2 = Sse41.MultiplyLow(b2, mask2);

        var sumSky = Sse2.Add(Sse2.Add(s0, s1), Sse2.Add(s2, s3));
        var sumBlock = Sse2.Add(Sse2.Add(b0, b1), Sse2.Add(b2, b3));

        // divide by popcnt (1, 2, 3, or 4)
        // popcnt is in bytes, need to expand to uint32 for division
        var popcntsU32 = Sse41.ConvertToVector128Int32(popcnts).AsUInt32();

        var sky = divpopcnt(sumSky, popcntsU32);
        var block = divpopcnt(sumBlock, popcntsU32);

        block = Sse2.ShiftLeftLogical(block, 4);
        var result = Sse2.Or(sky, block);

        // Pack 4 bytes (at positions 0, 4, 8, 12 when viewed as bytes) into a single uint32
        // Use shuffle to extract bytes 0, 4, 8, 12 then pack
        if (Ssse3.IsSupported) {
            var mask = Vector128.Create(0, 4, 8, 12, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF);
            var packed = Ssse3.Shuffle(result.AsByte(), mask);
            return packed.AsUInt32().ToScalar();
        }
        else {
            // like average2, extract each byte and pack manually
            return (result.GetElement(0) & 0xFFu) |
                   ((result.GetElement(1) & 0xFFu) << 8) |
                   ((result.GetElement(2) & 0xFFu) << 16) |
                   ((result.GetElement(3) & 0xFFu) << 24);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<uint> divpopcnt(Vector128<uint> sum, Vector128<uint> popcnt) {
        // popcnt is 1, 2, 3, or 4
        // Div by 1: no-op
        // Div by 2: >> 1
        // Div by 3: * 0x5556 >> 16 (approximation)
        // Div by 4: >> 2

        var div1 = sum;
        var div2 = Sse2.ShiftRightLogical(sum, 1);
        var div4 = Sse2.ShiftRightLogical(sum, 2);

        // Div by 3: multiply by reciprocal
        // 1/3 ≈ 0.333... ≈ 21845/65536 = 0x5555/0x10000
        // But for small integers (max sum = 4*15 = 60), we can use: (x * 0xAAAB) >> 17
        // Or for x <= 255: (x * 85) >> 8 (it won't fit tho?)
        var rcp3 = Vector128.Create(0xAAABu);
        var mul3 = Sse41.MultiplyLow(sum, rcp3);
        var div3 = Sse2.ShiftRightLogical(mul3, 17);

        var is1 = Sse2.CompareEqual(popcnt, Vector128.Create(1u));
        var is2 = Sse2.CompareEqual(popcnt, Vector128.Create(2u));
        var is3 = Sse2.CompareEqual(popcnt, Vector128.Create(3u));
        // is4 is implicit (else)

        // r = is1 ? divBy1 : (is2 ? divBy2 : (is3 ? divBy3 : divBy4))
        var r = Sse41.BlendVariable(div4.AsSingle(), div3.AsSingle(), is3.AsSingle()).AsUInt32();
        r = Sse41.BlendVariable(r.AsSingle(), div2.AsSingle(), is2.AsSingle()).AsUInt32();
        r = Sse41.BlendVariable(r.AsSingle(), div1.AsSingle(), is1.AsSingle()).AsUInt32();

        return r;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<uint> divpopcnt256(Vector256<uint> sum, Vector256<uint> popcnt) {
        var div1 = sum;
        var div2 = Avx2.ShiftRightLogical(sum, 1);
        var div4 = Avx2.ShiftRightLogical(sum, 2);

        var rcp3 = Vector256.Create(0xAAABu);
        var mul3 = Avx2.MultiplyLow(sum, rcp3);
        var div3 = Avx2.ShiftRightLogical(mul3, 17);

        var is1 = Avx2.CompareEqual(popcnt, Vector256.Create(1u));
        var is2 = Avx2.CompareEqual(popcnt, Vector256.Create(2u));
        var is3 = Avx2.CompareEqual(popcnt, Vector256.Create(3u));

        var r = Avx2.BlendVariable(div4, div3, is3);
        r = Avx2.BlendVariable(r, div2, is2);
        r = Avx2.BlendVariable(r, div1, is1);

        return r;
    }

    /// <summary>
    /// SIMD calculateVertexLightAndAO
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void calculateVertexLightAndAO_x4(
        // v0
        int x00, int y00, int z00, int x01, int y01, int z01, int x02, int y02, int z02,
        // v1
        int x10, int y10, int z10, int x11, int y11, int z11, int x12, int y12, int z12,
        // v2
        int x20, int y20, int z20, int x21, int y21, int z21, int x22, int y22, int z22,
        // v3
        int x30, int y30, int z30, int x31, int y31, int z31, int x32, int y32, int z32,
        byte lb,
        out FourBytes light,
        out FourBytes opacity) {
        Unsafe.SkipInit(out light);
        Unsafe.SkipInit(out opacity);

        int o00 = (y00 + 1) * LOCALCACHESIZE_SQ + (z00 + 1) * LOCALCACHESIZE + (x00 + 1);
        int o01 = (y01 + 1) * LOCALCACHESIZE_SQ + (z01 + 1) * LOCALCACHESIZE + (x01 + 1);
        int o02 = (y02 + 1) * LOCALCACHESIZE_SQ + (z02 + 1) * LOCALCACHESIZE + (x02 + 1);

        int o10 = (y10 + 1) * LOCALCACHESIZE_SQ + (z10 + 1) * LOCALCACHESIZE + (x10 + 1);
        int o11 = (y11 + 1) * LOCALCACHESIZE_SQ + (z11 + 1) * LOCALCACHESIZE + (x11 + 1);
        int o12 = (y12 + 1) * LOCALCACHESIZE_SQ + (z12 + 1) * LOCALCACHESIZE + (x12 + 1);

        int o20 = (y20 + 1) * LOCALCACHESIZE_SQ + (z20 + 1) * LOCALCACHESIZE + (x20 + 1);
        int o21 = (y21 + 1) * LOCALCACHESIZE_SQ + (z21 + 1) * LOCALCACHESIZE + (x21 + 1);
        int o22 = (y22 + 1) * LOCALCACHESIZE_SQ + (z22 + 1) * LOCALCACHESIZE + (x22 + 1);

        int o30 = (y30 + 1) * LOCALCACHESIZE_SQ + (z30 + 1) * LOCALCACHESIZE + (x30 + 1);
        int o31 = (y31 + 1) * LOCALCACHESIZE_SQ + (z31 + 1) * LOCALCACHESIZE + (x31 + 1);
        int o32 = (y32 + 1) * LOCALCACHESIZE_SQ + (z32 + 1) * LOCALCACHESIZE + (x32 + 1);

        uint l0 = (uint)(ctx.lightCache[o00] |
                                  (ctx.lightCache[o01] << 8) |
                                  (ctx.lightCache[o02] << 16) |
                                  (lb << 24));

        uint l1 = (uint)(ctx.lightCache[o10] |
                                  (ctx.lightCache[o11] << 8) |
                                  (ctx.lightCache[o12] << 16) |
                                  (lb << 24));

        uint l2 = (uint)(ctx.lightCache[o20] |
                                  (ctx.lightCache[o21] << 8) |
                                  (ctx.lightCache[o22] << 16) |
                                  (lb << 24));

        uint l3 = (uint)(ctx.lightCache[o30] |
                                  (ctx.lightCache[o31] << 8) |
                                  (ctx.lightCache[o32] << 16) |
                                  (lb << 24));

        byte o0 = (byte)((Unsafe.BitCast<bool, byte>(Block.fullBlock[ctx.blockCache[o00].getID()])) |
                               (Unsafe.BitCast<bool, byte>(Block.fullBlock[ctx.blockCache[o01].getID()]) << 1) |
                               (Unsafe.BitCast<bool, byte>(Block.fullBlock[ctx.blockCache[o02].getID()]) << 2));

        byte o1 = (byte)((Unsafe.BitCast<bool, byte>(Block.fullBlock[ctx.blockCache[o10].getID()])) |
                               (Unsafe.BitCast<bool, byte>(Block.fullBlock[ctx.blockCache[o11].getID()]) << 1) |
                               (Unsafe.BitCast<bool, byte>(Block.fullBlock[ctx.blockCache[o12].getID()]) << 2));

        byte o2 = (byte)((Unsafe.BitCast<bool, byte>(Block.fullBlock[ctx.blockCache[o20].getID()])) |
                               (Unsafe.BitCast<bool, byte>(Block.fullBlock[ctx.blockCache[o21].getID()]) << 1) |
                               (Unsafe.BitCast<bool, byte>(Block.fullBlock[ctx.blockCache[o22].getID()]) << 2));

        byte o3 = (byte)((Unsafe.BitCast<bool, byte>(Block.fullBlock[ctx.blockCache[o30].getID()])) |
                               (Unsafe.BitCast<bool, byte>(Block.fullBlock[ctx.blockCache[o31].getID()]) << 1) |
                               (Unsafe.BitCast<bool, byte>(Block.fullBlock[ctx.blockCache[o32].getID()]) << 2));

        var lightNibbles = Vector128.Create(l0, l1, l2, l3);
        uint oFlagsPacked = (uint)(o0 | (o1 << 8) | (o2 << 16) | (o3 << 24));

        light.Whole = average2x4_simd(lightNibbles, oFlagsPacked);
        opacity.Whole = oFlagsPacked;
    }

    /// <summary>
    /// SIMD getDirectionOffsetsAndData
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void getDirectionOffsetsAndData_simd(RawDirection dir, byte lb, out FourBytes light, out FourBytes o) {
        Unsafe.SkipInit(out o);
        Unsafe.SkipInit(out light);

        switch (dir) {
            case RawDirection.WEST:
                calculateVertexLightAndAO_x4(
                    -1, 0, 1, -1, 1, 0, -1, 1, 1,
                    -1, 0, 1, -1, -1, 0, -1, -1, 1,
                    -1, 0, -1, -1, -1, 0, -1, -1, -1,
                    -1, 0, -1, -1, 1, 0, -1, 1, -1,
                    lb, out light, out o);
                break;
            case RawDirection.EAST:
                calculateVertexLightAndAO_x4(
                    1, 0, -1, 1, 1, 0, 1, 1, -1,
                    1, 0, -1, 1, -1, 0, 1, -1, -1,
                    1, 0, 1, 1, -1, 0, 1, -1, 1,
                    1, 0, 1, 1, 1, 0, 1, 1, 1,
                    lb, out light, out o);
                break;
            case RawDirection.SOUTH:
                calculateVertexLightAndAO_x4(
                    -1, 0, -1, 0, 1, -1, -1, 1, -1,
                    -1, 0, -1, 0, -1, -1, -1, -1, -1,
                    1, 0, -1, 0, -1, -1, 1, -1, -1,
                    1, 0, -1, 0, 1, -1, 1, 1, -1,
                    lb, out light, out o);
                break;
            case RawDirection.NORTH:
                calculateVertexLightAndAO_x4(
                    1, 0, 1, 0, 1, 1, 1, 1, 1,
                    1, 0, 1, 0, -1, 1, 1, -1, 1,
                    -1, 0, 1, 0, -1, 1, -1, -1, 1,
                    -1, 0, 1, 0, 1, 1, -1, 1, 1,
                    lb, out light, out o);
                break;
            case RawDirection.DOWN:
                calculateVertexLightAndAO_x4(
                    0, -1, 1, 1, -1, 0, 1, -1, 1,
                    0, -1, -1, 1, -1, 0, 1, -1, -1,
                    0, -1, -1, -1, -1, 0, -1, -1, -1,
                    0, -1, 1, -1, -1, 0, -1, -1, 1,
                    lb, out light, out o);
                break;
            case RawDirection.UP:
                calculateVertexLightAndAO_x4(
                    0, 1, 1, -1, 1, 0, -1, 1, 1,
                    0, 1, -1, -1, 1, 0, -1, 1, -1,
                    0, 1, -1, 1, 1, 0, 1, 1, -1,
                    0, 1, 1, 1, 1, 0, 1, 1, 1,
                    lb, out light, out o);
                break;
            case RawDirection.NONE:
                // No directional faces - just set default values
                light = new FourBytes(lb, lb, lb, lb);
                o = new FourBytes(0, 0, 0, 0);
                break;
        }
    }
}
