using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 4 - Cohen-Sutherland line clipping against a rectangle [xmin,xmax]x[ymin,ymax].
    /// Each endpoint gets a 4-bit outcode (LEFT, RIGHT, BOTTOM, TOP).
    ///   both codes 0            -> trivially accept
    ///   (code0 &amp; code1) != 0   -> trivially reject (both outside the same side)
    ///   else pick an outside endpoint and move it to the boundary:
    ///        x = x0 + (x1-x0)(yEdge-y0)/(y1-y0)   (or the analogous formula for vertical edges), repeat.
    /// </summary>
    public static class CohenSutherland
    {
        const int INSIDE = 0, LEFT = 1, RIGHT = 2, BOTTOM = 4, TOP = 8;

        static int Code(float x, float y, float xmin, float ymin, float xmax, float ymax)
        {
            int c = INSIDE;
            if (x < xmin) c |= LEFT; else if (x > xmax) c |= RIGHT;
            if (y < ymin) c |= BOTTOM; else if (y > ymax) c |= TOP;
            return c;
        }

        /// <summary>Returns false if the segment is entirely outside; otherwise a,b are replaced with the clipped end points.</summary>
        public static bool Clip(ref Vector2 a, ref Vector2 b, float xmin, float ymin, float xmax, float ymax)
        {
            float x0 = a.x, y0 = a.y, x1 = b.x, y1 = b.y;
            int c0 = Code(x0, y0, xmin, ymin, xmax, ymax), c1 = Code(x1, y1, xmin, ymin, xmax, ymax);
            for (int guard = 0; guard < 8; guard++)
            {
                if ((c0 | c1) == 0) { a = new Vector2(x0, y0); b = new Vector2(x1, y1); return true; }
                if ((c0 & c1) != 0) return false;
                int co = c0 != 0 ? c0 : c1;
                float x = 0, y = 0;
                if ((co & TOP) != 0) { x = x0 + (x1 - x0) * (ymax - y0) / (y1 - y0); y = ymax; }
                else if ((co & BOTTOM) != 0) { x = x0 + (x1 - x0) * (ymin - y0) / (y1 - y0); y = ymin; }
                else if ((co & RIGHT) != 0) { y = y0 + (y1 - y0) * (xmax - x0) / (x1 - x0); x = xmax; }
                else if ((co & LEFT) != 0) { y = y0 + (y1 - y0) * (xmin - x0) / (x1 - x0); x = xmin; }
                if (co == c0) { x0 = x; y0 = y; c0 = Code(x0, y0, xmin, ymin, xmax, ymax); }
                else { x1 = x; y1 = y; c1 = Code(x1, y1, xmin, ymin, xmax, ymax); }
            }
            return false;
        }
    }
}
