using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPTS 8, 9, 13 - debug rendering views on the camera:
    ///   F wireframe (barycentric geometry shader overlay), N vertex normals as short lines, Z linearised depth buffer,
    ///   H HSV hue-shift (also fired automatically when the player is hit). Post-processing volume is created here too.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class DebugViews : MonoBehaviour
    {
        public static DebugViews I { get; private set; }

        public bool Wireframe, Normals, DepthView, HueShift;
        public PostFX Post { get; private set; }

        Camera cam;
        MeshRenderer hueQuad;
        Color clearColor;
        bool depthApplied;
        Mesh normalMesh;
        Vector3[] verts, norms; Mesh cachedFor;
        Vector3 lastNormalCenter = new Vector3(1e9f, 0, 0);
        float hitTimer;
        public int NormalLineCount { get; private set; }

        void Awake() { I = this; cam = GetComponent<Camera>(); }

        void Start()
        {
            Post = PostFX.Setup(cam);
            var res = LumenResources.I;
            var quad = MeshFactory.FullscreenQuad();
            clearColor = cam.backgroundColor;
            hueQuad = MakeQuad("HueShiftFX", quad, res.hue);
            normalMesh = new Mesh { name = "NormalLines" };
            normalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        MeshRenderer MakeQuad(string name, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, 0, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.enabled = false;
            return mr;
        }

        /// <summary>Z-buffer view: LumenLit prints linearised hardware depth; bloom/vignette/background are muted so grey levels are exact.</summary>
        void ApplyDepthView()
        {
            Shader.SetGlobalFloat("_LumenDepthView", DepthView ? 1f : 0f);
            Shader.SetGlobalFloat("_LumenDepthFar", 55f);
            if (DepthView == depthApplied) return;
            depthApplied = DepthView;
            cam.backgroundColor = DepthView ? Color.black : clearColor;
            if (Post != null) { if (DepthView) Post.SetEnabled(false, false, false); else Post.SetEnabled(true, true, true); }
        }

        public void TriggerHit() { hitTimer = 1.1f; }

        void Update()
        {
            if (LumenInput.Pressed(Key.F)) Wireframe = !Wireframe;
            if (LumenInput.Pressed(Key.N)) Normals = !Normals;
            if (LumenInput.Pressed(Key.Z)) DepthView = !DepthView;
            if (LumenInput.Pressed(Key.H)) HueShift = !HueShift;

            if (hitTimer > 0f) hitTimer -= Time.unscaledDeltaTime;

            ApplyDepthView();
            bool hue = HueShift || hitTimer > 0f;
            hueQuad.enabled = hue && !DepthView;
            if (hue)
            {
                var m = LumenResources.I.hue;
                float strength = HueShift ? 1f : Mathf.Clamp01(hitTimer / 0.4f);
                m.SetFloat("_Strength", strength);
                m.SetFloat("_HueDegrees", Time.unscaledTime * 360f);
                m.SetFloat("_SatBoost", 1.5f);
            }

            var wb = WorldBuilder.I;
            if (wb == null || wb.TerrainMesh == null) return;
            if (Wireframe) DrawWireframe(wb);
            if (Normals) DrawNormals(wb);
        }

        void DrawWireframe(WorldBuilder wb)
        {
            var wire = LumenResources.I.wire;
            Graphics.DrawMesh(wb.TerrainMesh, wb.TerrainTf.localToWorldMatrix, wire, 0);
            foreach (var s in wb.Sentinels)
            {
                if (s == null) continue;
                var mf = s.GetComponent<MeshFilter>();
                if (mf != null) Graphics.DrawMesh(mf.sharedMesh, s.transform.localToWorldMatrix, wire, 0);
            }
            foreach (var sh in wb.Shards)
                if (sh != null && !sh.Collected) Graphics.DrawMesh(sh.mesh, sh.CurrentMatrix, wire, 0);
        }

        /// <summary>Short lines along the smooth vertex normals of the terrain patch near the player.</summary>
        void DrawNormals(WorldBuilder wb)
        {
            if (cachedFor != wb.TerrainMesh) { verts = wb.TerrainMesh.vertices; norms = wb.TerrainMesh.normals; cachedFor = wb.TerrainMesh; lastNormalCenter = new Vector3(1e9f, 0, 0); }
            Vector3 c = wb.Orb.position;
            if ((c - lastNormalCenter).sqrMagnitude > 1f) { RebuildNormals(c); lastNormalCenter = c; }
            if (NormalLineCount > 0) Graphics.DrawMesh(normalMesh, Matrix4x4.identity, LumenResources.I.line, 0);
        }

        void RebuildNormals(Vector3 c)
        {
            int res = TerrainGenerator.LastRes;
            float step = TerrainGenerator.LastSize / (res - 1);
            int cx = Mathf.RoundToInt((c.x / TerrainGenerator.LastSize + 0.5f) * (res - 1));
            int cz = Mathf.RoundToInt((c.z / TerrainGenerator.LastSize + 0.5f) * (res - 1));
            int r = Mathf.RoundToInt(16f / step);
            var v = new List<Vector3>(); var col = new List<Color>(); var idx = new List<int>();
            for (int z = Mathf.Max(0, cz - r); z <= Mathf.Min(res - 1, cz + r); z += 2)
                for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(res - 1, cx + r); x += 2)
                {
                    int i = z * res + x;
                    idx.Add(v.Count); idx.Add(v.Count + 1);
                    v.Add(verts[i]); v.Add(verts[i] + norms[i] * 1.3f);
                    col.Add(new Color(0.2f, 1f, 1f, 0.9f)); col.Add(new Color(1f, 1f, 0.3f, 0.9f));
                }
            normalMesh.Clear();
            normalMesh.SetVertices(v);
            normalMesh.SetColors(col);
            normalMesh.SetIndices(idx, MeshTopology.Lines, 0);
            normalMesh.bounds = new Bounds(c, Vector3.one * 100f);
            NormalLineCount = idx.Count / 2;
        }
    }
}
