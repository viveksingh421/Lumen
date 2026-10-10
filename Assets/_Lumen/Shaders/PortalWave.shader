// CG CONCEPT 10 - Shader-based animation. The portal disc is displaced in the VERTEX shader by travelling sine waves
//   z' = A sin(k r - w t)    (concentric ripples)
// and the FRAGMENT shader paints a polar swirl pattern, sin(n*theta + k r - w t), fading out toward the rim.
Shader "Lumen/PortalWave"
{
    Properties
    {
        [HDR] _ColorA("Color A", Color) = (0.1, 1.2, 1.6, 1)
        [HDR] _ColorB("Color B", Color) = (1.6, 0.2, 1.4, 1)
        _WaveAmp("Wave Amplitude", Float) = 0.12
        _WaveFreq("Wave Frequency", Float) = 9
        _Speed("Speed", Float) = 3
        _Open("Open Amount", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Portal"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA; float4 _ColorB; float _WaveAmp; float _WaveFreq; float _Speed; float _Open;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float2 p = v.uv * 2.0 - 1.0;                 // disc uv in [-1,1]
                float r = length(p);
                float3 pos = v.positionOS.xyz;
                pos.z += _WaveAmp * sin(_WaveFreq * r - _Speed * _Time.y) * (1.0 - r * 0.5);
                o.positionCS = TransformObjectToHClip(pos);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;
                float r = length(p);
                if (r > 1.0) discard;
                float a = atan2(p.y, p.x);
                float swirl = 0.5 + 0.5 * sin(5.0 * a + 8.0 * r - _Speed * _Time.y * 1.3);
                float rings = 0.5 + 0.5 * sin(18.0 * r - _Speed * _Time.y * 2.0);
                float3 col = lerp(_ColorA.rgb, _ColorB.rgb, swirl) * (0.6 + 0.8 * rings);
                float edge = smoothstep(1.0, 0.7, r);
                float core = smoothstep(0.0, 0.5, r);
                float alpha = edge * (0.25 + 0.75 * core) * _Open;
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
