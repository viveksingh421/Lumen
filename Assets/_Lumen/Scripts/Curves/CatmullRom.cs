using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 5 - Catmull-Rom spline (interpolating cubic). Passes THROUGH every control point.
    /// For points P0..P3 the curve between P1 and P2 is
    ///   C(t) = 0.5 * [ 2P1 + (-P0+P2)t + (2P0-5P1+4P2-P3)t^2 + (-P0+3P1-3P2+P3)t^3 ]
    /// Used for the intro camera fly-through.
    /// </summary>
    public static class CatmullRom
    {
        public static Vector3 Evaluate(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        /// <summary>Open spline through 'pts'; u in [0, n-1]. End points are duplicated as phantom neighbours.</summary>
        public static Vector3 EvaluatePath(Vector3[] pts, float u)
        {
            int n = pts.Length;
            u = Mathf.Clamp(u, 0f, n - 1 - 1e-4f);
            int i = (int)u;
            float t = u - i;
            Vector3 p0 = pts[Mathf.Max(i - 1, 0)], p1 = pts[i], p2 = pts[Mathf.Min(i + 1, n - 1)], p3 = pts[Mathf.Min(i + 2, n - 1)];
            return Evaluate(p0, p1, p2, p3, t);
        }
    }
}
