using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using BlockGame.util;
using Molten;
using Molten.DoublePrecision;
using Plane = System.Numerics.Plane;

namespace BlockGame;

// mutable
[StructLayout(LayoutKind.Auto)]
public struct AABB {
    public static readonly AABB empty = new AABB(new Vector3D(0, 0, 0), new Vector3D(0, 0, 0));

    public Vector3D min;
    public Vector3D max;

    public double minX => min.X;
    public double minY => min.Y;
    public double minZ => min.Z;
    public double maxX => max.X;
    public double maxY => max.Y;
    public double maxZ => max.Z;

    public Vector3D centre => (max + min) * 0.5f; // Compute AABB center
    public Vector3D extents => max - centre; // Compute positive extents

    public AABB(Vector3D min, Vector3D max) {
        this.min = min;
        this.max = max;
    }

    public static AABB fromSize(Vector3D min, Vector3D size) {
        return new AABB(min, min + size);
    }

    public static void update(ref AABB aabb, Vector3D min, Vector3D size) {
        aabb.min = min;
        aabb.max = min + size;
    }

    public bool isFront(Plane plane) {
        // See http://zach.in.tu-clausthal.de/teaching/cg_literatur/lighthouse3d_view_frustum_culling/index.html

        // Inline Vector3.Dot(plane.Normal, negativeVertex) + plane.D;
        var x = plane.Normal.X >= 0 ? min.X : max.X;
        var y = plane.Normal.Y >= 0 ? min.Y : max.Y;
        var z = plane.Normal.Z >= 0 ? min.Z : max.Z;
        return Plane.DotCoordinate(plane, new Vector3((float)x, (float)y, (float)z)) > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool isFrontBottom(Plane plane) {
        // See http://zach.in.tu-clausthal.de/teaching/cg_literatur/lighthouse3d_view_frustum_culling/index.html

        // Inline Vector3.Dot(plane.Normal, negativeVertex) + plane.D;
        return plane.Normal.X * min.X + plane.Normal.Y * min.Y + plane.Normal.Z * min.Z + plane.D > 0 &&
               plane.Normal.X * max.X + plane.Normal.Y * min.Y + plane.Normal.Z * max.Z + plane.D > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool isFrontTop(Plane plane) {
        // See http://zach.in.tu-clausthal.de/teaching/cg_literatur/lighthouse3d_view_frustum_culling/index.html

        // Inline Vector3.Dot(plane.Normal, negativeVertex) + plane.D;
        return plane.Normal.X * min.X + plane.Normal.Y * max.Y + plane.Normal.Z * min.Z + plane.D > 0 &&
               plane.Normal.X * max.X + plane.Normal.Y * max.Y + plane.Normal.Z * max.Z + plane.D > 0;
    }

    /// <summary>
    /// Vectorised isFront check for two planes. Checks if p1 || p2 are in front of the AABB.
    /// </summary>
    public bool isFrontTwo(Plane p1, Plane p2) {
        // A plane fits on a 128bit SIMD register
        var vP1 = Vector128.LoadUnsafe(ref Unsafe.As<Plane, float>(ref p1));
        var vP2 = Vector128.LoadUnsafe(ref Unsafe.As<Plane, float>(ref p2));

        // Load the AABB min and max into SIMD registers
        // convert the min and max vectors to floats
        var mi = Vector256.LoadUnsafe(ref Unsafe.As<Vector3D, double>(ref min));
        var ma = Vector256.LoadUnsafe(ref Unsafe.As<Vector3D, double>(ref max));


        Vector128<float> vMin;
        Vector128<float> vMax;
        if (Avx2.IsSupported) {
            vMin = Avx.ConvertToVector128Single(mi);
            vMax = Avx.ConvertToVector128Single(ma);
        }
        else {
            vMin = Vector128.Create((float)mi[0], (float)mi[1], (float)mi[2], 0);
            vMax = Vector128.Create((float)ma[0], (float)ma[1], (float)ma[2], 0);
        }

        // Select min or max based on the plane normal
        var vPNormal1 = Vector128.GreaterThanOrEqual(vP1, Vector128<float>.Zero);
        var vPNormal2 = Vector128.GreaterThanOrEqual(vP2, Vector128<float>.Zero);

        Vector128<float> vCoord1;
        Vector128<float> vCoord2;
        if (Avx2.IsSupported) {
            // yes the blend is inverted! the CondtionalSelect is the same as a blend with the first two parameters swapped
            // the ConditionalSelect API is stupid af
            vCoord1 = Sse41.BlendVariable(vMax, vMin, vPNormal1);
            vCoord2 = Sse41.BlendVariable(vMax, vMin, vPNormal2);
        }
        else {
            vCoord1 = Vector128.ConditionalSelect(vPNormal1, vMin, vMax);
            vCoord2 = Vector128.ConditionalSelect(vPNormal2, vMin, vMax);
        }

        // Set the last element to one because the dot product requires it (otherwise we get D in its place which is obviously bullshit)
        vCoord1 = vCoord1.WithElement(3, 1);
        vCoord2 = vCoord2.WithElement(3, 1);

        // Compute the dot products
        var vDot = Vector128.Dot(vP1, vCoord1);
        var vDot2 = Vector128.Dot(vP2, vCoord2);

        // Return true if either dot product is greater than the plane distance
        return vDot > 0 | vDot2 > 0;
    }

    /// <summary>
    /// Vectorised isFrontTwo check for 8 AABBs at once. Returns a byte mask with each bit representing if the corresponding AABB is in front of either plane.
    /// Optimized to process multiple AABBs simultaneously using SIMD.
    /// </summary>
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe byte isFrontTwoEight(Span<AABB> aabbs, Plane p1, Plane p2) {
        // todo actually make this shit different
        // rn its just a copy of the other method
        //if (false && Avx512F.IsSupported) {
        //    return isFrontTwoEightAvx512(aabbs, p1, p2);
        //}
        //if (Avx2.IsSupported) {
            return isFrontTwoEightAvx2(aabbs, p1, p2);
        //}
        //return isFrontTwoEightFallback(aabbs, p1, p2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe byte isFrontTwoEightAvx512(Span<AABB> aabbs, Plane p1, Plane p2) {
        // Load planes once
        var vP1_128 = Vector128.LoadUnsafe(ref Unsafe.As<Plane, float>(ref p1));
        var vP2_128 = Vector128.LoadUnsafe(ref Unsafe.As<Plane, float>(ref p2));

        // [minX0-7, minY0-7, minZ0-7, maxX0-7, maxY0-7, maxZ0-7]
        Span<float> coords = stackalloc float[48]; // 8 AABBs * 6 components

        // Extract coordinates from all 8 AABBs into grouped format
        for (int i = 0; i < 8; i++) {
            var aabb = aabbs[i];
            coords[i] = (float)aabb.min.X;
            coords[i + 8] = (float)aabb.min.Y;
            coords[i + 16] = (float)aabb.min.Z;
            coords[i + 24] = (float)aabb.max.X;
            coords[i + 32] = (float)aabb.max.Y;
            coords[i + 40] = (float)aabb.max.Z;
        }
        
        
        var vMinX = Vector256.LoadUnsafe(ref coords[0]);
        var vMinY = Vector256.LoadUnsafe(ref coords[8]);
        var vMinZ = Vector256.LoadUnsafe(ref coords[16]);
        var vMaxX = Vector256.LoadUnsafe(ref coords[24]);
        var vMaxY = Vector256.LoadUnsafe(ref coords[32]);
        var vMaxZ = Vector256.LoadUnsafe(ref coords[40]);

        // Broadcast plane components for parallel comparison with all 8 AABBs
        var vPlaneX1 = Vector256.Create(vP1_128[0]);
        var vPlaneY1 = Vector256.Create(vP1_128[1]);
        var vPlaneZ1 = Vector256.Create(vP1_128[2]);
        var vPlaneD1 = Vector256.Create(vP1_128[3]);

        var vPlaneX2 = Vector256.Create(vP2_128[0]);
        var vPlaneY2 = Vector256.Create(vP2_128[1]);
        var vPlaneZ2 = Vector256.Create(vP2_128[2]);
        var vPlaneD2 = Vector256.Create(vP2_128[3]);

        // Select min or max coordinates based on plane normal signs - parallel for all 8 AABBs
        var vCoordX1 = Avx.BlendVariable(vMaxX, vMinX,
            Avx.Compare(vPlaneX1, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanOrEqualNonSignaling));
        var vCoordY1 = Avx.BlendVariable(vMaxY, vMinY,
            Avx.Compare(vPlaneY1, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanOrEqualNonSignaling));
        var vCoordZ1 = Avx.BlendVariable(vMaxZ, vMinZ,
            Avx.Compare(vPlaneZ1, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanOrEqualNonSignaling));

        var vCoordX2 = Avx.BlendVariable(vMaxX, vMinX,
            Avx.Compare(vPlaneX2, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanOrEqualNonSignaling));
        var vCoordY2 = Avx.BlendVariable(vMaxY, vMinY,
            Avx.Compare(vPlaneY2, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanOrEqualNonSignaling));
        var vCoordZ2 = Avx.BlendVariable(vMaxZ, vMinZ,
            Avx.Compare(vPlaneZ2, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanOrEqualNonSignaling));

        // Compute dot products for all 8 AABBs in parallel: dot = normal.x*coord.x + normal.y*coord.y + normal.z*coord.z + D
        var vDots1 = Avx.Add(
            Avx.Add(
                Avx.Add(
                    Avx.Multiply(vPlaneX1, vCoordX1),
                    Avx.Multiply(vPlaneY1, vCoordY1)),
                Avx.Multiply(vPlaneZ1, vCoordZ1)),
            vPlaneD1);

        var vDots2 = Avx.Add(
            Avx.Add(
                Avx.Add(
                    Avx.Multiply(vPlaneX2, vCoordX2),
                    Avx.Multiply(vPlaneY2, vCoordY2)),
                Avx.Multiply(vPlaneZ2, vCoordZ2)),
            vPlaneD2);

        // Check which AABBs are in front of either plane (dot product > 0)
        var positive1 = Avx.Compare(vDots1, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanNonSignaling);
        var positive2 = Avx.Compare(vDots2, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanNonSignaling);
        var anyPositive = Avx.Or(positive1, positive2);

        // Convert to byte mask - each bit represents one AABB
        return (byte)Avx.MoveMask(anyPositive);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe byte isFrontTwoEightAvx2(Span<AABB> aabbs, Plane p1, Plane p2) {
        // Load planes once
        var vP1_128 = Vector128.LoadUnsafe(ref Unsafe.As<Plane, float>(ref p1));
        var vP2_128 = Vector128.LoadUnsafe(ref Unsafe.As<Plane, float>(ref p2));

        // [minX0-7, minY0-7, minZ0-7, maxX0-7, maxY0-7, maxZ0-7]
        Span<float> coords = stackalloc float[48]; // 8 AABBs * 6 components

        // Extract coordinates from all 8 AABBs into grouped format
        for (int i = 0; i < 8; i++) {
            var aabb = aabbs[i];
            coords[i] = (float)aabb.min.X;
            coords[i + 8] = (float)aabb.min.Y;
            coords[i + 16] = (float)aabb.min.Z;
            coords[i + 24] = (float)aabb.max.X;
            coords[i + 32] = (float)aabb.max.Y;
            coords[i + 40] = (float)aabb.max.Z;
        }

        // Load coordinate vectors directly from sections
        var vMinX = Vector256.LoadUnsafe(ref coords[0]);
        var vMinY = Vector256.LoadUnsafe(ref coords[8]);
        var vMinZ = Vector256.LoadUnsafe(ref coords[16]);
        var vMaxX = Vector256.LoadUnsafe(ref coords[24]);
        var vMaxY = Vector256.LoadUnsafe(ref coords[32]);
        var vMaxZ = Vector256.LoadUnsafe(ref coords[40]);

        // Broadcast plane components for parallel comparison with all 8 AABBs
        var vPlaneX1 = Vector256.Create(vP1_128[0]);
        var vPlaneY1 = Vector256.Create(vP1_128[1]);
        var vPlaneZ1 = Vector256.Create(vP1_128[2]);
        var vPlaneD1 = Vector256.Create(vP1_128[3]);

        var vPlaneX2 = Vector256.Create(vP2_128[0]);
        var vPlaneY2 = Vector256.Create(vP2_128[1]);
        var vPlaneZ2 = Vector256.Create(vP2_128[2]);
        var vPlaneD2 = Vector256.Create(vP2_128[3]);

        // Select min or max coordinates based on plane normal signs - parallel for all 8 AABBs
        var vCoordX1 = Avx.BlendVariable(vMaxX, vMinX,
            Avx.Compare(vPlaneX1, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanOrEqualNonSignaling));
        var vCoordY1 = Avx.BlendVariable(vMaxY, vMinY,
            Avx.Compare(vPlaneY1, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanOrEqualNonSignaling));
        var vCoordZ1 = Avx.BlendVariable(vMaxZ, vMinZ,
            Avx.Compare(vPlaneZ1, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanOrEqualNonSignaling));

        var vCoordX2 = Avx.BlendVariable(vMaxX, vMinX,
            Avx.Compare(vPlaneX2, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanOrEqualNonSignaling));
        var vCoordY2 = Avx.BlendVariable(vMaxY, vMinY,
            Avx.Compare(vPlaneY2, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanOrEqualNonSignaling));
        var vCoordZ2 = Avx.BlendVariable(vMaxZ, vMinZ,
            Avx.Compare(vPlaneZ2, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanOrEqualNonSignaling));

        // Compute dot products for all 8 AABBs in parallel: dot = normal.x*coord.x + normal.y*coord.y + normal.z*coord.z + D
        var vDots1 = Avx.Add(
            Avx.Add(
                Avx.Add(
                    Avx.Multiply(vPlaneX1, vCoordX1),
                    Avx.Multiply(vPlaneY1, vCoordY1)),
                Avx.Multiply(vPlaneZ1, vCoordZ1)),
            vPlaneD1);

        var vDots2 = Avx.Add(
            Avx.Add(
                Avx.Add(
                    Avx.Multiply(vPlaneX2, vCoordX2),
                    Avx.Multiply(vPlaneY2, vCoordY2)),
                Avx.Multiply(vPlaneZ2, vCoordZ2)),
            vPlaneD2);

        // Check which AABBs are in front of either plane (dot product > 0)
        var positive1 = Avx.Compare(vDots1, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanNonSignaling);
        var positive2 = Avx.Compare(vDots2, Vector256<float>.Zero, FloatComparisonMode.OrderedGreaterThanNonSignaling);
        var anyPositive = Avx.Or(positive1, positive2);

        // Convert to byte mask - each bit represents one AABB
        return (byte)Avx.MoveMask(anyPositive);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe byte isFrontTwoEightFallback(Span<AABB> aabbs, Plane p1, Plane p2) {
        // Fallback implementation for non-AVX2 systems - still better than the original due to reduced overhead
        var vP1 = Vector128.LoadUnsafe(ref Unsafe.As<Plane, float>(ref p1));
        var vP2 = Vector128.LoadUnsafe(ref Unsafe.As<Plane, float>(ref p2));

        // Pre-compute plane normal comparisons once
        var vPNormal1 = Vector128.GreaterThanOrEqual(vP1, Vector128<float>.Zero);
        var vPNormal2 = Vector128.GreaterThanOrEqual(vP2, Vector128<float>.Zero);

        byte result = 0;

        // Unrolled loop for better performance
        for (int i = 0; i < 8; i++) {
            var aabb = aabbs[i];

            // Convert to float more efficiently
            var vMin = Vector128.Create((float)aabb.min.X, (float)aabb.min.Y, (float)aabb.min.Z, 1.0f);
            var vMax = Vector128.Create((float)aabb.max.X, (float)aabb.max.Y, (float)aabb.max.Z, 1.0f);

            // Select coordinates and compute dot products
            var vCoord1 = Vector128.ConditionalSelect(vPNormal1, vMin, vMax);
            var vCoord2 = Vector128.ConditionalSelect(vPNormal2, vMin, vMax);

            var vDot1 = Vector128.Dot(vP1, vCoord1);
            var vDot2 = Vector128.Dot(vP2, vCoord2);
            
            result |= (byte)((vDot1 > 0 | vDot2 > 0).toByte() << i);
        }

        return result;
    }

    public static bool isCollision(AABB box1, AABB box2) {
        return box1.maxX > box2.minX &&
               box1.minX < box2.maxX &&
               box1.maxY > box2.minY &&
               box1.minY < box2.maxY &&
               box1.maxZ > box2.minZ &&
               box1.minZ < box2.maxZ;
    }

    public static bool isCollision(AABB box, Vector3D point) {
        return point.X > box.minX &&
               point.X < box.maxX &&
               point.Y > box.minY &&
               point.Y < box.maxY &&
               point.Z > box.minZ &&
               point.Z < box.maxZ;
    }

    public static bool isCollisionX(AABB box1, AABB box2) {
        return box1.maxX > box2.minX &&
               box1.minX < box2.maxX;
    }

    public static bool isCollisionY(AABB box1, AABB box2) {
        return box1.maxY > box2.minY &&
               box1.minY < box2.maxY;
    }

    public static bool isCollisionZ(AABB box1, AABB box2) {
        return box1.maxZ > box2.minZ &&
               box1.minZ < box2.maxZ;
    }

    public override string ToString() {
        return $"{minX}, {minY}, {minZ}, {maxX}, {maxY}, {maxZ}";
    }

    public bool intersects(Plane plane) {
        // Compute the projection interval radius of b onto L(t) = b.c + t * p.n
        double r = extents.X * Math.Abs(plane.Normal.X) + extents.Y * Math.Abs(plane.Normal.Y) +
                   extents.Z * Math.Abs(plane.Normal.Z);

        // Compute distance of box center from plane
        double s = Vector3F.Dot(plane.Normal.toVec3FM(), (Vector3F)centre) - plane.D;

        // Intersection occurs when distance s falls within [-r,+r] interval
        return Math.Abs(s) <= r;
    }
}