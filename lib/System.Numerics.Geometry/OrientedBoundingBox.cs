using System;
using System.Collections.Generic;

namespace System.Numerics
{
    public class OrientedBoundingBox
    {
        /// <summary>
        /// Center location of the box.
        /// </summary>
        public Vector3 Center;

        /// <summary>
        /// Half the sizes of the box in each dimension (u,v,w). Positive values are expected.
        /// </summary>
        public Vector3 HalfSizes;

        /// <summary>
        /// Basis vectors (u,v,w) of the sides. Must always be normalized, and should be orthogonal for a proper rectangular cuboid.
        /// </summary>
        public Vector3 U;
        /// <summary>
        /// Basis vectors (u,v,w) of the sides. Must always be normalized, and should be orthogonal for a proper rectangular cuboid.
        /// </summary>
        public Vector3 V;
        /// <summary>
        /// Basis vectors (u,v,w) of the sides. Must always be normalized, and should be orthogonal for a proper rectangular cuboid.
        /// </summary>
        public Vector3 W;

        /// Empty constructor; creates an empty box
        public OrientedBoundingBox()
        {
        }


        public OrientedBoundingBox Clone()
        {
            var ret = new OrientedBoundingBox
            {
                Center = Center,
                HalfSizes = HalfSizes,
                U = U,
                V = V,
                W = W
            };

            return ret;
        }

        /// <summary>
        /// Constructs a new oriented box centered at particles center and with normalized side vectors u, v and w.
        /// These vectors should be mutually orthonormal for a proper rectangular box.
        /// The half-widths of the box in each dimension are given by the corresponding components of halfSizes.
        /// </summary>
        public OrientedBoundingBox(Vector3 center, Vector3 u, Vector3 v, Vector3 w, Vector3 halfSizes)
        {
            Center = center;
            HalfSizes = halfSizes;
            U = u;
            V = v;
            W = w;
        }

        public static explicit operator OrientedBoundingBox(BoundingBox bound)
        {
            OrientedBoundingBox o = new OrientedBoundingBox
            {
                Center = (bound.Min + bound.Max) / 2.0f,

                // the axes of an AABB are the world-space axes
                U = Vector3.UnitX,
                V = Vector3.UnitY,
                W = Vector3.UnitZ,

                // element-wise division by two to get half sizes (remember, [1] and [0] are the max and min coord points)
                HalfSizes = (bound.Max - bound.Min) * 0.5f
            };

            return o;
        }

        private void GetCorner(float u, float v, float w, out Vector3 @out)
        {
            @out = Center + U * (u * HalfSizes.X) + V * (v * HalfSizes.Y) + W * (w * HalfSizes.Z);
        }

        public void GetCorners(Vector3[] corners)
        {
            GetCorner(+1, 1, +1, out corners[0]);
            GetCorner(-1, 1, +1, out corners[1]);
            GetCorner(-1, 1, -1, out corners[2]);
            GetCorner(+1, 1, -1, out corners[3]);

            GetCorner(+1, -1, +1, out corners[4]);
            GetCorner(-1, -1, +1, out corners[5]);
            GetCorner(-1, -1, -1, out corners[6]);
            GetCorner(+1, -1, -1, out corners[7]);
        }


