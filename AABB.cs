using Silk.NET.Maths;
using Plane = System.Numerics.Plane;

namespace BlockGame;

public class AABB {
    public double minX => min.X;
    public double minY => min.Y;
    public double minZ => min.Z;
    public double maxX => max.X;
    public double maxY => max.Y;
    public double maxZ => max.Z;

    public Vector3D<double> min;
    public Vector3D<double> max;

    public Vector3D<double> centre => (max + min) * 0.5f; // Compute AABB center
    public Vector3D<double> extents => max - centre; // Compute positive extents

    public AABB(Vector3D<double> min, Vector3D<double> max) {
        this.min = min;
        this.max = max;
    }

    public static AABB fromSize(Vector3D<double> min, Vector3D<double> size) {
        return new AABB(min, min + size);
    }


    public void move(int x, int y, int z) {
    }

    public static bool isCollision(AABB box1, AABB box2) {
        return box1.maxX > box2.minX &&
               box1.minX < box2.maxX &&
               box1.maxY > box2.minY &&
               box1.minY < box2.maxY &&
               box1.maxZ > box2.minZ &&
               box1.minZ < box2.maxZ;
    }

    public static bool isCollision(AABB box, Vector3D<double> point) {
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

    public bool intersects(Plane<double> plane) {

        // Compute the projection interval radius of b onto L(t) = b.c + t * p.n
        double r = extents.X * Math.Abs(plane.Normal.X) + extents.Y * Math.Abs(plane.Normal.Y) + extents.Z * Math.Abs(plane.Normal.Z);

        // Compute distance of box center from plane
        double s = Vector3D.Dot(plane.Normal, centre) - plane.Distance;

        // Intersection occurs when distance s falls within [-r,+r] interval
        return Math.Abs(s) <= r;
    }
}