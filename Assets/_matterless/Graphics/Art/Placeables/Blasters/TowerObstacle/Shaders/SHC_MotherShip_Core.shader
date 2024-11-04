Shader "Unlit/MotherShipCore"
{
    Properties
    {
        [NoScaleOffset] _SummonNoise ("Summon Noise Texture", 2D) = "white" {}
        _Summon ("Summon", Range(0, 1)) = 0.0
        _ObjectHeight ("Object Height", Float) = 5.0
        [NoScaleOffset] _EnergyCoreNoise ("Energy Core Noise", 2D) = "white" {}
        [HDR] _EnergyColor ("Energy Color", Color) = (1, 1, 1, 1)
        [HDR] _EnergyDamagedColor ("Energy Damaged Color", Color) = (1, 1, 1, 1)
        _Damage ("Damage", Range(0, 1)) = 0.0
        _IsHit ("Is Hit", Range(0, 1)) = 0.0
}
    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"  
            "Queue" = "Geometry" 
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Zwrite On
        ZTest LEqual
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #define NUMBER_OF_FRAMES 64.0f
            #define NUMBER_OF_ROWS 8.0f
            #define NUMBER_OF_COLUMNS 8.0f

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
            #include "MatterlessCommon.hlsl"

            struct Attributes
            {
                float2 texcoord : TEXCOORD0;
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD3;
            };

            sampler _EnergyCoreNoise;
            sampler _SummonNoise;
            
            CBUFFER_START(UnityPerMaterial)
                float _Summon;
                float _ObjectHeight;
                half4 _EnergyColor;
                half3 _EnergyDamagedColor;
                float _Damage;
                float _IsHit;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output = (Varyings)0;

                output.uv = input.texcoord;
                output.positionOS = input.positionOS.xyz;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                half4 col;
                half perlinNoise = tex2D(_EnergyCoreNoise, input.uv + float2(0, _Time.y * 0.05)).r;
                
                col.xyz = lerp(_EnergyColor, _EnergyDamagedColor, saturate(_Damage + _IsHit)) * perlinNoise;
                
                col = SummonEffect(col, input.uv, _Time.y, _ObjectHeight, input.positionOS.y, _SummonNoise, _Summon);
                clip(col.a - 0.01);
                col.a *= saturate((1 - (input.positionOS.y / _ObjectHeight)) * 1.0f);
                
                return col;
            }
            ENDHLSL
        }
    }
}
