namespace BlockGame.util.meth.noise;

using System.Runtime.CompilerServices;

/**
 * K.jpg's OpenSimplex 2, faster variant
 * Modified to have an exponential slope distribution,
 * inspired by http://jcgt.org/published/0004/02/01/
 */
public class OpenSimplex2S_Exp
{
    private const long PRIME_X = 0x5205402B9270C86FL;
    private const long PRIME_Y = 0x598CD327003817B5L;
    private const long PRIME_Z = 0x5BCC226E9FA0BACBL;
    private const long HASH_MULTIPLIER = 0x53A3F72DEEC546F5L;
    private const long SEED_FLIP_3D = -0x52D547B2E96ED629L;
    private const long EXP_HASH_MULTIPLIER = 0x6C8E9CF570932BD5L; // different multiplier for exp distribution

    private const double ROOT2OVER2 = 0.7071067811865476;
    private const double SKEW_2D = 0.366025403784439;
    private const double UNSKEW_2D = -0.21132486540518713;

    private const double ROOT3OVER3 = 0.577350269189626;
    private const double FALLBACK_ROTATE_3D = 2.0 / 3.0;
    private const double ROTATE_3D_ORTHOGONALIZER = UNSKEW_2D;

    private const int N_GRADS_2D_EXPONENT = 7;
    private const int N_GRADS_3D_EXPONENT = 8;
    private const int N_GRADS_2D = 1 << N_GRADS_2D_EXPONENT;
    private const int N_GRADS_3D = 1 << N_GRADS_3D_EXPONENT;

    private const double NORMALIZER_2D = 0.01001634121365712;
    private const double NORMALIZER_3D = 0.07969837668935331;

    private const float RSQUARED_2D = 0.5f;
    private const float RSQUARED_3D = 0.6f;

    private readonly long seed;
    private readonly double[] expMultipliers;
    private const int EXP_TABLE_SIZE = 2048;
    private const int EXP_TABLE_MASK = 2047;

    /**
     * expBase - The base of the exponential distribution (must be > 1)
     * Controls how steep the exponential curve is
     * Higher values = more extreme distribution with larger amplitude variations
     *
     * expMin - Minimum amplitude threshold (0 to 1)
     * Sets the floor for amplitude multipliers
     * Prevents contributions from becoming too weak
     * Maps the uniform distribution [0,1] to [expMin, 1]
     */
    public OpenSimplex2S_Exp(long seed, double expBase, double expMin)
    {
        this.seed = seed;
        expMultipliers = new double[EXP_TABLE_SIZE];

        // generate exponential distribution table
        for (int i = 0; i < EXP_TABLE_SIZE; i++)
        {
            double expLevel = i * 1.0 / EXP_TABLE_SIZE;
            expLevel = expMin + (1 - expMin) * expLevel;
            expLevel *= 1 - 1 / expBase;
            expMultipliers[i] = -Math.Log(1 - expLevel) / Math.Log(expBase);
        }
    }

    /*
     * Noise Evaluators
     */

    /**
     * 2D Simplex noise, standard lattice orientation.
     */
    public double noise2(double x, double y)
    {
        // Get points for A2* lattice
        double s = SKEW_2D * (x + y);
        double xs = x + s, ys = y + s;

        return Noise2_UnskewedBase(xs, ys);
    }

    /**
     * 2D Simplex noise, with Y pointing down the main diagonal.
     * Might be better for a 2D sandbox style game, where Y is vertical.
     */
    public double noise2_XBeforeY(double x, double y)
    {
        // Skew transform and rotation baked into one.
        double xx = x * ROOT2OVER2;
        double yy = y * (ROOT2OVER2 * (1 + 2 * SKEW_2D));

        return Noise2_UnskewedBase(yy + xx, yy - xx);
    }

