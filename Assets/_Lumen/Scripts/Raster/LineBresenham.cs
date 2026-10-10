using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 3 - Bresenham's line algorithm (all octants, integer arithmetic only).
    /// Keeps an integer error term err = dx + dy (dy stored negative). Each step:
    ///   e2 = 2*err; if e2 >= dy: err += dy, x += sx;  if e2 &lt;= dx: err += dx, y += sy.
    /// No floating point and no rounding: the decision variable tells whether to step diagonally.
    /// </summary>
    public static class LineBresenham
    {
        public static void Draw(PixelCanvas c, int x0, int y0, int x1, int y1, Color32 col)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                c.Plot(x0, y0, col);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }
    }
}
