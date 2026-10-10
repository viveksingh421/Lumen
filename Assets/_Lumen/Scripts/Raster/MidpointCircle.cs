using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 3 - Midpoint circle algorithm. Uses 8-way symmetry: compute one octant (x from 0 to y)
    /// and mirror it. Decision parameter p = 1 - r evaluates the circle function at the midpoint between
    /// the two candidate pixels: if p &lt; 0 midpoint is inside -> keep y, p += 2x+1; else y--, p += 2(x-y)+1.
    /// </summary>
    public static class MidpointCircle
    {
        public static void Draw(PixelCanvas c, int cx, int cy, int r, Color32 col)
        {
            int x = 0, y = r, p = 1 - r;
            while (x <= y)
            {
                Octants(c, cx, cy, x, y, col);
                x++;
                if (p < 0) p += 2 * x + 1;
                else { y--; p += 2 * (x - y) + 1; }
            }
        }

        public static void Fill(PixelCanvas c, int cx, int cy, int r, Color32 col)
        {
            int x = 0, y = r, p = 1 - r;
            while (x <= y)
            {
                for (int i = cx - x; i <= cx + x; i++) { c.Plot(i, cy + y, col); c.Plot(i, cy - y, col); }
                for (int i = cx - y; i <= cx + y; i++) { c.Plot(i, cy + x, col); c.Plot(i, cy - x, col); }
                x++;
                if (p < 0) p += 2 * x + 1;
                else { y--; p += 2 * (x - y) + 1; }
            }
        }

        static void Octants(PixelCanvas c, int cx, int cy, int x, int y, Color32 col)
        {
            c.Plot(cx + x, cy + y, col); c.Plot(cx - x, cy + y, col);
            c.Plot(cx + x, cy - y, col); c.Plot(cx - x, cy - y, col);
            c.Plot(cx + y, cy + x, col); c.Plot(cx - y, cy + x, col);
            c.Plot(cx + y, cy - x, col); c.Plot(cx - y, cy - x, col);
        }
    }
}
