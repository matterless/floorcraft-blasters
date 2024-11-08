Shader "Hidden/Matterless/DimmBackround"
{       
    Properties
    {
        _MainTex ("Texture", 2D) = "white"
    }

    SubShader
    {
        
        
        Tags 
        {
            "RenderPipeline" = "UniversalRenderPipeline" 
            "RenderType"="Opaque" 
        }
        ZTest Always
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

            sampler2D _MainTex;
            float _DimmAmmount;

            half4 frag (v2f i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.uv) * (1 - _DimmAmmount);
                col.a = 1;
                return col;
            }
            ENDCG
        }
    }
}
