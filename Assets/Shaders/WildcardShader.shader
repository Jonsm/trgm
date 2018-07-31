Shader "Unlit/WildcardShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color1 ("Color1", Color) = (0,0,0,0)
		_Color2 ("Color2", Color) = (0,0,0,0)
		_Color3 ("Color3", Color) = (0,0,0,0)
		_Freq ("Frequency", Float) = 0
		_Tint ("Tint", Float) = 0
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
			float4 _Color1;
			float4 _Color2;
			float4 _Color3;
			float _Freq;
			float _Tint;
			
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
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				float t = _Time.y;
				float pi = 3.1415927;
				float c1 = pow(sin((2*pi*_Freq)*t),2);
				float c2 = pow(sin((2*pi*_Freq)*t+2*pi/3),2);
				float c3 = pow(sin((2*pi*_Freq)*t+4*pi/3),2);
				float4 blended = c1*_Color1 + c2*_Color2 + c3*_Color3;


				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col*(1-_Tint) + _Tint*blended;
			}
			ENDCG
		}
	}
}
