// MIT License - Copyright (C) The Mono.Xna Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Diagnostics;
using System.Numerics;
using Silk.NET.Maths;
using Plane = System.Numerics.Plane;

namespace BlockGame {
    /// <summary>
    /// Defines a viewing frustum for intersection operations.
    /// </summary>
    [DebuggerDisplay("{DebugDisplayString,nq}")]
    public class Frustum : IEquatable<Frustum> {

        #region Private Fields

        private Matrix4x4 _matrix;
        private readonly Vector3[] _corners = new Vector3[CornerCount];
        private readonly Plane[] _planes = new Plane[PlaneCount];

        #endregion

        #region Public Fields

        /// <summary>
        /// The number of planes in the frustum.
        /// </summary>
        public const int PlaneCount = 6;

        /// <summary>
        /// The number of corner points in the frustum.
        /// </summary>
        public const int CornerCount = 8;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the <see cref="Matrix"/> of the frustum.
        /// </summary>
        public Matrix4x4 Matrix {
            get => _matrix;
            set {
                _matrix = value;
                CreatePlanes(); // FIXME: The odds are the planes will be used a lot more often than the matrix
                CreateCorners(); // is updated, so this should help performance. I hope ;)
            }
        }

        /// <summary>
        /// Gets the near plane of the frustum.
        /// </summary>
        public Plane Near => _planes[0];

        /// <summary>
        /// Gets the far plane of the frustum.
        /// </summary>
        public Plane Far => _planes[1];

        /// <summary>
        /// Gets the left plane of the frustum.
        /// </summary>
        public Plane Left => _planes[2];

        /// <summary>
        /// Gets the right plane of the frustum.
        /// </summary>
        public Plane Right => _planes[3];

        /// <summary>
        /// Gets the top plane of the frustum.
        /// </summary>
        public Plane Top => _planes[4];

        /// <summary>
        /// Gets the bottom plane of the frustum.
        /// </summary>
        public Plane Bottom => _planes[5];

        #endregion

        #region Internal Properties

        internal string DebugDisplayString => string.Concat(
            "Near( ", _planes[0].ToString(), " )  \r\n",
            "Far( ", _planes[1].ToString(), " )  \r\n",
            "Left( ", _planes[2].ToString(), " )  \r\n",
            "Right( ", _planes[3].ToString(), " )  \r\n",
            "Top( ", _planes[4].ToString(), " )  \r\n",
            "Bottom( ", _planes[5].ToString(), " )  "
        );

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs the frustum by extracting the view planes from a matrix.
        /// </summary>
        /// <param name="value">Combined matrix which usually is (View * Projection).</param>
        public Frustum(Matrix4x4 value) {
            _matrix = value;
            CreatePlanes();
            CreateCorners();
        }

        #endregion

        #region Operators

        /// <summary>
        /// Compares whether two <see cref="Frustum"/> instances are equal.
        /// </summary>
        /// <param name="a"><see cref="Frustum"/> instance on the left of the equal sign.</param>
        /// <param name="b"><see cref="Frustum"/> instance on the right of the equal sign.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public static bool operator ==(Frustum a, Frustum b) {
            if (Equals(a, null))
                return (Equals(b, null));

            if (Equals(b, null))
                return (Equals(a, null));

            return a._matrix == (b._matrix);
        }

        /// <summary>
        /// Compares whether two <see cref="Frustum"/> instances are not equal.
        /// </summary>
        /// <param name="a"><see cref="Frustum"/> instance on the left of the not equal sign.</param>
        /// <param name="b"><see cref="Frustum"/> instance on the right of the not equal sign.</param>
        /// <returns><c>true</c> if the instances are not equal; <c>false</c> otherwise.</returns>
        public static bool operator !=(Frustum a, Frustum b) {
            return !(a == b);
        }

        #endregion

        #region Public Methods

        #region Contains

        /// <summary>
        /// Containment test between this <see cref="Frustum"/> and specified <see cref="BoundingBox"/>.
        /// </summary>
        /// <param name="box">A <see cref="BoundingBox"/> for testing.</param>
        /// <returns>Result of testing for containment between this <see cref="Frustum"/> and specified <see cref="BoundingBox"/>.</returns>
        public bool Contains(AABB box) {
            Contains(ref box, out var result);
            return result;
        }

