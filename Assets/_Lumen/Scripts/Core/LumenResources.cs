using System.Collections.Generic;
using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// Holds references to the material assets (so shaders are included in builds) and hands out runtime CLONES,
    /// which keeps the .mat assets clean while the Lab Panel changes values.
    /// </summary>
    public class LumenResources : MonoBehaviour
    {
        public static LumenResources I { get; private set; }

        [Header("Material assets (cloned at Awake)")]
        public Material terrain, orb, shard, sentinel, portal, wire, hue, grid, particle, line;

        public Texture2D GridTex { get; private set; }
        public Texture2D DotTex { get; private set; }
        public readonly List<Material> LitMaterials = new List<Material>();

        void Awake()
        {
            I = this;
            GridTex = ProceduralTextures.NeonGrid();
            DotTex = ProceduralTextures.SoftDot();

            terrain = Clone(terrain); orb = Clone(orb); shard = Clone(shard); sentinel = Clone(sentinel);
            portal = Clone(portal); wire = Clone(wire); hue = Clone(hue);
            grid = Clone(grid); particle = Clone(particle); line = Clone(line);

            terrain.SetTexture("_MainTex", GridTex);
            grid.SetTexture("_MainTex", GridTex);
            particle.SetTexture("_MainTex", DotTex);
            RegisterLit(terrain); RegisterLit(orb); RegisterLit(shard); RegisterLit(sentinel);
        }

        static Material Clone(Material m) => m != null ? new Material(m) { name = m.name + " (runtime)" } : null;

        public void RegisterLit(Material m) { if (m != null && !LitMaterials.Contains(m)) LitMaterials.Add(m); }

        /// <summary>A fresh lit material (e.g. one per Sentinel so each can tint independently).</summary>
        public Material NewLit(Material source)
        {
            var m = new Material(source);
            RegisterLit(m);
            return m;
        }
    }
}
