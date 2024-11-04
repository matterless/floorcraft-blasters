Shader "Matterless/VFX/Electric Crackle FloorLight"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _LightColor ("Color", Color) = (1,1,1,1)
        [HDR] _GlowColor ("Glow Color", Color) = (1,1,1,1)
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

        Cull Back
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
                float2 texcoord1 : TEXCOORD1;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
        
            CBUFFER_START(UnityPerMaterial)
                fixed4 _LightColor;
                fixed4 _GlowColor;
                float _Offset;
                float _FlipBookOne;
                float _FlipBookTwo;
                float _FlipBookThree;
                float _FlipBookFour;
                float _SyncedTime;
            CBUFFER_END
        
            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.vertex = UnityObjectToClipPos(v.vertex);

                //Assumes flip book textures of size 2048x1024
                const float texelSize = 0.0625f; //1.0/2048.0f * 128.0f;

                const float offset = fmod(floor(_Offset), 128);
                
                const float xOffset = fmod(offset, 16) * texelSize;
                const float yOffset = floor(offset / 16.0f) * texelSize * 2.0f;
                
                const float x = fmod(floor(_SyncedTime * 12.0f), 16) * texelSize + xOffset;
                const float y = floor(_SyncedTime * 12.0f / 16.0f) * texelSize * 2.0f + yOffset;

                OUT.texcoord = float2(v.texcoord.x + x, v.texcoord.y + y);
                OUT.texcoord1 = v.texcoord1;
                
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 lightMap = tex2D(_MainTex, IN.texcoord.xy);

                half intensity = max(max(max(lightMap.r * _FlipBookOne, lightMap.g * _FlipBookTwo), lightMap.b * _FlipBookThree), lightMap.a * _FlipBookFour);
                
                float maskCircle = saturate(1 - (length(IN.texcoord1.xy - 0.5) * 2));
                float glowMask = saturate(1 - (length(IN.texcoord1.xy - 0.5) * 7));
                glowMask *= glowMask;
                
                float alpha  = intensity * maskCircle;
                
                fixed4 color = ( _GlowColor * glowMask) + _LightColor * alpha;
                //color = lerp(_GlowColor * glowMask, _LightColor, alpha);
                return color;
            }
        ENDCG
        }
    }
}