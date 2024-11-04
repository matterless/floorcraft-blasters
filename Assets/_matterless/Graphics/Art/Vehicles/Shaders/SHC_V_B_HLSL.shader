// Upgrade NOTE: replaced 'defined USE_NORMAL_MAP' with 'defined (USE_NORMAL_MAP)'

Shader "Unlit/Matcap/VehicleBody"
{
    Properties
    {
        _MainColor ("Color", Color) = (1,1,1,1)
        _SecondaryColor ("Secondary Color", Color) = (1,1,1,1)
        _WindowColor ("Window Color", Color) = (1,1,1,1)
        _DecalColor ("DecalColor", Color) = (1,1,1,1)
        _DecalTex ("Decal", 2D) = "black" {}
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        
        [Toggle(_UNDERCARRIAGE)] _Undercarriage ("Undercarriage", Float) = 0
        [HDR][NoScaleOffset] _DiffuseMatcap ("Diffuse Matcap", 2D) = "white" {}
        [NoScaleOffset]  _SpecularMatcap ("Specular Matcap", 2D) = "white" {}
        [NoScaleOffset]  _AmbientOcclusion ("Ambient Occlusion", 2D) = "white" {}
        
        _DiffuseMatcapStrength ("Diffuse Strength", Range(0, 2)) = 1.0
        _SpecularMatcapStrength ("Specular Strength", Range(0, 2)) = 1.0
        _FernelPower ("Fernel Power", Range(0, 10)) = 1.0
        
        [NormalMap] _NormalMap ("Normal Map", 2D) = "bump" {}
        [NoScaleOffset] _MaterialMask ("Material Mask", 2D) = "white" {}
        
        _NoiseTexture ("Noise Texture", 2D) = "white" {}
        _Summon ("Summon", Range(0, 1)) = 0.0
        _ObjectHeight ("Object Height", Float) = 5.0
        
        [Toggle(_ANIMATED_DECAL)] _AnimatedDecal ("Animated Decal", Float) = 0
        _AnimatedDecalTexture ("Animated Decal Texture", 2D) = "white" {}
        
        _InnerLine ("Inner Line Texture", 2D) = "white" {}
        _InnerLineColor ("Inner Line Color", Color) = (1,1,1,1)
}
    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"  
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
            #pragma shader_feature_local _UNDERCARRIAGE
            #pragma multi_compile _ _ANIMATED_DECAL

            // core.hlsl
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"

            struct Attributes
            {
                float2 texcoord : TEXCOORD0;
                float2 texcoord2 : TEXCOORD1;
                float2 texcoord3 : TEXCOORD2;
                float3 texcoord4 : TEXCOORD3;
                float4 positionOS : POSITION;
                float4 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD2;
                float2 uv3 : TEXCOORD7;
                float3 uv4 : TEXCOORD1;
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD3;
                half3 normalWS : TEXCOORD4;
                half4 tangentWS : TEXCOORD5; // xyz: tangent, w: sign
                half3 viewDirWS : TEXCOORD6;
            };

            sampler _MainTex;
            sampler _AmbientOcclusion;
            sampler _DecalTex;
            sampler _DiffuseMatcap;
            sampler _SpecularMatcap;
            sampler _NormalMap;
            sampler _NoiseTexture;
            sampler _MaterialMask;
            sampler _InnerLine;
            #if defined (_ANIMATED_DECAL)
                sampler _AnimatedDecalTexture;
            #endif
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float4 _SecondaryColor;
                float4 _WindowColor;
                float4 _DecalColor;
                float4 _NormalMap_ST;
                float4 _InnerLineColor;
                float _DiffuseMatcapStrength;
                float _SpecularMatcapStrength;
                float _Summon;
                float _ObjectHeight;
                float _FernelPower;
            CBUFFER_END

            ///////////////////////////////////////////////////////////////////////////////
            //                  Vertex and Fragment functions                            //
            ///////////////////////////////////////////////////////////////////////////////
            Varyings vert (Attributes input)
            {
                Varyings output = (Varyings)0;

                output.uv = input.texcoord;
                output.uv4 = input.texcoord4;
                #ifdef _UNDERCARRIAGE
                    output.uv2 = TRANSFORM_TEX(input.texcoord.xy, _NormalMap);
                    
                #else
                    output.uv3 = input.texcoord3;
                    output.uv2 = input.texcoord2;
                #endif
                
                // Normals
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS.xyz, input.tangentOS);
                output.normalWS = normalInputs.normalWS;
                real sign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS = half4(normalInputs.tangentWS, sign);
                

                // Position
                output.positionOS = input.positionOS.xyz;
                float3 positionWS = TransformObjectToWorld(output.positionOS);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            #define ANIMATED_DECAL_SPEED 0.21
            #define ANIMATED_DECAL_POWER 10
            
            half4 frag (Varyings input) : SV_Target
            {
                // Decal Mask
                #if defined (_ANIMATED_DECAL)
                    half2 decalAlpha = tex2D(_DecalTex, input.uv).rg;
                #else
                    half decalAlpha = tex2D(_DecalTex, input.uv).r;
                #endif                
                half3 materialMask = tex2D(_MaterialMask, input.uv).rgb; // black body paint, red secondary color, green window

                // Diregard Normals for now
                // Generate TBN
                half3 normalTS = normalize(UnpackNormal(tex2D(_NormalMap, input.uv2)));
                #ifndef _UNDERCARRIAGE
                    normalTS = lerp(normalTS, half3(0,0,1), saturate(materialMask.r + materialMask.g + decalAlpha.r)); // No detail on decal area
                #endif
                float sgn = input.tangentWS.w;
                float3 bitangent = sgn * cross(input.normalWS, input.tangentWS.xyz);
                half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);

                // Transform normal to world space
                half3 normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tangentToWorld));
                float3 normalVS = TransformWorldToViewDir(normalWS);

                // Matcap UV
                float2 matCapUV = normalVS.xy * 0.5 + 0.5;
                float2 diffuseMatcapUV = TransformWorldToViewDir(input.normalWS).xy * 0.5 + 0.5;

                float fresnel = pow((1.0 - saturate(dot(normalWS, normalize(input.viewDirWS)))), _FernelPower);
                
                // Color Calculation
                #if defined(_UNDERCARRIAGE)
                    half4 col = tex2D(_MainTex, input.uv);
                #else
                    
                    half4 col = lerp(_MainColor, _SecondaryColor, materialMask.r);
                #if defined(_ANIMATED_DECAL)
                    half animatedDecalMask = tex2D(_AnimatedDecalTexture, input.uv2 + float2(0, -_Time.y * ANIMATED_DECAL_SPEED)).g;
                    half animatedDecalMask2 = tex2D(_AnimatedDecalTexture, input.uv2 * 0.5 + float2(_Time.y * ANIMATED_DECAL_SPEED, 0)).g;
                    half3 d = animatedDecalMask * animatedDecalMask2 * ANIMATED_DECAL_POWER * _DecalColor * decalAlpha.r;
                    col = lerp(col, _WindowColor + fresnel, materialMask.g);
                    col.xyz = saturate(lerp(col, _DecalColor , decalAlpha.g) + d * _DecalColor);
                #else
                    col = lerp(col, _WindowColor + fresnel, materialMask.g);
                    col = lerp(col, _DecalColor , decalAlpha.r);
                #endif
                
                #endif
                
                // Diffuse
                float diffuseMatcap = tex2D(_DiffuseMatcap, matCapUV).a;
                col = lerp(col, col * diffuseMatcap, _DiffuseMatcapStrength);

                // Specular
                #if defined(_ANIMATED_DECAL)
                col += tex2D(_SpecularMatcap, matCapUV).a * _SpecularMatcapStrength  + (fresnel  + sin(_Time.y * 5) * 0.1) * (1 - materialMask.g)  * _DecalColor;
                #else
                col += tex2D(_SpecularMatcap, matCapUV).a * _SpecularMatcapStrength;
                #endif
                col *= tex2D(_AmbientOcclusion, input.uv).r;

                #ifndef _UNDERCARRIAGE
                    float innerLine = tex2D(_InnerLine, input.uv3).r;
                    col = lerp(_InnerLineColor, col , innerLine);
                #endif
                

                //Summon
                half noise = tex2D(_NoiseTexture, input.uv * 5 - float2(0, _Time.y * 0.4)).r * 0.5  * tex2D(_NoiseTexture, input.uv * 4 - float2(0, _Time.y * 0.4)).g * 2.0 - 1.0;
                noise +=  1 - input.positionOS.y + _Summon * _ObjectHeight * 1.2 - 1;
                col.xyz += saturate(1 - noise) * 5;
                col.a = saturate(noise);
                clip(col.a - 0.01);
                
                
                return col;
            }
            ENDHLSL
        }
    }
}
