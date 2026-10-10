using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 1 - Geometric Transformations (3D, homogeneous coordinates).
    /// A point (x,y,z) is lifted to (x,y,z,1). Every affine transform becomes a 4x4 matrix M and
    /// is applied as p' = M * p (column vectors). Composition is matrix multiplication; the
    /// rightmost matrix is applied first:  TRS = T * Rz * Rx * Ry * S  (scale, then rotate, then translate).
    ///
    ///   Translate(tx,ty,tz)      Scale(sx,sy,sz)        RotateZ(a)
    ///   | 1 0 0 tx |             | sx 0  0  0 |         | cos -sin 0 0 |
    ///   | 0 1 0 ty |             | 0  sy 0  0 |         | sin  cos 0 0 |
    ///   | 0 0 1 tz |             | 0  0  sz 0 |         |  0    0  1 0 |
    ///   | 0 0 0 1  |             | 0  0  0  1 |         |  0    0  0 1 |
    ///
    /// Every matrix below is filled element-by-element (m.m00 = ...), never via Matrix4x4.TRS.
    /// </summary>
    public static class MatrixMath
    {
        public static Matrix4x4 Identity()
        {
            Matrix4x4 m = default;
            m.m00 = 1; m.m11 = 1; m.m22 = 1; m.m33 = 1;
            return m;
        }

        public static Matrix4x4 Translate(Vector3 t)
        {
            Matrix4x4 m = Identity();
            m.m03 = t.x; m.m13 = t.y; m.m23 = t.z;
            return m;
        }

        public static Matrix4x4 Scale(Vector3 s)
        {
            Matrix4x4 m = default;
            m.m00 = s.x; m.m11 = s.y; m.m22 = s.z; m.m33 = 1;
            return m;
        }

        /// <summary>Rotation about X by 'deg' degrees (right-handed).</summary>
        public static Matrix4x4 RotateX(float deg)
        {
            float a = deg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            Matrix4x4 m = Identity();
            m.m11 = c; m.m12 = -s;
            m.m21 = s; m.m22 = c;
            return m;
        }

        public static Matrix4x4 RotateY(float deg)
        {
            float a = deg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            Matrix4x4 m = Identity();
            m.m00 = c; m.m02 = s;
            m.m20 = -s; m.m22 = c;
            return m;
        }

        public static Matrix4x4 RotateZ(float deg)
        {
            float a = deg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            Matrix4x4 m = Identity();
            m.m00 = c; m.m01 = -s;
            m.m10 = s; m.m11 = c;
            return m;
        }

        /// <summary>General shear: each coordinate gets a multiple of the other two (xy means "x += xy*y").</summary>
        public static Matrix4x4 Shear(float xy, float xz, float yx, float yz, float zx, float zy)
        {
            Matrix4x4 m = Identity();
            m.m01 = xy; m.m02 = xz;
            m.m10 = yx; m.m12 = yz;
            m.m20 = zx; m.m21 = zy;
            return m;
        }

        /// <summary>Reflection through the plane with normal n (through the origin): p' = p - 2(p.n)n.</summary>
        public static Matrix4x4 Reflect(Vector3 n)
        {
            n.Normalize();
            Matrix4x4 m = Identity();
            m.m00 = 1 - 2 * n.x * n.x; m.m01 = -2 * n.x * n.y;    m.m02 = -2 * n.x * n.z;
            m.m10 = -2 * n.y * n.x;    m.m11 = 1 - 2 * n.y * n.y; m.m12 = -2 * n.y * n.z;
            m.m20 = -2 * n.z * n.x;    m.m21 = -2 * n.z * n.y;    m.m22 = 1 - 2 * n.z * n.z;
            return m;
        }

        /// <summary>Composite Translate * RotateY * RotateX * RotateZ * Scale (Unity's Z-X-Y Euler order: Z applied first).</summary>
        public static Matrix4x4 TRS(Vector3 t, Vector3 eulerDeg, Vector3 s)
        {
            Matrix4x4 r = RotateY(eulerDeg.y) * RotateX(eulerDeg.x) * RotateZ(eulerDeg.z);
            return Translate(t) * r * Scale(s);
        }

        /// <summary>Transform a point (w = 1) by M, including the perspective divide when w != 1.</summary>
        public static Vector3 MultiplyPoint(Matrix4x4 m, Vector3 p)
        {
            float x = m.m00 * p.x + m.m01 * p.y + m.m02 * p.z + m.m03;
            float y = m.m10 * p.x + m.m11 * p.y + m.m12 * p.z + m.m13;
            float z = m.m20 * p.x + m.m21 * p.y + m.m22 * p.z + m.m23;
            float w = m.m30 * p.x + m.m31 * p.y + m.m32 * p.z + m.m33;
            if (Mathf.Abs(w) > 1e-6f && Mathf.Abs(w - 1f) > 1e-6f) { x /= w; y /= w; z /= w; }
            return new Vector3(x, y, z);
        }

        /// <summary>Component-wise matrix interpolation (used for the projection morph).</summary>
        public static Matrix4x4 Lerp(Matrix4x4 a, Matrix4x4 b, float t)
        {
            Matrix4x4 m = default;
            for (int i = 0; i < 16; i++) m[i] = Mathf.Lerp(a[i], b[i], t);
            return m;
        }

        public static string Format(Matrix4x4 m)
        {
            return string.Format("{0,7:F2} {1,7:F2} {2,7:F2} {3,7:F2}\n{4,7:F2} {5,7:F2} {6,7:F2} {7,7:F2}\n{8,7:F2} {9,7:F2} {10,7:F2} {11,7:F2}\n{12,7:F2} {13,7:F2} {14,7:F2} {15,7:F2}",
                m.m00, m.m01, m.m02, m.m03, m.m10, m.m11, m.m12, m.m13,
                m.m20, m.m21, m.m22, m.m23, m.m30, m.m31, m.m32, m.m33);
        }
    }
}