    /**
     * 2D Simplex noise base.
     */
    private double Noise2_UnskewedBase(double xs, double ys)
    {
        // Get base points and offsets.
        int xsb = FastFloor(xs), ysb = FastFloor(ys);
        float xi = (float)(xs - xsb), yi = (float)(ys - ysb);

        // Prime pre-multiplication for hash.
        long xsbp = xsb * PRIME_X, ysbp = ysb * PRIME_Y;

        // Unskew.
        float t = (xi + yi) * (float)UNSKEW_2D;
        float dx0 = xi + t, dy0 = yi + t;

        // First vertex.
        double value = 0;
        float a0 = RSQUARED_2D - dx0 * dx0 - dy0 * dy0;
        if (a0 > 0)
        {
            double mult = GetExpMultiplier(seed, xsbp, ysbp);
            value = (a0 * a0) * (a0 * a0) * Grad(xsbp, ysbp, dx0, dy0) * mult;
        }

        // Second vertex.
        float a1 = (float)(2 * (1 + 2 * UNSKEW_2D) * (1 / UNSKEW_2D + 2)) * t + ((float)(-2 * (1 + 2 * UNSKEW_2D) * (1 + 2 * UNSKEW_2D)) + a0);
        if (a1 > 0)
        {
            float dx1 = dx0 - (float)(1 + 2 * UNSKEW_2D);
            float dy1 = dy0 - (float)(1 + 2 * UNSKEW_2D);
            double mult = GetExpMultiplier(seed, xsbp + PRIME_X, ysbp + PRIME_Y);
            value += (a1 * a1) * (a1 * a1) * Grad(xsbp + PRIME_X, ysbp + PRIME_Y, dx1, dy1) * mult;
        }

        // Third vertex.
        if (dy0 > dx0)
        {
            float dx2 = dx0 - (float)UNSKEW_2D;
            float dy2 = dy0 - (float)(UNSKEW_2D + 1);
            float a2 = RSQUARED_2D - dx2 * dx2 - dy2 * dy2;
            if (a2 > 0)
            {
                double mult = GetExpMultiplier(seed, xsbp, ysbp + PRIME_Y);
                value += (a2 * a2) * (a2 * a2) * Grad(xsbp, ysbp + PRIME_Y, dx2, dy2) * mult;
            }
        }
        else
        {
            float dx2 = dx0 - (float)(UNSKEW_2D + 1);
            float dy2 = dy0 - (float)UNSKEW_2D;
            float a2 = RSQUARED_2D - dx2 * dx2 - dy2 * dy2;
            if (a2 > 0)
            {
                double mult = GetExpMultiplier(seed, xsbp + PRIME_X, ysbp);
                value += (a2 * a2) * (a2 * a2) * Grad(xsbp + PRIME_X, ysbp, dx2, dy2) * mult;
            }
        }

        return value;
    }

    /**
     * 3D OpenSimplex2 noise, with better visual isotropy in (X, Y).
     * Recommended for 3D terrain and time-varied animations.
     * The Z coordinate should always be the "different" coordinate in whatever your use case is.
     * If Y is vertical in world coordinates, call noise3_XYBeforeZ(x, z, Y) or use noise3_XZBeforeY.
     * If Z is vertical in world coordinates, call noise3_XYBeforeZ(x, y, Z).
     * For a time varied animation, call noise3_XYBeforeZ(x, y, T).
     */
    public double noise3_XYBeforeZ(double x, double y, double z)
    {
        // Re-orient the cubic lattices without skewing, so Z points up the main lattice diagonal,
        // and the planes formed by XY are moved far out of alignment with the cube faces.
        // Orthonormal rotation. Not a skew transform.
        double xy = x + y;
        double s2 = xy * ROTATE_3D_ORTHOGONALIZER;
        double zz = z * ROOT3OVER3;
        double xr = x + s2 + zz;
        double yr = y + s2 + zz;
        double zr = xy * -ROOT3OVER3 + zz;

        // Evaluate both lattices to form a BCC lattice.
        return Noise3_UnrotatedBase(xr, yr, zr);
    }

    /**
     * 3D OpenSimplex2 noise, with better visual isotropy in (X, Z).
     * Recommended for 3D terrain and time-varied animations.
     * The Y coordinate should always be the "different" coordinate in whatever your use case is.
     * If Y is vertical in world coordinates, call noise3_XZBeforeY(x, Y, z).
     * If Z is vertical in world coordinates, call noise3_XZBeforeY(x, Z, y) or use noise3_XYBeforeZ.
     * For a time varied animation, call noise3_XZBeforeY(x, T, y) or use noise3_XYBeforeZ.
     */
    public double noise3_XZBeforeY(double x, double y, double z)
    {
        // Re-orient the cubic lattices without skewing, so Y points up the main lattice diagonal,
        // and the planes formed by XZ are moved far out of alignment with the cube faces.
        // Orthonormal rotation. Not a skew transform.
        double xz = x + z;
        double s2 = xz * ROTATE_3D_ORTHOGONALIZER;
        double yy = y * ROOT3OVER3;
        double xr = x + s2 + yy;
        double zr = z + s2 + yy;
        double yr = xz * -ROOT3OVER3 + yy;

        // Evaluate both lattices to form a BCC lattice.
        return Noise3_UnrotatedBase(xr, yr, zr);
    }

