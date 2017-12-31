Shader "NoteCAD/DraftLines" {
	
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_Width("Width", Float) = 1.0
		[HideInInspector]
		_Pixel("Pixel", Float) = 1.0
		[HideInInspector]
		_CamDir("CamDir", Vector) = (1,1,1,0)
		_Color("Color", Color) = (1,1,1,1)
	}
	
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 tangent: NORMAL;
				float4 params: TANGENT;
		};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Width;
			float _Pixel;
			float4 _CamDir;
			fixed4 _Color;
			
			v2f vert (appdata v) {
				v2f o;
				float pix = _Pixel / 2.0;
				float3 x = normalize(v.tangent) * _Width * pix;
				float3 y = normalize(cross(_CamDir.xyz, x)) * _Width * pix;

				float3 pos = v.vertex + v.params.x * x + v.params.y * y;

				o.vertex = UnityObjectToClipPos(pos);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				fixed4 col = tex2D(_MainTex, i.uv);
				return col * _Color;
			}
			ENDCG
		}
	}
}
