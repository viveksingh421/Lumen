using UnityEngine;
using UnityEngine.InputSystem;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 7 - owns the global lighting parameters consumed by LumenLit.shader.
    /// Keys 1-5 call Shader.SetGlobalFloat("_LumenMode") which instantly switches EVERY Lumen material.
    /// </summary>
    public class ShadingModeController : MonoBehaviour
    {
        public static ShadingModeController I { get; private set; }

        public enum Mode { Flat, Gouraud, Phong, BlinnPhong, Toon }
        public static readonly string[] ModeNames = { "Flat", "Gouraud", "Phong", "Blinn-Phong", "Toon" };

        public Mode Current = Mode.BlinnPhong;

        [Header("Global material multipliers (Lab Panel sliders)")]
        [Range(0, 2)] public float ka = 1f;
        [Range(0, 2)] public float kd = 1f;
        [Range(0, 2)] public float ks = 1f;
        [Range(0.1f, 4)] public float shininess = 1f;
        public bool cullBack = true;

        [Header("Lights")]
        public Vector3 dirLightDirection = new Vector3(-0.4f, -0.8f, -0.35f);
        public Color dirLightColor = new Color(0.45f, 0.38f, 0.8f);
        public Color ambient = new Color(0.30f, 0.22f, 0.50f);
        public Color pointColor = new Color(0.75f, 0.95f, 1f);
        public float pointIntensity = 3.2f;
        public Vector3 attenuation = new Vector3(1f, 0.09f, 0.032f);   // a, b, c in 1/(a + b d + c d^2)
        public Color fogColor = new Color(0.07f, 0.03f, 0.14f);
        public float fogDistance = 190f;

        Vector3 pointPos;
        bool pointOn = true;

        void Awake() { I = this; Apply(); }

        public static void SetPointLight(Vector3 p, bool on = true)
        {
            if (I == null) return;
            I.pointPos = p; I.pointOn = on;
        }

        public void SetMode(Mode m) { Current = m; Apply(); }

        void Update()
        {
            if (LumenInput.Pressed(Key.Digit1)) SetMode(Mode.Flat);
            if (LumenInput.Pressed(Key.Digit2)) SetMode(Mode.Gouraud);
            if (LumenInput.Pressed(Key.Digit3)) SetMode(Mode.Phong);
            if (LumenInput.Pressed(Key.Digit4)) SetMode(Mode.BlinnPhong);
            if (LumenInput.Pressed(Key.Digit5)) SetMode(Mode.Toon);
            Apply();
        }

        /// <summary>Back-face culling toggle (CG CONCEPT 8): Cull Back (2) vs Cull Off (0).</summary>
        public void ApplyCull()
        {
            if (LumenResources.I == null) return;
            foreach (var m in LumenResources.I.LitMaterials) m.SetFloat("_Cull", cullBack ? 2f : 0f);
        }

        void Apply()
        {
            Shader.SetGlobalFloat("_LumenMode", (int)Current);
            Shader.SetGlobalVector("_LumenK", new Vector4(ka, kd, ks, shininess));
            Shader.SetGlobalVector("_LumenDirLight", dirLightDirection.normalized);
            Shader.SetGlobalColor("_LumenDirColor", dirLightColor);
            Shader.SetGlobalColor("_LumenAmbient", ambient);
            Shader.SetGlobalVector("_LumenLightPos", new Vector4(pointPos.x, pointPos.y, pointPos.z, pointOn ? 1f : 0f));
            Shader.SetGlobalColor("_LumenLightColor", pointColor * pointIntensity);
            Shader.SetGlobalVector("_LumenAtten", new Vector4(attenuation.x, attenuation.y, attenuation.z, 0));
            Shader.SetGlobalVector("_LumenFog", new Vector4(fogColor.r, fogColor.g, fogColor.b, fogDistance));
        }
    }
}
