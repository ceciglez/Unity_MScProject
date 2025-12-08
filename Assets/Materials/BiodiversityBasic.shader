Shader "Custom/BiodiversityBasic"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlobalSaturation ("Saturation", Float) = 1.0
        _ShowDebug ("Debug", Int) = 0
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
                
                // Simple saturation adjustment
                fixed gray = dot(col.rgb, fixed3(0.299, 0.587, 0.114));
                col.rgb = lerp(fixed3(gray, gray, gray), col.rgb, _GlobalSaturation);
                
                // Debug overlay
                if (_ShowDebug > 0)
                {
                    col.rgb = lerp(col.rgb, fixed3(1, 0, 1), 0.2);
                }
                
                return col;
            }
            ENDCG
        }
    }
}