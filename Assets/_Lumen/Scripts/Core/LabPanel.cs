using UnityEngine;
using UnityEngine.InputSystem;

namespace Lumen
{
    /// <summary>
    /// The CG Lab Panel (Tab). A semi-transparent side panel to switch rendering techniques live and read algorithm stats.
    /// While open, GameManager slows time to 0.25 so the evaluator can watch the effect.
    /// </summary>
    public class LabPanel : MonoBehaviour
    {
        public static LabPanel I { get; private set; }

        public bool Open;
        public RadarRenderer radar;

        Vector2 scroll;
        GUIStyle head, txt, mono, btn, toggle, slider;
        bool bloomOn = true, vignetteOn = true, gradeOn = true;

        void Awake() { I = this; }

        void Update()
        {
            var gm = GameManager.I;
            if (gm != null && (gm.State == GameManager.GameState.Won || gm.State == GameManager.GameState.Lost)) { Open = false; return; }
            if (LumenInput.Pressed(Key.Tab)) Open = !Open;
            if (Open && LumenInput.Pressed(Key.R) && gm != null) gm.Regenerate();
        }

        void MakeStyles(float s)
        {
            if (head == null)
            {
                head = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                txt = new GUIStyle(GUI.skin.label) { wordWrap = true };
                mono = new GUIStyle(GUI.skin.label);
                try { mono.font = Font.CreateDynamicFontFromOSFont("Consolas", 12); } catch { /* fall back to default font */ }
                btn = new GUIStyle(GUI.skin.button);
                toggle = new GUIStyle(GUI.skin.toggle);
                slider = new GUIStyle(GUI.skin.horizontalSlider);
            }
            int f = Mathf.RoundToInt(14 * s);
            head.fontSize = Mathf.RoundToInt(16 * s); head.normal.textColor = new Color(0.5f, 1f, 1f);
            txt.fontSize = f; txt.normal.textColor = Color.white;
            mono.fontSize = Mathf.RoundToInt(13 * s); mono.normal.textColor = new Color(0.8f, 1f, 0.8f);
            btn.fontSize = f; toggle.fontSize = f; toggle.normal.textColor = Color.white; toggle.onNormal.textColor = new Color(0.5f, 1f, 1f);
        }

