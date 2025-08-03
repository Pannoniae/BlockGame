using System.Numerics;
using System.Runtime.CompilerServices;
using BlockGame.GL.vertexformats;
using Molten;
using Silk.NET.Maths;
using Plane = System.Numerics.Plane;
using Vector3D = Molten.DoublePrecision.Vector3D;

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
public static partial class Meth {

    public static Vector3 toVec3(this Vector3D vec) {
        return new Vector3((float)vec.X, (float)vec.Y, (float)vec.Z);
    }
    public static Vector3 toVec3(this Vector3D<float> vec) {
        return new Vector3(vec.X, vec.Y, vec.Z);
    }
    public static Vector3 toVec3(this Vector3I vec) {
        return new Vector3(vec.X, vec.Y, vec.Z);
    }
    public static Vector3 toVec3(this Vector3F vec) {
        return new Vector3(vec.X, vec.Y, vec.Z);
    }
    
    public static Vector4 toVec4(this Color4 color) {
        return new Vector4(color.R, color.G, color.B, color.A);
    }
    
    public static Vector4 toVec4(this Color color) {
        return new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
    }
    
    public static Vector4 toVec4(this Color4b color) {
        return new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
    }

    public static Color4b to4b(this Color color) {
        return new Color4b(color.R, color.G, color.B, color.A);
    }
    
    public static Color4b to4b(this Color4 color) {
        return new Color4b((byte)(color.R * 255), (byte)(color.G * 255), (byte)(color.B * 255), (byte)(color.A * 255));
    }
    
    public static Color toColor(this Color4b color) {
        return new Color(color.R, color.G, color.B, color.A);
    }
    
    public static Color4 toColor4(this Color4b color) {
        return new Color4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
    }
    
    public static Color4 toColor4(this Color color) {
        return new Color4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
    }

    public static Vector3F toVec3F(this Vector3D vec) {
        return new Vector3F((float)vec.X, (float)vec.Y, (float)vec.Z);
    }
    public static Vector3F toVec3F(this Vector3D<float> vec) {
        return new Vector3F(vec.X, vec.Y, vec.Z);
    }
    public static Vector3D<float> toVec3F(this Vector3 vec) {
        return new Vector3D<float>(vec.X, vec.Y, vec.Z);
    }
    public static Vector3D toVec3D(this Vector3 vec) {
        return new Vector3D(vec.X, vec.Y, vec.Z);
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

    public static Vector3I toBlockPos(this Vector3D currentPos) {
        return new Vector3I((int)Math.Floor(currentPos.X), (int)Math.Floor(currentPos.Y),
            (int)Math.Floor(currentPos.Z));
    }

    public static Vector3I toBlockPos(this Vector3D<float> currentPos) {
        return new Vector3I((int)Math.Floor(currentPos.X), (int)Math.Floor(currentPos.Y),
            (int)Math.Floor(currentPos.Z));
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

    public static Vector3F withoutY(this Vector3F vec) {
        return new Vector3F(vec.X, 0, vec.Z);
    }

    public static Vector3D withoutY(this Vector3D vec) {
        return new Vector3D(vec.X, 0, vec.Z);
    }

    public static Vector3I withoutY(this Vector3I vec) {
        return new Vector3I(vec.X, 0, vec.Z);
    }
}