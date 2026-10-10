using UnityEngine;

namespace Lumen
{
    /// <summary>Hand-built meshes (vertex + index buffers written by code).</summary>
    public static class MeshFactory
    {
        /// <summary>Elongated octahedron (crystal) with flat per-face normals: 8 faces x 3 unique vertices.</summary>
        public static Mesh Crystal(float radius, float halfHeight)
        {
            Vector3 top = new Vector3(0, halfHeight, 0), bot = new Vector3(0, -halfHeight, 0);
            Vector3[] eq = { new Vector3(radius, 0, 0), new Vector3(0, 0, radius), new Vector3(-radius, 0, 0), new Vector3(0, 0, -radius) };
            var v = new Vector3[24]; var n = new Vector3[24]; var tri = new int[24]; var uv = new Vector2[24]; var col = new Color[24];
            int k = 0;
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = eq[i], b = eq[(i + 1) % 4];
                AddFace(v, n, uv, col, tri, ref k, top, b, a);    // upper face (CCW seen from outside)
                AddFace(v, n, uv, col, tri, ref k, bot, a, b);    // lower face
            }
            var m = new Mesh { name = "Crystal", vertices = v, normals = n, uv = uv, colors = col, triangles = tri };
            m.RecalculateBounds();
            return m;
        }

        static void AddFace(Vector3[] v, Vector3[] n, Vector2[] uv, Color[] col, int[] tri, ref int k, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 fn = Vector3.Cross(b - a, c - a).normalized;
            Vector3[] p = { a, b, c };
            Vector2[] t = { new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(1, 0) };
            for (int i = 0; i < 3; i++) { v[k] = p[i]; n[k] = fn; uv[k] = t[i]; col[k] = Color.white; tri[k] = k; k++; }
        }

