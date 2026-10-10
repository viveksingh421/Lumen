using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 3 - Midpoint ellipse algorithm (4-way symmetry, two regions).
    /// Ellipse: f(x,y) = ry^2 x^2 + rx^2 y^2 - rx^2 ry^2.
    /// Region 1: slope magnitude &lt; 1 (step in x); region 2: slope &gt;= 1 (step in y). The switch happens when
    /// 2 ry^2 x >= 2 rx^2 y. The decision variable is f evaluated at the midpoint between candidates.
    /// </summary>
    public static class MidpointEllipse
    {
        public static void Draw(PixelCanvas c, int cx, int cy, int rx, int ry, Color32 col)
        {
            long rx2 = (long)rx * rx, ry2 = (long)ry * ry;
            long x = 0, y = ry;
            long dx = 2 * ry2 * x, dy = 2 * rx2 * y;

            // Region 1
            long p1 = ry2 - rx2 * ry + rx2 / 4;
            while (dx < dy)
            {
                Quad(c, cx, cy, (int)x, (int)y, col);
                x++; dx += 2 * ry2;
                if (p1 < 0) p1 += dx + ry2;
                else { y--; dy -= 2 * rx2; p1 += dx - dy + ry2; }
            }
            // Region 2
            long p2 = ry2 * (2 * x + 1) * (2 * x + 1) / 4 + rx2 * (y - 1) * (y - 1) - rx2 * ry2;
            while (y >= 0)
            {
                Quad(c, cx, cy, (int)x, (int)y, col);
                y--; dy -= 2 * rx2;
                if (p2 > 0) p2 += rx2 - dy;
                else { x++; dx += 2 * ry2; p2 += dx - dy + rx2; }
            }
        }

        static void Quad(PixelCanvas c, int cx, int cy, int x, int y, Color32 col)
        {
            c.Plot(cx + x, cy + y, col); c.Plot(cx - x, cy + y, col);
            c.Plot(cx + x, cy - y, col); c.Plot(cx - x, cy - y, col);
        }
    }
}
