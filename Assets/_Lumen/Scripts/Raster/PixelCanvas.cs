using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// A CPU frame-buffer: every scan-conversion algorithm writes pixels into this, one at a time.
    /// Counts plotted pixels so the Lab Panel can compare algorithms.
    /// </summary>
    public class PixelCanvas
    {
        public readonly int Width, Height;
        public readonly Color32[] Pixels;
        public int PlotCount;

        public PixelCanvas(int w, int h) { Width = w; Height = h; Pixels = new Color32[w * h]; }

        public void Clear(Color32 c) { for (int i = 0; i < Pixels.Length; i++) Pixels[i] = c; PlotCount = 0; }

        public void Plot(int x, int y, Color32 c)
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;   // guard: never write off-buffer
            Pixels[y * Width + x] = c;
            PlotCount++;
        }

        /// <summary>Alpha-over blend, used for the translucent view cone.</summary>
        public void Blend(int x, int y, Color32 c)
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
            Color32 d = Pixels[y * Width + x];
            float a = c.a / 255f;
            Pixels[y * Width + x] = new Color32(
                (byte)(c.r * a + d.r * (1 - a)), (byte)(c.g * a + d.g * (1 - a)), (byte)(c.b * a + d.b * (1 - a)), 255);
            PlotCount++;
        }

        public Color32 Get(int x, int y) => ((uint)x < (uint)Width && (uint)y < (uint)Height) ? Pixels[y * Width + x] : default;
    }
}