    /**
     * 3D OpenSimplex2 noise, fallback rotation option
     * Use noise3_XYBeforeZ or noise3_XZBeforeY instead, wherever appropriate.
     */
    public double noise3_Classic(double x, double y, double z)
    {
        // Re-orient the cubic lattices via rotation, to produce a familiar look.
        // Orthonormal rotation. Not a skew transform.
        double r = FALLBACK_ROTATE_3D * (x + y + z);
        double xr = r - x, yr = r - y, zr = r - z;

        // Evaluate both lattices to form a BCC lattice.
        return Noise3_UnrotatedBase(xr, yr, zr);
    }

    /**
     * Generate overlapping cubic lattices for 3D OpenSimplex2 noise.
     */
    private double Noise3_UnrotatedBase(double xr, double yr, double zr)
    {
        // Get base points and offsets.
        int xrb = FastRound(xr), yrb = FastRound(yr), zrb = FastRound(zr);
        float xri = (float)(xr - xrb), yri = (float)(yr - yrb), zri = (float)(zr - zrb);

        // -1 if positive, 1 if negative.
        int xNSign = (int)(-1.0f - xri) | 1, yNSign = (int)(-1.0f - yri) | 1, zNSign = (int)(-1.0f - zri) | 1;

        // Compute absolute values, using the above as a shortcut.
        float ax0 = xNSign * -xri, ay0 = yNSign * -yri, az0 = zNSign * -zri;

        // Prime pre-multiplication for hash.
        long xrbp = xrb * PRIME_X, yrbp = yrb * PRIME_Y, zrbp = zrb * PRIME_Z;

        // Loop: Pick an edge on each lattice copy.
        double value = 0;
        float a = (RSQUARED_3D - xri * xri) - (yri * yri + zri * zri);
        long currentSeed = seed;
        for (int l = 0; ; l++)
        {
            // Closest point on cube.
            if (a > 0)
            {
                double mult = GetExpMultiplier(currentSeed, xrbp, yrbp, zrbp);
                value += (a * a) * (a * a) * Grad(xrbp, yrbp, zrbp, xri, yri, zri) * mult;
            }

            // Second-closest point.
            if (ax0 >= ay0 && ax0 >= az0)
            {
                float b = a + ax0 + ax0;
                if (b > 1)
                {
                    b -= 1;
                    double mult = GetExpMultiplier(currentSeed, xrbp - xNSign * PRIME_X, yrbp, zrbp);
                    value += (b * b) * (b * b) * Grad(xrbp - xNSign * PRIME_X, yrbp, zrbp, xri + xNSign, yri, zri) * mult;
                }
            }
            else if (ay0 > ax0 && ay0 >= az0)
            {
                float b = a + ay0 + ay0;
                if (b > 1)
                {
                    b -= 1;
                    double mult = GetExpMultiplier(currentSeed, xrbp, yrbp - yNSign * PRIME_Y, zrbp);
                    value += (b * b) * (b * b) * Grad(xrbp, yrbp - yNSign * PRIME_Y, zrbp, xri, yri + yNSign, zri) * mult;
                }
            }
            else
            {
                float b = a + az0 + az0;
                if (b > 1)
                {
                    b -= 1;
                    double mult = GetExpMultiplier(currentSeed, xrbp, yrbp, zrbp - zNSign * PRIME_Z);
                    value += (b * b) * (b * b) * Grad(xrbp, yrbp, zrbp - zNSign * PRIME_Z, xri, yri, zri + zNSign) * mult;
                }
            }

            // Break from loop if we're done, skipping updates below.
            if (l == 1) break;

            // Update absolute value.
            ax0 = 0.5f - ax0;
            ay0 = 0.5f - ay0;
            az0 = 0.5f - az0;

            // Update relative coordinate.
            xri = xNSign * ax0;
            yri = yNSign * ay0;
            zri = zNSign * az0;

            // Update falloff.
            a += (0.75f - ax0) - (ay0 + az0);

            // Update prime for hash.
            xrbp += (xNSign >> 1) & PRIME_X;
            yrbp += (yNSign >> 1) & PRIME_Y;
            zrbp += (zNSign >> 1) & PRIME_Z;

            // Update the reverse sign indicators.
            xNSign = -xNSign;
            yNSign = -yNSign;
            zNSign = -zNSign;

            // And finally update the seed for the other lattice copy.
            currentSeed ^= SEED_FLIP_3D;
        }

        return value;
    }

