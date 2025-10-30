// cheats!

global using Color = Molten.Color;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlockGame.world;
using Molten;
using Silk.NET.Maths;
using Plane = System.Numerics.Plane;
using Vector3D = Molten.DoublePrecision.Vector3D;

namespace BlockGame.util;

/// <summary>
/// It's like math but meth ;)
/// </summary>
public static partial class Meth {

    public const double eta = phiF + psiF;
    public const float etaF = phiF + psiF;
    
    public const double phi = 1.61803398874989484820458683436;
    public const float phiF = 1.61803398874989484820458683436f;
    
    public const double psi = 0.61803398874989484820458683436;
    public const float psiF = 0.61803398874989484820458683436f;

    //public const double chi = psi * psi;
    //public const float chiF = psiF * psiF;

    public const double rho = 1 - psi;
    public const float rhoF = 1 - psiF;

    public const double kappa = 1 + rho;
    public const float kappaF = 1 + rhoF;
    
    public const double sqrt2 = 1.4142135623730951; 
    public const float sqrt2F = 1.4142135623730951f; 
    
    public static volatile byte[] waste;
    
    /**
     * I'm not entirely sure in the maths but this seems to work okay so she's a keeper
     */
    public static uint f2b(Vector4 c) {
        var max = new Vector4(255);
        c *= max; // Scale to [0, 255]
        c += new Vector4(0.5f); // Add 0.5 for rounding
        // Clamp the values to the range [0, 255]
        c = Vector4.Min(Vector4.Max(c, Vector4.Zero), max);
        
        // Convert to uint
        byte r = (byte)c.X;
        byte g = (byte)c.Y;
        byte b = (byte)c.Z;
        byte a = (byte)c.W;
    
        return (uint)(r | (g << 8) | (b << 16) | (a << 24));
    }

    public static Vector4 b2f(uint rgba)
    {
        const float inv255 = 1f / 255f;
        var vec = new Vector4(
            (rgba & 0xFF),
            ((rgba >> 8) & 0xFF),
            ((rgba >> 16) & 0xFF),
            ((rgba >> 24) & 0xFF)
        );
        return vec * inv255;
    }

    public static Vector3D copy(Vector3D input) {
        return new Vector3D(input.X, input.Y, input.Z);
    }

    public static Vector3F copy(Vector3F input) {
        return new Vector3F(input.X, input.Y, input.Z);
    }

