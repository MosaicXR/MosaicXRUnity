Shader "Stereoscopic/StereoImage_SideBySide"
{
	//reference this:
	//   https://docs.unity3d.com/Manual/SinglePassStereoRendering.html
	// this assumes left eye is on left of texture
	Properties
	{
		[NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
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
			
			    	UNITY_VERTEX_INPUT_INSTANCE_ID //Insert
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 uv : TEXCOORD0;
			   
				UNITY_VERTEX_OUTPUT_STEREO //Insert
			};

			sampler2D _MainTex;
			
			v2f vert (appdata v)
			{
				v2f o;

			    	UNITY_SETUP_INSTANCE_ID(v); //Insert
    				UNITY_INITIALIZE_OUTPUT(v2f, o); //Insert
    				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); //Insert

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = float4(v.uv * fixed2(0.5,1) + fixed2(unity_StereoEyeIndex * 0.5, 0), v.uv);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv.xy, ddx(i.uv.zw), ddy(i.uv.zw));
				return col;
			}
			ENDCG
		}
	}
}