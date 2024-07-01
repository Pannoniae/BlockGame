using System;
using System.Diagnostics;

namespace System.Numerics
{
    [DebuggerDisplay("{DebugDisplayString,nq}")]
    public struct Ray : IEquatable<Ray>
    {
        #region Public Fields

        
        public Vector3 Direction;
      
        
        public Vector3 Position;

        #endregion


        #region Public Constructors

        public Ray(Vector3 position, Vector3 direction)
        {
            this.Position = position;
            this.Direction = direction;
        }

        #endregion


        #region Public Methods

        public override bool Equals(object obj)
        {
            return (obj is Ray) ? this.Equals((Ray)obj) : false;
        }

        
        public bool Equals(Ray other)
        {
            return this.Position.Equals(other.Position) && this.Direction.Equals(other.Direction);
        }

        
        public override int GetHashCode()
        {
            return Position.GetHashCode() ^ Direction.GetHashCode();
        }

        // adapted from http://www.scratchapixel.com/lessons/3d-basic-lessons/lesson-7-intersecting-simple-shapes/ray-box-intersection/
        public float? Intersects(BoundingBox box)
        {
            const float Epsilon = 1e-6f;

            float? tMin = null, tMax = null;

            if (Math.Abs(Direction.X) < Epsilon)
            {
                if (Position.X < box.Min.X || Position.X > box.Max.X)
                    return null;
            }
            else
            {
                tMin = (box.Min.X - Position.X) / Direction.X;
                tMax = (box.Max.X - Position.X) / Direction.X;

                if (tMin > tMax)
                {
                    var temp = tMin;
                    tMin = tMax;
                    tMax = temp;
                }
            }

            if (Math.Abs(Direction.Y) < Epsilon)
            {
                if (Position.Y < box.Min.Y || Position.Y > box.Max.Y)
                    return null;
            }
            else
            {
                var tMinY = (box.Min.Y - Position.Y) / Direction.Y;
                var tMaxY = (box.Max.Y - Position.Y) / Direction.Y;

                if (tMinY > tMaxY)
                {
                    var temp = tMinY;
                    tMinY = tMaxY;
                    tMaxY = temp;
                }

                if ((tMin.HasValue && tMin > tMaxY) || (tMax.HasValue && tMinY > tMax))
                    return null;

                if (!tMin.HasValue || tMinY > tMin) tMin = tMinY;
                if (!tMax.HasValue || tMaxY < tMax) tMax = tMaxY;
            }

            if (Math.Abs(Direction.Z) < Epsilon)
            {
                if (Position.Z < box.Min.Z || Position.Z > box.Max.Z)
                    return null;
            }
            else
            {
                var tMinZ = (box.Min.Z - Position.Z) / Direction.Z;
                var tMaxZ = (box.Max.Z - Position.Z) / Direction.Z;

                if (tMinZ > tMaxZ)
                {
                    var temp = tMinZ;
                    tMinZ = tMaxZ;
                    tMaxZ = temp;
                }

                if ((tMin.HasValue && tMin > tMaxZ) || (tMax.HasValue && tMinZ > tMax))
                    return null;

                if (!tMin.HasValue || tMinZ > tMin) tMin = tMinZ;
                if (!tMax.HasValue || tMaxZ < tMax) tMax = tMaxZ;
            }

            // having a positive tMin and a negative tMax means the ray is inside the box
            // we expect the intesection distance to be 0 in that case
            if ((tMin.HasValue && tMin < 0) && tMax > 0) return 0;

            // a negative tMin means that the intersection point is behind the ray's origin
            // we discard these as not hitting the AABB
            if (tMin < 0) return null;

            return tMin;
        }


        public void Intersects(ref BoundingBox box, out float? result)
        {
			result = Intersects(box);
        }


        public float? Intersects(BoundingFrustum frustum)
        {
            return frustum.Intersects(this);
        }


        public float? Intersects(BoundingSphere sphere)
        {
            Intersects(ref sphere, out float? result);
            return result;
        }


        public void Intersects(ref BoundingSphere sphere, out float? result)
        {
            // Find the vector between where the ray starts the the sphere's centre
            Vector3 difference = sphere.Center - this.Position;

            float differenceLengthSquared = difference.LengthSquared();
            float sphereRadiusSquared = sphere.Radius * sphere.Radius;

            // If the distance between the ray start and the sphere's centre is less than
            // the radius of the sphere, it means we've intersected. N.B. checking the LengthSquared is faster.
            if (differenceLengthSquared < sphereRadiusSquared)
            {
                result = 0.0f;
                return;
            }

            var distanceAlongRay = Vector3.Dot(this.Direction, difference);
            // If the ray is pointing away from the sphere then we don't ever intersect
            if (distanceAlongRay < 0)
            {
                result = null;
                return;
            }

            // Next we kinda use Pythagoras to check if we are within the bounds of the sphere
            // if x = radius of sphere
            // if y = distance between ray position and sphere centre
            // if z = the distance we've travelled along the ray
            // if x^2 + z^2 - y^2 < 0, we do not intersect
            float dist = sphereRadiusSquared + distanceAlongRay * distanceAlongRay - differenceLengthSquared;

            result = (dist < 0) ? null : distanceAlongRay - (float?)Math.Sqrt(dist);
        }


