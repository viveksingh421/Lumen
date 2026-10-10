using UnityEngine;

namespace Lumen
{
    /// <summary>CG CONCEPT 6 - textures generated in code (no image assets).</summary>
    public static class ProceduralTextures
    {
        /// <summary>Neon grid: dark cells with bright lines at the borders, softly anti-aliased by distance to the edge.</summary>
        public static Texture2D NeonGrid(int size = 256, int cells = 4)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "NeonGrid", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8 };
            var px = new Color32[size * size];
            float cell = size / (float)cells;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float fx = (x % cell) / cell, fy = (y % cell) / cell;
                    float d = Mathf.Min(Mathf.Min(fx, 1 - fx), Mathf.Min(fy, 1 - fy)) * cell;   // pixels to nearest line
                    float line = Mathf.Clamp01(1.6f - d * 0.8f);
                    line *= line;
                    float v = 0.10f + 0.9f * line;
                    px[y * size + x] = new Color32((byte)(v * 255), (byte)(v * 255), (byte)(v * 255), 255);
                }
            tex.SetPixels32(px);
            tex.Apply(true);
            return tex;
        }

        /// <summary>Soft round sprite used by all particle systems.</summary>
        public static Texture2D SoftDot(int size = 64)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "SoftDot", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2 - 1, dy = (y + 0.5f) / size * 2 - 1;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    a *= a;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }
    }
}
