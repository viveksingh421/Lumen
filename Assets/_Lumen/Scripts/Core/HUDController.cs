using UnityEngine;

namespace Lumen
{
    /// <summary>Immediate-mode HUD: shards, timer, health, radar, mode labels, FPS, pause / win / lose screens.</summary>
    public class HUDController : MonoBehaviour
    {
        public RadarRenderer radar;
        GUIStyle label, big, btn, small, title, center;
        float fps, fpsAccum; int fpsFrames;

        void Update()
        {
            fpsAccum += Time.unscaledDeltaTime; fpsFrames++;
            if (fpsAccum >= 0.4f) { fps = fpsFrames / fpsAccum; fpsAccum = 0; fpsFrames = 0; }
        }

        void Styles(float s)
        {
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                big = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                title = new GUIStyle(big);
                small = new GUIStyle(GUI.skin.label);
                center = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
                btn = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            }
            label.wordWrap = false; big.wordWrap = false; small.wordWrap = false; center.fontSize = Mathf.RoundToInt(13 * s);
            label.fontSize = Mathf.RoundToInt(20 * s); big.fontSize = Mathf.RoundToInt(26 * s);
            title.fontSize = Mathf.RoundToInt(54 * s); small.fontSize = Mathf.RoundToInt(13 * s); btn.fontSize = Mathf.RoundToInt(22 * s);
        }