        /// <summary>
        /// Containment test between this <see cref="Frustum"/> and specified <see cref="BoundingBox"/>.
        /// </summary>
        /// <param name="box">A <see cref="BoundingBox"/> for testing.</param>
        /// <param name="result">Result of testing for containment between this <see cref="Frustum"/> and specified <see cref="BoundingBox"/> as an output parameter.</param>
        public void Contains(ref AABB box, out bool result) {
            var intersects = false;
            for (var i = 0; i < PlaneCount; ++i) {
                bool planeIntersection;
                planeIntersection =
                    box.intersects(new Plane<double>(new Vector3D<double>(_planes[i].Normal.X, _planes[i].Normal.Y, _planes[i].Normal.Z), _planes[i].D));
                if (planeIntersection) {
                    result = planeIntersection;
                    return;
                }
            }
            result = false;
        }

        /// <summary>
        /// Containment test between this <see cref="Frustum"/> and specified <see cref="Vector3"/>.
        /// </summary>
        /// <param name="point">A <see cref="Vector3"/> for testing.</param>
        /// <returns>Result of testing for containment between this <see cref="Frustum"/> and specified <see cref="Vector3"/>.</returns>
        public bool Contains(Vector3 point) {
            Contains(ref point, out bool result);
            return result;
        }

        /// <summary>
        /// Containment test between this <see cref="Frustum"/> and specified <see cref="Vector3"/>.
        /// </summary>
        /// <param name="point">A <see cref="Vector3"/> for testing.</param>
        /// <param name="result">Result of testing for containment between this <see cref="Frustum"/> and specified <see cref="Vector3"/> as an output parameter.</param>
        public void Contains(ref Vector3 point, out bool result) {
            for (var i = 0; i < PlaneCount; ++i) {
                // TODO: we might want to inline this for performance reasons
                if (_planes[i].planeSide(point) > 0) {
                    result = false;
                    return;
                }
            }
            result = true;
        }

        #endregion