    public static Vector3I copy(Vector3I input) {
        return new Vector3I(input.X, input.Y, input.Z);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void normi(this ref Vector3D v) {
        double lengthSq = double.MultiplyAddEstimate(v.X, v.X, double.MultiplyAddEstimate(v.Y, v.Y, v.Z * v.Z));
        double invLength = double.ReciprocalSqrtEstimate(lengthSq);
        v.X *= invLength;
        v.Y *= invLength;
        v.Z *= invLength;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void normi(this ref Vector3 v) {
        float lengthSq = float.MultiplyAddEstimate(v.X, v.X, float.MultiplyAddEstimate(v.Y, v.Y, v.Z * v.Z));
        float invLength = float.ReciprocalSqrtEstimate(lengthSq);
        v.X *= invLength;
        v.Y *= invLength;
        v.Z *= invLength;
    }

    /**
     * float.SinCos is accurate. It's also slow. Sometimes we don't care, but this time we do.
     * This one gets roughly the right value in roughly the right time!
     *
     * NEEDS to be in ±2π! Otherwise it will be fucked.
     */
    public static void fsincos(float x, out float sin, out float cos) {
        // todo move out to meth
        const float PI = 3.14159265f;
        const float HALF_PI = 1.57079633f;
        const float TWO_PI = 6.28318531f;
        const float INV_TWO_PI = 0.15915494f;
    
        // Reduce to [-π, π]
        x -= MathF.Round(x * INV_TWO_PI) * TWO_PI;
    
        // Determine quadrant and reduce to [-π/4, π/4]
        float absX = MathF.Abs(x);
        int quad = (int)(absX * 2.0f / PI + 0.5f);
        float y = absX - quad * HALF_PI;
    
        // Apply approximation on reduced range
        float y2 = y * y;
        float s = y * (1.0f - y2 * 0.16666667f);
        float c = 1.0f - y2 * 0.5f;
        
        Unsafe.SkipInit(out sin);
        Unsafe.SkipInit(out cos);
    
        // Apply quadrant corrections
        switch (quad & 3) {
            case 0:
                sin = s;
                cos = c;
                break;
            case 1:
                sin = c;
                cos = -s;
                break;
            case 2:
                sin = -s;
                cos = -c;
                break;
            case 3:
                sin = -c;
                cos = s;
                break;
        }
    
        // Handle negative input
        if (x < 0) {
            sin = -sin;
        }
    }

    /// <summary>
    /// Correct mod which works with negative numbers. i.e. -1 mod 3 is 2 and -3 mod 3 is 0.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int mod(int x, int m) {
        int r = x % m;
        return r < 0 ? r + m : r;
    }

    /// <summary>
    /// Test GC, scaled by dt so it's per sec
    /// </summary>
    public static void wasteMemory(double dt, float megs) {
        waste = new byte[(int)(megs * 1024 * 1024 * dt)];
    }
    public static float deg2rad(float degrees) {
        return degrees * float.Pi / 180f;
    }
    
    public const float d2r = MathF.PI / 180f;
    public const float r2d = 180f / MathF.PI;
    
    public static float rad2deg(float radians) {
        return radians * 180f / float.Pi;
    }

    /// <summary>
    /// Maps a value from one range to another range.
    /// For example, mapRange(0.75f, 0.5f, 1.0f, 0f, 2f) maps 0.75 from range [0.5-1.0] to range [0-2], returning 1.0
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float mapRange(float value, float fromStart, float fromEnd, float toStart, float toEnd) {
        if (value < fromStart) return toStart;
        if (value > fromEnd) return toEnd;
        float t = (value - fromStart) / (fromEnd - fromStart);
        return toStart + t * (toEnd - toStart);
    }

    /// <summary>
    /// Fade in from 0 to 1 over a range
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float fadeIn(float value, float start, float end) {
        return mapRange(value, start, end, 0f, 1f);
    }

    /// <summary>
    /// Fade out from 1 to 0 over a range
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float fadeOut(float value, float start, float end) {
        return mapRange(value, start, end, 1f, 0f);
    }

    public static Vector3I getRandomCoord(XRandom random, int maxX, int maxY, int maxZ) {
        var randomValue = random.Next(maxX * maxY * maxZ);
        var x = randomValue % maxX;
        var y = randomValue / maxX % maxY;
        var z = randomValue / (maxX * maxY);
        return new Vector3I(x, y, z);
    }

    public static Vector3I getRandomCoord(XRandom random, int maxX, int maxY, int maxZ, int minX, int minY, int minZ) {
        return getRandomCoord(random, maxX - minX, maxY - minY, maxZ - minZ) + new Vector3I(minX, minY, minZ);
    }

    public static float ToDegrees(double radians) {
        return (float)(radians * 180.0 / Math.PI);
    }

    public static float lerp(float start, float end, float amount) {
        return start + (end - start) * Math.Clamp(amount, 0.0f, 1.0f);
    }

    public static double lerp(double start, double end, double amount) {
        return start + (end - start) * Math.Clamp(amount, 0.0f, 1.0f);
    }

    public static Vector3 lerp(Vector3 start, Vector3 end, float amount) {
        return start + (end - start) * Math.Clamp(amount, 0.0f, 1.0f);
    }

    /** returns shortest angular difference between two angles in degrees (-180 to 180) */
    public static float angleDiff(float from, float to) {
        float diff = (to - from + 540f) % 360f - 180f;
        return diff;
    }

    /** lerp between angles, taking shortest path */
    public static float lerpAngle(float from, float to, float amount) {
        return from + angleDiff(from, to) * Math.Clamp(amount, 0.0f, 1.0f);
    }

    /** clamps the angle from -180 to 180 */
    public static Vector3 clampAngle(Vector3 a) {
        return new Vector3(
            (a.X + 180f) % 360f - 180f,
            (a.Y + 180f) % 360f - 180f,
            (a.Z + 180f) % 360f - 180f
        );
    }

    public static float clampAngle(float a) {
        return (a + 180f) % 360f - 180f;
    }

    public static Matrix4x4 to4x4(this Matrix4F mat) {
        return Unsafe.BitCast<Matrix4F, Matrix4x4>(mat);
    }

    public static Matrix4F to4F(Matrix4x4 mat) {
        return Unsafe.BitCast<Matrix4x4, Matrix4F>(mat);
    }

    public static int toBlockPos(this double d) {
        return (int)Math.Floor(d);
    }
    
    public static int toBlockPos(this float f) {
        return (int)Math.Floor(f);
    }

    public static Vector3D<T> withoutY<T>(this Vector3D<T> vec) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T> {
        return new Vector3D<T>(vec.X, default, vec.Z);
    }

    public static Vector3 transformVertex(float vx, float vy, float vz, bool brot, Vector3 pivot, float angle, Vector3 ax) {
        if (!brot) {
            return new Vector3(vx, vy, vz);
        }

        var point = new Vector3(vx, vy, vz) - pivot;
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);
            
        Vector3 rotated = ax.X != 0 ? new Vector3(point.X, point.Y * cos - point.Z * sin, point.Y * sin + point.Z * cos) :
            ax.Y != 0 ? new Vector3(point.X * cos + point.Z * sin, point.Y, -point.X * sin + point.Z * cos) :
            ax.Z != 0 ? new Vector3(point.X * cos - point.Y * sin, point.X * sin + point.Y * cos, point.Z) : point;

        return rotated + pivot;
    }

