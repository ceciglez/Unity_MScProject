Shader "Custom/HeightBasedTerrain"
{
    Properties
    {
        [Header(Low Elevation)]
        _LowTex ("Low Texture (Grass/Valley)", 2D) = "white" {}
        _LowColor ("Low Tint", Color) = (1,1,1,1)
        
        [Header(Mid Elevation)]
        _MidTex ("Mid Texture (Dirt/Hill)", 2D) = "white" {}
        _MidColor ("Mid Tint", Color) = (1,1,1,1)
        
        [Header(High Elevation)]
        _HighTex ("High Texture (Rock/Snow)", 2D) = "white" {}
        _HighColor ("High Tint", Color) = (1,1,1,1)
        
        [Header(Height Thresholds)]
        _LowThreshold ("Low Threshold", Range(-100, 500)) = 0
        _MidThreshold ("Mid Threshold", Range(-100, 500)) = 50
        _HighThreshold ("High Threshold", Range(-100, 500)) = 150
        _BlendDistance ("Blend Distance", Range(1, 100)) = 10
        
        [Header(Settings)]
        _TexScale ("Texture Scale", Float) = 1.0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        [Header(Biodiversity Effects)]
        [Toggle] _UseBiodiversitySaturation ("Use Biodiversity Saturation", Float) = 1
        _BiodiversityIntensity ("Biodiversity Effect Intensity", Range(0, 5)) = 1.5
        [Toggle] _UseSpotlightEffect ("Enable Dramatic Hotspots", Float) = 1
        _SpotlightBoost ("Hotspot Brightness Boost", Range(1, 8)) = 3.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        
        sampler2D _LowTex;
        sampler2D _MidTex;
        sampler2D _HighTex;
        
        float4 _LowColor;
        float4 _MidColor;
        float4 _HighColor;
        
        float _TexScale;
        half _Glossiness;
        half _Metallic;
        
        float _LowThreshold;
        float _MidThreshold;
        float _HighThreshold;
        float _BlendDistance;
        float _UseBiodiversitySaturation;
        float _BiodiversityIntensity;
        // float _UseSpotlightEffect; // Removed duplicate
        float _SpotlightBoost;
        
        // Global biodiversity properties
        float _GlobalSaturation;
        float _SpotlightIntensity;
        float _UseSpotlightEffect;
        
        struct Input
        {
            float2 uv_LowTex;
            float3 worldPos;
        };
        
        // HSV conversion functions for saturation adjustment
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
            // Use world position for texture coordinates (triplanar-like)
            float2 texCoord = IN.worldPos.xz * _TexScale;
            
            // Sample all three textures
            fixed4 lowTex = tex2D(_LowTex, texCoord) * _LowColor;
            fixed4 midTex = tex2D(_MidTex, texCoord) * _MidColor;
            fixed4 highTex = tex2D(_HighTex, texCoord) * _HighColor;
            
            // Calculate weights based on height
            float height = IN.worldPos.y;
            
            float lowWeight = 1.0 - saturate((height - _LowThreshold) / _BlendDistance);
            float midWeight = 1.0 - saturate(abs(height - _MidThreshold) / _BlendDistance);
            float highWeight = saturate((height - _HighThreshold) / _BlendDistance);
            
            // Normalize weights
            float totalWeight = lowWeight + midWeight + highWeight;
            if (totalWeight > 0)
            {
                lowWeight /= totalWeight;
                midWeight /= totalWeight;
                highWeight /= totalWeight;
            }
            
            // Blend textures
            fixed4 finalColor = lowTex * lowWeight + midTex * midWeight + highTex * highWeight;
            
            // Apply biodiversity saturation if enabled
            if (_UseBiodiversitySaturation > 0.5 && _GlobalSaturation > 0)
            {
                // Convert to HSV for saturation adjustment
                float3 hsv = rgb2hsv(finalColor.rgb);
                
                // Apply global biodiversity saturation with dramatic effect
                float saturationMultiplier = lerp(1.0, _GlobalSaturation, _BiodiversityIntensity);
                hsv.y *= saturationMultiplier;
                hsv.y = saturate(hsv.y);
                
                // DRAMATIC SPOTLIGHT EFFECT for hotspots
                if (_UseSpotlightEffect > 0.5 && _GlobalSaturation > 2.0)
                {
                    // Brightness boost for biodiversity hotspots
                    float hotspotStrength = saturate((_GlobalSaturation - 2.0) / 2.0);
                    hsv.z *= (1.0 + hotspotStrength * _SpotlightBoost);
                    hsv.z = saturate(hsv.z);
                    
                    // Extra saturation for hotspots
                    hsv.y *= (1.0 + hotspotStrength);
                    hsv.y = saturate(hsv.y);
                }
                
                // Convert back to RGB
                finalColor.rgb = hsv2rgb(hsv);
            }
            
            o.Albedo = finalColor.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    
    FallBack "Diffuse"
}
