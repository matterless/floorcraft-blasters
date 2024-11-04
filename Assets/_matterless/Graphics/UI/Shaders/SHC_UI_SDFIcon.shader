Shader "UI/SDF_Icon"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        
        _Softness("Softness", Float) = 0.05
        _OutlineSize("Outline Size", Float) = 0.5
        _ShadowColor ("Shadow Color", Color) = (0.1, 0.1, 0.1, 1)
        _ShadowDilate ("Shadow Dilate", Float) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Fog {Mode Off}
        ZTest Always
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 param : TEXCOORD2; // x: bias y: softness z: outlineSize w: scale
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float _Softness;
            fixed4 _ShadowColor;
            float _ShadowDilate;
            float _OutlineSize;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                float scale = rsqrt(dot(OUT.vertex.w, OUT.vertex.w)) * 5.0f;
                float bias = 0.5 + (0.5/scale);
                float softness = _Softness * scale;
                float outline = _OutlineSize * scale;

                OUT.param = float4(bias, softness, outline, scale);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                
                
                return OUT;
            }

            #define shadowOffset float2(0, 0.005f)
            
            fixed4 frag(v2f IN) : SV_Target
            {
                float distance = tex2D(_MainTex, IN.texcoord).a;
                
                float sd = (IN.param.x - distance) * IN.param.w;

                half alpha = 1 - saturate((sd - IN.param.z * 0.5 + IN.param.y * 0.5) / (1.0 + IN.param.y));
                half outlineAlpha = saturate((sd + IN.param.z * 0.5)) * sqrt(min(1.0, IN.param.z));

                half3 faceColor = IN.color.rgb;
                half3 outlineColor = float3(0, 0, 0) * outlineAlpha;
                fixed4 color = float4(0.0f, 0.0f, 0.0f, 1.0f);
                color.rgb = lerp(faceColor, outlineColor, outlineAlpha);
                color *= alpha;
                
                float shadowDistance = tex2D(_MainTex, IN.texcoord.xy + shadowOffset).a * 2;
                color += _ShadowColor * saturate(shadowDistance - _ShadowDilate) * (1 - color.a);

                color *= IN.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                
                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}