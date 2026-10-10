using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 6 - Gradient (Perlin) noise, hand implemented, plus fractal Brownian motion.
    ///
    /// 1. Space is divided into an integer lattice; each lattice corner has a pseudo-random unit gradient (from a permutation table).
    /// 2. For a sample point, take the dot product of each corner gradient with the vector corner->point.
    /// 3. Blend the four (2D) results with the quintic fade  f(t) = 6t^5 - 15t^4 + 10t^3  (C2-continuous).
    ///
    /// fBm sums octaves:  sum_i persistence^i * noise(p * lacunarity^i)
    /// </summary>
    public class PerlinNoise
    {
        readonly int[] perm = new int[512];

        public PerlinNoise(int seed)
        {
            var p = new int[256];
            for (int i = 0; i < 256; i++) p[i] = i;
            var rng = new System.Random(seed);
            for (int i = 255; i > 0; i--) { int j = rng.Next(i + 1); (p[i], p[j]) = (p[j], p[i]); }
            for (int i = 0; i < 512; i++) perm[i] = p[i & 255];
        }

        static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

        // 8 evenly spaced unit gradients selected by hash
        static float Grad(int hash, float x, float y)
        {
            switch (hash & 7)
            {
                case 0: return x + y;
                case 1: return -x + y;
                case 2: return x - y;
                case 3: return -x - y;
                case 4: return x;
                case 5: return -x;
                case 6: return y;
                default: return -y;
            }
        }

        /// <summary>Single octave, output roughly in [-1,1].</summary>
        public float Noise(float x, float y)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float xf = x - xi, yf = y - yi;
            xi &= 255; yi &= 255;
            float u = Fade(xf), v = Fade(yf);
            int aa = perm[perm[xi] + yi], ab = perm[perm[xi] + yi + 1];
            int ba = perm[perm[xi + 1] + yi], bb = perm[perm[xi + 1] + yi + 1];
            float x1 = Mathf.Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
            float x2 = Mathf.Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);
            return Mathf.Lerp(x1, x2, v);
        }

        /// <summary>Fractal noise normalised to roughly [0,1].</summary>
        public float Fractal(float x, float y, int octaves, float lacunarity, float persistence)
        {
            float amp = 1f, freq = 1f, sum = 0f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Noise(x * freq, y * freq) * amp;
                norm += amp;
                amp *= persistence;
                freq *= lacunarity;
            }
            return Mathf.Clamp01(sum / norm * 0.5f + 0.5f);
        }
    }
}