        void OnGUI()
        {
            if (!Open) return;
            float s = Screen.height / 720f;
            MakeStyles(s);

            float w = 390 * s, top = 125 * s;
            Rect area = new Rect(10 * s, top, w, Screen.height - top - 34 * s);
            var old = GUI.color; GUI.color = new Color(0.03f, 0.02f, 0.12f, 0.82f);
            GUI.DrawTexture(area, Texture2D.whiteTexture); GUI.color = old;

            GUILayout.BeginArea(new Rect(area.x + 8 * s, area.y + 6 * s, area.width - 16 * s, area.height - 12 * s));
            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label("CG LAB PANEL   (Tab to close, time x0.25)", head);

            // ---------------- shading
            var sh = ShadingModeController.I;
            if (sh != null)
            {
                GUILayout.Label("Illumination / shading  (keys 1-5)", head);
                int m = GUILayout.SelectionGrid((int)sh.Current, ShadingModeController.ModeNames, 3, btn);
                if (m != (int)sh.Current) sh.SetMode((ShadingModeController.Mode)m);
                sh.ka = Slider("ka ambient", sh.ka, 0f, 2f);
                sh.kd = Slider("kd diffuse", sh.kd, 0f, 2f);
                sh.ks = Slider("ks specular", sh.ks, 0f, 2f);
                sh.shininess = Slider("shininess x", sh.shininess, 0.1f, 4f);
                bool cull = GUILayout.Toggle(sh.cullBack, " Back-face culling (Cull Back / Off)", toggle);
                if (cull != sh.cullBack) { sh.cullBack = cull; sh.ApplyCull(); }
                sh.pointIntensity = Slider("orb light intensity", sh.pointIntensity, 0f, 8f);
            }

            // ---------------- projection
            var pr = ProjectionController.I;
            if (pr != null)
            {
                GUILayout.Space(6);
                GUILayout.Label("Projection  (key P)", head);
                bool ortho = GUILayout.Toggle(pr.TargetOrtho, " Orthographic   [" + pr.Label + "]", toggle);
                if (ortho != pr.TargetOrtho) pr.SetOrtho(ortho);
                pr.fov = Slider("FOV (deg)", pr.fov, 20f, 110f);
            }

            // ---------------- raster / clipping
            if (radar != null)
            {
                GUILayout.Space(6);
                GUILayout.Label("Radar rasterisation & clipping", head);
                radar.UseBresenham = GUILayout.Toggle(radar.UseBresenham, " Bresenham lines (off = DDA)", toggle);
                radar.ShowUnclipped = GUILayout.Toggle(radar.ShowUnclipped, " Show UNCLIPPED geometry (clipping off)", toggle);
                GUILayout.Label("active algo this frame: " + radar.LastPixels + " px total, lines " + radar.LastLineMicros.ToString("F0") + " us", txt);
                GUILayout.Label("benchmark: 400 random lines\n DDA        " + radar.DdaPixels + " px   " + radar.DdaMicros.ToString("F0") + " us\n Bresenham  " + radar.BresPixels + " px   " + radar.BresMicros.ToString("F0") + " us", mono);
            }

            // ---------------- debug views
            var dv = DebugViews.I;
            if (dv != null)
            {
                GUILayout.Space(6);
                GUILayout.Label("Debug views", head);
                dv.Wireframe = GUILayout.Toggle(dv.Wireframe, " Wireframe  (F)", toggle);
                dv.Normals = GUILayout.Toggle(dv.Normals, " Vertex normals  (N)   lines: " + dv.NormalLineCount, toggle);
                dv.DepthView = GUILayout.Toggle(dv.DepthView, " Z-buffer / depth view  (Z)", toggle);
                dv.HueShift = GUILayout.Toggle(dv.HueShift, " RGB->HSV hue shift  (H)", toggle);
                if (CurveVisualizer.I != null) CurveVisualizer.I.Visible = GUILayout.Toggle(CurveVisualizer.I.Visible, " Bezier / Catmull-Rom curves  (B)", toggle);
                if (dv.Post != null)
                {
                    bool b = GUILayout.Toggle(bloomOn, " Bloom", toggle), v = GUILayout.Toggle(vignetteOn, " Vignette", toggle), g = GUILayout.Toggle(gradeOn, " Colour grading", toggle);
                    if (b != bloomOn || v != vignetteOn || g != gradeOn) { bloomOn = b; vignetteOn = v; gradeOn = g; dv.Post.SetEnabled(b, v, g); }
                }
            }

            // ---------------- world
            var wb = WorldBuilder.I;
            if (wb != null)
            {
                GUILayout.Space(6);
                GUILayout.Label("Procedural world", head);
                GUILayout.Label("seed " + wb.Seed + "   terrain " + TerrainGenerator.LastRes + "x" + TerrainGenerator.LastRes + " verts (" + (TerrainGenerator.LastRes * TerrainGenerator.LastRes) + ")", txt);
                if (GUILayout.Button("Regenerate world with new seed  (R)", btn)) GameManager.I.Regenerate();

                GUILayout.Space(6);
                GUILayout.Label("Transformation matrix of nearest shard", head);
                Shard near = wb.Orb != null ? wb.NearestShard(wb.Orb.position) : null;
                GUILayout.Label(near != null ? "M = T * Ry(spin) * Rx(tilt) * S(pulse)\n" + MatrixMath.Format(near.CurrentMatrix) : "(no shard left)", mono);

                GUILayout.Space(6);
                GUILayout.Label("Ray casting (light pulse)", head);
                if (wb.pulse != null)
                    GUILayout.Label("last: " + wb.pulse.LastResult + "\norigin " + wb.pulse.LastRayOrigin.ToString("F1") + "\ndir    " + wb.pulse.LastRayDir.ToString("F2"), mono);
                foreach (var sn in wb.Sentinels) if (sn != null) GUILayout.Label("Sentinel  " + sn.Current + "  speed ~" + (sn.baseSpeed * (1f + 0.09f * GameManager.I.ShardsCollected)).ToString("F1"), txt);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        float Slider(string label, float v, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, txt, GUILayout.Width(130 * Screen.height / 720f));
            v = GUILayout.HorizontalSlider(v, min, max, GUILayout.ExpandWidth(true));
            GUILayout.Label(v.ToString("F2"), txt, GUILayout.Width(46 * Screen.height / 720f));
            GUILayout.EndHorizontal();
            return v;
        }
    }
}
