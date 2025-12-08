Shader "Custom/SimpleBiodiversityTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        [Toggle] _UseSimpsonsIndex ("Use Simpson's Index", Float) = 1
        _DiversityIntensity ("Diversity Effect Intensity", Range(0, 5)) = 2.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _UseSimpsonsIndex;
            float _DiversityIntensity;
            
            // Global biodiversity property
            float _GlobalDiversitySaturation;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the texture
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                
                // Apply simple biodiversity effect
                if (_UseSimpsonsIndex > 0.5 && _GlobalDiversitySaturation > 0)
                {
                    // Simple saturation adjustment - no HSV conversion needed
                    float saturation = _GlobalDiversitySaturation * _DiversityIntensity;
                    
                    // Desaturate by lerping toward grayscale
                    float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                    col.rgb = lerp(float3(gray, gray, gray), col.rgb * saturation, 0.8);
                    
                    // Add some brightness if high saturation
                    if (saturation > 2.0)
                    {
                        col.rgb *= 1.5; // Brightness boost
                    }
                }
                
                return col;
            }
            ENDCG
        }
    }
}