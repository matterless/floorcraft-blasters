Shader "Unlit/Enemy/VirusShield"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 0)
        [HDR] _FresnelColor ("Fresnel Color", Color) = (1, 1, 1, 1)
        _FresnelPower ("Fresnel Power", Range(0, 20)) = 0.0
        
        [HDR][NoScaleOffset] _DiffuseMatcap ("Diffuse Matcap", 2D) = "white" {}
        [NoScaleOffset]  _SpecularMatcap ("Specular Matcap", 2D) = "white" {}
        _DiffuseMatcapStrength ("Diffuse Strength", Range(0, 2)) = 1.0
        _SpecularMatcapStrength ("Specular Strength", Range(0, 2)) = 1.0
        [NormalMap] _NormalMap ("Normal Map", 2D) = "bump" {}
        _SummonNoise ("Summon Noise Texture", 2D) = "white" {}
        _Summon ("Summon", Range(0, 1)) = 0.0
        _RotationAxis ("Rotation Axis", Vector) = (0, 0, 0)
}
    
    SubShader
    {
        
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"  
            "Queue" = "Geometry" 
        }
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
            #include "MatterlessCommon.hlsl"
            struct Attributes
            {
                float2 texcoord : TEXCOORD0;
                float2 frontTexcoord : TEXCOORD1;
                float2 sideTexcoord : TEXCOORD2;
                float4 positionOS : POSITION;
                float4 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 tangentWS : TEXCOORD3;
                half3 viewDirWS : TEXCOORD4;
            };

            sampler _MainTex;
            sampler _NormalMap;
            sampler _DiffuseMatcap;
            sampler _SpecularMatcap;
            sampler _SummonNoise;

            CBUFFER_START(UnityPerMaterial)
                float _DiffuseMatcapStrength;
                float _SpecularMatcapStrength;
                half4 _FresnelColor;
                half _FresnelPower;
                float _Summon;
                float3 _EmissionColor;
                float3 _RotationAxis;
                float2 _SyncedTime;
            CBUFFER_END

            void Unity_RotateAboutAxis_Radians_float(float3 In, float3 Axis, float Rotation, out float3 Out)
            {
                float s = sin(Rotation);
                float c = cos(Rotation);
                float one_minus_c = 1.0 - c;

                Axis = normalize(Axis);
                float3x3 rot_mat = 
                {   one_minus_c * Axis.x * Axis.x + c, one_minus_c * Axis.x * Axis.y - Axis.z * s, one_minus_c * Axis.z * Axis.x + Axis.y * s,
                    one_minus_c * Axis.x * Axis.y + Axis.z * s, one_minus_c * Axis.y * Axis.y + c, one_minus_c * Axis.y * Axis.z - Axis.x * s,
                    one_minus_c * Axis.z * Axis.x - Axis.y * s, one_minus_c * Axis.y * Axis.z + Axis.x * s, one_minus_c * Axis.z * Axis.z + c
                };
                Out = mul(rot_mat,  In);
            }

            
            ///////////////////////////////////////////////////////////////////////////////
            //                  Vertex and Fragment functions                            //
            ///////////////////////////////////////////////////////////////////////////////
            Varyings vert (Attributes input)
            {
                Varyings output = (Varyings)0;
                
                float3 positionOS = input.positionOS.xyz;

                float3 bakedPivot = (float3(input.frontTexcoord.xy, input.sideTexcoord.x) * 2.0 - 1.0) * float3(-1, 1, -1);

                float3 scaleAroundPivot = positionOS - bakedPivot;
                Unity_RotateAboutAxis_Radians_float(scaleAroundPivot, float3(1, 1, 1) * bakedPivot.y, _SyncedTime.y , scaleAroundPivot);
                scaleAroundPivot += bakedPivot;
                output.positionOS = float3(scaleAroundPivot);
                Unity_RotateAboutAxis_Radians_float(scaleAroundPivot, _RotationAxis, _SyncedTime, scaleAroundPivot);
                output.uv = input.texcoord;
                output.positionCS = TransformObjectToHClip(scaleAroundPivot.xyz);


                float3 scaledNormal = input.normalOS.xyz - bakedPivot;
                Unity_RotateAboutAxis_Radians_float(scaledNormal, float3(1, 1, 1) * bakedPivot.y, _SyncedTime.y , scaledNormal);
                scaledNormal += bakedPivot;
                Unity_RotateAboutAxis_Radians_float(scaledNormal, _RotationAxis, _SyncedTime, scaledNormal);

                float3 scaledTangent = (input.tangentOS.xyz - bakedPivot);
                Unity_RotateAboutAxis_Radians_float(scaledTangent, float3(1, 1, 1) * bakedPivot.y, _SyncedTime.y , scaledTangent);
                scaledTangent += bakedPivot;
                Unity_RotateAboutAxis_Radians_float(scaledTangent, _RotationAxis, _SyncedTime, scaledTangent);

                // Normals
                VertexNormalInputs normalInputs = GetVertexNormalInputs(scaledNormal, float4(scaledTangent, input.tangentOS.w));
                output.normalWS = normalInputs.normalWS;
                real sign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS = half4(normalInputs.tangentWS, sign);

                output.viewDirWS = GetWorldSpaceViewDir(TransformObjectToWorld(scaleAroundPivot.xyz));
                
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                half3 normalTS = normalize(UnpackNormal(tex2D(_NormalMap, input.uv.xy)));
                float sgn = input.tangentWS.w;
                float3 bitangent = sgn * cross(input.normalWS, input.tangentWS.xyz);
                half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);

                // Transform normal to world space
                half3 normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tangentToWorld));
                float3 normalVS = TransformWorldToViewDir(normalWS);

                float2 matCapUV = normalVS.xy * 0.5 + 0.5;
                
                half4 col = tex2D(_MainTex, input.uv);
                half emmision = col.a;
                float diffuseMatcap = tex2D(_DiffuseMatcap, matCapUV).a;
                col = lerp(col, col * diffuseMatcap, _DiffuseMatcapStrength);

                col += tex2D(_SpecularMatcap, matCapUV).a * _SpecularMatcapStrength;
                col.xyz = lerp(col.xyz, _EmissionColor.rgb, emmision);
                
                float fresnel = pow((1.0 - saturate(dot(normalWS, normalize(input.viewDirWS)))), _FresnelPower);

                col += fresnel * _FresnelColor;
                col = SummonEffect(col, input.uv.xy, _Time.y, 6.0, input.positionOS.y + 1.5, _SummonNoise, _Summon);
                clip(col.a - 0.01);
                
                return col;
            }
            ENDHLSL
        }
    }
}
