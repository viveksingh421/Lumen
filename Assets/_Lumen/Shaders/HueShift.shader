// CG CONCEPT 13 - Colour spaces. Full-screen effect: RGB -> HSV, rotate the hue, HSV -> RGB.
//   V = max(R,G,B); S = (V - min)/V; H from which channel is max:  H = 60 * ((G-B)/(V-min) mod 6) ...
// Used as the "stunned / hit" screen effect (hue spins over time, saturation boosted) and as a Lab Panel toggle.
Shader "Lumen/HueShift"
{
    Properties { _Strength("Strength", Range(0,1)) = 1  _HueDegrees("Hue Shift (deg)", Float) = 0  _SatBoost("Saturation Boost", Float) = 1.3 }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay+1" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "HueShift"
            Tags { "LightMode"="UniversalForward" }
            ZTest Always ZWrite Off Cull Off Blend Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Strength; float _HueDegrees; float _SatBoost;
            CBUFFER_END

            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(float4 positionOS : POSITION)
            {
                Varyings o;
                o.positionCS = float4(positionOS.x * 2.0, positionOS.y * 2.0 * _ProjectionParams.x, UNITY_NEAR_CLIP_VALUE, 1.0);
                return o;
            }

            float3 RGBtoHSV(float3 c)
            {
                float mx = max(c.r, max(c.g, c.b)), mn = min(c.r, min(c.g, c.b)), d = mx - mn;
                float h = 0;
                if (d > 1e-5)
                {
                    if (mx == c.r) h = fmod((c.g - c.b) / d + 6.0, 6.0);
                    else if (mx == c.g) h = (c.b - c.r) / d + 2.0;
                    else h = (c.r - c.g) / d + 4.0;
                    h /= 6.0;
                }
                return float3(h, mx > 1e-5 ? d / mx : 0, mx);
            }

            float3 HSVtoRGB(float3 c)
            {
                float3 k = abs(frac(c.x + float3(0, 2.0/3.0, 1.0/3.0)) * 6.0 - 3.0);
                return c.z * lerp(float3(1,1,1), saturate(k - 1.0), c.y);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.positionCS.xy / _ScaledScreenParams.xy;
                float3 src = SampleSceneColor(uv);
                float3 hsv = RGBtoHSV(src);
                hsv.x = frac(hsv.x + _HueDegrees / 360.0);
                hsv.y = saturate(hsv.y * _SatBoost);
                return half4(lerp(src, HSVtoRGB(hsv), _Strength), 1);
            }
            ENDHLSL
        }
    }
}
