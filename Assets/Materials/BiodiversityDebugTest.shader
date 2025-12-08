Shader "Custom/BiodiversityDebugTest"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlobalSaturation ("Saturation", Float) = 1.0
        _ShowDebug ("Debug", Int) = 0
        _TestColor ("Test Color", Color) = (1,0,1,1)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZWrite Off 
        ZTest Always
        
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
            float _GlobalSaturation;
            int _ShowDebug;
            fixed4 _TestColor;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // EXTREME saturation test - should be very obvious
                fixed gray = dot(col.rgb, fixed3(0.299, 0.587, 0.114));
                
                // More dramatic saturation effect
                if (_GlobalSaturation > 1.5)
                {
                    // Hyper-saturate
                    col.rgb = lerp(fixed3(gray, gray, gray), col.rgb, _GlobalSaturation * 2.0);
                }
                else if (_GlobalSaturation < 0.5)
                {
                    // Heavy desaturation
                    col.rgb = lerp(col.rgb, fixed3(gray, gray, gray), 0.8);
                }
                else
                {
                    // Normal saturation
                    col.rgb = lerp(fixed3(gray, gray, gray), col.rgb, _GlobalSaturation);
                }
                
                // Debug overlay - should be VERY visible
                if (_ShowDebug > 0)
                {
                    // Add strong pink overlay
                    col.rgb = lerp(col.rgb, _TestColor.rgb, 0.5);
                }
                
                // Additional test - add green tint to confirm shader is running
                if (_GlobalSaturation > 2.5)
                {
                    col.rgb += fixed3(0, 0.2, 0); // Green tint
                }
                
                return col;
            }
            ENDCG
        }
    }
}