        /// <summary>
        /// Compares whether current instance is equal to specified <see cref="Frustum"/>.
        /// </summary>
        /// <param name="other">The <see cref="Frustum"/> to compare.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public bool Equals(Frustum other) {
            return (this == other);
        }

        /// <summary>
        /// Compares whether current instance is equal to specified <see cref="Frustum"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare.</param>
        /// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
        public override bool Equals(object obj) {
            return (obj is Frustum) && this == ((Frustum)obj);
        }

        /// <summary>
        /// Returns a copy of internal corners array.
        /// </summary>
        /// <returns>The array of corners.</returns>
        public Vector3[] GetCorners() {
            return (Vector3[])_corners.Clone();
        }

        /// <summary>
        /// Returns a copy of internal corners array.
        /// </summary>
        /// <param name="corners">The array which values will be replaced to corner values of this instance. It must have size of <see cref="Frustum.CornerCount"/>.</param>
        public void GetCorners(Vector3[] corners) {
            if (corners == null) throw new ArgumentNullException("corners");
            if (corners.Length < CornerCount) throw new ArgumentOutOfRangeException("corners");

            _corners.CopyTo(corners, 0);
        }

        /// <summary>
        /// Gets the hash code of this <see cref="Frustum"/>.
        /// </summary>
        /// <returns>Hash code of this <see cref="Frustum"/>.</returns>
        public override int GetHashCode() {
            return _matrix.GetHashCode();
        }

        /// <summary>
        /// Gets whether or not a specified <see cref="BoundingBox"/> intersects with this <see cref="Frustum"/>.
        /// </summary>
        /// <param name="box">A <see cref="BoundingBox"/> for intersection test.</param>
        /// <returns><c>true</c> if specified <see cref="BoundingBox"/> intersects with this <see cref="Frustum"/>; <c>false</c> otherwise.</returns>
        public bool Intersects(AABB box) {
            var result = false;
            this.Intersects(ref box, out result);
            return result;
        }

        /// <summary>
        /// Gets whether or not a specified <see cref="BoundingBox"/> intersects with this <see cref="Frustum"/>.
        /// </summary>
        /// <param name="box">A <see cref="BoundingBox"/> for intersection test.</param>
        /// <param name="result"><c>true</c> if specified <see cref="BoundingBox"/> intersects with this <see cref="Frustum"/>; <c>false</c> otherwise as an output parameter.</param>
        public void Intersects(ref AABB box, out bool result) {
            Contains(ref box, out bool containment);
            result = containment;
        }

        /// <summary>
        /// Returns a <see cref="String"/> representation of this <see cref="Frustum"/> in the format:
        /// {Near:[nearPlane] Far:[farPlane] Left:[leftPlane] Right:[rightPlane] Top:[topPlane] Bottom:[bottomPlane]}
        /// </summary>
        /// <returns><see cref="String"/> representation of this <see cref="Frustum"/>.</returns>
        public override string ToString() {
            return "{Near: " + _planes[0] +
                   " Far:" + _planes[1] +
                   " Left:" + _planes[2] +
                   " Right:" + _planes[3] +
                   " Top:" + _planes[4] +
                   " Bottom:" + _planes[5] +
                   "}";
        }

        #endregion

        #region Private Methods

        private void CreateCorners() {
            IntersectionPoint(ref _planes[0], ref _planes[2], ref _planes[4], out _corners[0]);
            IntersectionPoint(ref _planes[0], ref _planes[3], ref _planes[4], out _corners[1]);
            IntersectionPoint(ref _planes[0], ref _planes[3], ref _planes[5], out _corners[2]);
            IntersectionPoint(ref _planes[0], ref _planes[2], ref _planes[5], out _corners[3]);
            IntersectionPoint(ref _planes[1], ref _planes[2], ref _planes[4], out _corners[4]);
            IntersectionPoint(ref _planes[1], ref _planes[3], ref _planes[4], out _corners[5]);
            IntersectionPoint(ref _planes[1], ref _planes[3], ref _planes[5], out _corners[6]);
            IntersectionPoint(ref _planes[1], ref _planes[2], ref _planes[5], out _corners[7]);
        }

        private void CreatePlanes() {
            _planes[0] = new Plane(-_matrix.M13, -_matrix.M23, -_matrix.M33, -_matrix.M43);
            _planes[1] = new Plane(_matrix.M13 - _matrix.M14, _matrix.M23 - _matrix.M24, _matrix.M33 - _matrix.M34,
                _matrix.M43 - _matrix.M44);
            _planes[2] = new Plane(-_matrix.M14 - _matrix.M11, -_matrix.M24 - _matrix.M21, -_matrix.M34 - _matrix.M31,
                -_matrix.M44 - _matrix.M41);
            _planes[3] = new Plane(_matrix.M11 - _matrix.M14, _matrix.M21 - _matrix.M24, _matrix.M31 - _matrix.M34,
                _matrix.M41 - _matrix.M44);
            _planes[4] = new Plane(_matrix.M12 - _matrix.M14, _matrix.M22 - _matrix.M24, _matrix.M32 - _matrix.M34,
                _matrix.M42 - _matrix.M44);
            _planes[5] = new Plane(-_matrix.M14 - _matrix.M12, -_matrix.M24 - _matrix.M22, -_matrix.M34 - _matrix.M32,
                -_matrix.M44 - _matrix.M42);

            NormalizePlane(ref _planes[0]);
            NormalizePlane(ref _planes[1]);
            NormalizePlane(ref _planes[2]);
            NormalizePlane(ref _planes[3]);
            NormalizePlane(ref _planes[4]);
            NormalizePlane(ref _planes[5]);
        }

        private static void IntersectionPoint(ref Plane a, ref Plane b, ref Plane c, out Vector3 result) {
            // Formula used
            //                d1 ( N2 * N3 ) + d2 ( N3 * N1 ) + d3 ( N1 * N2 )
            //P =   -------------------------------------------------------------------------
            //                             N1 . ( N2 * N3 )
            //
            // Note: N refers to the normal, d refers to the displacement. '.' means dot product. '*' means cross product

            Vector3 v1, v2, v3;
            Vector3 cross;

            cross = Vector3.Cross(b.Normal, c.Normal);

            float f;
            f = Vector3.Dot(a.Normal, cross);
            f *= -1.0f;

            cross = Vector3.Cross(b.Normal, c.Normal);
            v1 = Vector3.Multiply(cross, a.D);
            //v1 = (a.D * (Vector3.Cross(b.Normal, c.Normal)));


            cross = Vector3.Cross(c.Normal, a.Normal);
            v2 = Vector3.Multiply(cross, b.D);
            //v2 = (b.D * (Vector3.Cross(c.Normal, a.Normal)));


            cross = Vector3.Cross(a.Normal, b.Normal);
            v3 = Vector3.Multiply(cross, c.D);
            //v3 = (c.D * (Vector3.Cross(a.Normal, b.Normal)));

            result.X = (v1.X + v2.X + v3.X) / f;
            result.Y = (v1.Y + v2.Y + v3.Y) / f;
            result.Z = (v1.Z + v2.Z + v3.Z) / f;
        }

        private void NormalizePlane(ref Plane p) {
            float factor = 1f / p.Normal.Length();
            p.Normal.X *= factor;
            p.Normal.Y *= factor;
            p.Normal.Z *= factor;
            p.D *= factor;
        }

        #endregion

    }
}

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