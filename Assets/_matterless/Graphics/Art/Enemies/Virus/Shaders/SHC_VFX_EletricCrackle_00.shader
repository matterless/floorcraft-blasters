Shader "Matterless/VFX/Electric Crackle"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (1,1,1,1)
        _Softness("Softness", Float) = 0.05
        _Offset("Offset", Range(0, 128)) = 0
        
        [Toggle(_FlipBookOne)] _FlipBookOne ("Flip Book One", Range(0, 1)) = 0
        [Toggle(_FlipBookTwo)] _FlipBookTwo ("Flip Book Two", Range(0, 1)) = 0
        [Toggle(_FlipBookThree)] _FlipBookThree ("Flip Book Three", Range(0, 1)) = 0
        [Toggle(_FlipBookFour)] _FlipBookFour ("Flip Book Four", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        Fog {Mode Off}
        ZWrite Off
        ZTest LEqual
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float4 texcoord  : TEXCOORD0;
                float softness : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            
        
            CBUFFER_START(UnityPerMaterial)
                fixed4 _Color;
                float _Offset;
                float _FlipBookOne;
                float _FlipBookTwo;
                float _FlipBookThree;
                float _FlipBookFour;
                float _Softness;
                float _SyncedTime;
            CBUFFER_END
        
            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.vertex = UnityObjectToClipPos(v.vertex);

                float pixelSize = OUT.vertex.w;
                pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float scale = rsqrt(dot(pixelSize, pixelSize)) * 5.0f;
                
                if (UNITY_MATRIX_P[3][3] == 0) scale = lerp(abs(scale) * (1 - 0), scale, abs(dot(UnityObjectToWorldNormal(v.normal.xyz), normalize(WorldSpaceViewDir(v.vertex)))));
                float bias = 0.5 + (0.5/scale);

                //Assumes flip book textures of size 2048x1024
                const float texelSize = 0.0625f; //1.0/2048.0f * 128.0f;

                const float offset = fmod(floor(_Offset), 128);
                
                const float xOffset = fmod(offset, 16) * texelSize;
                const float yOffset = floor(offset / 16.0f) * texelSize * 2.0f;
                
                const float x = fmod(floor(_SyncedTime * 12.0f), 16) * texelSize + xOffset;
                const float y = floor(_SyncedTime * 12.0f / 16.0f) * texelSize * 2.0f + yOffset;

                OUT.texcoord = float4(v.texcoord.x + x, v.texcoord.y + y, bias, scale);
                OUT.softness = _Softness * scale;
                
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                
                
                float4 dTex = tex2D(_MainTex, IN.texcoord.xy);
                
                float distance = max(max(max(dTex.r * _FlipBookOne, dTex.g * _FlipBookTwo), dTex.b * _FlipBookThree), dTex.a * _FlipBookFour);
                
                float sd = (IN.texcoord.z - distance) * IN.texcoord.w;

                half alpha = 1 - saturate((sd - 0.5f + IN.softness * 0.5) / (1.0 + IN.softness));
                fixed4 color = _Color * alpha;

                return color;
            }
        ENDCG
        }
    }
}