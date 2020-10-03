Shader "Gradient Skybox" {
	Properties {
		[Header(Gradient Colors)]
		_ColorT ("Top Color", Color) = (1,1,1,1)
		_ColorM ("Middle Color", Color) = (1,1,1,1)
		_ColorB ("Bottom Color", Color) = (1,1,1,1)
		
		[Header(Gradient Settings)]
		_ExponentT ("Upper Exponent", Float) = 1.0
		_ExponentB ("Lower Exponent", Float) = 1.0
		_Intensity ("Intensity", Float) = 1.0

		[Header(Hue Shift)]
		[Toggle(HUE_SHIFT)] _HueShift ("Shift Hue", Float) = 0
		[ShowIf(HUE_SHIFT)] _HueShiftAmount ("Hue Shift Amount", Range(0, 1)) = 0
		[ShowIf(HUE_SHIFT)] _HueShiftSpeed ("Hue Shift Speed", Float) = 0
		
		[Header(Sun Settings)]
		[Toggle(SUN)] _Sun ("Show Sun", Float) = 0
		[ShowIf(SUN)] _SunColor("Sun Color", Color) = (1, 1, 1, 1)
		[ShowIf(SUN)] _SunSize("Sun Size", Range(0,1)) = 0.3
		[ShowIf(SUN)] _SunAzimuth("Sun Azimuth", Range(0,360)) = 60
		[ShowIf(SUN)] _SunAltitude("Sun Altitude", Range(-90,90)) = 30
		[ShowIf(SUN)] _SunIntensity("Sun Intensity", Float) = 2.0
		[HideInInspector] _SunVector("Sun Vector", Vector) = (1,0,0,0)
		
		[Header(Additional Settings)]
		[Toggle(DITHER)] _Dither ("Add Screenspace Dither", Float) = 0
	}
	SubShader {
		Tags { "RenderType"="Background" "Queue"="Background" "DisableBatching"="True" "IgnoreProjector"="True" "PreviewType"="Skybox" }
		Fog { Mode Off }
		ZWrite Off
		Cull Back

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature SUN
			#pragma shader_feature DITHER
			#pragma shader_feature HUE_SHIFT
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "UnityCG.cginc"

			fixed3 _ColorT;
			fixed3 _ColorM;
			fixed3 _ColorB;
			half _ExponentT;
			half _ExponentB;
			half _Intensity;

			#ifdef SUN
			fixed3 _SunColor;
			half _SunSize;
			half _SunIntensity;
			half3 _SunVector;
			#endif

			#ifdef HUE_SHIFT
			half _HueShiftAmount, _HueShiftSpeed;
			#endif
	
			#ifdef DITHER
			float3 ScreenSpaceDither(float2 screenpos)
			{
				float3 dither = dot(float2(171.0, 231.0), screenpos + _Time.yy).xxx;
				dither.rgb = frac(dither / float3(103.0, 71.0, 97.0)) - float3(0.5, 0.5, 0.5);
				return (dither / 255.0);
			}
			#endif
			
			struct appdata {
				float4 vertex : POSITION;
				float3 texcoord : TEXCOORD0;
			};

			struct Varyings {
				float4 vertex : SV_POSITION;
				float3 texcoord : TEXCOORD0;
			};

			Varyings vert(appdata v) {
				Varyings o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.texcoord;
				return o;
			}

			half3 rgb2hsv(in half3 c) {
				half4 K = half4(0., -.33333333333333333333, .6666666666666666666, -1.);
				half4 p = c.g < c.b ? half4(c.bg, K.wz) : half4(c.gb, K.xy);
				half4 q = c.r < p.x ? half4(p.xyw, c.r) : half4(c.r, p.yzx);
				float d = q.x - min(q.w, q.y);
				float e = 1.0e-10;
				return half3(abs(q.z + (q.w - q.y) / (6. * d + e)), 
							d / (q.x + e), 
							q.x);
			}

			half3 hsv2rgb(in half3 hsb) {
				half3 rgb = clamp(abs(fmod(hsb.x * 6. + half3(0., 4., 2.), 
										6.) - 3.) - 1.,
									0.,
									1.);
				return hsb.z * lerp(half3(1., 1., 1.), rgb, hsb.y);
			}
			
			fixed4 frag (Varyings i) : SV_Target {
				float3 n = normalize(i.texcoord);
			
				float factorT = 1.0 - pow(min(1.0, 1.0 - n.y), _ExponentT);
				float factorB = 1.0 - pow(min(1.0, 1.0 + n.y), _ExponentB);
				float factorM = 1.0 - factorT - factorB;
				
				fixed3 color = (_ColorT * factorT + _ColorM * factorM + _ColorB * factorB) * _Intensity;
				
				#if SUN
				color += min(pow(max(0, dot(n, _SunVector)), pow(50000.0, 1.0 - _SunSize)), 1) * _SunColor * _SunIntensity;
				#endif

				#ifdef HUE_SHIFT
				half3 hsv = rgb2hsv(color);
				hsv.x -= (sin(_Time.y * _HueShiftSpeed) * .5 + .5) * _HueShiftAmount;
				color = hsv2rgb(hsv);
				#endif

				#if DITHER
				color += ScreenSpaceDither(i.vertex.xy);
				#endif

				return fixed4(color, 1);
			}
			ENDCG
		}
	}
}