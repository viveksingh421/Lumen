// CG CONCEPT 9 - Wireframe via barycentric coordinates.
// A geometry shader receives each triangle and emits its 3 vertices with barycentric coords (1,0,0),(0,1,0),(0,0,1).
// Interpolated across the triangle, the smallest component is ~0 on an edge; fwidth() gives constant pixel-width lines.
Shader "Lumen/Wireframe"
{
    Properties
    {
        _WireColor("Wire Color", Color) = (0.2, 1, 0.9, 1)
        _Width("Line Width (px)", Float) = 1.2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Wire"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -1
            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _WireColor;
                float _Width;
            CBUFFER_END

            struct v2g { float4 positionCS : SV_POSITION; };
            struct g2f { float4 positionCS : SV_POSITION; float3 bary : TEXCOORD0; };

            v2g vert(float4 positionOS : POSITION) { v2g o; o.positionCS = TransformObjectToHClip(positionOS.xyz); return o; }

            [maxvertexcount(3)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> stream)
            {
                g2f o;
                o.positionCS = IN[0].positionCS; o.bary = float3(1, 0, 0); stream.Append(o);
                o.positionCS = IN[1].positionCS; o.bary = float3(0, 1, 0); stream.Append(o);
                o.positionCS = IN[2].positionCS; o.bary = float3(0, 0, 1); stream.Append(o);
            }

            half4 frag(g2f i) : SV_Target
            {
                float3 d = fwidth(i.bary);
                float3 a = smoothstep(float3(0,0,0), d * _Width, i.bary);
                float edge = min(min(a.x, a.y), a.z);
                float alpha = 1.0 - edge;
                if (alpha < 0.02) discard;
                return half4(_WireColor.rgb, alpha * _WireColor.a);
            }
            ENDHLSL
        }
    }
}
