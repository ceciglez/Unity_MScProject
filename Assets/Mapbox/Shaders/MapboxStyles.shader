// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "Mapbox/MapboxStyles"
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
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		float4 _BaseColor;
		float4 _DetailColor1;
		float4 _DetailColor2;

		sampler2D _BaseTex;
		sampler2D _DetailTex1;
		sampler2D _DetailTex2;

		float _Emission;
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
			float2 uv_BaseTex, uv_DetailTex1, uv_DetailTex2;
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



		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 baseTexture = tex2D (_BaseTex, IN.uv_BaseTex);

			fixed4 detailTexture1 = tex2D (_DetailTex1, IN.uv_DetailTex1);
			fixed4 detailTexture2 = tex2D (_DetailTex2, IN.uv_DetailTex2);

			fixed4 baseDetail1_Result = lerp(_BaseColor, _DetailColor1, detailTexture1.a);

			fixed4 detail1Detail2_Result  = lerp(baseDetail1_Result, _DetailColor2, detailTexture2.a);

			fixed4 c = baseTexture *= detail1Detail2_Result;
			
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
			
			half3 e = c.rgb;

			o.Albedo = c.rgb;
			o.Emission = e * _Emission;
			o.Alpha = 1.0;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
