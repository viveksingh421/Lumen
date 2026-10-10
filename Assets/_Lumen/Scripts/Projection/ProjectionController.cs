using UnityEngine;
using UnityEngine.InputSystem;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 2 - Viewing &amp; projection, built by hand (OpenGL convention; Unity converts for the GPU).
    ///
    /// Perspective (frustum from vertical FOV):  t = n*tan(fov/2), r = t*aspect
    ///   | n/r  0    0            0          |      Clip.w = -z_eye  -> perspective divide x/w, y/w gives foreshortening
    ///   | 0   n/t   0            0          |
    ///   | 0    0  -(f+n)/(f-n) -2fn/(f-n)   |
    ///   | 0    0   -1            0          |
    /// Orthographic (box l,r,b,t,n,f):
    ///   | 2/(r-l) 0 0 -(r+l)/(r-l) |   parallel projection: w stays 1, no foreshortening
    ///   | 0 2/(t-b) 0 -(t+b)/(t-b) |
    ///   | 0 0 -2/(f-n) -(f+n)/(f-n)|
    ///   | 0 0 0 1                  |
    /// P toggles between them by lerping the two matrices over 1 s. The ortho box is sized so the player's focus
    /// distance keeps the same on-screen size (half-height = d*tan(fov/2)) -> a dolly-zoom-like flattening.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ProjectionController : MonoBehaviour
    {
        public static ProjectionController I { get; private set; }

        [Range(20, 110)] public float fov = 60f;
        public float nearClip = 0.3f, farClip = 400f;
        public Transform focus;
        public float morphDuration = 1f;

        /// <summary>0 = perspective, 1 = orthographic.</summary>
        public float Morph { get; private set; }
        public bool TargetOrtho { get; private set; }
        public Matrix4x4 CurrentMatrix { get; private set; }
        Camera cam;

        void Awake() { I = this; cam = GetComponent<Camera>(); cam.nearClipPlane = nearClip; cam.farClipPlane = farClip; }

        public void SetOrtho(bool ortho) { TargetOrtho = ortho; }

        void Update()
        {
            if (LumenInput.Pressed(Key.P)) TargetOrtho = !TargetOrtho;
            float target = TargetOrtho ? 1f : 0f;
            float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, morphDuration);
            Morph = Mathf.MoveTowards(Morph, target, step);
            Shader.SetGlobalFloat("_LumenOrtho", Morph > 0.5f ? 1f : 0f);
        }

        void LateUpdate()
        {
            cam.nearClipPlane = nearClip; cam.farClipPlane = farClip;
            float aspect = cam.aspect;
            float dist = focus != null ? Vector3.Distance(transform.position, focus.position) : 10f;
            Matrix4x4 p = Perspective(fov, aspect, nearClip, farClip);
            float halfH = dist * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            Matrix4x4 o = Orthographic(-halfH * aspect, halfH * aspect, -halfH, halfH, nearClip, farClip);
            float k = Easing.EaseInOut(Morph);
            CurrentMatrix = k <= 0f ? p : (k >= 1f ? o : MatrixMath.Lerp(p, o, k));
            cam.projectionMatrix = CurrentMatrix;
        }

        public static Matrix4x4 Perspective(float fovDeg, float aspect, float n, float f)
        {
            float t = Mathf.Tan(fovDeg * 0.5f * Mathf.Deg2Rad);
            Matrix4x4 m = default;
            m.m00 = 1f / (aspect * t);
            m.m11 = 1f / t;
            m.m22 = -(f + n) / (f - n);
            m.m23 = -2f * f * n / (f - n);
            m.m32 = -1f;
            return m;
        }

        public static Matrix4x4 Orthographic(float l, float r, float b, float t, float n, float f)
        {
            Matrix4x4 m = default;
            m.m00 = 2f / (r - l); m.m03 = -(r + l) / (r - l);
            m.m11 = 2f / (t - b); m.m13 = -(t + b) / (t - b);
            m.m22 = -2f / (f - n); m.m23 = -(f + n) / (f - n);
            m.m33 = 1f;
            return m;
        }

        public string Label => Morph < 0.01f ? "Perspective" : (Morph > 0.99f ? "Orthographic" : "Morphing " + Mathf.RoundToInt(Morph * 100) + "%");
    }
}