    // printers
    public static string print<T>(this T[] arr) {
        return "[" + string.Join(", ", arr) + "]";
    }

    public static string print(this Matrix4x4 mat) {
        return $"Matrix: [{mat.M11}, {mat.M12}, {mat.M13}, {mat.M14}]\n" +
               $"\t[{mat.M21}, {mat.M22}, {mat.M23}, {mat.M24}]\n" +
               $"\t[{mat.M31}, {mat.M32}, {mat.M33}, {mat.M34}]\n" +
               $"\t[{mat.M41}, {mat.M42}, {mat.M43}, {mat.M44}]";
    }

    public static string print(this MatrixStack mat) {
        return mat.top.print();
    }

    public static float clamp(float x, float min, float max) {
        if (x < min) {
            return min;
        }

        if (x > max) {
            return max;
        }

        return x;
    }

    public static int clamp(int x, int min, int max) {
        if (x < min) {
            return min;
        }

        if (x > max) {
            return max;
        }

        return x;
    }

    public static double clamp(double x, double min, double max) {
        if (x < min) {
            return min;
        }

        if (x > max) {
            return max;
        }

        return x;
    }

    public static meth.BoundingBox toBB(this AABB aabb) {
        return new meth.BoundingBox(aabb.min.toVec3(), aabb.max.toVec3());
    }

    public static AABB toAABB(this meth.BoundingBox bb) {
        return new AABB(bb.Min.toVec3D(), bb.Max.toVec3D());
    }

    extension(byte b) {
        public byte skylight() {
            return (byte)(b & 0xF);
        }

        public byte blocklight() {
            return (byte)((b >> 4) & 0xF);
        }
    }

    extension(Vector3D v) {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3D norm() {

            double lengthSq = double.MultiplyAddEstimate(v.X, v.X, double.MultiplyAddEstimate(v.Y, v.Y, v.Z * v.Z));
            double invLength = double.ReciprocalSqrtEstimate(lengthSq);
            return new Vector3D(v.X * invLength, v.Y * invLength, v.Z * invLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double dot(Vector3D b) {
            return double.MultiplyAddEstimate(v.X, b.X, double.MultiplyAddEstimate(v.Y, b.Y, v.Z * b.Z));
        }

        public Vector3 toVec3() {
            return new Vector3((float)v.X, (float)v.Y, (float)v.Z);
        }

        public Vector3F toVec3F() {
            return new Vector3F((float)v.X, (float)v.Y, (float)v.Z);
        }

        public Vector3I toBlockPos() {
            return new Vector3I((int)Math.Floor(v.X), (int)Math.Floor(v.Y),
                (int)Math.Floor(v.Z));
        }

        public Vector3D withoutY() {
            return new Vector3D(v.X, 0, v.Z);
        }
    }

