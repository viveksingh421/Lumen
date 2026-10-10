using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 3 - Digital Differential Analyzer line algorithm.
    /// steps = max(|dx|,|dy|); increments xInc = dx/steps, yInc = dy/steps (one of them is +-1).
    /// Each step: x += xInc, y += yInc, plot(round(x), round(y)). Uses floating point adds + rounding.
    /// </summary>
    public static class LineDDA
    {
        public static void Draw(PixelCanvas c, int x0, int y0, int x1, int y1, Color32 col)
        {
            int dx = x1 - x0, dy = y1 - y0;
            int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
            if (steps == 0) { c.Plot(x0, y0, col); return; }
            float xInc = dx / (float)steps, yInc = dy / (float)steps;
            float x = x0, y = y0;
            for (int i = 0; i <= steps; i++)
            {
                c.Plot(Mathf.RoundToInt(x), Mathf.RoundToInt(y), col);
                x += xInc; y += yInc;
            }
        }
    }
}
