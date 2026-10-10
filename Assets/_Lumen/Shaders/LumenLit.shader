// CG CONCEPT 7 - Illumination & shading models, hand written for URP (HLSL).
//
//   I = ka*Ia*albedo  +  sum_lights [ kd*albedo*max(N.L,0)  +  ks*spec ] * radiance * attenuation
//
//   Mode 0  FLAT         per-FACE normal rebuilt from screen derivatives:  N = normalize(cross(ddx(P), ddy(P)))
//   Mode 1  GOURAUD      lighting evaluated in the VERTEX shader, colour interpolated across the triangle
//   Mode 2  PHONG        per-PIXEL lighting, specular = (R.V)^n  with  R = reflect(-L,N)
//   Mode 3  BLINN-PHONG  per-pixel, specular = (N.H)^n with half vector H = normalize(L+V)  (cheaper, no reflect)
//   Mode 4  TOON         N.L quantised to bands + rim light + inverted-hull outline pass
//
//   Point light attenuation:  att = 1 / (a + b*d + c*d^2)      (_LumenAtten = a,b,c)
// The mode is a GLOBAL float (_LumenMode) so keys 1-5 switch every Lumen material at once via Shader.SetGlobalFloat.
Shader "Lumen/LumenLit"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _MainTex("Grid Texture", 2D) = "white" {}
        _Tiling("Grid Tiling", Float) = 40
        _Scroll("Grid Scroll (xy)", Vector) = (0,0,0,0)
        _VertexColorAmount("Vertex Color Amount", Range(0,1)) = 0
        _GridAmount("Grid Glow Amount", Range(0,2)) = 0
        _GridColor("Grid Glow Color", Color) = (0,1,1,1)
        [HDR] _EmissionColor("Emission", Color) = (0,0,0,1)
        _Ka("Ka (ambient)", Range(0,1)) = 0.25
        _Kd("Kd (diffuse)", Range(0,1)) = 0.85
        _Ks("Ks (specular)", Range(0,1)) = 0.5
        _Shininess("Shininess", Range(1,256)) = 32
        _OutlineColor("Toon Outline Color", Color) = (0,0,0,1)
        _OutlineWidth("Toon Outline Width", Float) = 0.04
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _MainTex_ST;
            float  _Tiling;
            float4 _Scroll;
            float  _VertexColorAmount;
            float  _GridAmount;
            float4 _GridColor;
            float4 _EmissionColor;
            float  _Ka, _Kd, _Ks, _Shininess;
            float4 _OutlineColor;
            float  _OutlineWidth;
        CBUFFER_END

        // ---- globals set from C# (ShadingModeController / OrbController) ----
        float  _LumenMode;          // 0..4
        float4 _LumenLightPos;      // xyz = point light (player orb), w = on/off
        float4 _LumenLightColor;    // rgb * intensity
        float4 _LumenAtten;         // a, b, c of 1/(a + b d + c d^2)
        float4 _LumenDirLight;      // xyz = direction light travels (world)
        float4 _LumenDirColor;
        float4 _LumenAmbient;
        float4 _LumenK;             // global multipliers: x=ka y=kd z=ks w=shininess
        float4 _LumenFog;           // rgb colour, w = distance
        float  _LumenDepthView;     // 1 = Z-buffer visualisation (key Z)
        float  _LumenDepthFar;      // distance mapped to black
        float  _LumenOrtho;         // 1 when the projection is orthographic

        struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 normalWS   : TEXCOORD1;
            float2 uv         : TEXCOORD2;
            float4 color      : TEXCOORD3;
            float3 vLit       : TEXCOORD4;   // Gouraud: lighting computed per vertex
        };

        float3 Albedo(float4 vcol)
        {
            return _BaseColor.rgb * lerp(float3(1,1,1), vcol.rgb, _VertexColorAmount);
        }

        // One light contribution. L points from surface to light.
        float3 LightTerm(float3 albedo, float3 N, float3 V, float3 L, float3 radiance, float att, int mode)
        {
            float ka = _Ka, kd = _Kd * _LumenK.y, ks = _Ks * _LumenK.z;
            float shin = max(1.0, _Shininess * _LumenK.w);
            float ndl = saturate(dot(N, L));
            float spec;
            if (mode == 3) { float3 H = normalize(L + V); spec = pow(saturate(dot(N, H)), shin * 4.0); }   // Blinn-Phong
            else           { float3 R = reflect(-L, N);   spec = pow(saturate(dot(R, V)), shin); }          // Phong
            spec *= step(0.0001, ndl);

            if (mode == 4)   // toon: 3 hard-ish diffuse bands, binary highlight
            {
                float band = floor(ndl * 3.0) / 3.0;
                ndl = lerp(band, smoothstep(0.0, 1.0, band + 0.15), 0.35);
                spec = smoothstep(0.45, 0.5, spec) * 0.8;
            }
            return (kd * albedo * ndl + ks * spec) * radiance * att;
        }

        float3 Shade(float3 albedo, float3 N, float3 P, float3 V, int mode)
        {
            float3 ambient = _Ka * _LumenK.x * albedo * _LumenAmbient.rgb;
            float3 col = ambient;
            // directional "moon" light
            col += LightTerm(albedo, N, V, normalize(-_LumenDirLight.xyz), _LumenDirColor.rgb, 1.0, mode);
            // moving point light (the orb) with 1/(a+bd+cd^2) attenuation
            float3 toL = _LumenLightPos.xyz - P;
            float d = length(toL);
            float att = 1.0 / (_LumenAtten.x + _LumenAtten.y * d + _LumenAtten.z * d * d);
            col += LightTerm(albedo, N, V, toL / max(d, 1e-4), _LumenLightColor.rgb, att * _LumenLightPos.w, mode);
            if (mode == 4) col += pow(1.0 - saturate(dot(N, V)), 3.0) * float3(0.9, 0.3, 1.0) * 0.6;   // rim light
            return col;
        }

        float GridLine(float2 uv)
        {
            float g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv * _Tiling + _Scroll.xy * _Time.y).r;
            return saturate((g - 0.12) / 0.88);
        }

        // CG CONCEPT 8 - Z-buffer view. SV_Position.z IS the value the depth test compares/stores for this fragment
        // (hardware depth: non-linear 1/z, reversed on modern GPUs). We linearise it and print grey = 1 - eyeDepth/far.
        //   perspective:  eye = 1 / (zb.z * d + zb.w)     orthographic: eye = near + (1-d)(far-near)
        half4 ZBufferView(float rawDepth)
        {
            float eye;
            if (_LumenOrtho > 0.5)
            {
                #if UNITY_REVERSED_Z
                    rawDepth = 1.0 - rawDepth;
                #endif
                eye = _ProjectionParams.y + rawDepth * (_ProjectionParams.z - _ProjectionParams.y);
            }
            else eye = LinearEyeDepth(rawDepth, _ZBufferParams);
            float g = pow(saturate(1.0 - eye / _LumenDepthFar), 1.4);
            return half4(g, g, g, 1);
        }

        Varyings Vert(Attributes IN)
        {
            Varyings OUT;
            VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
            OUT.positionCS = vp.positionCS;
            OUT.positionWS = vp.positionWS;
            OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
            OUT.uv = IN.uv;
            OUT.color = IN.color;
            OUT.vLit = 0;
            if (_LumenMode > 0.5 && _LumenMode < 1.5)   // Gouraud: light per vertex
            {
                float3 V = normalize(GetCameraPositionWS() - vp.positionWS);
                OUT.vLit = Shade(Albedo(IN.color), normalize(OUT.normalWS), vp.positionWS, V, 1);
            }
            return OUT;
        }

        half4 Frag(Varyings IN) : SV_Target
        {
            int mode = (int)(_LumenMode + 0.5);
            if (_LumenDepthView > 0.5) return ZBufferView(IN.positionCS.z);
            float3 P = IN.positionWS;
            float3 V = normalize(GetCameraPositionWS() - P);
            float3 N = normalize(IN.normalWS);
            float3 albedo = Albedo(IN.color);
            float gl = GridLine(IN.uv) * _GridAmount;

            float3 col;
            if (mode == 1)
            {
                col = IN.vLit;                                   // already lit per vertex
            }
            else
            {
                if (mode == 0)                                   // flat: face normal from derivatives
                {
                    float3 fn = normalize(cross(ddx(P), ddy(P)));
                    N = dot(fn, N) < 0 ? -fn : fn;
                }
                col = Shade(albedo, N, P, V, mode);
            }
            col += _EmissionColor.rgb + _GridColor.rgb * gl;

            float dist = distance(GetCameraPositionWS(), P);
            float fog = saturate(dist / _LumenFog.w); fog *= fog;
            col = lerp(col, _LumenFog.rgb, fog);
            return half4(col, 1);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }

        // Toon outline: inverted hull (back faces pushed out along the normal). Collapsed unless mode == 4.
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            HLSLPROGRAM
            #pragma vertex OutVert
            #pragma fragment OutFrag
            Varyings OutVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                if (_LumenMode < 3.5 || _LumenDepthView > 0.5) { OUT.positionCS = float4(2, 2, 2, 0); return OUT; }   // degenerate -> clipped away
                float3 pos = IN.positionOS.xyz + IN.normalOS * _OutlineWidth;
                OUT.positionCS = TransformObjectToHClip(pos);
                return OUT;
            }
            half4 OutFrag(Varyings IN) : SV_Target { return half4(_OutlineColor.rgb, 1); }
            ENDHLSL
        }

        // Needed so the camera depth texture (Z-buffer view) contains these objects.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex DVert
            #pragma fragment DFrag
            float4 DVert(float4 positionOS : POSITION) : SV_POSITION { return TransformObjectToHClip(positionOS.xyz); }
            half DFrag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