        static void Shadowed(Rect r, string text, GUIStyle st, Color c)
        {
            var old = GUI.color;
            st.normal.textColor = Color.black; GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, st);
            st.normal.textColor = c; GUI.color = old;
            GUI.Label(r, text, st);
        }

        void OnGUI()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            float s = Screen.height / 720f;
            Styles(s);

            DrawHud(gm, s);

            if (gm.HitFlash > 0f)
            {
                var old = GUI.color; GUI.color = new Color(1f, 0.1f, 0.1f, 0.35f * gm.HitFlash);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = old;
            }

            switch (gm.State)
            {
                case GameManager.GameState.Paused: DrawPause(gm, s); break;
                case GameManager.GameState.Won: DrawEnd(gm, s, true); break;
                case GameManager.GameState.Lost: DrawEnd(gm, s, false); break;
            }
        }

        void DrawHud(GameManager gm, float s)
        {
            float pad = 14 * s;
            // top-left stats
            int t = Mathf.CeilToInt(gm.TimeLeft);
            Color tc = gm.TimeLeft < 30f ? new Color(1f, 0.35f, 0.3f) : Color.white;
            Shadowed(new Rect(pad, pad, 360 * s, 30 * s), "SHARDS  " + gm.ShardsCollected + " / " + WorldBuilder.ShardCount, label, new Color(1f, 0.92f, 0.4f));
            Shadowed(new Rect(pad, pad + 30 * s, 360 * s, 30 * s), "TIME  " + (t / 60) + ":" + (t % 60).ToString("00"), label, tc);
            string hp = "";
            for (int i = 0; i < GameManager.MaxHealth; i++) hp += i < gm.Health ? "■ " : "□ ";
            Shadowed(new Rect(pad, pad + 60 * s, 360 * s, 30 * s), "HEALTH  " + hp, label, new Color(1f, 0.45f, 0.65f));
            var wb = WorldBuilder.I;
            Shadowed(new Rect(pad, pad + 92 * s, 400 * s, 20 * s), "Seed " + (wb != null ? wb.Seed : 0) + "    FPS " + Mathf.RoundToInt(fps), small, new Color(0.8f, 0.85f, 1f));

            // top-centre mode labels
            var sh = ShadingModeController.I; var pr = ProjectionController.I; var dv = DebugViews.I;
            string modes = "Shading: " + (sh != null ? ShadingModeController.ModeNames[(int)sh.Current] : "-") + "   |   Projection: " + (pr != null ? pr.Label : "-");
            string flags = (dv != null && dv.Wireframe ? "[Wire] " : "") + (dv != null && dv.Normals ? "[Normals] " : "") + (dv != null && dv.DepthView ? "[Z-buffer] " : "")
                         + (CurveVisualizer.I != null && CurveVisualizer.I.Visible ? "[Bezier] " : "");
            float tw = Mathf.Min(Screen.width - 520 * s, 1000 * s);
            Shadowed(new Rect(Screen.width * 0.5f - tw * 0.5f, pad, tw, 32 * s), modes, big, Color.white);
            if (flags.Length > 0) Shadowed(new Rect(Screen.width * 0.5f - tw * 0.5f, pad + 32 * s, tw, 24 * s), flags, center, new Color(0.5f, 1f, 1f));

            // radar
            if (radar != null && radar.Texture != null)
            {
                float rs = 210 * s;
                Rect rr = new Rect(Screen.width - rs - pad, pad, rs, rs);
                GUI.DrawTexture(rr, radar.Texture, ScaleMode.StretchToFill, false);
                string al = radar.UseBresenham ? "Bresenham" : "DDA";
                Shadowed(new Rect(rr.x, rr.yMax, rs, 20 * s), "RADAR  " + al + (radar.ShowUnclipped ? "  (unclipped)" : "  (clipped)"), small, new Color(0.6f, 0.8f, 1f));
            }

            // toast
            if (!string.IsNullOrEmpty(gm.ToastText))
                Shadowed(new Rect(0, Screen.height * 0.2f, Screen.width, 40 * s), gm.ToastText, big, new Color(0.7f, 1f, 1f));

            if (gm.State == GameManager.GameState.Playing)
            {
                float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
                var old = GUI.color; GUI.color = new Color(1, 1, 1, 0.6f);
                GUI.DrawTexture(new Rect(cx - 1, cy - 8 * s, 2, 16 * s), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(cx - 8 * s, cy - 1, 16 * s, 2), Texture2D.whiteTexture);
                GUI.color = old;
            }

            if (gm.State == GameManager.GameState.Intro)
                Shadowed(new Rect(0, Screen.height - 70 * s, Screen.width, 40 * s), "SPACE / click to skip the intro", big, Color.white);

            Shadowed(new Rect(pad, Screen.height - 26 * s, 900 * s, 20 * s),
                "WASD move  Space jump/boost  LMB pulse  Mouse orbit  Tab Lab  1-5 shading  P projection  F wire  N normals  Z depth  B bezier  H hue  Esc pause", small, new Color(0.7f, 0.75f, 1f, 0.9f));
        }

        void Dim()
        {
            var old = GUI.color; GUI.color = new Color(0.02f, 0, 0.08f, 0.72f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = old;
        }

        void DrawPause(GameManager gm, float s)
        {
            Dim();
            float w = 320 * s, h = 54 * s, x = (Screen.width - w) * 0.5f, y = Screen.height * 0.34f;
            Shadowed(new Rect(0, y - 90 * s, Screen.width, 70 * s), "PAUSED", title, new Color(0.6f, 1f, 1f));
            if (GUI.Button(new Rect(x, y, w, h), "Resume", btn)) gm.Resume();
            if (GUI.Button(new Rect(x, y + h + 12 * s, w, h), "Restart", btn)) gm.Restart();
            if (GUI.Button(new Rect(x, y + 2 * (h + 12 * s), w, h), "Main Menu", btn)) gm.ToMenu();
        }

        void DrawEnd(GameManager gm, float s, bool won)
        {
            Dim();
            float w = 320 * s, h = 54 * s, x = (Screen.width - w) * 0.5f, y = Screen.height * 0.5f;
            Shadowed(new Rect(0, Screen.height * 0.16f, Screen.width, 80 * s), won ? "YOU ESCAPED!" : (gm.Health <= 0 ? "CAUGHT BY A SENTINEL" : "OUT OF TIME"), title, won ? new Color(0.5f, 1f, 0.7f) : new Color(1f, 0.4f, 0.4f));
            Shadowed(new Rect(0, Screen.height * 0.30f, Screen.width, 40 * s), "Score  " + gm.Score, big, new Color(1f, 0.92f, 0.4f));
            Shadowed(new Rect(0, Screen.height * 0.30f + 38 * s, Screen.width, 30 * s),
                "= time left " + Mathf.CeilToInt(gm.TimeLeft) + " s x 10   +   shards " + gm.ShardsCollected + " x 100", center, Color.white);
            if (GUI.Button(new Rect(x, y, w, h), "Restart", btn)) gm.Restart();
            if (GUI.Button(new Rect(x, y + h + 12 * s, w, h), "Main Menu", btn)) gm.ToMenu();
        }
    }
}
