using System.Collections.Generic;
using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 3 - Scan-line polygon fill (even-odd rule).
    /// For each scan line y (sampled at y+0.5): intersect it with every polygon edge that straddles it,
    /// sort the x-intersections, and fill the spans between pairs (x0,x1), (x2,x3), ...
    /// Edge crossing:  x = x_a + (y - y_a) * (x_b - x_a) / (y_b - y_a).
    /// </summary>
    public static class ScanlineFill
    {
        static readonly List<float> xs = new List<float>(16);

        public static void Fill(PixelCanvas c, IList<Vector2> poly, Color32 col, bool blend)
        {
            if (poly == null || poly.Count < 3) return;
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < poly.Count; i++) { minY = Mathf.Min(minY, poly[i].y); maxY = Mathf.Max(maxY, poly[i].y); }
            int y0 = Mathf.Max(0, Mathf.FloorToInt(minY)), y1 = Mathf.Min(c.Height - 1, Mathf.CeilToInt(maxY));

            for (int y = y0; y <= y1; y++)
            {
                float sy = y + 0.5f;
                xs.Clear();
                for (int i = 0; i < poly.Count; i++)
                {
                    Vector2 a = poly[i], b = poly[(i + 1) % poly.Count];
                    if ((a.y <= sy && b.y > sy) || (b.y <= sy && a.y > sy))
                        xs.Add(a.x + (sy - a.y) * (b.x - a.x) / (b.y - a.y));
                }
                xs.Sort();
                for (int k = 0; k + 1 < xs.Count; k += 2)
                {
                    int xa = Mathf.Max(0, Mathf.CeilToInt(xs[k] - 0.5f)), xb = Mathf.Min(c.Width - 1, Mathf.FloorToInt(xs[k + 1] - 0.5f));
                    for (int x = xa; x <= xb; x++) { if (blend) c.Blend(x, y, col); else c.Plot(x, y, col); }
                }
            }
        }
    }
}
