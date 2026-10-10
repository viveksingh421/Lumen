using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lumen.EditorTools
{
    /// <summary>Menu: Lumen/Build Materials and Scenes. Creates all material assets, the MainMenu and Game scenes and the Build Settings.</summary>
    public static class LumenSceneBuilder
    {
        const string MatDir = "Assets/_Lumen/Materials";
        const string SceneDir = "Assets/_Lumen/Scenes";

        static Material Mat(string name, string shaderName, System.Action<Material> setup)
        {
            string path = MatDir + "/" + name + ".mat";
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new System.Exception("Shader not found: " + shaderName);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            setup(m);
            EditorUtility.SetDirty(m);
            return m;
        }

        [MenuItem("Lumen/Build Materials and Scenes")]
        public static void BuildAll()
        {
            var mats = BuildMaterials();
            AssetDatabase.SaveAssets();
            BuildMenuScene(mats);
            BuildGameScene(mats);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SceneDir + "/MainMenu.unity", true),
                new EditorBuildSettingsScene(SceneDir + "/Game.unity", true),
            };
            AssetDatabase.SaveAssets();
            Debug.Log("Lumen: materials + scenes built, Build Settings updated.");
        }

        class Mats { public Material terrain, orb, shard, sentinel, portal, wire, hue, grid, particle, line; }

        static Mats BuildMaterials()
        {
            var m = new Mats();
            m.terrain = Mat("Terrain", "Lumen/LumenLit", x =>
            {
                x.SetColor("_BaseColor", Color.white); x.SetFloat("_VertexColorAmount", 1f); x.SetFloat("_GridAmount", 0.9f);
                x.SetColor("_GridColor", new Color(0.25f, 1.1f, 1.3f)); x.SetFloat("_Tiling", 10f); x.SetVector("_Scroll", new Vector4(0, 0.05f, 0, 0));
                x.SetFloat("_Ka", 0.3f); x.SetFloat("_Kd", 0.9f); x.SetFloat("_Ks", 0.35f); x.SetFloat("_Shininess", 24f);
            });
            m.orb = Mat("Orb", "Lumen/LumenLit", x =>
            {
                x.SetColor("_BaseColor", new Color(0.8f, 1f, 1f)); x.SetColor("_EmissionColor", new Color(2.2f, 3.0f, 3.4f));
                x.SetFloat("_Ka", 0.8f); x.SetFloat("_Kd", 0.4f); x.SetFloat("_Ks", 0.9f); x.SetFloat("_Shininess", 64f);
            });
            m.shard = Mat("Shard", "Lumen/LumenLit", x =>
            {
                x.SetColor("_BaseColor", new Color(1f, 0.85f, 0.3f)); x.SetColor("_EmissionColor", new Color(1.5f, 1.0f, 0.15f));
                x.SetFloat("_Ka", 0.6f); x.SetFloat("_Kd", 0.8f); x.SetFloat("_Ks", 1f); x.SetFloat("_Shininess", 80f);
            });
            m.sentinel = Mat("Sentinel", "Lumen/LumenLit", x =>
            {
                x.SetColor("_BaseColor", new Color(0.9f, 0.22f, 0.18f)); x.SetColor("_EmissionColor", new Color(0.8f, 0.18f, 0.15f));
                x.SetFloat("_Ka", 0.5f); x.SetFloat("_Kd", 0.9f); x.SetFloat("_Ks", 0.6f); x.SetFloat("_Shininess", 40f);
                x.SetFloat("_OutlineWidth", 0.06f);
            });
            m.portal = Mat("Portal", "Lumen/PortalWave", x => { });
            m.wire = Mat("Wireframe", "Lumen/Wireframe", x => { x.SetColor("_WireColor", new Color(0.2f, 1f, 0.9f, 0.9f)); });
            m.hue = Mat("HueShift", "Lumen/HueShift", x => { });
            m.grid = Mat("GridFloor", "Lumen/GridScroll", x =>
            {
                x.SetColor("_Color", new Color(0.3f, 0.9f, 1.6f, 0.9f)); x.SetFloat("_Tiling", 160f);
                x.SetVector("_Scroll", new Vector4(0, 0.25f, 0, 0)); x.SetFloat("_FadeRadius", 420f);
            });
            m.particle = Mat("Particle", "Lumen/ParticleAdditive", x => { x.SetColor("_Tint", new Color(1.4f, 1.4f, 1.4f, 1f)); });
            m.line = Mat("Line", "Lumen/LineUnlit", x => { x.SetColor("_Tint", Color.white); });
            return m;
        }

        static void Assign(LumenResources r, Mats m)
        {
            r.terrain = m.terrain; r.orb = m.orb; r.shard = m.shard; r.sentinel = m.sentinel; r.portal = m.portal; r.wire = m.wire;
            r.hue = m.hue; r.grid = m.grid; r.particle = m.particle; r.line = m.line;
        }

        static Camera MakeCamera(Scene scene)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.03f, 0.14f);
            cam.allowHDR = true;
            cam.nearClipPlane = 0.3f; cam.farClipPlane = 400f; cam.fieldOfView = 60f;
            return cam;
        }

        static void BuildMenuScene(Mats mats)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = MakeCamera(scene);
            cam.transform.position = new Vector3(0, 1.2f, -3f);

            var root = new GameObject("Lumen");
            Assign(root.AddComponent<LumenResources>(), mats);
            root.AddComponent<ShadingModeController>();
            root.AddComponent<MenuController>();
            EditorSceneManager.SaveScene(scene, SceneDir + "/MainMenu.unity");
        }

        static void BuildGameScene(Mats mats)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = MakeCamera(scene);
            cam.transform.position = new Vector3(0, 12, -12);
            var proj = cam.gameObject.AddComponent<ProjectionController>();
            var orbit = cam.gameObject.AddComponent<OrbitCamera>();
            cam.gameObject.AddComponent<DebugViews>();

            var root = new GameObject("Lumen");
            Assign(root.AddComponent<LumenResources>(), mats);
            root.AddComponent<ShadingModeController>();
            var wb = root.AddComponent<WorldBuilder>(); wb.orbitCam = orbit;
            var radar = root.AddComponent<RadarRenderer>();
            root.AddComponent<CurveVisualizer>();
            var gm = root.AddComponent<GameManager>(); gm.orbitCam = orbit; gm.world = wb;
            var hud = root.AddComponent<HUDController>(); hud.radar = radar;
            var lab = root.AddComponent<LabPanel>(); lab.radar = radar;
            EditorSceneManager.SaveScene(scene, SceneDir + "/Game.unity");
        }
    }
}
