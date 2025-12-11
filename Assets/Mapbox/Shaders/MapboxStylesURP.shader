Shader "Mapbox/MapboxStylesURP"
{
    Properties
    {
        _BaseColor ("BaseColor", Color) = (1,1,1,1)
        _DetailColor1 ("DetailColor1", Color) = (1,1,1,1)
        _DetailColor2 ("DetailColor2", Color) = (1,1,1,1)

        _BaseTex ("Base", 2D) = "white" {}
        _DetailTex1 ("Detail_1", 2D) = "white" {}
        _DetailTex2 ("Detail_2", 2D) = "white" {}

        _Emission ("Emission", Range(0.0, 1.0)) = 0.1

        [Header(Biodiversity Effects)]
        [Toggle] _UseBiodiversitySaturation ("Use Biodiversity Saturation", Float) = 1
        _BiodiversityIntensity ("Biodiversity Effect Intensity", Range(0, 2)) = 0.8
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            TEXTURE2D(_BaseTex);
            SAMPLER(sampler_BaseTex);
            TEXTURE2D(_DetailTex1);
            SAMPLER(sampler_DetailTex1);
            TEXTURE2D(_DetailTex2);
            SAMPLER(sampler_DetailTex2);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _DetailColor1;
                float4 _DetailColor2;
                float4 _BaseTex_ST;
                float4 _DetailTex1_ST;
                float4 _DetailTex2_ST;
                float _Emission;
                float _UseBiodiversitySaturation;
                float _BiodiversityIntensity;
                float _SpotlightBoost;

                // Global biodiversity properties
                float _GlobalSaturation;
                float _SpotlightIntensity;
                float _UseSpotlightEffect;
            CBUFFER_END

            // HSV conversion functions
            float3 rgb2hsv(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample textures
                float4 baseTexture = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, input.uv);
                float4 detailTexture1 = SAMPLE_TEXTURE2D(_DetailTex1, sampler_DetailTex1, input.uv);
                float4 detailTexture2 = SAMPLE_TEXTURE2D(_DetailTex2, sampler_DetailTex2, input.uv);

                // Blend colors based on detail texture alphas
                float4 baseDetail1_Result = lerp(_BaseColor, _DetailColor1, detailTexture1.a);
                float4 detail1Detail2_Result = lerp(baseDetail1_Result, _DetailColor2, detailTexture2.a);
                float4 c = baseTexture * detail1Detail2_Result;

                // Apply biodiversity saturation if enabled
                if (_UseBiodiversitySaturation > 0.5 && _GlobalSaturation > 0)
                {
                    // Convert to HSV for saturation adjustment
                    float3 hsv = rgb2hsv(c.rgb);

                    // Apply global biodiversity saturation with dramatic effect
                    float saturationMultiplier = lerp(1.0, _GlobalSaturation, _BiodiversityIntensity);
                    hsv.y *= saturationMultiplier;
                    hsv.y = saturate(hsv.y);

                    // DRAMATIC SPOTLIGHT EFFECT for hotspots
                    if (_UseSpotlightEffect > 0.5 && _GlobalSaturation > 2.5)
                    {
                        // Brightness boost for biodiversity hotspots
                        float hotspotStrength = saturate((_GlobalSaturation - 2.5) / 2.0);
                        hsv.z *= (1.0 + hotspotStrength * _SpotlightBoost);
                        hsv.z = saturate(hsv.z);

                        // Extra saturation and slight color shift for hotspots
                        hsv.y *= (1.0 + hotspotStrength * 0.5);
                        hsv.x += hotspotStrength * 0.05; // Slight hue shift toward warmer colors
                        hsv.y = saturate(hsv.y);
                        hsv.x = frac(hsv.x);
                    }

                    // Convert back to RGB
                    c.rgb = hsv2rgb(hsv);
                }

                // Simple lighting
                Light mainLight = GetMainLight();
                float3 normalWS = normalize(input.normalWS);
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 lighting = mainLight.color * NdotL + half3(0.2, 0.2, 0.2); // ambient

                // Apply lighting and emission
                float3 finalColor = c.rgb * lighting;
                float3 emission = c.rgb * _Emission;
                finalColor += emission;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // Shadow casting pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma target 3.0

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
            };

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
