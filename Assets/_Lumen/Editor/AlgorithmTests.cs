using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Lumen.EditorTools
{
    /// <summary>Quick sanity checks for the hand-written CG algorithms. Menu: Lumen/Run Algorithm Tests.</summary>
    public static class AlgorithmTests
    {
        static int pass, fail;

        static void Check(string name, bool ok)
        {
            if (ok) { pass++; Debug.Log("[PASS] " + name); }
            else { fail++; Debug.LogError("[FAIL] " + name); }
        }

        static bool Near(Matrix4x4 a, Matrix4x4 b, float eps = 1e-4f)
        {
            for (int i = 0; i < 16; i++) if (Mathf.Abs(a[i] - b[i]) > eps) return false;
            return true;
        }

        [MenuItem("Lumen/Run Algorithm Tests")]
        public static void Run()
        {
            pass = fail = 0;

            // Transformations
            var t = new Vector3(1, 2, 3); var e = new Vector3(20, 40, 60); var s = new Vector3(1, 2, 0.5f);
            Check("MatrixMath.TRS == Matrix4x4.TRS", Near(MatrixMath.TRS(t, e, s), Matrix4x4.TRS(t, Quaternion.Euler(e), s)));
            Check("RotateY(90) maps +X to -Z", (MatrixMath.MultiplyPoint(MatrixMath.RotateY(90), Vector3.right) - Vector3.back).magnitude < 1e-4f);
            Check("Reflect(x) flips x", (MatrixMath.MultiplyPoint(MatrixMath.Reflect(Vector3.right), new Vector3(2, 3, 4)) - new Vector3(-2, 3, 4)).magnitude < 1e-4f);

            // Curves
            Vector3 p0 = new Vector3(0, 0, 0), p1 = new Vector3(1, 3, 0), p2 = new Vector3(4, 3, 1), p3 = new Vector3(5, 0, 2);
            bool same = true;
            for (float u = 0; u <= 1.0001f; u += 0.1f) same &= (Bezier.DeCasteljau(p0, p1, p2, p3, u) - Bezier.Bernstein(p0, p1, p2, p3, u)).magnitude < 1e-4f;
            Check("de Casteljau == Bernstein", same);
            var cr = CatmullRom.Evaluate(p0, p1, p2, p3, 0f);
            Check("CatmullRom passes through P1 at t=0", (cr - p1).magnitude < 1e-4f);

            // Quaternion
            Quaternion qa = Quaternion.Euler(0, 0, 0), qb = Quaternion.Euler(0, 90, 0);
            Check("Slerp midpoint = 45 deg", Quaternion.Angle(QuaternionMath.Slerp(qa, qb, 0.5f), Quaternion.Euler(0, 45, 0)) < 0.01f);

            // Noise
            var n = new PerlinNoise(7);
            Check("Perlin deterministic", n.Fractal(3.3f, 1.1f, 4, 2, 0.5f) == new PerlinNoise(7).Fractal(3.3f, 1.1f, 4, 2, 0.5f));
            Check("Perlin zero at lattice points", Mathf.Abs(n.Noise(5, 9)) < 1e-5f);

            // Rays
            Check("Ray-sphere hit", RayIntersect.RaySphere(Vector3.zero, Vector3.forward, new Vector3(0, 0, 5), 1, out float ts) && Mathf.Abs(ts - 4) < 1e-4f);
            Check("Ray-sphere miss", !RayIntersect.RaySphere(Vector3.zero, Vector3.forward, new Vector3(0, 3, 5), 1, out _));
            Check("Ray-AABB hit", RayIntersect.RayAABB(Vector3.zero, Vector3.forward, new Vector3(-1, -1, 4), new Vector3(1, 1, 6), out float tb) && Mathf.Abs(tb - 4) < 1e-4f);

            // Raster
            var c = new PixelCanvas(64, 64);
            LineBresenham.Draw(c, 3, 4, 40, 17, new Color32(255, 255, 255, 255));
            Check("Bresenham endpoints", c.Get(3, 4).r == 255 && c.Get(40, 17).r == 255);
            Check("Bresenham pixel count = max(dx,dy)+1", c.PlotCount == 38);
            var d = new PixelCanvas(64, 64);
            LineDDA.Draw(d, 3, 4, 40, 17, new Color32(255, 255, 255, 255));
            Check("DDA endpoints", d.Get(3, 4).r == 255 && d.Get(40, 17).r == 255);
            var ci = new PixelCanvas(64, 64);
            MidpointCircle.Draw(ci, 32, 32, 10, new Color32(255, 255, 255, 255));
            Check("Circle on axes", ci.Get(42, 32).r == 255 && ci.Get(32, 22).r == 255 && ci.Get(22, 32).r == 255);
            var el = new PixelCanvas(64, 64);
            MidpointEllipse.Draw(el, 32, 32, 20, 8, new Color32(255, 255, 255, 255));
            Check("Ellipse on axes", el.Get(52, 32).r == 255 && el.Get(32, 40).r == 255);
            var f = new PixelCanvas(32, 32);
            ScanlineFill.Fill(f, new List<Vector2> { new Vector2(4, 4), new Vector2(20, 4), new Vector2(20, 20), new Vector2(4, 20) }, new Color32(255, 255, 255, 255), false);
            Check("Scanline fill interior", f.Get(10, 10).r == 255 && f.Get(2, 2).r == 0 && f.PlotCount > 200);

            // Clipping
            Vector2 a = new Vector2(-5, 5), b = new Vector2(15, 5);
            Check("Cohen-Sutherland clips", CohenSutherland.Clip(ref a, ref b, 0, 0, 10, 10) && Mathf.Abs(a.x) < 1e-4f && Mathf.Abs(b.x - 10) < 1e-4f);
            Vector2 r = new Vector2(-5, -5), q = new Vector2(-1, 20);
            Check("Cohen-Sutherland rejects", !CohenSutherland.Clip(ref r, ref q, 0, 0, 10, 10));
            var poly = SutherlandHodgman.ClipRect(new List<Vector2> { new Vector2(-5, -5), new Vector2(15, -5), new Vector2(15, 15), new Vector2(-5, 15) }, 0, 0, 10, 10);
            Check("Sutherland-Hodgman -> 4 vertices in window", poly.Count == 4 && poly.TrueForAll(v => v.x >= -1e-4f && v.x <= 10.0001f && v.y >= -1e-4f && v.y <= 10.0001f));

            if (fail == 0) Debug.Log("ALGORITHM TESTS: ALL " + pass + " PASSED");
            else Debug.LogError("ALGORITHM TESTS: " + fail + " FAILED, " + pass + " passed");
        }
    }
}
