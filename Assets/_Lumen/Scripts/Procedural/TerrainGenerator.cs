using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 6 - Procedural terrain mesh from fractal Perlin noise.
    /// Vertex grid res x res; height y = fBm(x,z) * amplitude * falloff (edges are raised into walls so the orb stays inside).
    /// Normals are computed by hand: for every triangle n = (b-a) x (c-a); the face normal is added to its three
    /// vertices and the sum normalised (area-weighted smooth normals). UVs = grid coordinates; colour = height gradient.
    /// </summary>
    public static class TerrainGenerator
    {
        public struct Settings
        {
            public int Resolution;     // vertices per side
            public float Size;         // world size
            public float Amplitude;
            public float Frequency;
            public int Octaves;
            public float Lacunarity;
            public float Persistence;
            public int Seed;

            public static Settings Default(int seed) => new Settings
            {
                Resolution = 200, Size = 160f, Amplitude = 9f, Frequency = 0.035f,
                Octaves = 5, Lacunarity = 2f, Persistence = 0.5f, Seed = seed
            };
        }

        public static float[] LastHeights;
        public static int LastRes;
        public static float LastSize;

        /// <summary>Terrain height (world Y) at world XZ using the last generated grid (bilinear).</summary>
        public static float HeightAt(float x, float z)
        {
            if (LastHeights == null) return 0f;
            float fx = (x / LastSize + 0.5f) * (LastRes - 1), fz = (z / LastSize + 0.5f) * (LastRes - 1);
            fx = Mathf.Clamp(fx, 0, LastRes - 1.001f); fz = Mathf.Clamp(fz, 0, LastRes - 1.001f);
            int ix = (int)fx, iz = (int)fz; float tx = fx - ix, tz = fz - iz;
            float h00 = LastHeights[iz * LastRes + ix], h10 = LastHeights[iz * LastRes + ix + 1];
            float h01 = LastHeights[(iz + 1) * LastRes + ix], h11 = LastHeights[(iz + 1) * LastRes + ix + 1];
            return Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), tz);
        }

        public static Mesh Generate(Settings s)
        {
            int res = s.Resolution;
            var noise = new PerlinNoise(s.Seed);
            var verts = new Vector3[res * res];
            var uvs = new Vector2[res * res];
            var cols = new Color[res * res];
            var heights = new float[res * res];
            float step = s.Size / (res - 1), half = s.Size * 0.5f;
            float ox = (s.Seed % 1000) * 0.37f, oz = (s.Seed % 777) * 0.51f;

            for (int z = 0; z < res; z++)
                for (int x = 0; x < res; x++)
                {
                    float wx = x * step - half, wz = z * step - half;
                    float n = noise.Fractal((wx + ox) * s.Frequency, (wz + oz) * s.Frequency, s.Octaves, s.Lacunarity, s.Persistence);
                    // flatten the middle slightly so the start area is easy, raise the rim into a wall
                    float r = Mathf.Sqrt(wx * wx + wz * wz) / half;
                    float rim = Mathf.Clamp01((r - 0.82f) / 0.18f);
                    float h = (n - 0.45f) * 2f * s.Amplitude * (1f - 0.5f * Mathf.Clamp01(1f - r * 3f)) + rim * rim * 14f;
                    int i = z * res + x;
                    heights[i] = h;
                    verts[i] = new Vector3(wx, h, wz);
                    uvs[i] = new Vector2(x / (float)(res - 1), z / (float)(res - 1));
                }

            for (int i = 0; i < cols.Length; i++) cols[i] = HeightColor(Mathf.InverseLerp(-s.Amplitude * 0.9f, s.Amplitude * 1.2f, heights[i]));

            var tris = new int[(res - 1) * (res - 1) * 6];
            int t = 0;
            for (int z = 0; z < res - 1; z++)
                for (int x = 0; x < res - 1; x++)
                {
                    int a = z * res + x, b = a + 1, c = a + res, d = c + 1;
                    tris[t++] = a; tris[t++] = c; tris[t++] = b;
                    tris[t++] = b; tris[t++] = c; tris[t++] = d;
                }

            var normals = new Vector3[verts.Length];
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 pa = verts[tris[i]], pb = verts[tris[i + 1]], pc = verts[tris[i + 2]];
                Vector3 fn = Vector3.Cross(pb - pa, pc - pa);        // length = 2*area -> area weighting for free
                normals[tris[i]] += fn; normals[tris[i + 1]] += fn; normals[tris[i + 2]] += fn;
            }
            for (int i = 0; i < normals.Length; i++) normals[i] = normals[i].normalized;

            LastHeights = heights; LastRes = res; LastSize = s.Size;

            var mesh = new Mesh { name = "LumenTerrain", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = verts; mesh.normals = normals; mesh.uv = uvs; mesh.colors = cols; mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Synthwave gradient: deep indigo valleys -> magenta slopes -> cyan peaks.</summary>
        static Color HeightColor(float t)
        {
            Color lo = new Color(0.10f, 0.04f, 0.28f), mid = new Color(0.62f, 0.10f, 0.55f), hi = new Color(0.20f, 0.75f, 0.95f);
            return t < 0.5f ? Color.Lerp(lo, mid, t * 2f) : Color.Lerp(mid, hi, (t - 0.5f) * 2f);
        }
    }
}
