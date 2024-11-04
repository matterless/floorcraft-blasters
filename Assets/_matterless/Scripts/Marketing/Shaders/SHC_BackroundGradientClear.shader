Shader "Hidden/Matterless/BackroundGradientClear"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1,1,1,1)
        [HDR] _Color2 ("Color2", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags 
        {
            "RenderPipeline" = "UniversalRenderPipeline" 
            "RenderType"="Opaque" 
        }
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 _Color;
            float4 _Color2;
            sampler2D _BlueNoise;
            float4 _BlueNoise_TexelSize;
            
            #define A 0.95
            #define B 0.05
            #define C 1.6

            half4 frag (v2f i) : SV_Target
            {
                const float gradient = i.uv.y;
                float4 col = lerp(_Color2, _Color, gradient);

                float2 noiseUV = i.uv * _ScreenParams.xy * _BlueNoise_TexelSize.xy;
                half3 noise = tex2D(_BlueNoise, noiseUV);
 
                //Assume HDR format
                half3 dither = (noise * 2.0 - 0.5) / half3(352.0, 352, 320.0);
                col.rgb += dither;
                
                col.a = 1;

                return col;
            }
            ENDCG
        }
    }
}
