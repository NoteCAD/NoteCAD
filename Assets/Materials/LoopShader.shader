Shader "NoteCAD/LoopShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1) 
	}
	SubShader
	{
		Tags {
			"Queue" = "Transparent"
			"RenderType"="Transparent"
		}
		LOD 100
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		Offset -1, -1

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
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
			float4 _Color;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return _Color;
			}
			ENDCG
		}
	}
}
