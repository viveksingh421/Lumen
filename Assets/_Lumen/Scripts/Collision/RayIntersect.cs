using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 12 - Analytic ray intersection.
    ///
    /// Ray-sphere: solve |o + t d - c|^2 = r^2  ->  t^2 + 2(d.m) t + (m.m - r^2) = 0 with m = o - c (d unit).
    ///             discriminant = (d.m)^2 - (m.m - r^2); t = -(d.m) -+ sqrt(disc).
    /// Ray-AABB:   slab method. For each axis find t where the ray enters/leaves the slab;
    ///             tNear = max of entries, tFar = min of exits; hit iff tNear &lt;= tFar and tFar &gt;= 0.
    /// Sphere-sphere overlap: |c1-c2| &lt; r1+r2 (compared squared, no sqrt).
    /// </summary>
    public static class RayIntersect
    {
        public static bool RaySphere(Vector3 o, Vector3 d, Vector3 c, float r, out float t)
        {
            t = 0f;
            Vector3 m = o - c;
            float b = Vector3.Dot(m, d);
            float cc = Vector3.Dot(m, m) - r * r;
            if (cc > 0f && b > 0f) return false;        // origin outside and pointing away
            float disc = b * b - cc;
            if (disc < 0f) return false;
            t = -b - Mathf.Sqrt(disc);
            if (t < 0f) t = 0f;                          // origin inside sphere
            return true;
        }

        public static bool RayAABB(Vector3 o, Vector3 d, Vector3 min, Vector3 max, out float t)
        {
            t = 0f;
            float tNear = 0f, tFar = float.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                float oi = o[i], di = d[i];
                if (Mathf.Abs(di) < 1e-8f)
                {
                    if (oi < min[i] || oi > max[i]) return false;   // parallel and outside slab
                    continue;
                }
                float inv = 1f / di;
                float t1 = (min[i] - oi) * inv, t2 = (max[i] - oi) * inv;
                if (t1 > t2) { float s = t1; t1 = t2; t2 = s; }
                tNear = Mathf.Max(tNear, t1);
                tFar = Mathf.Min(tFar, t2);
                if (tNear > tFar) return false;
            }
            t = tNear;
            return true;
        }

        public static bool SphereSphere(Vector3 c1, float r1, Vector3 c2, float r2)
        {
            Vector3 d = c1 - c2;
            float rr = r1 + r2;
            return d.x * d.x + d.y * d.y + d.z * d.z < rr * rr;
        }

        /// <summary>View-cone test: target within half-angle of forward (dot product) and within range.</summary>
        public static bool InViewCone(Vector3 eye, Vector3 forward, Vector3 target, float halfAngleDeg, float range)
        {
            Vector3 to = target - eye;
            float dist = to.magnitude;
            if (dist > range || dist < 1e-4f) return dist < 1e-4f;
            return Vector3.Dot(forward.normalized, to / dist) >= Mathf.Cos(halfAngleDeg * Mathf.Deg2Rad);
        }
    }
}
