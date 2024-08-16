using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BlockGame.util;
using Molten;
using Molten.DoublePrecision;
using Plane = System.Numerics.Plane;

namespace BlockGame;

// mutable
[StructLayout(LayoutKind.Auto)]
public struct AABB {
    public double minX => min.X;
    public double minY => min.Y;
    public double minZ => min.Z;
    public double maxX => max.X;
    public double maxY => max.Y;
    public double maxZ => max.Z;

    public Vector3D min;
    public Vector3D max;
    public static readonly AABB empty = new AABB(new Vector3D(0, 0, 0), new Vector3D(0, 0, 0));

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
        double r = extents.X * Math.Abs(plane.Normal.X) + extents.Y * Math.Abs(plane.Normal.Y) + extents.Z * Math.Abs(plane.Normal.Z);

        // Compute distance of box center from plane
        double s = Vector3F.Dot(plane.Normal.toVec3FM(), (Vector3F)centre) - plane.D;

        // Intersection occurs when distance s falls within [-r,+r] interval
        return Math.Abs(s) <= r;
    }
}