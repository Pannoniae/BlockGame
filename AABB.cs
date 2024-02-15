using Silk.NET.Maths;

namespace BlockGame;

public class AABB {
    public double minX, minY, minZ;
    public double maxX, maxY, maxZ;

    public AABB(Vector3D<double> min, Vector3D<double> max) {
        minX = min.X;
        minY = min.Y;
        minZ = min.Z;

        maxX = max.X;
        maxY = max.Y;
        maxZ = max.Z;
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
}