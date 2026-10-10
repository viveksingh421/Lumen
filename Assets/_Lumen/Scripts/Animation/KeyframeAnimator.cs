using System.Collections.Generic;
using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 10 - Keyframe animation. Each key stores time, scale and Euler rotation, plus the interpolation
    /// mode used to reach the NEXT key (linear / ease-in-out / cubic Hermite). Scale is interpolated component-wise
    /// with the eased parameter; rotation is interpolated with the hand-written quaternion slerp.
    /// </summary>
    public class KeyframeAnimator
    {
        public struct Key
        {
            public float Time; public Vector3 Scale; public Vector3 Euler; public Easing.Mode Ease;
            public Key(float t, float s, Vector3 euler, Easing.Mode ease) { Time = t; Scale = new Vector3(s, s, s); Euler = euler; Ease = ease; }
        }

        public readonly List<Key> Keys = new List<Key>();
        public bool Loop;

        public float Duration => Keys.Count == 0 ? 0f : Keys[Keys.Count - 1].Time;

        public void Evaluate(float time, out Vector3 scale, out Quaternion rotation)
        {
            if (Keys.Count == 0) { scale = Vector3.one; rotation = Quaternion.identity; return; }
            float d = Duration;
            if (Loop && d > 0f) time = time % d; else time = Mathf.Clamp(time, 0f, d);

            int i = 0;
            while (i < Keys.Count - 2 && time > Keys[i + 1].Time) i++;
            Key a = Keys[i], b = Keys[Mathf.Min(i + 1, Keys.Count - 1)];
            float span = b.Time - a.Time;
            float u = span > 1e-5f ? Mathf.Clamp01((time - a.Time) / span) : 1f;
            float k = Easing.Apply(a.Ease, u);
            scale = Vector3.LerpUnclamped(a.Scale, b.Scale, k);
            rotation = QuaternionMath.Slerp(Quaternion.Euler(a.Euler), Quaternion.Euler(b.Euler), k);
        }
    }
}
