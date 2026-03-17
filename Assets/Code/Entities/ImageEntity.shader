Shader "NoteCAD/ImageEntity"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o, o.vertex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// Quality downsampling without mipmaps
				float2 dx = ddx(i.uv);
				float2 dy = ddy(i.uv);
				int subdiv = 4;

				float4 col = float4(0.0, 0.0, 0.0, 0.0);

				for (int x = 0; x < subdiv; x++) {
					for (int y = 0; y < subdiv; y++) {
						col += tex2Dlod(_MainTex, float4(i.uv + dx * x / (subdiv) + dy * y / (subdiv), 0, 0));
					}
				}
				col /= subdiv * subdiv;
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
