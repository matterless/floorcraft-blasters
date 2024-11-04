Shader "Unlit/SHC_SP_Gems_00"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _FillAmmount ("Fill Ammount", Float) = 0
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

            fixed4 _Color;
            float4 _MainTex_ST;
            float _FillAmmount;

            v2f vert (appdata v)
            {
                v2f o;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                float3 animatedPosition = v.vertex.xyz;
                float radialAnimation = sin((_Time.z + floor(v.uv.x)*12 )  * 1) * .03f;
                float hecticAnimation = sin((_Time.z + floor(v.uv.x)*12 )  * 5) * .03f;
                animatedPosition.y += lerp(radialAnimation,hecticAnimation, _FillAmmount );
                float3 culledVertex = floor(v.uv.x + 1) > _FillAmmount * 13 ? 1.0f / 0.0f :  animatedPosition;
                o.vertex = UnityObjectToClipPos(culledVertex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = _Color;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
