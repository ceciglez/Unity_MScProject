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
        
        struct Input
        {
            float2 uv_LowTex;
            float3 worldPos;
        };
        
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
            
            o.Albedo = finalColor.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    
    FallBack "Diffuse"
}
