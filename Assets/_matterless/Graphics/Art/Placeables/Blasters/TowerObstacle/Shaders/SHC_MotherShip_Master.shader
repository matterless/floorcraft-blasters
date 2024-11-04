Shader "Unlit/Matcap/MotherShipMaster"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Albedo (RGB)", 2D) = "white" {}
        
        [HDR][NoScaleOffset] _DiffuseMatcap ("Diffuse Matcap", 2D) = "white" {}
        [NoScaleOffset]  _SpecularMatcap ("Specular Matcap", 2D) = "white" {}
        _DiffuseMatcapStrength ("Diffuse Strength", Range(0, 2)) = 1.0
        _SpecularMatcapStrength ("Specular Strength", Range(0, 2)) = 1.0
        [NormalMap] _NormalMap ("Normal Map", 2D) = "bump" {}
        _SummonNoise ("Summon Noise Texture", 2D) = "white" {}
        _Summon ("Summon", Range(0, 1)) = 0.0
        _ObjectHeight ("Object Height", Float) = 5.0
        [NoScaleOffset] _FlipBookTexture ("Flip Book Texture", 2D) = "white" {}
        [NoScaleOffset] _LCDColorTexture ("LCD Color Texture", 2D) = "white" {}
        [HDR] _EnergyColor ("Energy Color", Color) = (1, 1, 1, 1)
        [HDR] _EnergyDamagedColor ("Energy Damaged Color", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _PerlinNoise ("Perlin Noise", 2D) = "white" {}
        [NoScaleOffset] _EnergyNoise ("Energy Noise", 2D) = "white" {}
        [NoScaleOffset] _DamageMask ("Damage Mask", 2D) = "white" {}
        [NoScaleOffset] _DamageAlbedo ("Damage Albedo", 2D) = "white" {}
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
        
        Blend One OneMinusSrcAlpha
        Cull Off
        Zwrite On
        ZTest LEqual
        
        Stencil
        {
            Ref  128
            Comp Always
            Pass Replace
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #define NUMBER_OF_FRAMES 64.0f
            #define NUMBER_OF_ROWS 8.0f
            #define NUMBER_OF_COLUMNS 8.0f
            #define FLIP_BOOK_ANIMATION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
            #include "MatterlessCommon.hlsl"

            struct Attributes
            {
                float2 texcoord : TEXCOORD0;
                float4 positionOS : POSITION;
                float4 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float3 vertexColor : COLOR;
            };

            struct Varyings
            {
                float3 uv : TEXCOORD0;
                float4 flipBookUV : TEXCOORD1; // z is mask w is perlin noise mask
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD3;
                half3 normalWS : TEXCOORD4;
                half4 tangentWS : TEXCOORD5; // xyz: tangent, w: sign
                
            };

            sampler _MainTex;
            sampler _PerlinNoise;
            sampler _FlipBookTexture;
            sampler _DiffuseMatcap;
            sampler _SpecularMatcap;
            sampler _NormalMap;
            sampler _SummonNoise;
            sampler _LCDColorTexture;
            sampler _EnergyNoise;
            sampler _DamageMask;
            sampler _DamageAlbedo;
            
            CBUFFER_START(UnityPerMaterial)
                float4 _NormalMap_ST;
                float4 _FlipBookTexture_TexelSize;
                float _DiffuseMatcapStrength;
                float _SpecularMatcapStrength;
                float _Summon;
                float _ObjectHeight;
                half3 _EnergyColor;
                half3 _EnergyDamagedColor;
                float _Damage;
                float _IsHit;
            CBUFFER_END

            

            ///////////////////////////////////////////////////////////////////////////////
            //                  Vertex and Fragment functions                            //
            ///////////////////////////////////////////////////////////////////////////////
            Varyings vert (Attributes input)
            {
                Varyings output = (Varyings)0;

                output.uv.xy = input.texcoord;
                output.uv.z = input.vertexColor.b;
                const float2 frameSize = _FlipBookTexture_TexelSize.xy * _FlipBookTexture_TexelSize.zw / NUMBER_OF_ROWS;
                output.flipBookUV.xy = FlipBookUV(frameSize, 0, input.texcoord.xy);
                output.flipBookUV.zw = input.vertexColor.rg;
                
                // Normals
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS.xyz, input.tangentOS);
                output.normalWS = normalInputs.normalWS;
                real sign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS = half4(normalInputs.tangentWS, sign);

                // Position
                output.positionOS = input.positionOS.xyz;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                half3 normalTS = normalize(UnpackNormal(tex2D(_NormalMap, input.uv.xy)));
                normalTS = lerp(normalTS, float3(0, 0, 1), input.flipBookUV.z + input.flipBookUV.w);
                
                float sgn = input.tangentWS.w;
                float3 bitangent = sgn * cross(input.normalWS, input.tangentWS.xyz);
                half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);

                // Transform normal to world space
                half3 normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tangentToWorld));
                float3 normalVS = TransformWorldToViewDir(normalWS);
                
                // Matcap UV
                float2 matCapUV = normalVS.xy * 0.5 + 0.5;
                half4 col = tex2D(_MainTex, input.uv.xy);
                half3 flipBookColor = tex2D(_FlipBookTexture, round(input.flipBookUV.xy * float2(2048.0f, 2048.0f) + 0.5f) / float2(2048.0f, 2048.0f)).rgb;
                half3 perlinNoise = tex2D(_PerlinNoise, input.uv.xy + float2(0, _Time.y * 0.05)).rgb;
                half damageMask = tex2D(_DamageMask, input.uv.xy).r;
                half4 damageTex = tex2D(_DamageAlbedo, input.uv.xy);
                float damage = damageMask < pow(_Damage, 0.454545454545f)  ? 1 : 0;
                damageTex.r += damageTex.a * saturate(1 - abs(frac(input.uv.x + _Time.y * 0.1)  - 0.5) * 10.0f) * 2.0f;
                
                col.xyz = lerp(col, damageTex.xyz, damage);
                half3 lcdColor = tex2D(_LCDColorTexture, input.uv.xy * 2048.0f).rgb * 3.5f;
                half energyNoise1 = tex2D(_EnergyNoise, input.uv.xy + float2(0 , -_Time.y * 0.1)).g;
                half energyNoise2 = tex2D(_EnergyNoise, input.uv.xy * 0.5 + float2(_Time.y * 0.1, 0)).g;
                
                // Diffuse
                float diffuseMatcap = tex2D(_DiffuseMatcap, matCapUV).a;
                col = lerp(col, col * diffuseMatcap, _DiffuseMatcapStrength);

                // Specular
                col += tex2D(_SpecularMatcap, matCapUV).a * _SpecularMatcapStrength;

                half3 EnergyCoreColor = lerp(_EnergyColor,_EnergyDamagedColor, saturate(_Damage + _IsHit));
                col.xyz = lerp(col.xyz, flipBookColor * lcdColor, input.flipBookUV.z);
                col.xyz = lerp(col.xyz, perlinNoise * EnergyCoreColor, input.flipBookUV.w);
                col.xyz = lerp(col.xyz, saturate( energyNoise1 * energyNoise2 * 10) * EnergyCoreColor, input.uv.b);
                
                col = SummonEffect(col, input.uv.xy, _Time.y, _ObjectHeight, input.positionOS.y, _SummonNoise, _Summon);
                
                clip(col.a - 0.01);
                return col;
            }
            ENDHLSL
        }
    }
}