        /// <summary>
        /// Check if a given ray intersects this box. Must not be used if IsEmpty() is true.
        /// See Real-Time Rendering, Third Edition by T. Akenine-Möller, el_particles. 741--744.    
        /// @param[out] tMin,tMax Distance in the positive direction from the origin of the ray to the
        ///             entry and exit points in the box, provided that the ray intersects the box. if
        ///             the ray does not intersect the box, no values are written to these variables.
        ///             If the origin is inside the box, then this is counted as an intersection and one
        ///             of @minDistance minDistance and @maxDistance maxDistance may be negative.
        ///
        /// </summary>
        /// <param key="origin">origin Origin of the ray.</param>
        /// <param key="dir">dir Direction vector of the ray, defining the positive direction of the ray. Must be of unit length.</param>
        /// <returns>true If the ray originating in @el_particles origin and with unit direction vector @el_particles dir intersects this box, false otherwise</returns>
        public bool Intersects(Ray ray, out float minDistance, out float maxDistance)
        {
            Vector3 origin = ray.Position;
            Vector3 dir = ray.Direction;

            minDistance = 0;
            maxDistance = 0;
            // See Real-Time Rendering, Third Edition, particles. 743
            float tMin = float.NegativeInfinity;
            float tMax = float.PositiveInfinity;

            Vector3 p = Center - origin;
            {
                // test the ray for intersections with the slab whose normal vector is m_Basis[i]
                float e = Vector3.Dot(U, p); // distance between the ray origin and the box center projected onto the slab normal
                float f = Vector3.Dot(U, dir); // cosine of the angle between the slab normal and the ray direction

                if (Math.Abs(f) > 1e-10f)
                {
                    // Determine the distances t1 and t2 from the origin of the ray to the points where it intersects
                    // the slab. See docs/ray_intersect.pdf for why/how this works.
                    float invF = 1.0f / f;
                    float t1 = (e + HalfSizes.X) * invF;
                    float t2 = (e - HalfSizes.X) * invF;

                    // make sure t1 <= t2, swap if necessary
                    if (t1 > t2)
                    {
                        float tmp = t1;
                        t1 = t2;
                        t2 = tmp;
                    }

                    // update the overall tMin and tMax if necessary
                    if (t1 > tMin) tMin = t1;
                    if (t2 < tMax) tMax = t2;

                    // try to break out of the loop as fast as possible by checking for some conditions
                    if (tMin > tMax) return false; // ray misses the box
                    if (tMax < 0) return false; // box is behind the ray origin
                }
                else
                {
                    // the ray is parallel to the slab currently being tested, or is as close to parallel
                    // as makes no difference; return false if the ray is outside of the slab.
                    if (e > HalfSizes.X || -e > HalfSizes.X)
                    {
                        return false;
                    }
                }
            }

            {
                // test the ray for intersections with the slab whose normal vector is m_Basis[i]
                float e = Vector3.Dot(V, p); // distance between the ray origin and the box center projected onto the slab normal
                float f = Vector3.Dot(V, dir); // cosine of the angle between the slab normal and the ray direction

                if (Math.Abs(f) > 1e-10f)
                {
                    // Determine the distances t1 and t2 from the origin of the ray to the points where it intersects
                    // the slab. See docs/ray_intersect.pdf for why/how this works.
                    float invF = 1.0f / f;
                    float t1 = (e + HalfSizes.Y) * invF;
                    float t2 = (e - HalfSizes.Y) * invF;

                    // make sure t1 <= t2, swap if necessary
                    if (t1 > t2)
                    {
                        float tmp = t1;
                        t1 = t2;
                        t2 = tmp;
                    }

                    // update the overall tMin and tMax if necessary
                    if (t1 > tMin) tMin = t1;
                    if (t2 < tMax) tMax = t2;

                    // try to break out of the loop as fast as possible by checking for some conditions
                    if (tMin > tMax) return false; // ray misses the box
                    if (tMax < 0) return false; // box is behind the ray origin
                }
                else
                {
                    // the ray is parallel to the slab currently being tested, or is as close to parallel
                    // as makes no difference; return false if the ray is outside of the slab.
                    if (e > HalfSizes.Y || -e > HalfSizes.Y)
                    {
                        return false;
                    }
                }
            }

            {
                // test the ray for intersections with the slab whose normal vector is m_Basis[i]
                float e = Vector3.Dot(W, p); // distance between the ray origin and the box center projected onto the slab normal
                float f = Vector3.Dot(W, dir); // cosine of the angle between the slab normal and the ray direction

                if (Math.Abs(f) > 1e-10f)
                {
                    // Determine the distances t1 and t2 from the origin of the ray to the points where it intersects
                    // the slab. See docs/ray_intersect.pdf for why/how this works.
                    float invF = 1.0f / f;
                    float t1 = (e + HalfSizes.Z) * invF;
                    float t2 = (e - HalfSizes.Z) * invF;

                    // make sure t1 <= t2, swap if necessary
                    if (t1 > t2)
                    {
                        float tmp = t1;
                        t1 = t2;
                        t2 = tmp;
                    }

                    // update the overall tMin and tMax if necessary
                    if (t1 > tMin) tMin = t1;
                    if (t2 < tMax) tMax = t2;

                    // try to break out of the loop as fast as possible by checking for some conditions
                    if (tMin > tMax) return false; // ray misses the box
                    if (tMax < 0) return false; // box is behind the ray origin
                }
                else
                {
                    // the ray is parallel to the slab currently being tested, or is as close to parallel
                    // as makes no difference; return false if the ray is outside of the slab.
                    if (e > HalfSizes.Z || -e > HalfSizes.Z)
                    {
                        return false;
                    }
                }
            }

            minDistance = tMin;
            maxDistance = tMax;
            return true;
        }

        public bool Intersects(Ray ray)
        {
            float minDistance;
            float maxDistance;

            return Intersects(ray, out minDistance, out maxDistance);
        }

        public bool Intersects(BoundingFrustum frustum)
        {
            foreach (var v in GetCorners())
            {
                if (frustum.Contains(v) != ContainmentType.Disjoint)
                {
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<Plane> GetPlanes()
        {
            U = Vector3.Normalize(U);
            V = Vector3.Normalize(V);
            W = Vector3.Normalize(W);

            yield return new Plane(U, HalfSizes.X);
            yield return new Plane(-U, -HalfSizes.X);
            yield return new Plane(V, HalfSizes.Y);
            yield return new Plane(-V, -HalfSizes.Y);
            yield return new Plane(W, HalfSizes.Z);
            yield return new Plane(-W, -HalfSizes.Z);
        }

        public IEnumerable<Vector3> GetCorners()
        {
            yield return Center + U * (+1 * HalfSizes.X) + V * (+1 * HalfSizes.Y) + W * (+1 * HalfSizes.Z);
            yield return Center + U * (-1 * HalfSizes.X) + V * (+1 * HalfSizes.Y) + W * (+1 * HalfSizes.Z);
            yield return Center + U * (-1 * HalfSizes.X) + V * (+1 * HalfSizes.Y) + W * (-1 * HalfSizes.Z);
            yield return Center + U * (+1 * HalfSizes.X) + V * (+1 * HalfSizes.Y) + W * (-1 * HalfSizes.Z);

            yield return Center + U * (+1 * HalfSizes.X) + V * (-1 * HalfSizes.Y) + W * (+1 * HalfSizes.Z);
            yield return Center + U * (-1 * HalfSizes.X) + V * (-1 * HalfSizes.Y) + W * (+1 * HalfSizes.Z);
            yield return Center + U * (-1 * HalfSizes.X) + V * (-1 * HalfSizes.Y) + W * (-1 * HalfSizes.Z);
            yield return Center + U * (+1 * HalfSizes.X) + V * (-1 * HalfSizes.Y) + W * (-1 * HalfSizes.Z);
        }


        public void Transform(Matrix4x4 transform)
        {
            Transform(ref transform);
        }

        public void Transform(ref Matrix4x4 transform)
        {
            U = Vector3.TransformNormal(U, transform);
            V = Vector3.TransformNormal(V, transform);
            W = Vector3.TransformNormal(W, transform);

            Center += transform.Translation;
        }
    }
}