using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

public static partial class OpenSimplex2 {
    private const int PX32 = 501125321;
    private const int PY32 = 1136930381;
    private const int PZ32 = 1720413743;
    private const int HASH_MUL32 = 0x27d4eb2d;
    private const int SEED_FLIP32 = 0x5C4D8B21;

    public static bool simdSupported => Avx2.IsSupported && Fma.IsSupported;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe Vector256<float> grad8(float* g, Vector256<int> seed,
        Vector256<int> xp, Vector256<int> yp, Vector256<int> zp,
        Vector256<float> dx, Vector256<float> dy, Vector256<float> dz) {
        var h = (seed ^ xp ^ yp ^ zp) * Vector256.Create(HASH_MUL32);
        h ^= Vector256.ShiftRightLogical(h, 15);
        var gi = h & Vector256.Create((N_GRADS_3D - 1) << 2);
        var gx = Avx2.GatherVector256(g, gi, 4);
        var gy = Avx2.GatherVector256(g + 1, gi, 4);
        var gz = Avx2.GatherVector256(g + 2, gi, 4);
        return Fma.MultiplyAdd(gx, dx, Fma.MultiplyAdd(gy, dy, gz * dz));
    }

    public static unsafe Vector256<float> noise3x8(int seed, Vector256<float> x, Vector256<float> y, Vector256<float> z) {
        var zero = Vector256<float>.Zero;
        var half = Vector256.Create(0.5f);
        var one = Vector256.Create(1f);
        var negOne = Vector256.Create(-1f);

        // fallback rotation
        var r = Vector256.Create((float)FALLBACK_ROTATE_3D) * (x + y + z);
        var xr = r - x;
        var yr = r - y;
        var zr = r - z;

        // round to nearest
        var xrb = Avx.ConvertToVector256Int32(xr);
        var yrb = Avx.ConvertToVector256Int32(yr);
        var zrb = Avx.ConvertToVector256Int32(zr);
        var xri = xr - Avx.ConvertToVector256Single(xrb);
        var yri = yr - Avx.ConvertToVector256Single(yrb);
        var zri = zr - Avx.ConvertToVector256Single(zrb);

        // NSign = -1 if the offset is positive, +1 if negative
        var xNeg = Vector256.LessThan(xri, zero);
        var yNeg = Vector256.LessThan(yri, zero);
        var zNeg = Vector256.LessThan(zri, zero);
        var xNS = Vector256.ConditionalSelect(xNeg, one, negOne);
        var yNS = Vector256.ConditionalSelect(yNeg, one, negOne);
        var zNS = Vector256.ConditionalSelect(zNeg, one, negOne);

        var ax0 = Vector256.Abs(xri);
        var ay0 = Vector256.Abs(yri);
        var az0 = Vector256.Abs(zri);

        var px = Vector256.Create(PX32);
        var py = Vector256.Create(PY32);
        var pz = Vector256.Create(PZ32);
        var xp = xrb * px;
        var yp = yrb * py;
        var zp = zrb * pz;

        var seedV = Vector256.Create(seed);
        var value = zero;
        var a = Vector256.Create(RSQUARED_3D) - xri * xri - (yri * yri + zri * zri);

        fixed (float* g = GRADIENTS_3D) {
            for (int l = 0;; l++) {
                // closest point on this lattice copy
                var am = Vector256.GreaterThan(a, zero);
                var a2 = a * a;
                value += am & (a2 * a2 * grad8(g, seedV, xp, yp, zp, xri, yri, zri));

                // second-closest: step along the axis with the largest offset
                var mx = Vector256.GreaterThanOrEqual(ax0, ay0) & Vector256.GreaterThanOrEqual(ax0, az0);
                var my = Vector256.AndNot(Vector256.GreaterThan(ay0, ax0) & Vector256.GreaterThanOrEqual(ay0, az0), mx);
                var mz = ~(mx | my);
                var bAdd = Vector256.ConditionalSelect(mx, ax0, Vector256.ConditionalSelect(my, ay0, az0));
                var b = a + bAdd + bAdd;
                var bm = Vector256.GreaterThan(b, one);
                b -= one;

                // prime += -NSign * P on the chosen axis: NSign=-1 -> +P, NSign=+1 -> -P
                var xp2 = xp + (Vector256.ConditionalSelect(xNeg.AsInt32(), -px, px) & mx.AsInt32());
                var yp2 = yp + (Vector256.ConditionalSelect(yNeg.AsInt32(), -py, py) & my.AsInt32());
                var zp2 = zp + (Vector256.ConditionalSelect(zNeg.AsInt32(), -pz, pz) & mz.AsInt32());
                var dx2 = xri + (xNS & mx);
                var dy2 = yri + (yNS & my);
                var dz2 = zri + (zNS & mz);

                var b2 = b * b;
                value += bm & (b2 * b2 * grad8(g, seedV, xp2, yp2, zp2, dx2, dy2, dz2));

                if (l == 1) {
                    break;
                }

                // flip to the other lattice copy
                ax0 = half - ax0;
                ay0 = half - ay0;
                az0 = half - az0;
                xri = xNS * ax0;
                yri = yNS * ay0;
                zri = zNS * az0;
                a += Vector256.Create(0.75f) - ax0 - (ay0 + az0);

                // (NSign >> 1) & P: add P where NSign == -1, i.e. where the offset was positive
                xp += Vector256.AndNot(px, xNeg.AsInt32());
                yp += Vector256.AndNot(py, yNeg.AsInt32());
                zp += Vector256.AndNot(pz, zNeg.AsInt32());

                xNS = -xNS;
                yNS = -yNS;
                zNS = -zNS;
                xNeg = ~xNeg;
                yNeg = ~yNeg;
                zNeg = ~zNeg;
                seedV ^= Vector256.Create(SEED_FLIP32);
            }
        }

        return value;
    }
}
