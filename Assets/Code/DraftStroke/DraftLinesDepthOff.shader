Shader "NoteCAD/DraftLinesDepthOff" {

	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_Width("Width", Float) = 1.0
		_StippleWidth("StippleWidth", Float) = 1.0
		[HideInInspector]
		_PatternLength("PatternLength", Float) = 1.0
		[HideInInspector]
		_Pixel("Pixel", Float) = 1.0
		[HideInInspector]
		_CamDir("CamDir", Vector) = (1,1,1,0)
		_CamRight("CamRight", Vector) = (1,0,1,0)
		_Color("Color", Color) = (1,1,1,1)
	}
	
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100
		AlphaToMask On

		Pass {
			Offset -1, -1
			ZTest Always
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing				
			
			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 tangent: NORMAL;
				float4 params: TANGENT;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 cap: TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Width;
			float _StippleWidth;
			float _PatternLength;
			float _Pixel;
			float4 _CamDir;
			float4 _CamRight;
			fixed4 _Color;

			v2f vert (appdata v) {
				const float feather = 0.7;
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				float pix = _Pixel / 2.0;
				float3 tang = normalize(v.tangent.xyz);
				float3 dir = mul((float3x3)unity_WorldToObject, (float3)_CamDir);
				if (all(v.tangent.xyz == float3(0, 0, 0))) {
					tang = normalize(mul((float3x3)unity_WorldToObject, (float3)_CamRight));
				}
				float cap = _Width * pix + feather * _Pixel;
				float3 x = tang * cap;
				float3 y = normalize(cross(dir, tang)) * cap;

				float3 pos = v.vertex + v.params.x * x + v.params.y * y;

				o.vertex = UnityObjectToClipPos(pos);
				float2 uv = v.uv;
				uv.x += v.params.x * cap;
				float len = length(v.tangent.xyz);
				if(v.params.x == -1.0) {
					o.cap = float2(-cap / cap, len / cap);
				} else {
					o.cap = float2((len + cap) / cap, len / cap);
				}

				o.uv = TRANSFORM_TEX(uv, _MainTex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target {
				const float feather = 0.5;
				float patternScale = _PatternLength * _StippleWidth * _Pixel;
				fixed4 v = tex2D(_MainTex, float2(i.uv.x / patternScale, 0.0));
				float val = dot(v, float4(1.0, 1.0 / 255.0, 1.0 / 65025.0, 1.0 / 160581375.0));
				float pix = _Pixel / 2.0;
				float pat = val * patternScale / (_Width * pix);
				float c = (_Width / 2.0 + feather) / (_Width / 2.0);
				float cap = (max(i.cap.x - i.cap.y, 0.0) - min(i.cap.x, 0.0)) * c;
				float dist = length(float2(max(cap, pat), i.uv.y * c));

				float f = 2.0 * feather / (_Width / 2.0 + feather);
				float k = smoothstep(1.0 - f, 1.0+ f, dist);
				if (k == 1.0) discard;
				return float4(_Color.rgb, _Color.a * (1.0 - k));
			}
			ENDCG

		}
	}
}
