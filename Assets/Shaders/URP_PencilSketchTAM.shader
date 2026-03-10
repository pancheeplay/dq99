Shader "DQ99/URP/PencilSketchTAM"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.88, 0.86, 0.82, 1)

        _Hatch0 ("Hatch 0 (Light)", 2D) = "white" {}
        _Hatch1 ("Hatch 1", 2D) = "white" {}
        _Hatch2 ("Hatch 2", 2D) = "white" {}
        _Hatch3 ("Hatch 3", 2D) = "white" {}
        _Hatch4 ("Hatch 4", 2D) = "white" {}
        _Hatch5 ("Hatch 5 (Dark)", 2D) = "white" {}

        _HatchTiling ("Hatch Tiling", Range(1, 64)) = 12
        _HatchStrength ("Hatch Strength", Range(0, 1)) = 0.55
        _HatchFade ("Hatch Anti-Flicker Fade", Range(0, 12)) = 2.2

        _ToneBias ("Tone Bias", Range(-1, 1)) = 0
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.35
        _LightResponse ("Light Response", Range(0.1, 3)) = 1.1

        _OutlineColor ("Outline Color", Color) = (0.14, 0.14, 0.14, 1)
        _OutlineStrength ("Outline Strength", Range(0, 1)) = 0.65
        _OutlinePower ("Outline Power", Range(0.5, 8)) = 3

        _PaperTex ("Paper Texture", 2D) = "white" {}
        _PaperTiling ("Paper Tiling", Range(0.1, 16)) = 2
        _PaperStrength ("Paper Strength", Range(0, 0.2)) = 0.03

        _GrayScale ("Gray Scale", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                float fogCoord : TEXCOORD5;
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_Hatch0); SAMPLER(sampler_Hatch0);
            TEXTURE2D(_Hatch1); SAMPLER(sampler_Hatch1);
            TEXTURE2D(_Hatch2); SAMPLER(sampler_Hatch2);
            TEXTURE2D(_Hatch3); SAMPLER(sampler_Hatch3);
            TEXTURE2D(_Hatch4); SAMPLER(sampler_Hatch4);
            TEXTURE2D(_Hatch5); SAMPLER(sampler_Hatch5);
            TEXTURE2D(_PaperTex); SAMPLER(sampler_PaperTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _PaperTex_ST;
                half4 _BaseColor;
                half4 _OutlineColor;
                float _HatchTiling;
                float _HatchStrength;
                float _HatchFade;
                float _ToneBias;
                float _AmbientStrength;
                float _LightResponse;
                float _OutlineStrength;
                float _OutlinePower;
                float _PaperTiling;
                float _PaperStrength;
                float _GrayScale;
            CBUFFER_END

            float3 ComputeTone(float3 normalWS, float3 positionWS)
            {
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
                float nDotMain = saturate(dot(normalWS, mainLight.direction));
                float3 lit = mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation * nDotMain);

                #if defined(_ADDITIONAL_LIGHTS)
                    uint lightCount = GetAdditionalLightsCount();
                    for (uint li = 0u; li < lightCount; li++)
                    {
                        Light addLight = GetAdditionalLight(li, positionWS);
                        float nDotAdd = saturate(dot(normalWS, addLight.direction));
                        lit += addLight.color * (addLight.distanceAttenuation * addLight.shadowAttenuation * nDotAdd);
                    }
                #endif

                float3 ambient = SampleSH(normalWS) * _AmbientStrength;
                return lit + ambient;
            }

            float ComputeHatch(float2 hatchUV)
            {
                float2 dx = ddx(hatchUV);
                float2 dy = ddy(hatchUV);

                float h0 = SAMPLE_TEXTURE2D_GRAD(_Hatch0, sampler_Hatch0, hatchUV, dx, dy).r;
                float h1 = SAMPLE_TEXTURE2D_GRAD(_Hatch1, sampler_Hatch1, hatchUV, dx, dy).r;
                float h2 = SAMPLE_TEXTURE2D_GRAD(_Hatch2, sampler_Hatch2, hatchUV, dx, dy).r;
                float h3 = SAMPLE_TEXTURE2D_GRAD(_Hatch3, sampler_Hatch3, hatchUV, dx, dy).r;
                float h4 = SAMPLE_TEXTURE2D_GRAD(_Hatch4, sampler_Hatch4, hatchUV, dx, dy).r;
                float h5 = SAMPLE_TEXTURE2D_GRAD(_Hatch5, sampler_Hatch5, hatchUV, dx, dy).r;

                float footprint = max(length(dx), length(dy));
                float antiFlicker = saturate(1.0 - footprint * _HatchFade);

                return lerp(1.0, (h0 + h1 + h2 + h3 + h4 + h5) / 6.0, antiFlicker);
            }

            float ComputeTAMTone(float tone, float2 hatchUV)
            {
                float hatch0 = SAMPLE_TEXTURE2D_GRAD(_Hatch0, sampler_Hatch0, hatchUV, ddx(hatchUV), ddy(hatchUV)).r;
                float hatch1 = SAMPLE_TEXTURE2D_GRAD(_Hatch1, sampler_Hatch1, hatchUV, ddx(hatchUV), ddy(hatchUV)).r;
                float hatch2 = SAMPLE_TEXTURE2D_GRAD(_Hatch2, sampler_Hatch2, hatchUV, ddx(hatchUV), ddy(hatchUV)).r;
                float hatch3 = SAMPLE_TEXTURE2D_GRAD(_Hatch3, sampler_Hatch3, hatchUV, ddx(hatchUV), ddy(hatchUV)).r;
                float hatch4 = SAMPLE_TEXTURE2D_GRAD(_Hatch4, sampler_Hatch4, hatchUV, ddx(hatchUV), ddy(hatchUV)).r;
                float hatch5 = SAMPLE_TEXTURE2D_GRAD(_Hatch5, sampler_Hatch5, hatchUV, ddx(hatchUV), ddy(hatchUV)).r;

                float darkness = saturate(1.0 - tone) * 6.0;
                float w0 = saturate(1.0 - abs(darkness - 0.0));
                float w1 = saturate(1.0 - abs(darkness - 1.0));
                float w2 = saturate(1.0 - abs(darkness - 2.0));
                float w3 = saturate(1.0 - abs(darkness - 3.0));
                float w4 = saturate(1.0 - abs(darkness - 4.0));
                float w5 = saturate(1.0 - abs(darkness - 5.0));
                float w6 = saturate(1.0 - abs(darkness - 6.0));

                float weightSum = max(1e-4, w0 + w1 + w2 + w3 + w4 + w5 + w6);
                float mixed = (hatch0 * w0 + hatch1 * w1 + hatch2 * w2 + hatch3 * w3 + hatch4 * w4 + hatch5 * w5) / weightSum;

                // Brightest region should stay close to paper white.
                float brightLift = smoothstep(0.82, 1.0, tone);
                mixed = lerp(mixed, 1.0, brightLift);

                float2 dx = ddx(hatchUV);
                float2 dy = ddy(hatchUV);
                float footprint = max(length(dx), length(dy));
                float antiFlicker = saturate(1.0 - footprint * _HatchFade);

                return lerp(1.0, mixed, antiFlicker);
            }

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs nor = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS = normalize(nor.normalWS);
                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(pos.positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.shadowCoord = TransformWorldToShadowCoord(pos.positionWS);
                OUT.fogCoord = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb;
                float3 albedo = baseTex * _BaseColor.rgb;

                float3 lit = ComputeTone(normalWS, IN.positionWS);
                float tone = saturate(dot(lit, float3(0.299, 0.587, 0.114)) * _LightResponse + _ToneBias);

                float2 hatchUV = IN.uv * _HatchTiling;
                float hatchTone = ComputeTAMTone(tone, hatchUV);
                float3 shaded = albedo * lerp(1.0, hatchTone, _HatchStrength);

                float2 paperUV = TRANSFORM_TEX(IN.uv, _PaperTex) * _PaperTiling;
                float paper = SAMPLE_TEXTURE2D(_PaperTex, sampler_PaperTex, paperUV).r;
                shaded *= lerp(1.0, paper, _PaperStrength);

                float fresnel = pow(1.0 - saturate(dot(normalWS, normalize(IN.viewDirWS))), _OutlinePower) * _OutlineStrength;
                float3 color = lerp(shaded, _OutlineColor.rgb, saturate(fresnel));

                float gray = dot(color, float3(0.299, 0.587, 0.114));
                color = lerp(color, gray.xxx, _GrayScale);

                half4 outCol = half4(saturate(color), 1.0);
                outCol.rgb = MixFog(outCol.rgb, IN.fogCoord);
                return outCol;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }
}
