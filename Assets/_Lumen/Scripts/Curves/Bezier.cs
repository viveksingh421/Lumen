using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 5 - Cubic Bezier curves, two equivalent evaluations.
    ///
    /// Bernstein form:  B(t) = (1-t)^3 P0 + 3(1-t)^2 t P1 + 3(1-t) t^2 P2 + t^3 P3
    /// de Casteljau:    repeated linear interpolation of the control polygon:
    ///                  Q0=lerp(P0,P1,t) Q1=lerp(P1,P2,t) Q2=lerp(P2,P3,t)
    ///                  R0=lerp(Q0,Q1,t) R1=lerp(Q1,Q2,t)   B=lerp(R0,R1,t)
    /// Derivative:      B'(t) = 3[(1-t)^2 (P1-P0) + 2(1-t)t (P2-P1) + t^2 (P3-P2)]
    /// A closed chain of segments with matching end tangents (C1) gives a smooth patrol loop.
    /// </summary>
    public static class Bezier
    {
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);

        public static Vector3 DeCasteljau(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            Vector3 q0 = Lerp(p0, p1, t), q1 = Lerp(p1, p2, t), q2 = Lerp(p2, p3, t);
            Vector3 r0 = Lerp(q0, q1, t), r1 = Lerp(q1, q2, t);
            return Lerp(r0, r1, t);
        }

        public static Vector3 Bernstein(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            float b0 = u * u * u, b1 = 3f * u * u * t, b2 = 3f * u * t * t, b3 = t * t * t;
            return p0 * b0 + p1 * b1 + p2 * b2 + p3 * b3;
        }

        public static Vector3 Tangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            return 3f * (u * u * (p1 - p0) + 2f * u * t * (p2 - p1) + t * t * (p3 - p2));
        }

        /// <summary>
        /// Build a closed, C1-smooth chain of cubic segments through 'anchors'. Control points are
        /// P1 = A_i + d_i/3, P2 = A_{i+1} - d_{i+1}/3 where d_i = (A_{i+1} - A_{i-1})/2 (Catmull-Rom tangent).
        /// Returns 3*n+1 points: [A0 c c A1 c c A2 ... A0].
        /// </summary>
        public static Vector3[] BuildClosedChain(Vector3[] anchors)
        {
            int n = anchors.Length;
            var pts = new Vector3[3 * n + 1];
            for (int i = 0; i < n; i++)
            {
                Vector3 a = anchors[i], b = anchors[(i + 1) % n];
                Vector3 da = (b - anchors[(i - 1 + n) % n]) * 0.5f;
                Vector3 db = (anchors[(i + 2) % n] - a) * 0.5f;
                pts[3 * i] = a;
                pts[3 * i + 1] = a + da / 3f;
                pts[3 * i + 2] = b - db / 3f;
            }
            pts[3 * n] = anchors[0];
            return pts;
        }

        /// <summary>Evaluate a chain (3*n+1 points) at u in [0,n): integer part picks the segment.</summary>
        public static Vector3 EvaluateChain(Vector3[] pts, float u)
        {
            int n = (pts.Length - 1) / 3;
            u = ((u % n) + n) % n;
            int seg = Mathf.Min((int)u, n - 1);
            float t = u - seg;
            int i = seg * 3;
            return DeCasteljau(pts[i], pts[i + 1], pts[i + 2], pts[i + 3], t);
        }

        public static Vector3 ChainTangent(Vector3[] pts, float u)
        {
            int n = (pts.Length - 1) / 3;
            u = ((u % n) + n) % n;
            int seg = Mathf.Min((int)u, n - 1);
            int i = seg * 3;
            return Tangent(pts[i], pts[i + 1], pts[i + 2], pts[i + 3], u - seg);
        }
    }
}
