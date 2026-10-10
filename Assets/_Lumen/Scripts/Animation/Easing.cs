using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// Interpolation curves for keyframe animation (CG CONCEPT 10). All map t in [0,1] to [0,1].
    ///   Linear:       f(t) = t
    ///   EaseInOut:    f(t) = 3t^2 - 2t^3          (smoothstep, zero slope at both ends)
    ///   Hermite:      cubic Hermite basis  h00 p0 + h10 m0 + h01 p1 + h11 m1  (see <see cref="Hermite"/>)
    /// </summary>
    public static class Easing
    {
        public enum Mode { Linear, EaseInOut, Hermite }

        public static float Linear(float t) => t;
        public static float EaseInOut(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        /// <summary>Cubic Hermite spline value for end-points p0,p1 and tangents m0,m1 at parameter t.</summary>
        public static float Hermite(float p0, float p1, float m0, float m1, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            float h00 = 2 * t3 - 3 * t2 + 1;
            float h10 = t3 - 2 * t2 + t;
            float h01 = -2 * t3 + 3 * t2;
            float h11 = t3 - t2;
            return h00 * p0 + h10 * m0 + h01 * p1 + h11 * m1;
        }

        public static float Apply(Mode mode, float t)
        {
            switch (mode)
            {
                case Mode.EaseInOut: return EaseInOut(t);
                case Mode.Hermite: return Hermite(0f, 1f, 0.5f, 0.5f, Mathf.Clamp01(t)); // gentle S-curve w/ non-zero end slopes
                default: return Linear(t);
            }
        }
    }
}
