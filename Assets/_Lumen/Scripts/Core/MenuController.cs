using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lumen
{
    /// <summary>Main menu: animated glowing title, Play / Controls / CG Concepts / Quit, spinning matrix-driven crystals as backdrop.</summary>
    public class MenuController : MonoBehaviour
    {
        enum Page { Main, Controls, Concepts }
        Page page = Page.Main;
        GUIStyle title, btn, body, head;
        Mesh crystal;
        Camera cam;

        static readonly string[] concepts =
        {
            "1  Geometric transformations - hand-built 4x4 matrices (translate, scale, rotate, shear, reflect, TRS) animate every shard.",
            "2  Viewing & projection - perspective/orthographic matrices built by hand, morphed with a matrix lerp (P).",
            "3  Scan conversion - DDA, Bresenham, midpoint circle/ellipse and scan-line fill draw the radar pixel by pixel.",
            "4  Clipping - Cohen-Sutherland (lines) and Sutherland-Hodgman (polygons) clip radar geometry to its window.",
            "5  Curves - cubic Bezier (de Casteljau & Bernstein) patrol loops, Catmull-Rom intro camera path (B).",
            "6  Procedural content - fractal Perlin noise terrain with hand-computed normals, generated neon grid texture, UV scrolling.",
            "7  Illumination - Flat, Gouraud, Phong, Blinn-Phong and Toon shading in one custom shader, attenuated point light (1-5).",
            "8  Hidden surface removal - Z-buffer depth visualisation (Z) and back-face culling toggle.",
            "9  Debug views - barycentric wireframe overlay (F) and vertex-normal visualisation (N).",
            "10 Animation - keyframes with linear / ease / Hermite curves, hand-written quaternion slerp, squash & stretch, shader waves.",
            "11 Particle systems - additive burst, trail, swirl and dust systems built entirely in code.",
            "12 Ray casting - hand-written ray-sphere and ray-AABB tests for the light pulse; sphere-sphere pickups; view-cone vision.",
            "13 Colour & post-processing - bloom, vignette, grading, and an RGB->HSV hue-shift screen effect (H / when hit)."
        };

        void Start()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            crystal = MeshFactory.Crystal(0.9f, 1.8f);
            cam = Camera.main;
            if (cam != null) PostFX.Setup(cam);
            ShadingModeController.SetPointLight(new Vector3(0, 3, -2));
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Destroy(floor.GetComponent<Collider>());
            floor.transform.position = new Vector3(0, -2.5f, 0); floor.transform.localScale = new Vector3(40, 1, 40);
            floor.GetComponent<Renderer>().sharedMaterial = LumenResources.I.grid;
            LumenResources.I.grid.SetFloat("_Tiling", 80f);
        }

        void Update()
        {
            // backdrop: five shards on a ring, each model matrix composed from MatrixMath
            for (int i = 0; i < 5; i++)
            {
                float a = i * 1.2566f + Time.time * 0.25f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 5f, 1f + Mathf.Sin(Time.time * 1.5f + i) * 0.4f, 6f + Mathf.Sin(a) * 2f);
                Matrix4x4 m = MatrixMath.Translate(p) * MatrixMath.RotateY(Time.time * 70f + i * 40f) * MatrixMath.RotateZ(12f) * MatrixMath.Scale(Vector3.one * 0.8f);
                Graphics.DrawMesh(crystal, m, LumenResources.I.shard, 0);
            }
            if (cam != null) cam.transform.position = new Vector3(Mathf.Sin(Time.time * 0.2f) * 1.5f, 1.2f, -3f);
        }

        void Styles(float s)
        {
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                btn = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
                body = new GUIStyle(GUI.skin.label) { wordWrap = true };
                head = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            }
            title.fontSize = Mathf.RoundToInt(110 * s); btn.fontSize = Mathf.RoundToInt(24 * s);
            body.fontSize = Mathf.RoundToInt(17 * s); head.fontSize = Mathf.RoundToInt(34 * s);
        }

        void OnGUI()
        {
            float s = Screen.height / 720f;
            Styles(s);
            float glow = 0.6f + 0.4f * Mathf.Sin(Time.time * 2.2f);

            if (page == Page.Main)
            {
                // glow = a few offset, tinted copies behind the title
                Rect tr = new Rect(0, Screen.height * 0.08f, Screen.width, 140 * s);
                for (int i = 4; i >= 1; i--)
                {
                    title.normal.textColor = new Color(1f, 0.2f, 0.9f, 0.10f * glow);
                    GUI.Label(new Rect(tr.x - i * 2, tr.y, tr.width, tr.height), "LUMEN", title);
                    GUI.Label(new Rect(tr.x + i * 2, tr.y, tr.width, tr.height), "LUMEN", title);
                }
                title.normal.textColor = Color.Lerp(new Color(0.4f, 0.9f, 1f), new Color(1f, 0.5f, 1f), glow);
                GUI.Label(tr, "LUMEN", title);
                head.normal.textColor = new Color(0.8f, 0.8f, 1f, 0.9f);
                GUI.Label(new Rect(0, tr.yMax - 10 * s, Screen.width, 40 * s), "a computer graphics game", head);

                float w = 320 * s, h = 56 * s, x = (Screen.width - w) * 0.5f, y = Screen.height * 0.50f, g = 12 * s;
                if (GUI.Button(new Rect(x, y, w, h), "Play", btn)) SceneManager.LoadScene("Game");
                if (GUI.Button(new Rect(x, y + (h + g), w, h), "Controls", btn)) page = Page.Controls;
                if (GUI.Button(new Rect(x, y + 2 * (h + g), w, h), "CG Concepts", btn)) page = Page.Concepts;
                if (GUI.Button(new Rect(x, y + 3 * (h + g), w, h), "Quit", btn))
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                }
            }
            else
            {
                var old = GUI.color; GUI.color = new Color(0.02f, 0, 0.1f, 0.88f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = old;
                head.normal.textColor = new Color(0.5f, 1f, 1f);
                GUI.Label(new Rect(0, 20 * s, Screen.width, 50 * s), page == Page.Controls ? "CONTROLS" : "COMPUTER GRAPHICS CONCEPTS", head);
                body.normal.textColor = Color.white;
                Rect r = new Rect(Screen.width * 0.12f, 90 * s, Screen.width * 0.76f, Screen.height - 180 * s);
                GUI.Label(r, page == Page.Controls ? ControlsText() : string.Join("\n\n", concepts), body);
                if (GUI.Button(new Rect((Screen.width - 240 * s) * 0.5f, Screen.height - 70 * s, 240 * s, 48 * s), "Back", btn)) page = Page.Main;
            }
        }

        static string ControlsText() =>
            "WASD            move the orb (camera-relative)\n" +
            "Space           jump  (press again in the air to boost)\n" +
            "Left mouse      fire light pulse (ray cast) - stuns Sentinels\n" +
            "Mouse           orbit camera\n" +
            "Tab             CG Lab Panel (live switches, sliders, stats)\n" +
            "1 - 5           shading: Flat / Gouraud / Phong / Blinn-Phong / Toon\n" +
            "P               perspective <-> orthographic (animated)\n" +
            "F               wireframe overlay\n" +
            "N               vertex normals\n" +
            "Z               depth-buffer (Z-buffer) view\n" +
            "B               Bezier / Catmull-Rom curves\n" +
            "H               RGB->HSV hue-shift effect\n" +
            "Esc             pause\n\n" +
            "GOAL: collect 12 Light Shards within 180 s, avoid the Sentinels, then enter the Portal.";
    }
}
