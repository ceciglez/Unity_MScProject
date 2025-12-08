Shader "Custom/BiodiversityFullScreen"
{
    Properties
    {
        _MainTex("Source", 2D) = "white" {}
        _GlobalSaturation("Global Saturation", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "BiodiversityFullScreenPass"

            HLSLPROGRAM


            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_MainTex);
            SAMPLER(sampler_MainTex);

            float _GlobalSaturation;
            int _ShowDebug;

            // Optional hotspot data
            float4 _HotspotPositions[20];
            float _HotspotRadii[20];
            int _HotspotCount;
            float _FalloffPower;

            // RGB to HSV conversion
            float3 RGBtoHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            // HSV to RGB conversion
            float3 HSVtoRGB(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float4 color = SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, uv);

                // Convert to HSV for saturation manipulation
                float3 hsv = RGBtoHSV(color.rgb);


                // Clamp _GlobalSaturation to [0.1, 3.0] for safety
                float safeSat = clamp(_GlobalSaturation, 0.1, 3.0);

                // Always neutral at 1.0, never fully dark
                if (abs(safeSat - 1.0) < 0.01)
                {
                    hsv.y = hsv.y;
                    hsv.z = hsv.z;
                }
                else if (safeSat < 1.0)
                {
                    // Desaturate, but never fully gray or dark
                    hsv.y = lerp(hsv.y, 0.15, 1.0 - safeSat);
                    hsv.z = max(lerp(hsv.z, 0.85, 1.0 - safeSat), 0.85); // Never below 0.85
                }
                else
                {
                    // Boost saturation, but clamp
                    hsv.y = min(hsv.y * safeSat, 1.0);
                    hsv.z = min(hsv.z * lerp(1.0, 1.15, (safeSat-1.0)/2.0), 1.0);
                }

                // Convert back to RGB
                float3 finalColor = HSVtoRGB(hsv);

                // If _GlobalSaturation is way out of range, show debug magenta
                if (safeSat < 0.05 || safeSat > 5.0)
                {
                    finalColor = float3(1,0,1);
                }

                // Optional: Hotspot-based effects (if supported)
                if (_HotspotCount > 0)
                {
                    // Simple screen-space hotspot effect
                    float2 screenCenter = float2(0.5, 0.5);
                    float distanceFromCenter = distance(uv, screenCenter);
                    
                    // Create a radial effect based on hotspots
                    float hotspotInfluence = 0.0;
                    for (int i = 0; i < min(_HotspotCount, 5); i++) // Limit for performance
                    {
                        float intensity = _HotspotPositions[i].w;
                        hotspotInfluence += intensity;
                    }
                    hotspotInfluence = saturate(hotspotInfluence / 5.0);
                    
                    // Apply subtle radial enhancement for hotspot areas
                    if (hotspotInfluence > 0.5)
                    {
                        float radialBoost = 1.0 + (0.3 * hotspotInfluence * (1.0 - distanceFromCenter));
                        finalColor *= radialBoost;
                    }
                }

                // Debug visualization
                if (_ShowDebug > 0)
                {
                    finalColor = lerp(finalColor, float3(1, 0, 1), 0.2);
                }

                return float4(finalColor, color.a);
            }
            ENDHLSL
        }
    }
}