        /// <summary>Flat disc in the XY plane (normal -Z/+Z), uv in [0,1]. rings x segments polar grid.</summary>
        public static Mesh Disc(float radius, int rings = 24, int segments = 64)
        {
            var v = new Vector3[1 + rings * segments]; var uv = new Vector2[v.Length]; var n = new Vector3[v.Length];
            v[0] = Vector3.zero; uv[0] = new Vector2(0.5f, 0.5f); n[0] = Vector3.back;
            for (int r = 1; r <= rings; r++)
                for (int s = 0; s < segments; s++)
                {
                    float rad = radius * r / rings, a = s * Mathf.PI * 2f / segments;
                    int i = 1 + (r - 1) * segments + s;
                    v[i] = new Vector3(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad, 0);
                    uv[i] = new Vector2(0.5f + 0.5f * r / rings * Mathf.Cos(a), 0.5f + 0.5f * r / rings * Mathf.Sin(a));
                    n[i] = Vector3.back;
                }
            var tri = new System.Collections.Generic.List<int>();
            for (int s = 0; s < segments; s++) { tri.Add(0); tri.Add(1 + s); tri.Add(1 + (s + 1) % segments); }
            for (int r = 1; r < rings; r++)
                for (int s = 0; s < segments; s++)
                {
                    int a = 1 + (r - 1) * segments + s, b = 1 + (r - 1) * segments + (s + 1) % segments;
                    int c = 1 + r * segments + s, d = 1 + r * segments + (s + 1) % segments;
                    tri.Add(a); tri.Add(c); tri.Add(b); tri.Add(b); tri.Add(c); tri.Add(d);
                }
            var m = new Mesh { name = "Disc", vertices = v, uv = uv, normals = n, triangles = tri.ToArray() };
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Quad covering [-0.5,0.5]; used as a full-screen overlay (vertex shader writes clip space).</summary>
        public static Mesh FullscreenQuad()
        {
            var m = new Mesh { name = "FullscreenQuad" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0) };
            m.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            m.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);   // never frustum-culled
            return m;
        }
        /// <summary>Procedural low-poly dragon mesh with body segments, tapered head, horns, wings, and tail.</summary>
        public static Mesh Dragon()
        {
            var vList = new System.Collections.Generic.List<Vector3>();
            var nList = new System.Collections.Generic.List<Vector3>();
            var uvList = new System.Collections.Generic.List<Vector2>();
            var colList = new System.Collections.Generic.List<Color>();
            var triList = new System.Collections.Generic.List<int>();

            void Face(Vector3 a, Vector3 b, Vector3 c)
            {
                Vector3 fn = Vector3.Cross(b - a, c - a).normalized;
                int start = vList.Count;
                vList.Add(a); vList.Add(b); vList.Add(c);
                nList.Add(fn); nList.Add(fn); nList.Add(fn);
                uvList.Add(new Vector2(0.5f, 1f)); uvList.Add(new Vector2(0f, 0f)); uvList.Add(new Vector2(1f, 0f));
                colList.Add(Color.white); colList.Add(Color.white); colList.Add(Color.white);
                triList.Add(start); triList.Add(start + 1); triList.Add(start + 2);
            }

            void Quad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
            {
                Face(p0, p1, p2);
                Face(p0, p2, p3);
            }

            // --- 1. Dragon Body / Spine (connected rings along +Z forward) ---
            // Z goes from tail (-2.2) to snout (+1.8)
            var rings = new (float z, float y, float rx, float ry)[]
            {
                (-2.3f, 0.4f, 0.05f, 0.05f), // tail tip
                (-1.7f, 0.2f, 0.22f, 0.20f), // rear tail
                (-1.0f, 0.0f, 0.42f, 0.40f), // lower body / pelvis
                (-0.2f, 0.05f, 0.65f, 0.60f), // main chest / torso
                ( 0.5f, 0.25f, 0.48f, 0.48f), // upper chest / neck base
                ( 1.1f, 0.55f, 0.32f, 0.35f), // neck mid
                ( 1.5f, 0.70f, 0.40f, 0.38f), // head back / skull
                ( 1.9f, 0.55f, 0.25f, 0.22f), // snout mid
                ( 2.2f, 0.45f, 0.12f, 0.12f)  // snout tip
            };

            const int segCount = 6;
            var ringVerts = new Vector3[rings.Length, segCount];

            for (int r = 0; r < rings.Length; r++)
            {
                var ring = rings[r];
                for (int s = 0; s < segCount; s++)
                {
                    float angle = s * Mathf.PI * 2f / segCount;
                    float x = Mathf.Sin(angle) * ring.rx;
                    float y = ring.y + Mathf.Cos(angle) * ring.ry;
                    ringVerts[r, s] = new Vector3(x, y, ring.z);
                }
            }

            // Connect rings
            for (int r = 0; r < rings.Length - 1; r++)
            {
                for (int s = 0; s < segCount; s++)
                {
                    int next = (s + 1) % segCount;
                    Vector3 p0 = ringVerts[r, s];
                    Vector3 p1 = ringVerts[r, next];
                    Vector3 p2 = ringVerts[r + 1, next];
                    Vector3 p3 = ringVerts[r + 1, s];
                    Quad(p0, p1, p2, p3);
                }
            }

            // Cap tail tip
            Vector3 tailTip = new Vector3(0, rings[0].y, rings[0].z - 0.25f);
            for (int s = 0; s < segCount; s++)
            {
                int next = (s + 1) % segCount;
                Face(tailTip, ringVerts[0, next], ringVerts[0, s]);
            }

            // Cap snout tip
            Vector3 snoutTip = new Vector3(0, rings[rings.Length - 1].y, rings[rings.Length - 1].z + 0.25f);
            for (int s = 0; s < segCount; s++)
            {
                int next = (s + 1) % segCount;
                Face(snoutTip, ringVerts[rings.Length - 1, s], ringVerts[rings.Length - 1, next]);
            }

            // --- 2. Dragon Horns (left and right curving back-up from skull) ---
            Vector3 skullBaseL = new Vector3(-0.25f, 0.95f, 1.4f);
            Vector3 skullBaseR = new Vector3( 0.25f, 0.95f, 1.4f);
            Vector3 hornMidL   = new Vector3(-0.45f, 1.35f, 1.1f);
            Vector3 hornMidR   = new Vector3( 0.45f, 1.35f, 1.1f);
            Vector3 hornTipL   = new Vector3(-0.65f, 1.70f, 0.7f);
            Vector3 hornTipR   = new Vector3( 0.65f, 1.70f, 0.7f);

            void Horn(Vector3 b, Vector3 m, Vector3 t, float sign)
            {
                Vector3 w1 = b + new Vector3(sign * 0.12f, 0, 0);
                Vector3 w2 = b + new Vector3(0, 0, -0.15f);
                Vector3 w3 = b + new Vector3(0, 0, 0.15f);
                Quad(w1, w2, m, b);
                Quad(w2, w3, m, b);
                Quad(w3, w1, m, b);
                Face(m, w1, t);
                Face(m, w2, t);
                Face(m, w3, t);
            }
            Horn(skullBaseL, hornMidL, hornTipL, -1f);
            Horn(skullBaseR, hornMidR, hornTipR, 1f);

            // --- 3. Dragon Wings (membrane triangles with thickness) ---
            // Wing root at chest/torso, joint high and wide, tip extended, trailing edge to waist
            Vector3 rootL = new Vector3(-0.45f, 0.45f, 0.2f);
            Vector3 rootR = new Vector3( 0.45f, 0.45f, 0.2f);
            Vector3 jointL = new Vector3(-1.9f, 1.25f, 0.35f);
            Vector3 jointR = new Vector3( 1.9f, 1.25f, 0.35f);
            Vector3 tipL = new Vector3(-2.8f, 1.1f, -0.4f);
            Vector3 tipR = new Vector3( 2.8f, 1.1f, -0.4f);
            Vector3 trailL = new Vector3(-1.3f, 0.35f, -0.8f);
            Vector3 trailR = new Vector3( 1.3f, 0.35f, -0.8f);
            Vector3 waistL = new Vector3(-0.35f, 0.15f, -0.7f);
            Vector3 waistR = new Vector3( 0.35f, 0.15f, -0.7f);

            void Wing(Vector3 root, Vector3 joint, Vector3 tip, Vector3 trail, Vector3 waist, bool flip)
            {
                if (!flip)
                {
                    Quad(root, joint, tip, trail);
                    Face(root, trail, waist);
                    // Double-sided wing
                    Quad(trail, tip, joint, root);
                    Face(waist, trail, root);
                }
                else
                {
                    Quad(joint, root, trail, tip);
                    Face(trail, root, waist);
                    // Double-sided wing
                    Quad(tip, trail, root, joint);
                    Face(waist, root, trail);
                }
            }
            Wing(rootL, jointL, tipL, trailL, waistL, false);
            Wing(rootR, jointR, tipR, trailR, waistR, true);

            // --- 4. Dorsal Spikes along back ---
            for (int i = 0; i < 5; i++)
            {
                float sz = Mathf.Lerp(0.8f, -1.5f, i / 4f);
                float sy = 0.45f - Mathf.Abs(sz) * 0.1f;
                float spikeH = 0.32f;
                Vector3 sb1 = new Vector3(-0.08f, sy, sz - 0.08f);
                Vector3 sb2 = new Vector3( 0.08f, sy, sz - 0.08f);
                Vector3 sb3 = new Vector3( 0.08f, sy, sz + 0.08f);
                Vector3 sb4 = new Vector3(-0.08f, sy, sz + 0.08f);
                Vector3 spt = new Vector3(0, sy + spikeH, sz);
                Face(spt, sb1, sb2);
                Face(spt, sb2, sb3);
                Face(spt, sb3, sb4);
                Face(spt, sb4, sb1);
            }

            var m = new Mesh
            {
                name = "Dragon",
                vertices = vList.ToArray(),
                normals = nList.ToArray(),
                uv = uvList.ToArray(),
                colors = colList.ToArray(),
                triangles = triList.ToArray()
            };
            m.RecalculateBounds();
            return m;
        }
    }
}
