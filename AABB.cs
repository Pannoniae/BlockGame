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

    public AABB(Vector3D<int> min, Vector3D<double> size) {
        minX = min.X;
        minY = min.Y;
        minZ = min.Z;

        maxX = min.X + size.X;
        maxY = min.Y + size.Y;
        maxZ = min.Z + size.Z;
    }
}