    extension(Vector3D<float> vec) {
        public Vector3 toVec3() {
            return new Vector3(vec.X, vec.Y, vec.Z);
        }

        public Vector3F toVec3F() {
            return new Vector3F(vec.X, vec.Y, vec.Z);
        }

        public Vector3I toBlockPos() {
            return new Vector3I((int)Math.Floor(vec.X), (int)Math.Floor(vec.Y),
                (int)Math.Floor(vec.Z));
        }
    }

    extension(Vector3F vec) {
        public Vector3 toVec3() {
            return new Vector3(vec.X, vec.Y, vec.Z);
        }

        public Vector3D toVec3D() {
            return new Vector3D(vec.X, vec.Y, vec.Z);
        }

        public Vector3F withoutY() {
            return new Vector3F(vec.X, 0, vec.Z);
        }
    }

    extension(Vector3I vec) {
        public Vector3 toVec3() {
            return new Vector3(vec.X, vec.Y, vec.Z);
        }

        public Vector3I withoutY() {
            return new Vector3I(vec.X, 0, vec.Z);
        }
    }

    extension(Vector3 a) {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float dot(Vector3 b) {
            return float.MultiplyAddEstimate(a.X, b.X, float.MultiplyAddEstimate(a.Y, b.Y, a.Z * b.Z));
        }

        public Vector3D<float> toVec3F() {
            return new Vector3D<float>(a.X, a.Y, a.Z);
        }

        public Vector3D toVec3D() {
            return new Vector3D(a.X, a.Y, a.Z);
        }

        public Vector3F toVec3FM() {
            return new Vector3F(a.X, a.Y, a.Z);
        }
    }

    extension(Color color) {
        public Vector4 toVec4() {
            return new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        }

        public Color4 toColor4() {
            return new Color4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        }
    }

    extension(Color4 color) {
        public Vector4 toVec4() {
            return new Vector4(color.R, color.G, color.B, color.A);
        }

        public Color toC() {
            return new Color((byte)(color.R * 255), (byte)(color.G * 255), (byte)(color.B * 255), (byte)(color.A * 255));
        }
    }

    extension(bool value) {
        public byte toByte() {
            return Unsafe.BitCast<bool, byte>(value);
        }

        public string yes() {
            return value ? "y" : "n";
        }
    }
}

/** yesn't */
public enum TriState : byte {
    FALSE = 0,
    TRUE = 1,
    FILE_NOT_FOUND = 2
}

/// <summary>
/// North = +Z
/// South = -Z
/// West = -X
/// East = +X
/// Doubles as a normal too
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct Direction(int x, int y, int z) {
    public readonly int x = x;
    public readonly int y = y;
    public readonly int z = z;

    public static int min = 0;
    public static int max = 6;

    public static readonly Vector3I WEST = new(-1, 0, 0);
    public static readonly Vector3I EAST = new(1, 0, 0);
    public static readonly Vector3I SOUTH = new(0, 0, -1);
    public static readonly Vector3I NORTH = new(0, 0, 1);
    public static readonly Vector3I DOWN = new(0, -1, 0);
    public static readonly Vector3I UP = new(0, 1, 0);
    public static readonly Vector3I SELF = new(0, 0, 0);

    public static readonly Vector3I[] directions = [WEST, EAST, SOUTH, NORTH, DOWN, UP];
    public static readonly Vector3I[] directionsLight = [DOWN, UP, WEST, EAST, SOUTH, NORTH];
    public static readonly Vector3I[] directionsNoDown = [DOWN, UP, WEST, EAST, SOUTH, NORTH];
    public static readonly Vector3I[] directionsWaterSpread = [DOWN, WEST, EAST, SOUTH, NORTH];
    public static readonly Vector3I[] directionsHorizontal = [WEST, EAST, SOUTH, NORTH];
    public static readonly Vector3I[] directionsDiag = [WEST, EAST, SOUTH, NORTH, DOWN, UP, WEST + SOUTH, WEST + NORTH, EAST + SOUTH, EAST + NORTH];
    public static readonly Vector3I[] directionsAll = new Vector3I[27];
    public static readonly Vector3I[] directionsSelf = [WEST, EAST, SOUTH, NORTH, DOWN, UP, SELF];

    static Direction() {
        // construct 27-box of all directions
        int i = 0;
        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                for (int z = -1; z <= 1; z++) {
                    directionsAll[i] = new Vector3I(x, y, z);
                    // don't forget to increment, you silly you!:P
                    i++;
                }
            }
        }
    }
    public static Vector3I getDirection(RawDirection dir) {
        return dir switch {
            RawDirection.WEST => WEST,
            RawDirection.EAST => EAST,
            RawDirection.SOUTH => SOUTH,
            RawDirection.NORTH => NORTH,
            RawDirection.DOWN => DOWN,
            RawDirection.UP => UP,
            _ => SELF
        };
    }
    
