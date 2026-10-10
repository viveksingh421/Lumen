using UnityEngine;
using UnityEngine.InputSystem;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 5 - 'B' toggles drawing of every Sentinel's Bezier chain (LineRenderer sampled with de Casteljau),
    /// its control polygon and control points (yellow = anchors on the curve, magenta = off-curve handles),
    /// plus the Catmull-Rom intro camera path (green).
    /// </summary>
    public class CurveVisualizer : MonoBehaviour
    {
        public static CurveVisualizer I { get; private set; }
        public bool Visible;
        Transform root;

        void Awake() { I = this; }

        void Update()
        {
            if (LumenInput.Pressed(Key.B)) Visible = !Visible;
            if (root != null && root.gameObject.activeSelf != Visible) root.gameObject.SetActive(Visible);
        }

        public void Rebuild()
        {
            if (root != null) Destroy(root.gameObject);
            root = new GameObject("CurveViz").transform;
            root.gameObject.SetActive(Visible);
            var wb = WorldBuilder.I;
            if (wb == null) return;

            foreach (var s in wb.Sentinels)
            {
                if (s == null || s.Chain == null) continue;
                int n = (s.Chain.Length - 1) / 3;
                var curve = new Vector3[n * 24 + 1];
                for (int i = 0; i < curve.Length; i++) curve[i] = Bezier.EvaluateChain(s.Chain, i / 24f);
                Line("BezierCurve", curve, new Color(1f, 0.35f, 0.3f, 1f), 0.22f);
                Line("ControlPolygon", s.Chain, new Color(0.7f, 0.7f, 0.9f, 0.55f), 0.07f);
                for (int i = 0; i < s.Chain.Length; i++)
                    Dot(s.Chain[i], i % 3 == 0 ? new Color(1f, 0.95f, 0.2f, 1f) : new Color(1f, 0.3f, 0.9f, 1f), i % 3 == 0 ? 0.7f : 0.45f);
            }

            var gm = GameManager.I;
            if (gm != null && gm.IntroPath != null)
            {
                var pts = new Vector3[(gm.IntroPath.Length - 1) * 16 + 1];
                for (int i = 0; i < pts.Length; i++) pts[i] = CatmullRom.EvaluatePath(gm.IntroPath, i / 16f);
                Line("CatmullRom", pts, new Color(0.3f, 1f, 0.5f, 1f), 0.25f);
                foreach (var p in gm.IntroPath) Dot(p, new Color(0.3f, 1f, 0.5f, 1f), 0.9f);
            }
        }

        void Line(string name, Vector3[] pts, Color c, float width)
        {
            var go = new GameObject(name); go.transform.SetParent(root, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = pts.Length; lr.SetPositions(pts);
            lr.widthMultiplier = width;
            lr.startColor = lr.endColor = c;
            lr.numCornerVertices = 2;
            lr.sharedMaterial = LumenResources.I.line;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void Dot(Vector3 p, Color c, float size)
        {
            var go = new GameObject("Point"); go.transform.SetParent(root, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true; lr.positionCount = 2;
            lr.SetPosition(0, p); lr.SetPosition(1, p + Vector3.up * 0.02f);
            lr.widthMultiplier = size; lr.numCapVertices = 8;
            lr.startColor = lr.endColor = c;
            lr.sharedMaterial = LumenResources.I.line;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
