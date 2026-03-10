Shader "DQ99/SketchBuilding"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.86, 0.84, 0.80, 1)
        _ShadowColor ("Shadow Color", Color) = (0.52, 0.50, 0.48, 1)
        _LineColor ("Line Color", Color) = (0.16, 0.16, 0.16, 1)
        _HatchScale ("Hatch Scale", Range(2, 64)) = 14
        _HatchThickness ("Hatch Thickness", Range(0.05, 0.45)) = 0.2
        _HatchStrength ("Hatch Strength", Range(0, 1)) = 0.45
        _HatchFade ("Hatch Distance Fade", Range(0.5, 8)) = 2.4
        _HatchAngle ("Hatch Angle", Range(-90, 90)) = 24
        _HatchJitter ("Hatch Jitter", Range(0, 40)) = 10
        _CrossHatch ("Cross Hatch (Dark Only)", Range(0, 1)) = 0.22
        _StrokeBreakup ("Stroke Breakup", Range(0, 1)) = 0.35
        _ShadeThreshold ("Shade Threshold", Range(0, 1)) = 0.42
        _ShadeSmoothness ("Shade Smoothness", Range(0.001, 0.3)) = 0.06
        _EdgeThreshold ("Edge Threshold", Range(0, 1)) = 0.55
        _EdgeSmoothness ("Edge Smoothness", Range(0.001, 0.3)) = 0.08
        _EdgeStrength ("Edge Strength", Range(0, 1)) = 0.9
        _PaperNoiseScale ("Paper Noise Scale", Range(0.1, 20)) = 3
        _PaperNoiseStrength ("Paper Noise Strength", Range(0, 0.08)) = 0.008
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadowColor;
                half4 _LineColor;
                float _HatchScale;
                float _HatchThickness;
                float _HatchStrength;
                float _HatchFade;
                float _HatchAngle;
                float _HatchJitter;
                float _CrossHatch;
                float _StrokeBreakup;
                float _ShadeThreshold;
                float _ShadeSmoothness;
                float _EdgeThreshold;
                float _EdgeSmoothness;
                float _EdgeStrength;
                float _PaperNoiseScale;
                float _PaperNoiseStrength;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 34.45);
                return frac(p.x * p.y);
            }

            float StripeAA(float value, float thickness)
            {
                float phase = frac(value);
                float dist = abs(phase - 0.5);
                float aa = max(1e-4, fwidth(value));
                return 1.0 - smoothstep(thickness - aa, thickness + aa, dist);
            }

            float2 Rotate2D(float2 v, float r)
            {
                float s = sin(r);
                float c = cos(r);
                return float2(v.x * c - v.y * s, v.x * s + v.y * c);
            }

            Varyings Vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs posInputs = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(v.normalOS);

                o.positionCS = posInputs.positionCS;
                o.normalWS = normalize(normalInputs.normalWS);
                o.viewDirWS = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                o.positionOS = v.positionOS.xyz;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half3 normalWS = normalize(i.normalWS);
                Light mainLight = GetMainLight();
                float ndotl = saturate(dot(normalWS, mainLight.direction));
                float shade = 1.0 - ndotl;

                float shadeMask = smoothstep(
                    _ShadeThreshold - _ShadeSmoothness,
                    _ShadeThreshold + _ShadeSmoothness,
                    shade
                );

                float nv = 1.0 - saturate(dot(normalWS, normalize(i.viewDirWS)));
                float edge = smoothstep(
                    _EdgeThreshold - _EdgeSmoothness,
                    _EdgeThreshold + _EdgeSmoothness,
                    nv
                ) * _EdgeStrength;

                float2 hatchCoord = i.positionOS.xz * _HatchScale;
                float2 cell = floor(hatchCoord * 0.35);
                float dirJitter = (Hash21(cell + float2(7.7, 3.1)) - 0.5) * _HatchJitter;
                float mainAngle = radians(_HatchAngle + dirJitter);

                float2 dirMain = float2(cos(mainAngle), sin(mainAngle));
                float2 dirAcross = float2(-dirMain.y, dirMain.x);

                float axisMain = dot(hatchCoord, dirMain);
                float axisAcross = dot(hatchCoord, dirAcross);

                float warp = (Hash21(floor(hatchCoord * 0.5) + float2(2.4, 9.8)) - 0.5) * 0.45;
                float hatchMain = StripeAA(axisMain + warp, _HatchThickness);

                float strokeGateNoise = Hash21(floor(float2(axisAcross * 0.45, axisMain * 0.2)));
                float strokeGate = step(_StrokeBreakup, strokeGateNoise);
                hatchMain *= strokeGate;

                float2 dirCross = Rotate2D(dirMain, radians(27.0));
                float axisCross = dot(hatchCoord, dirCross);
                float crossWarp = (Hash21(floor(hatchCoord * 0.55) + float2(11.2, 1.9)) - 0.5) * 0.35;
                float hatchCross = StripeAA(axisCross + crossWarp, _HatchThickness * 0.9);
                hatchCross *= smoothstep(0.7, 1.0, shadeMask) * _CrossHatch;

                float hatchMask = saturate(max(hatchMain, hatchCross));

                // Fade out high-frequency hatching when it is too dense for current pixel footprint.
                float hatchFootprint = max(fwidth(axisMain), fwidth(axisCross));
                float hatchStability = saturate(1.0 - hatchFootprint * _HatchFade);
                float hatch = hatchMask * shadeMask * _HatchStrength * hatchStability;

                float noise = Hash21(i.positionOS.xz * _PaperNoiseScale) - 0.5;
                float3 baseTone = lerp(_BaseColor.rgb, _ShadowColor.rgb, shadeMask);
                baseTone *= (1.0 + noise * _PaperNoiseStrength);

                float3 color = lerp(baseTone, _LineColor.rgb, saturate(hatch));
                color = lerp(color, _LineColor.rgb, saturate(edge));
                color *= mainLight.color;

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