    public static RawDirection getOpposite(RawDirection dir) {
        return dir switch {
            RawDirection.WEST => RawDirection.EAST,
            RawDirection.EAST => RawDirection.WEST,
            RawDirection.SOUTH => RawDirection.NORTH,
            RawDirection.NORTH => RawDirection.SOUTH,
            RawDirection.DOWN => RawDirection.UP,
            RawDirection.UP => RawDirection.DOWN,
            _ => RawDirection.NONE
        };
    }

    public static RawDirection getRawDirection(Vector3I dir) {
        if (dir == WEST) {
            return RawDirection.WEST;
        }
        if (dir == EAST) {
            return RawDirection.EAST;
        }
        if (dir == SOUTH) {
            return RawDirection.SOUTH;
        }
        if (dir == NORTH) {
            return RawDirection.NORTH;
        }
        if (dir == DOWN) {
            return RawDirection.DOWN;
        }
        if (dir == UP) {
            return RawDirection.UP;
        }

        return RawDirection.NONE;
    }
    
    public Vector3I toVec() {
        return Unsafe.BitCast<Direction, Vector3I>(this);
    }
}

public static class DirectionExtensions {

    public static Direction toDir(this Vector3I dir) {
        return Unsafe.BitCast<Vector3I, Direction>(dir);
    }
    
    public static RawDirection opposite(this RawDirection dir) {
        return Direction.getOpposite(dir);
    }
}

public static class PlaneExtensions {
    /// <summary>
    /// -1 if on the back side, 0 if on the plane, 1 if on the front side
    /// </summary>
    /// <param name="plane"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    public static int planeSide(this Plane plane, Vector3 point) {
        float dist = Vector3.Dot(plane.Normal, point) + plane.D;
        if (dist == 0.0F) {
            return 0;
        }
        return dist < 0.0F ? -1 : 1;
    }
}

public static class ArrayExtensions {
    public static T[] fill<T>(this T[] arr, T val) {
        new Span<T>(arr).Fill(val);
        return arr;
    }
}

public static class UnsafeListAccessor<T> {
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_items")]
    public static extern ref T[] getItems(List<T> list);

    public static Span<T> AsUnsafeSpanOnBackingArray(List<T> list) {
        return new Span<T>(getItems(list));
    }
}

public enum RawDirection : byte {
    WEST = 0,
    EAST = 1,
    SOUTH = 2,
    NORTH = 3,
    DOWN = 4,
    UP = 5,
    /** NOT A REAL DIRECTION, just a loop terminator */
    MAX = 6,
    NONE = 13 // 13 is 5 with the 4th bit set to 1
}

/**
 * Horizontal raw direction
 */
public enum RawDirectionExt : byte {
    WEST = 0,
    EAST = 1,
    SOUTH = 2,
    NORTH = 3,
    SW = 4,
    SE = 5,
    NW = 6,
    NE = 7,
    /** NOT A REAL DIRECTION, just a loop terminator */
    MAX = 9,
}
