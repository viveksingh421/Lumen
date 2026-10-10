// CG CONCEPT 6/10 - Texture tiling + UV scrolling. Infinite-looking synthwave floor: the procedural grid texture is
// tiled (uv * tiling) and animated (uv += scroll * time); it fades radially so the horizon blends into the fog colour.
Shader "Lumen/GridScroll"
{
    Properties
    {
        _MainTex("Grid", 2D) = "white" {}
        [HDR] _Color("Color", Color) = (0.1, 0.9, 1, 1)
        _Tiling("Tiling", Float) = 60
        _Scroll("Scroll (xy)", Vector) = (0, 0.15, 0, 0)
        _FadeRadius("Fade Radius", Float) = 400
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Grid"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST; float4 _Color; float _Tiling; float4 _Scroll; float _FadeRadius;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs vp = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = vp.positionCS; o.positionWS = vp.positionWS; o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv * _Tiling + _Scroll.xy * _Time.y).r;
                float gl = saturate((g - 0.12) / 0.88);
                float fade = saturate(1.0 - length(i.positionWS.xz) / _FadeRadius);
                return half4(_Color.rgb, gl * fade * _Color.a);
            }
            ENDHLSL
        }
    }
}