    /*
     * Utility
     */

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double GetExpMultiplier(long seed, long xp, long yp)
    {
        long hash = seed ^ xp ^ yp;
        hash *= EXP_HASH_MULTIPLIER;
        hash ^= hash >> 32;
        int index = (int)(hash & EXP_TABLE_MASK);
        return expMultipliers[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double GetExpMultiplier(long seed, long xp, long yp, long zp)
    {
        long hash = (seed ^ xp) ^ (yp ^ zp);
        hash *= EXP_HASH_MULTIPLIER;
        hash ^= hash >> 32;
        int index = (int)(hash & EXP_TABLE_MASK);
        return expMultipliers[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Grad(long xsvp, long ysvp, float dx, float dy)
    {
        long hash = seed ^ xsvp ^ ysvp;
        hash *= HASH_MULTIPLIER;
        hash ^= hash >> (64 - N_GRADS_2D_EXPONENT + 1);
        int gi = (int)hash & ((N_GRADS_2D - 1) << 1);
        return GRADIENTS_2D[gi | 0] * dx + GRADIENTS_2D[gi | 1] * dy;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Grad(long xrvp, long yrvp, long zrvp, float dx, float dy, float dz)
    {
        long hash = (seed ^ xrvp) ^ (yrvp ^ zrvp);
        hash *= HASH_MULTIPLIER;
        hash ^= hash >> (64 - N_GRADS_3D_EXPONENT + 2);
        int gi = (int)hash & ((N_GRADS_3D - 1) << 2);
        return GRADIENTS_3D[gi | 0] * dx + GRADIENTS_3D[gi | 1] * dy + GRADIENTS_3D[gi | 2] * dz;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FastFloor(double x)
    {
        int xi = (int)x;
        return x < xi ? xi - 1 : xi;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FastRound(double x)
    {
        return x < 0 ? (int)(x - 0.5) : (int)(x + 0.5);
    }

    /*
     * Gradients
     */

    private static readonly float[] GRADIENTS_2D;
    private static readonly float[] GRADIENTS_3D;
    static OpenSimplex2S_Exp()
    {
        GRADIENTS_2D = new float[N_GRADS_2D * 2];
        float[] grad2 = {
             0.38268343236509f,   0.923879532511287f,
             0.923879532511287f,  0.38268343236509f,
             0.923879532511287f, -0.38268343236509f,
             0.38268343236509f,  -0.923879532511287f,
            -0.38268343236509f,  -0.923879532511287f,
            -0.923879532511287f, -0.38268343236509f,
            -0.923879532511287f,  0.38268343236509f,
            -0.38268343236509f,   0.923879532511287f,
            //-------------------------------------//
             0.130526192220052f,  0.99144486137381f,
             0.608761429008721f,  0.793353340291235f,
             0.793353340291235f,  0.608761429008721f,
             0.99144486137381f,   0.130526192220051f,
             0.99144486137381f,  -0.130526192220051f,
             0.793353340291235f, -0.60876142900872f,
             0.608761429008721f, -0.793353340291235f,
             0.130526192220052f, -0.99144486137381f,
            -0.130526192220052f, -0.99144486137381f,
            -0.608761429008721f, -0.793353340291235f,
            -0.793353340291235f, -0.608761429008721f,
            -0.99144486137381f,  -0.130526192220052f,
            -0.99144486137381f,   0.130526192220051f,
            -0.793353340291235f,  0.608761429008721f,
            -0.608761429008721f,  0.793353340291235f,
            -0.130526192220052f,  0.99144486137381f,
        };
        for (int i = 0; i < grad2.Length; i++)
        {
            grad2[i] = (float)(grad2[i] / NORMALIZER_2D);
        }
        for (int i = 0, j = 0; i < GRADIENTS_2D.Length; i++, j++)
        {
            if (j == grad2.Length) j = 0;
            GRADIENTS_2D[i] = grad2[j];
        }

        GRADIENTS_3D = new float[N_GRADS_3D * 4];
        float[] grad3 = {
             2.22474487139f,       2.22474487139f,      -1.0f,                 0.0f,
             2.22474487139f,       2.22474487139f,       1.0f,                 0.0f,
             3.0862664687972017f,  1.1721513422464978f,  0.0f,                 0.0f,
             1.1721513422464978f,  3.0862664687972017f,  0.0f,                 0.0f,
            -2.22474487139f,       2.22474487139f,      -1.0f,                 0.0f,
            -2.22474487139f,       2.22474487139f,       1.0f,                 0.0f,
            -1.1721513422464978f,  3.0862664687972017f,  0.0f,                 0.0f,
            -3.0862664687972017f,  1.1721513422464978f,  0.0f,                 0.0f,
            -1.0f,                -2.22474487139f,      -2.22474487139f,       0.0f,
             1.0f,                -2.22474487139f,      -2.22474487139f,       0.0f,
             0.0f,                -3.0862664687972017f, -1.1721513422464978f,  0.0f,
             0.0f,                -1.1721513422464978f, -3.0862664687972017f,  0.0f,
            -1.0f,                -2.22474487139f,       2.22474487139f,       0.0f,
             1.0f,                -2.22474487139f,       2.22474487139f,       0.0f,
             0.0f,                -1.1721513422464978f,  3.0862664687972017f,  0.0f,
             0.0f,                -3.0862664687972017f,  1.1721513422464978f,  0.0f,
            //--------------------------------------------------------------------//
            -2.22474487139f,      -2.22474487139f,      -1.0f,                 0.0f,
            -2.22474487139f,      -2.22474487139f,       1.0f,                 0.0f,
            -3.0862664687972017f, -1.1721513422464978f,  0.0f,                 0.0f,
            -1.1721513422464978f, -3.0862664687972017f,  0.0f,                 0.0f,
            -2.22474487139f,      -1.0f,                -2.22474487139f,       0.0f,
            -2.22474487139f,       1.0f,                -2.22474487139f,       0.0f,
            -1.1721513422464978f,  0.0f,                -3.0862664687972017f,  0.0f,
            -3.0862664687972017f,  0.0f,                -1.1721513422464978f,  0.0f,
            -2.22474487139f,      -1.0f,                 2.22474487139f,       0.0f,
            -2.22474487139f,       1.0f,                 2.22474487139f,       0.0f,
            -3.0862664687972017f,  0.0f,                 1.1721513422464978f,  0.0f,
            -1.1721513422464978f,  0.0f,                 3.0862664687972017f,  0.0f,
            -1.0f,                 2.22474487139f,      -2.22474487139f,       0.0f,
             1.0f,                 2.22474487139f,      -2.22474487139f,       0.0f,
             0.0f,                 1.1721513422464978f, -3.0862664687972017f,  0.0f,
             0.0f,                 3.0862664687972017f, -1.1721513422464978f,  0.0f,
            -1.0f,                 2.22474487139f,       2.22474487139f,       0.0f,
             1.0f,                 2.22474487139f,       2.22474487139f,       0.0f,
             0.0f,                 3.0862664687972017f,  1.1721513422464978f,  0.0f,
             0.0f,                 1.1721513422464978f,  3.0862664687972017f,  0.0f,
             2.22474487139f,      -2.22474487139f,      -1.0f,                 0.0f,
             2.22474487139f,      -2.22474487139f,       1.0f,                 0.0f,
             1.1721513422464978f, -3.0862664687972017f,  0.0f,                 0.0f,
             3.0862664687972017f, -1.1721513422464978f,  0.0f,                 0.0f,
             2.22474487139f,      -1.0f,                -2.22474487139f,       0.0f,
             2.22474487139f,       1.0f,                -2.22474487139f,       0.0f,
             3.0862664687972017f,  0.0f,                -1.1721513422464978f,  0.0f,
             1.1721513422464978f,  0.0f,                -3.0862664687972017f,  0.0f,
             2.22474487139f,      -1.0f,                 2.22474487139f,       0.0f,
             2.22474487139f,       1.0f,                 2.22474487139f,       0.0f,
             1.1721513422464978f,  0.0f,                 3.0862664687972017f,  0.0f,
             3.0862664687972017f,  0.0f,                 1.1721513422464978f,  0.0f,
        };
        for (int i = 0; i < grad3.Length; i++)
        {
            grad3[i] = (float)(grad3[i] / NORMALIZER_3D);
        }
        for (int i = 0, j = 0; i < GRADIENTS_3D.Length; i++, j++)
        {
            if (j == grad3.Length) j = 0;
            GRADIENTS_3D[i] = grad3[j];
        }
    }
}