Shader "Custom/BiodiversityTerrain"
{
    Properties
    {
        [Header(Base Terrain Textures)]
        _BaseTex ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        
        [Header(Simpson Diversity Saturation)]
        _BaseSaturation ("Base Saturation", Range(0, 2)) = 1.0
        [Toggle] _UseSimpsonsIndex ("Use Simpson's Index", Float) = 1
        _DiversityIntensity ("Diversity Effect Intensity", Range(0, 5)) = 2.0
        _MinSaturation ("Min Saturation (Low Diversity)", Range(0, 1)) = 0.1
        _MaxSaturation ("Max Saturation (High Diversity)", Range(1, 5)) = 3.0
        
        [Header(Spotlight Hotspot Effect)]
        [Toggle] _UseSpotlightEffect ("Enable Spotlight Hotspots", Float) = 1
        _SpotlightIntensity ("Spotlight Brightness", Range(1, 10)) = 4.0
        _HotspotThreshold ("Hotspot Threshold", Range(0.5, 1)) = 0.7
        _HotspotGlow ("Hotspot Glow Color", Color) = (1, 1, 0.5, 1)
        
        [Header(Material Properties)]
        _TexScale ("Texture Scale", Float) = 1.0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        [Header(Animation)]
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1.0
        _PulseIntensity ("Pulse Intensity", Range(0, 1)) = 0.1
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        
        sampler2D _BaseTex;
        
        float4 _BaseColor;
        
        float _TexScale;
        half _Glossiness;
        half _Metallic;
        float _BaseSaturation;
        float _UseSimpsonsIndex;
        float _DiversityIntensity;
        float _MinSaturation;
        float _MaxSaturation;
        float _PulseSpeed;
        float _PulseIntensity;
        float _UseSpotlightEffect;
        float _SpotlightIntensity;
        float _HotspotThreshold;
        float4 _HotspotGlow;
        
        // Global properties set by BiodiversityScoreManager
        float _GlobalDiversitySaturation;
        
        struct Input
        {
            float2 uv_BaseTex;
            float3 worldPos;
        };
        
        // Per-instance properties for local diversity effects
        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float, _LocalDiversitySaturation)
            UNITY_DEFINE_INSTANCED_PROP(float, _SimpsonsIndex)
        UNITY_INSTANCING_BUFFER_END(Props)
        
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
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Sample base texture
            float2 texCoord = IN.worldPos.xz * _TexScale;
            fixed4 baseTex = tex2D(_BaseTex, texCoord);
            fixed4 baseColor = baseTex * _BaseColor;
            
            // Get diversity data
            float localDiversitySaturation = UNITY_ACCESS_INSTANCED_PROP(Props, _LocalDiversitySaturation);
            float simpsonsIndex = UNITY_ACCESS_INSTANCED_PROP(Props, _SimpsonsIndex);
            
            // Start with base saturation
            float finalSaturation = _BaseSaturation;
            float brightness = 1.0;
            float3 finalColor = baseColor.rgb;
            
            // Apply Simpson's diversity saturation if enabled
            if (_UseSimpsonsIndex > 0.5)
            {
                // Use local diversity saturation if available, otherwise use global
                float diversitySaturation = localDiversitySaturation > 0 ? localDiversitySaturation : _GlobalDiversitySaturation;
                
                // Map Simpson's index to saturation range (much more dramatic)
                float saturationMultiplier = lerp(_MinSaturation, _MaxSaturation, simpsonsIndex);
                
                // Apply with intensity control
                finalSaturation *= lerp(1.0, saturationMultiplier, _DiversityIntensity);
                
                // SPOTLIGHT HOTSPOT EFFECT
                if (_UseSpotlightEffect > 0.5 && simpsonsIndex > _HotspotThreshold)
                {
                    // Calculate hotspot intensity
                    float hotspotStrength = (simpsonsIndex - _HotspotThreshold) / (1.0 - _HotspotThreshold);
                    
                    // Dramatic brightness boost for hotspots
                    brightness += hotspotStrength * _SpotlightIntensity;
                    
                    // Extra saturation for hotspots
                    finalSaturation *= (1.0 + hotspotStrength * 2.0);
                    
                    // Add warm glow color to hotspots
                    float3 glowColor = _HotspotGlow.rgb;
                    finalColor = lerp(finalColor, finalColor * glowColor, hotspotStrength * 0.3);
                    
                    // Pulsing effect for extreme hotspots
                    if (simpsonsIndex > 0.85)
                    {
                        float pulse = 1.0 + sin(_Time.y * _PulseSpeed * 2.0) * _PulseIntensity * hotspotStrength;
                        brightness *= pulse;
                        finalSaturation *= pulse;
                    }
                }
                
                // Subtle pulse for moderate diversity
                else if (simpsonsIndex > 0.5)
                {
                    float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseIntensity * 0.5;
                    finalSaturation *= pulse;
                }
            }
            
            // Convert to HSV for dramatic saturation adjustment
            float3 hsv = rgb2hsv(finalColor);
            hsv.y *= finalSaturation; // Apply dramatic saturation
            hsv.y = saturate(hsv.y);  // Clamp to valid range
            hsv.z *= brightness;      // Apply brightness boost
            hsv.z = saturate(hsv.z);  // Clamp brightness
            
            // Convert back to RGB
            finalColor = hsv2rgb(hsv);
            
            // Final color output
            o.Albedo = finalColor;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = baseColor.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}