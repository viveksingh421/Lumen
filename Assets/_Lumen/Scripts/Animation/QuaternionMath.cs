using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 10 - Quaternion interpolation (hand-written).
    /// A unit quaternion q = (x,y,z,w) encodes a rotation. Spherical linear interpolation walks along
    /// the great arc between two rotations at constant angular speed:
    ///
    ///   slerp(a,b,t) = sin((1-t)W)/sin(W) * a + sin(tW)/sin(W) * b,   cos(W) = a.b
    ///
    /// If a.b &lt; 0 we negate b (q and -q are the same rotation) so we take the short way round.
    /// For tiny angles sin(W) ~ 0, so we fall back to normalized linear interpolation (nlerp).
    /// </summary>
    public static class QuaternionMath
    {
        public static float Dot(Quaternion a, Quaternion b) => a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;

        public static Quaternion Normalize(Quaternion q)
        {
            float m = Mathf.Sqrt(Dot(q, q));
            if (m < 1e-8f) return new Quaternion(0, 0, 0, 1);
            float inv = 1f / m;
            return new Quaternion(q.x * inv, q.y * inv, q.z * inv, q.w * inv);
        }

        public static Quaternion Slerp(Quaternion a, Quaternion b, float t)
        {
            float cosW = Dot(a, b);
            if (cosW < 0f) { b = new Quaternion(-b.x, -b.y, -b.z, -b.w); cosW = -cosW; }

            float s0, s1;
            if (cosW > 0.9995f) { s0 = 1f - t; s1 = t; }          // nearly parallel: nlerp
            else
            {
                float w = Mathf.Acos(cosW);
                float sinW = Mathf.Sin(w);
                s0 = Mathf.Sin((1f - t) * w) / sinW;
                s1 = Mathf.Sin(t * w) / sinW;
            }
            return Normalize(new Quaternion(s0 * a.x + s1 * b.x, s0 * a.y + s1 * b.y, s0 * a.z + s1 * b.z, s0 * a.w + s1 * b.w));
        }

        /// <summary>Axis-angle to quaternion: q = (axis*sin(a/2), cos(a/2)).</summary>
        public static Quaternion FromAxisAngle(Vector3 axis, float deg)
        {
            float h = deg * Mathf.Deg2Rad * 0.5f, s = Mathf.Sin(h);
            axis.Normalize();
            return new Quaternion(axis.x * s, axis.y * s, axis.z * s, Mathf.Cos(h));
        }

        /// <summary>Quaternion looking along 'forward' with 'up' as the reference up (built from an orthonormal basis).</summary>
        public static Quaternion LookRotation(Vector3 forward, Vector3 up)
        {
            Vector3 f = forward.normalized;
            Vector3 r = Vector3.Cross(up, f).normalized;
            Vector3 u = Vector3.Cross(f, r);
            // rotation matrix columns (r,u,f) -> quaternion
            float m00 = r.x, m01 = u.x, m02 = f.x, m10 = r.y, m11 = u.y, m12 = f.y, m20 = r.z, m21 = u.z, m22 = f.z;
            float tr = m00 + m11 + m22, x, y, z, w;
            if (tr > 0f)
            {
                float s = Mathf.Sqrt(tr + 1f) * 2f;
                w = 0.25f * s; x = (m21 - m12) / s; y = (m02 - m20) / s; z = (m10 - m01) / s;
            }
            else if (m00 > m11 && m00 > m22)
            {
                float s = Mathf.Sqrt(1f + m00 - m11 - m22) * 2f;
                w = (m21 - m12) / s; x = 0.25f * s; y = (m01 + m10) / s; z = (m02 + m20) / s;
            }
            else if (m11 > m22)
            {
                float s = Mathf.Sqrt(1f + m11 - m00 - m22) * 2f;
                w = (m02 - m20) / s; x = (m01 + m10) / s; y = 0.25f * s; z = (m12 + m21) / s;
            }
            else
            {
                float s = Mathf.Sqrt(1f + m22 - m00 - m11) * 2f;
                w = (m10 - m01) / s; x = (m02 + m20) / s; y = (m12 + m21) / s; z = 0.25f * s;
            }
            return Normalize(new Quaternion(x, y, z, w));
        }
    }
}
