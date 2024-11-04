Shader "Unlit/Enemy/Virus"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [HDR] _SpotColor ("Spot Color", Color) = (1, 1, 1, 1)
        [HDR] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [HDR] _FresnelColor ("Fresnel Color", Color) = (1, 1, 1, 1)
        
        _FresnelPower ("Fresnel Power", Range(0, 20)) = 0.0
        
        [HDR][NoScaleOffset] _DiffuseMatcap ("Diffuse Matcap", 2D) = "white" {}
        [NoScaleOffset]  _SpecularMatcap ("Specular Matcap", 2D) = "white" {}
        _DiffuseMatcapStrength ("Diffuse Strength", Range(0, 2)) = 1.0
        _SpecularMatcapStrength ("Specular Strength", Range(0, 2)) = 1.0
        _DisplacementStrength ("Displacement Strength", Range(0, 1)) = 0.0
        _AnimationOffset ("Animation Offset", Vector) = (0, 0, 0, 0)
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
            #include "SimplexNoise3D.hlsl"

            struct Attributes
            {
                float2 texcoord : TEXCOORD0;
                float4 positionOS : POSITION;
                float4 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                half3 viewDirWS : TEXCOORD1;
                half3 normalOS : TEXCOORD2;
                half4 tangentOS : TEXCOORD3;
                float3 c : TEXCOORD5;
            };

            sampler _MainTex;
            sampler _DiffuseMatcap;
            sampler _SpecularMatcap;

            CBUFFER_START(UnityPerMaterial)
                float _DiffuseMatcapStrength;
                float _SpecularMatcapStrength;
                float _DisplacementStrength;
                half4 _SpotColor;
                half4 _BaseColor;
                half4 _FresnelColor;
                half _FresnelPower;
                float3 _AnimationOffset;
            CBUFFER_END

            /*
             * For normal vector reconstruction used the method described by
             * Stefan Gustavson https://stegu.github.io/psrdnoise/3d-tutorial/bumpmapping.pdf
             */

            #define SAMPLE_SPEED 0.075f
            #define INNER_DERIVATIVE_SCALE 0.19948f

            
            ///////////////////////////////////////////////////////////////////////////////
            //                  Vertex and Fragment functions                            //
            ///////////////////////////////////////////////////////////////////////////////
            Varyings vert (Attributes input)
            {
                Varyings output = (Varyings)0;

                float3 positionOS = input.positionOS.xyz;
                float3 samplePosition = (positionOS * INNER_DERIVATIVE_SCALE) + SAMPLE_SPEED /** (float3(_Time.y, _Time.z, _Time.w)*/ * 0.1f * _AnimationOffset; //_Time.y * 5.5;// * 1.0/_Scale;  //* 1/0.3f * 1/0.0225f;

                float4 gradientNoise;
                SimplexNoise3DGradient_float4(samplePosition, gradientNoise);
                
                float noise = gradientNoise.w;
                float3 noiseGradient = (gradientNoise.xyz * INNER_DERIVATIVE_SCALE);

                positionOS = positionOS + noise * input.normalOS.xyz * _DisplacementStrength;

                float3 N_ = noiseGradient - dot(noiseGradient, input.normalOS.xyz) * input.normalOS.xyz;
                output.normalOS = normalize(input.normalOS.xyz - _DisplacementStrength * N_);
                output.uv = input.texcoord;
                output.positionCS = TransformObjectToHClip(positionOS.xyz);
                float3 positionWS = TransformObjectToWorld(positionOS.xyz);
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                output.c = distance(positionOS, float3(0, 0, 0));
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                half4 col = half4(1, 1, 1, 1);
                
                // Lighting
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS, true);
                float3 normalVS = TransformWorldToViewDir(normalWS);

                float2 matCapUV = normalVS.xy * 0.5 + 0.5;

                float diffuseMatcap = tex2D(_DiffuseMatcap, matCapUV).a;
                col = lerp(col, col * diffuseMatcap, _DiffuseMatcapStrength);

                float fresnel = pow((1.0 - saturate(dot(normalize(normalWS), normalize(input.viewDirWS)))), _FresnelPower);

                float spotMask = smoothstep(1.25, 1.65, input.c);
                float spotNoise= tex2D(_MainTex, input.uv).r;
                
                col.xyz = lerp(_BaseColor.rgb, _SpotColor.rgb * saturate(spotNoise + 0.3), spotMask);
                col = lerp(col, col * diffuseMatcap, _DiffuseMatcapStrength);
                col += tex2D(_SpecularMatcap, matCapUV).a * _SpecularMatcapStrength;
                col.xyz += fresnel * _FresnelColor.rgb;
                col.a = 1;
                clip(col.a - 0.01);
                
                return col;
            }
            ENDHLSL
        }
    }
}
