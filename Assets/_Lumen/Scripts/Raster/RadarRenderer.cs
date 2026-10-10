using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPTS 3 + 4 - the radar minimap is rasterised on the CPU, pixel by pixel, into a 256x256 PixelCanvas every 3 frames:
    ///   range rings      -> midpoint circle          blips        -> filled midpoint circle
    ///   cone edges/lines -> DDA or Bresenham         portal icon  -> midpoint ellipse
    ///   view cones       -> Sutherland-Hodgman clip + scan-line fill
    /// Lines are clipped to the radar window by Cohen-Sutherland BEFORE rasterisation (toggle to see the unclipped result).
    /// </summary>
    public class RadarRenderer : MonoBehaviour
    {
        public const int Size = 256;
        public const float WorldRange = 45f;
        public const int Inset = 28;                       // clip window = canvas shrunk by this margin

        public bool UseBresenham = true;
        public bool ShowUnclipped = false;

        public PixelCanvas Canvas { get; private set; }
        public Texture2D Texture { get; private set; }
        public int LastPixels { get; private set; }
        public double LastLineMicros { get; private set; }

        // benchmark (same random line set drawn with both algorithms)
        public int DdaPixels, BresPixels;
        public double DdaMicros, BresMicros;

        readonly float xmin = Inset, ymin = Inset, xmax = Size - 1 - Inset, ymax = Size - 1 - Inset;
        float scale;
        int frame;
        long lineTicks;
        float nextBench;

        static readonly Color32 bg = new Color32(8, 6, 22, 255), windowBg = new Color32(14, 10, 40, 255), ring = new Color32(60, 70, 140, 255),
            cyan = new Color32(80, 255, 255, 255), cyanFill = new Color32(60, 230, 255, 70), red = new Color32(255, 70, 60, 255),
            redFill = new Color32(255, 60, 50, 60), yellow = new Color32(255, 230, 70, 255), magenta = new Color32(255, 70, 230, 255),
            frameCol = new Color32(120, 130, 255, 255), dim = new Color32(110, 120, 200, 255), orange = new Color32(255, 180, 40, 255), ice = new Color32(120, 230, 255, 255);

        void Awake()
        {
            Canvas = new PixelCanvas(Size, Size);
            Texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "RadarTex" };
            scale = (Size * 0.5f - Inset) / WorldRange;
        }

        void Update()
        {
            if (Time.unscaledTime > nextBench) { Benchmark(); nextBench = Time.unscaledTime + 1.5f; }
            if (++frame % 3 != 0) return;
            if (WorldBuilder.I == null || WorldBuilder.I.Orb == null) return;
            Draw();
            Texture.SetPixels32(Canvas.Pixels);
            Texture.Apply(false);
        }

        // ------------------------------------------------------------- helpers
        Vector2 ToRadar(Vector3 world, Vector3 centre, Vector2 fwd, Vector2 right)
        {
            Vector2 d = new Vector2(world.x - centre.x, world.z - centre.z);
            return new Vector2(Size * 0.5f + Vector2.Dot(d, right) * scale, Size * 0.5f + Vector2.Dot(d, fwd) * scale);
        }

        void Line(Vector2 a, Vector2 b, Color32 col)
        {
            if (!ShowUnclipped && !CohenSutherland.Clip(ref a, ref b, xmin, ymin, xmax, ymax)) return;
            long t0 = Stopwatch.GetTimestamp();
            int x0 = Mathf.RoundToInt(a.x), y0 = Mathf.RoundToInt(a.y), x1 = Mathf.RoundToInt(b.x), y1 = Mathf.RoundToInt(b.y);
            if (UseBresenham) LineBresenham.Draw(Canvas, x0, y0, x1, y1, col); else LineDDA.Draw(Canvas, x0, y0, x1, y1, col);
            lineTicks += Stopwatch.GetTimestamp() - t0;
        }

        void Cone(Vector2 apex, Vector2 dir, float halfDeg, float length, Color32 fill, Color32 edge)
        {
            var poly = new List<Vector2> { apex };
            for (int i = 0; i <= 6; i++)
            {
                float a = Mathf.Lerp(-halfDeg, halfDeg, i / 6f) * Mathf.Deg2Rad;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                poly.Add(apex + new Vector2(dir.x * c - dir.y * s, dir.x * s + dir.y * c) * length);
            }
            var clipped = ShowUnclipped ? poly : SutherlandHodgman.ClipRect(poly, xmin, ymin, xmax, ymax);
            ScanlineFill.Fill(Canvas, clipped, fill, true);
            Line(poly[0], poly[1], edge);
            Line(poly[0], poly[poly.Count - 1], edge);
        }

        /// <summary>Marker position for something possibly outside the window: pull it back to the window edge (Cohen-Sutherland).</summary>
        bool Marker(Vector2 centre, ref Vector2 p, out bool clamped)
        {
            clamped = false;
            Vector2 a = centre, b = p;
            if (b.x >= xmin && b.x <= xmax && b.y >= ymin && b.y <= ymax) return true;
            if (!CohenSutherland.Clip(ref a, ref b, xmin, ymin, xmax, ymax)) return false;
            p = b; clamped = true; return true;
        }

        // ------------------------------------------------------------- frame
        void Draw()
        {
            var wb = WorldBuilder.I;
            var cam = Camera.main;
            Vector3 centre = wb.Orb.position;
            float yaw = cam != null ? cam.transform.eulerAngles.y * Mathf.Deg2Rad : 0f;
            Vector2 fwd = new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw)), right = new Vector2(fwd.y, -fwd.x);
            Vector2 c = new Vector2(Size * 0.5f, Size * 0.5f);

            Canvas.Clear(bg);
            lineTicks = 0;
            // window background
            for (int y = Inset; y <= Size - 1 - Inset; y++) for (int x = Inset; x <= Size - 1 - Inset; x++) Canvas.Pixels[y * Size + x] = windowBg;
            // window frame + range rings
            Line2(new Vector2(xmin, ymin), new Vector2(xmax, ymin), frameCol); Line2(new Vector2(xmax, ymin), new Vector2(xmax, ymax), frameCol);
            Line2(new Vector2(xmax, ymax), new Vector2(xmin, ymax), frameCol); Line2(new Vector2(xmin, ymax), new Vector2(xmin, ymin), frameCol);
            for (int i = 1; i <= 3; i++) MidpointCircle.Draw(Canvas, Size / 2, Size / 2, Mathf.RoundToInt(WorldRange * scale * i / 3f), ring);
            Line(new Vector2(c.x, ymin), new Vector2(c.x, ymax), ring); Line(new Vector2(xmin, c.y), new Vector2(xmax, c.y), ring);

            // player's view cone (clipped polygon + fill + Bresenham/DDA edges)
            Cone(c, Vector2.up, 35f, 150f, cyanFill, cyan);

            // line to nearest shard (clipped to the window)
            var near = wb.NearestShard(centre);
            if (near != null) Line(c, ToRadar(near.transform.position, centre, fwd, right), new Color32(255, 230, 70, 120));

            // shards
            foreach (var s in wb.Shards)
            {
                if (s == null || s.Collected) continue;
                Vector2 p = ToRadar(s.transform.position, centre, fwd, right);
                if (!Marker(c, ref p, out bool clamped) && !ShowUnclipped) continue;
                int px = Mathf.RoundToInt(p.x), py = Mathf.RoundToInt(p.y);
                Color32 col = clamped ? orange : yellow;
                Canvas.Plot(px, py, col); Canvas.Plot(px + 1, py, col); Canvas.Plot(px - 1, py, col); Canvas.Plot(px, py + 1, col); Canvas.Plot(px, py - 1, col);
            }

            // portal (ellipse icon, filled when open)
            if (wb.Portal != null)
            {
                Vector2 p = ToRadar(wb.Portal.center, centre, fwd, right);
                if (Marker(c, ref p, out _) || ShowUnclipped)
                {
                    int px = Mathf.RoundToInt(p.x), py = Mathf.RoundToInt(p.y);
                    MidpointEllipse.Draw(Canvas, px, py, 7, 4, magenta);
                    if (wb.Portal.IsOpen) MidpointEllipse.Draw(Canvas, px, py, 4, 2, magenta);
                }
            }

            // sentinels: cone + blip
            foreach (var s in wb.Sentinels)
            {
                if (s == null) continue;
                Vector2 p = ToRadar(s.transform.position, centre, fwd, right);
                Vector3 f3 = s.transform.forward;
                Vector2 f = new Vector2(Vector2.Dot(new Vector2(f3.x, f3.z), right), Vector2.Dot(new Vector2(f3.x, f3.z), fwd)).normalized;
                Color32 edge = s.Current == Sentinel.State.Chase ? yellow : (s.Current == Sentinel.State.Stunned ? ice : red);
                Color32 fill = s.Current == Sentinel.State.Chase ? new Color32(255, 220, 60, 70) : (s.Current == Sentinel.State.Stunned ? new Color32(80, 220, 255, 40) : redFill);
                if (s.Current != Sentinel.State.Stunned) Cone(p, f, s.SightHalfAngle, s.SightRange * scale, fill, edge);
                if (Marker(c, ref p, out bool clamped) || ShowUnclipped)
                {
                    int px = Mathf.RoundToInt(p.x), py = Mathf.RoundToInt(p.y);
                    MidpointCircle.Fill(Canvas, px, py, clamped ? 2 : 3, edge);
                    if (s.Current == Sentinel.State.Chase) MidpointCircle.Draw(Canvas, px, py, 6, edge);
                }
            }

            // player blip
            MidpointCircle.Fill(Canvas, Size / 2, Size / 2, 3, cyan);
            MidpointCircle.Draw(Canvas, Size / 2, Size / 2, 5, cyan);

            LastPixels = Canvas.PlotCount;
            LastLineMicros = lineTicks * 1e6 / Stopwatch.Frequency;
        }

        // window frame: unclipped, always Bresenham
        void Line2(Vector2 a, Vector2 b, Color32 col) =>
            LineBresenham.Draw(Canvas, Mathf.RoundToInt(a.x), Mathf.RoundToInt(a.y), Mathf.RoundToInt(b.x), Mathf.RoundToInt(b.y), col);

        /// <summary>Draw the same 400 pseudo-random lines with each algorithm; report pixel counts and CPU time.</summary>
        void Benchmark()
        {
            var scratch = new PixelCanvas(Size, Size);
            var rng = new System.Random(42);
            var pts = new int[400 * 4];
            for (int i = 0; i < pts.Length; i++) pts[i] = rng.Next(0, Size);
            Color32 w = new Color32(255, 255, 255, 255);

            long t0 = Stopwatch.GetTimestamp();
            for (int i = 0; i < pts.Length; i += 4) LineDDA.Draw(scratch, pts[i], pts[i + 1], pts[i + 2], pts[i + 3], w);
            long t1 = Stopwatch.GetTimestamp();
            DdaPixels = scratch.PlotCount; DdaMicros = (t1 - t0) * 1e6 / Stopwatch.Frequency;

            scratch.PlotCount = 0;
            t0 = Stopwatch.GetTimestamp();
            for (int i = 0; i < pts.Length; i += 4) LineBresenham.Draw(scratch, pts[i], pts[i + 1], pts[i + 2], pts[i + 3], w);
            t1 = Stopwatch.GetTimestamp();
            BresPixels = scratch.PlotCount; BresMicros = (t1 - t0) * 1e6 / Stopwatch.Frequency;
        }
    }
}