        public float? Intersects(Plane plane)
        {
            float? result;
            Intersects(ref plane, out result);
            return result;
        }

        public void Intersects(ref Plane plane, out float? result)
        {
            var den = Vector3.Dot(Direction, plane.Normal);
            if (Math.Abs(den) < 0.00001f)
            {
                result = null;
                return;
            }

            result = (-plane.D - Vector3.Dot(plane.Normal, Position)) / den;

            if (result < 0.0f)
            {
                if (result < -0.00001f)
                {
                    result = null;
                    return;
                }

                result = 0.0f;
            }
        }


        /// <summary>
        /// Intersect a ray with triangle defined by vertices v0, v1, v2.
        /// </summary>
        /// <returns>returns true if ray hits triangle at distance less than dist, or false otherwise.</returns>
        public float? Intersects(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            const float EPSILON = 0.00001f;

            // calculate edge vectors
            var edge0 = v1 - v0;
            var edge1 = v2 - v0;

            // begin calculating determinant - also used to calculate U parameter
            var pvec = Vector3.Cross(this.Direction, edge1);

            // if determinant is near zero, ray lies in plane of triangle
            var det = Vector3.Dot(edge0, pvec);
            if (Math.Abs(det) < EPSILON)
            {
                return null;
            }

            float inv_det = 1.0f / det;

            // calculate vector from vert0 to ray origin
            var tvec = this.Position - v0;

            // calculate U parameter, test bounds
            float u = Vector3.Dot(tvec, pvec) * inv_det;
            if (u < -0.01f || u > 1.01f)
            {
                return null;
            }

            // prepare to test V parameter
            var qvec = Vector3.Cross(tvec, edge0);

            // calculate V parameter and test bounds
            float v = Vector3.Dot(this.Direction, qvec) * inv_det;
            if (v < 0.0f || u + v > 1.0f)
            {
                return null;
            }

            // calculate distance to intersection point from ray origin
            float d = Vector3.Dot(edge1, qvec) * inv_det;

            return d;
        }

        /// <summary>
        /// Intersect a ray with triangle defined by vertices v0, v1, v2.
        /// </summary>
        /// <returns>returns true if ray hits triangle at distance less than dist, or false otherwise.</returns>
        public bool Intersects(ref Vector3 v0, ref Vector3 v1, ref Vector3 v2, out float dist)
        {
            const float EPSILON = 0.00001f;

            dist = 0;

            // calculate edge vectors
            var edge0 = v1 - v0;
            var edge1 = v2 - v0;

            // begin calculating determinant - also used to calculate U parameter
            var pvec = Vector3.Cross(this.Direction, edge1);

            // if determinant is near zero, ray lies in plane of triangle
            var det = Vector3.Dot(edge0, pvec);
            if (Math.Abs(det) < EPSILON)
            {
                return false;
            }

            float inv_det = 1.0f / det;

            // calculate vector from vert0 to ray origin
            var tvec = this.Position - v0;

            // calculate U parameter, test bounds
            float u = Vector3.Dot(tvec, pvec) * inv_det;
            if (u < -0.01f || u > 1.01f)
            {
                return false;
            }

            // prepare to test V parameter
            var qvec = Vector3.Cross(tvec, edge0);

            // calculate V parameter and test bounds
            float v = Vector3.Dot(this.Direction, qvec) * inv_det;
            if (v < 0.0f || u + v > 1.0f)
            {
                return false;
            }

            // calculate distance to intersection point from ray origin
            dist = Vector3.Dot(edge1, qvec) * inv_det;

            return true;
        }

        /// <summary>
        /// Intersects the Ray against an OrientedBoundingBox.
        /// </summary>
        /// <param name="obb">OrientedBoundingBox to intersect against.</param>
        /// <returns>True if the two intersect.</returns>
        public bool Intersects(OrientedBoundingBox obb)
        {
            return obb.Intersects(this);
        }

        /// <summary>
        /// Intersects the Ray against an OrientedBoundingBox.
        /// </summary>
        /// <param name="obb">OrientedBoundingBox to intersect against.</param>
        /// <param name="minDistance">Min intersect distance</param>
        /// <param name="maxDistance">Max intersect distance</param>
        /// <returns>True if the two intersect.</returns>
        public bool Intersects(OrientedBoundingBox obb, out float minDistance, out float maxDistance)
        {
            return obb.Intersects(this, out minDistance, out maxDistance);
        }

        public static bool operator !=(Ray a, Ray b)
        {
            return !a.Equals(b);
        }


        public static bool operator ==(Ray a, Ray b)
        {
            return a.Equals(b);
        }

        internal string DebugDisplayString
        {
            get
            {
                return string.Concat(
                    "Pos( ", this.Position.ToString(), " )  \r\n",
                    "Dir( ", this.Direction.ToString(), " )"
                );
            }
        }

        public override string ToString()
        {
            return "{{Position:" + Position.ToString() + " Direction:" + Direction.ToString() + "}}";
        }
		
		#endregion
    }
}