// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Matterless/UI/BoostCharge"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}          // UI image component requires this to be named _MainTex even tho we don't use it
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)                    // Background -> Reuse the color property from the UI image component
        _ChargingColorDark ("Charging Dark Color", Color) = (0.5,0.5,0.5,1)     // Dark Blue
        _ChargingColorLight ("Charging Light Color", Color) = (0.5,0.5,1,1)     // Light Blue
        _FullChargeColorDark ("Full Charge Dark Color", Color) = (1,1,1,1)           // Dark Orange
        _FullChargeColorLight ("Full Charge Light Color", Color) = (1,1,1,1)          // Light Orange
        _Overlay ("Overlay", 2D) = "white" {}
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255

        [HideInInspector] _ColorMask ("Color Mask", Float) = 15

        [HideInInspector] [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
        
            float _Fill;

            sampler2D _MainTex;
            sampler2D _Overlay;
            fixed4 _Color; // Background
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            fixed4 _ChargingColorDark;
            fixed4 _ChargingColorLight;
            fixed4 _FullChargeColorDark;
            fixed4 _FullChargeColorLight;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.screenCoordinates = ComputeScreenPos(OUT.vertex) * float2(_ScreenParams.x/_ScreenParams.y, 1);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

                OUT.color = v.color * _Color;
                return OUT;
            }

            #define ANGLE -0.05
            #define OVERLAY_TILING 12
            #define OVERLAY_STRENGTH 0.06
            
            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;

                float mask = 1 - ((uv.x - _Fill + uv.y * ANGLE) > 0.0f);
                
                const half4 chargingGradient = lerp(_ChargingColorDark, _ChargingColorLight, uv.x - uv.y );
                const half4 fullChargeGradient = lerp(_FullChargeColorDark, _FullChargeColorLight, uv.x - uv.y);
                half overlay = tex2D(_Overlay, IN.screenCoordinates * OVERLAY_TILING).r;
                overlay = saturate(overlay * (1.0 - IN.texcoord.y * 2)) * OVERLAY_STRENGTH * mask;
                
                half4 color;
                color.xyz = lerp(IN.color, chargingGradient, mask);
                color.xyz = lerp(color, fullChargeGradient, _Fill > 0.99f);
                color.xyz = saturate(color.xyz + overlay);
                color.a = 1.0f;
                
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