using System.Numerics;
using System.Runtime.CompilerServices;
using Molten;
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
    public static Vector3 toVec3(this Vector3F vec) {
        return new Vector3(vec.X, vec.Y, vec.Z);
    }

    public static Vector3F toVec3F(this Vector3D<double> vec) {
        return new Vector3F((float)vec.X, (float)vec.Y, (float)vec.Z);
    }
    public static Vector3F toVec3F(this Vector3D<float> vec) {
        return new Vector3F(vec.X, vec.Y, vec.Z);
    }
    public static Vector3D<float> toVec3F(this Vector3 vec) {
        return new Vector3D<float>(vec.X, vec.Y, vec.Z);
    }
    public static Vector3D<double> toVec3D(this Vector3 vec) {
        return new Vector3D<double>(vec.X, vec.Y, vec.Z);
    }
    public static Vector3F toVec3FM(this Vector3 vec) {
        return new Vector3F(vec.X, vec.Y, vec.Z);
    }

    public static Matrix4x4 to4x4(this Matrix4F mat) {
        return Unsafe.BitCast<Matrix4F, Matrix4x4>(mat);
    }

    public static Matrix4F to4F(Matrix4x4 mat) {
        return Unsafe.BitCast<Matrix4x4, Matrix4F>(mat);
    }


    public static Vector3D<int> toBlockPos(this Vector3D<double> currentPos) {
        return new Vector3D<int>((int)Math.Floor(currentPos.X), (int)Math.Floor(currentPos.Y),
            (int)Math.Floor(currentPos.Z));
    }

    public static Vector3D<int> toBlockPos(this Vector3D<float> currentPos) {
        return new Vector3D<int>((int)Math.Floor(currentPos.X), (int)Math.Floor(currentPos.Y),
            (int)Math.Floor(currentPos.Z));
    }

    public static Vector3D<T> withoutY<T>(this Vector3D<T> vec) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T> {
        return new Vector3D<T>(vec.X, default, vec.Z);
    }
}