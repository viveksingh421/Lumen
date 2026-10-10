using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 13 - Post-processing through a URP Volume built in code: bloom (bright-pass + blur + add),
    /// vignette (radial darkening), colour grading (contrast/saturation) and ACES tone-mapping.
    /// </summary>
    public class PostFX
    {
        public Bloom Bloom;
        public Vignette Vignette;
        public ColorAdjustments Grade;
        public Volume Volume;

        public static PostFX Setup(Camera cam)
        {
            var fx = new PostFX();
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.requiresColorOption = CameraOverrideOption.On;     // _CameraOpaqueTexture for the hue-shift effect

            var go = new GameObject("PostFX Volume");
            fx.Volume = go.AddComponent<Volume>();
            fx.Volume.isGlobal = true;
            fx.Volume.priority = 10;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            fx.Bloom = profile.Add<Bloom>(true);
            fx.Bloom.threshold.Override(0.85f);
            fx.Bloom.intensity.Override(1.2f);
            fx.Bloom.scatter.Override(0.7f);

            fx.Vignette = profile.Add<Vignette>(true);
            fx.Vignette.intensity.Override(0.38f);
            fx.Vignette.smoothness.Override(0.55f);

            fx.Grade = profile.Add<ColorAdjustments>(true);
            fx.Grade.contrast.Override(14f);
            fx.Grade.saturation.Override(18f);

            var tm = profile.Add<Tonemapping>(true);
            tm.mode.Override(TonemappingMode.ACES);

            fx.Volume.sharedProfile = profile;
            return fx;
        }

        public void SetEnabled(bool bloom, bool vignette, bool grade)
        {
            Bloom.active = bloom; Vignette.active = vignette; Grade.active = grade;
        }
    }
}
