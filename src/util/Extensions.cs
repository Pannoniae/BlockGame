using System.Numerics;
using Silk.NET.Maths;
using Plane = System.Numerics.Plane;

namespace BlockGame.util;

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


// convert vectors between different libraries
public static class VectorExtensions {

    public static Vector3 toVec3(this Vector3D<double> vec) {
        return new Vector3((float)vec.X, (float)vec.Y, (float)vec.Z);
    }
    public static Vector3 toVec3(this Vector3D<float> vec) {
        return new Vector3(vec.X, vec.Y, vec.Z);
    }
    public static Vector3D<float> toVec3F(this Vector3 vec) {
        return new Vector3D<float>(vec.X, vec.Y, vec.Z);
    }

    public static Vector3D<T> withoutY<T>(this Vector3D<T> vec) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T> {
        return new Vector3D<T>(vec.X, default, vec.Z);
    }
}