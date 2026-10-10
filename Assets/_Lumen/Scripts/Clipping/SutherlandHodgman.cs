using System.Collections.Generic;
using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 4 - Sutherland-Hodgman polygon clipping against a convex window (here: an axis-aligned rectangle).
    /// The polygon is clipped against ONE window edge at a time (pipeline of 4 stages). For each polygon edge (S -> E):
    ///   S in,  E in  -> output E
    ///   S in,  E out -> output intersection I
    ///   S out, E in  -> output I, then E
    ///   S out, E out -> output nothing
    /// </summary>
    public static class SutherlandHodgman
    {
        public static List<Vector2> ClipRect(IList<Vector2> poly, float xmin, float ymin, float xmax, float ymax)
        {
            var cur = new List<Vector2>(poly);
            cur = ClipEdge(cur, p => p.x >= xmin, (s, e) => Intersect(s, e, true, xmin));
            cur = ClipEdge(cur, p => p.x <= xmax, (s, e) => Intersect(s, e, true, xmax));
            cur = ClipEdge(cur, p => p.y >= ymin, (s, e) => Intersect(s, e, false, ymin));
            cur = ClipEdge(cur, p => p.y <= ymax, (s, e) => Intersect(s, e, false, ymax));
            return cur;
        }

        static List<Vector2> ClipEdge(List<Vector2> input, System.Func<Vector2, bool> inside, System.Func<Vector2, Vector2, Vector2> intersect)
        {
            var output = new List<Vector2>(input.Count + 4);
            if (input.Count == 0) return output;
            Vector2 s = input[input.Count - 1];
            for (int i = 0; i < input.Count; i++)
            {
                Vector2 e = input[i];
                bool sIn = inside(s), eIn = inside(e);
                if (eIn) { if (!sIn) output.Add(intersect(s, e)); output.Add(e); }
                else if (sIn) output.Add(intersect(s, e));
                s = e;
            }
            return output;
        }

        /// <summary>Intersection of segment s-e with the vertical (xEdge) or horizontal line at 'value'.</summary>
        static Vector2 Intersect(Vector2 s, Vector2 e, bool vertical, float value)
        {
            if (vertical)
            {
                float t = (value - s.x) / (e.x - s.x);
                return new Vector2(value, s.y + t * (e.y - s.y));
            }
            else
            {
                float t = (value - s.y) / (e.y - s.y);
                return new Vector2(s.x + t * (e.x - s.x), value);
            }
        }
    }
}
