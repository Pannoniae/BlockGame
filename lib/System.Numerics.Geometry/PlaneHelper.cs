using System;
using System.Diagnostics;


namespace System.Numerics
{
    public static class PlaneHelper
    {
        /// <summary>
        /// Returns a value indicating what side (positive/negative) of a plane a point is
        /// </summary>
        /// <param name="point">The point to check with</param>
        /// <param name="plane">The plane to check against</param>
        /// <returns>Greater than zero if on the positive side, less than zero if on the negative size, 0 otherwise</returns>
        public static float ClassifyPoint(ref Vector3 point, ref Plane plane)
        {
            return point.X * plane.Normal.X + point.Y * plane.Normal.Y + point.Z * plane.Normal.Z + plane.D;
        }

        /// <summary>
        /// Returns the perpendicular distance from a point to a plane
        /// </summary>
        /// <param name="point">The point to check</param>
        /// <param name="plane">The place to check</param>
        /// <returns>The perpendicular distance from the point to the plane</returns>
        public static float PerpendicularDistance(ref Vector3 point, ref Plane plane)
        {
            // dist = (ax + by + cz + d) / sqrt(a*a + b*b + c*c)
            return Math.Abs((plane.Normal.X * point.X + plane.Normal.Y * point.Y + plane.Normal.Z * point.Z)
                                    / (float)Math.Sqrt(plane.Normal.X * plane.Normal.X + plane.Normal.Y * plane.Normal.Y + plane.Normal.Z * plane.Normal.Z));
        }


        public static float Dot(this Plane plane, Vector4 value)
        {
            return ((((plane.Normal.X * value.X) + (plane.Normal.Y * value.Y)) + (plane.Normal.Z * value.Z)) + (plane.D * value.W));
        }
        public static float Dot(ref Plane plane, ref Vector4 value)
        {
            return ((((plane.Normal.X * value.X) + (plane.Normal.Y * value.Y)) + (plane.Normal.Z * value.Z)) + (plane.D * value.W));
        }

        public static void Dot(ref Plane plane, ref Vector4 value, out float result)
        {
            result = (((plane.Normal.X * value.X) + (plane.Normal.Y * value.Y)) + (plane.Normal.Z * value.Z)) + (plane.D * value.W);
        }

        public static void Dot(this Plane plane, ref Vector4 value, out float result)
        {
            result = (((plane.Normal.X * value.X) + (plane.Normal.Y * value.Y)) + (plane.Normal.Z * value.Z)) + (plane.D * value.W);
        }

        public static float DotCoordinate(this Plane plane, Vector3 value)
        {
            return ((((plane.Normal.X * value.X) + (plane.Normal.Y * value.Y)) + (plane.Normal.Z * value.Z)) + plane.D);
        }

        public static void DotCoordinate(this Plane plane, ref Vector3 value, out float result)
        {
            result = (((plane.Normal.X * value.X) + (plane.Normal.Y * value.Y)) + (plane.Normal.Z * value.Z)) + plane.D;
        }

        public static float DotNormal(this Plane plane, Vector3 value)
        {
            return (((plane.Normal.X * value.X) + (plane.Normal.Y * value.Y)) + (plane.Normal.Z * value.Z));
        }

        public static void DotNormal(this Plane plane, ref Vector3 value, out float result)
        {
            result = ((plane.Normal.X * value.X) + (plane.Normal.Y * value.Y)) + (plane.Normal.Z * value.Z);
        }


        /// <summary>
        /// Transforms a normalized plane by a matrix.
        /// </summary>
        /// <param name="plane">The normalized plane to transform.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed plane.</returns>
        public static Plane Transform(this Plane plane, Matrix4x4 matrix)
        {
            Plane result;
            Transform(ref plane, ref matrix, out result);
            return result;
        }

        /// <summary>
        /// Transforms a normalized plane by a matrix.
        /// </summary>
        /// <param name="plane">The normalized plane to transform.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <param name="result">The transformed plane.</param>
        public static void Transform(ref Plane plane, ref Matrix4x4 matrix, out Plane result)
        {
            // See "Transforming Normals" in http://www.glprogramming.com/red/appendixf.html
            // for an explanation of how this works.

            Matrix4x4 transformedMatrix;
            Matrix4x4.Invert(matrix, out transformedMatrix);
            transformedMatrix = Matrix4x4.Transpose(transformedMatrix);

            var vector = new Vector4(plane.Normal, plane.D);

            Vector4 transformedVector = Vector4.Transform(vector, transformedMatrix);

            result = new Plane(transformedVector);
        }

        /// <summary>
        /// Transforms a normalized plane by a quaternion rotation.
        /// </summary>
        /// <param name="plane">The normalized plane to transform.</param>
        /// <param name="rotation">The quaternion rotation.</param>
        /// <returns>The transformed plane.</returns>
        public static Plane Transform(this Plane plane, Quaternion rotation)
        {
            Plane result;
            Transform(ref plane, ref rotation, out result);
            return result;
        }

        /// <summary>
        /// Transforms a normalized plane by a quaternion rotation.
        /// </summary>
        /// <param name="plane">The normalized plane to transform.</param>
        /// <param name="rotation">The quaternion rotation.</param>
        /// <param name="result">The transformed plane.</param>
        public static void Transform(ref Plane plane, ref Quaternion rotation, out Plane result)
        {
            result.Normal = Vector3.Transform(plane.Normal, rotation);
            result.D = plane.D;
        }

        //     public void Normalize()
        //     {
        //float factor;
        //Vector3 normal = Normal;
        //Normal = Vector3.Normalize(Normal);
        //factor = (float)Math.Sqrt(Normal.X * Normal.X + Normal.Y * Normal.Y + Normal.Z * Normal.Z) / 
        //		(float)Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
        //D = D * factor;
        //     }

        public static Plane Normalize(this Plane value)
        {
            Normalize(ref value, out Plane ret);
            return ret;
        }

        public static void Normalize(ref Plane value, out Plane result)
        {
            float factor;
            result.Normal = Vector3.Normalize(value.Normal);
            factor = (float)Math.Sqrt(result.Normal.X * result.Normal.X + result.Normal.Y * result.Normal.Y + result.Normal.Z * result.Normal.Z) /
                    (float)Math.Sqrt(value.Normal.X * value.Normal.X + value.Normal.Y * value.Normal.Y + value.Normal.Z * value.Normal.Z);
            result.D = value.D * factor;
        }


        public static PlaneIntersectionType Intersects(this Plane plane, BoundingBox box)
        {
            return box.Intersects(plane);
        }

        public static void Intersects(this Plane plane, ref BoundingBox box, out PlaneIntersectionType result)
        {
            box.Intersects(ref plane, out result);
        }

        public static PlaneIntersectionType Intersects(this Plane plane, BoundingFrustum frustum)
        {
            return frustum.Intersects(plane);
        }

        public static PlaneIntersectionType Intersects(this Plane plane, BoundingSphere sphere)
        {
            return sphere.Intersects(plane);
        }

        public static void Intersects(this Plane plane, ref BoundingSphere sphere, out PlaneIntersectionType result)
        {
            sphere.Intersects(ref plane, out result);
        }

        internal static PlaneIntersectionType Intersects(this Plane plane, ref Vector3 point)
        {
            DotCoordinate(plane, ref point, out var distance);

            if (distance > 0)
                return PlaneIntersectionType.Front;

            if (distance < 0)
                return PlaneIntersectionType.Back;

            return PlaneIntersectionType.Intersecting;
        }
    }
}

