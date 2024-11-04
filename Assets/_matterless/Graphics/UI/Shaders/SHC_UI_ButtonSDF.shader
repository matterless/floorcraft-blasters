// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Matterless/UI/ButtonSDF"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [NoScaleOffset] _Overlay ("Overlay", 2D) = "white" {}
        _SecondaryColor ("Secondary Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        [Toggle(OUTLINE)] _UseOutline ("Outline", Float) = 0
        [Toggle(GRADIENT)] _UseGradient ("Gradient", Float) = 0
        [Toggle(OVERLAY)] _UseOverlay ("Overlay", Float) = 0
        [Toggle(ONLY_OUTLINE)] _UseOnlyOutline ("Only Outline", Float) = 0
        [Toggle(TOON_OUTLINE)] _UseToonOutline ("Toon Outline", Float) = 0
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
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

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
            #pragma multi_compile_local _ OUTLINE
            #pragma multi_compile_local _ GRADIENT
            #pragma multi_compile_local _ OVERLAY
            #pragma multi_compile_local _ ONLY_OUTLINE
            #pragma multi_compile_local _ TOON_OUTLINE

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
                float2 screenCoordinates : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _Overlay;
            sampler2D _Stripes;
            fixed4 _Color;
            fixed4 _SecondaryColor;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _Alpha;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.screenCoordinates = ComputeScreenPos(OUT.vertex) * float2(_ScreenParams.x/_ScreenParams.y, 1);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                

                OUT.color = v.color;
                return OUT;
            }

            #define OVERLAY_TILING 13
            #define OVERLAY_STRENGTH 0.08
        

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color;
                half d = tex2D(_MainTex, IN.texcoord).r;
                
                #ifdef GRADIENT
                    color = lerp(_Color, _SecondaryColor, saturate(IN.texcoord.x - IN.texcoord.y - 0.1) );
                #else
                    color.xyz = IN.color.xyz;
                #endif
                
                #ifdef OUTLINE
                    #ifdef TOON_OUTLINE
                        float alpha = smoothstep(0.65, 0.75, d);
                    #else
                        float alpha = smoothstep(0.45, 0.55, d);
                    #endif
                    color.xyz = lerp(color, float3(1, 1, 1), IN.texcoord.y * saturate(IN.color.a * 2.0 - 1.0) * (1.0 - alpha));
                #endif
                
                #ifdef OVERLAY
                    half overlay = tex2D(_Overlay, IN.screenCoordinates * OVERLAY_TILING).r;
                    overlay = saturate(overlay * (1.0 - IN.texcoord.y * 2)) * OVERLAY_STRENGTH;
                    color.xyz = saturate(color.xyz + overlay);
                #endif

                #ifdef TOON_OUTLINE
                    float alpha2 = smoothstep(0.40, 0.50, d);
                    color.xyz = lerp(color, float3(0, 0, 0), (1.0 - alpha2));
                #endif
                

                #ifdef ONLY_OUTLINE
                    #ifndef OUTLINE
                        color.a = smoothstep(0.1, 0.2, d) * (IN.color.a) * 1 - smoothstep(0.40, 0.50, d);//alpha2;
                    #else
                        color.a = smoothstep(0.1, 0.2, d) * (IN.color.a) * 1 - alpha;
                    #endif
                #else
                    color.a = smoothstep(0.1, 0.2, d) * (IN.color.a);
                #endif
                
                
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